using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Helpers;
using YggdrasilWebApp.Models.Views;
using YggdrasilWebApp.Models.Yggdrasil;
using YggdrasilWebApp.Services;

namespace YggdrasilWebApp.Controllers;

[Authorize]
public sealed class PonySkinController : Controller
{
	private readonly AppDbContext _db;
	private readonly IWebHostEnvironment _env;
	private readonly TexturesService _textures;

	private const long MaxUploadBytes = 10 * 1024 * 1024;

	public PonySkinController(AppDbContext db, IWebHostEnvironment env, TexturesService textures)
	{
		_db = db;
		_env = env;
		_textures = textures;
	}

	// ===========================
	// UI: ТОЛЬКО ДЛЯ ТЕКУЩЕГО ЮЗЕРА
	// ===========================

	[HttpGet]
	public async Task<IActionResult> Index(CancellationToken ct)
	{
		var (userId, profile) = await GetMyProfileOrNotFound(ct);
		if (profile is null) return NotFound();

		var path = GetSkinPath(profile.Id);
		var exists = System.IO.File.Exists(path);
		DateTime? updated = exists ? System.IO.File.GetLastWriteTimeUtc(path) : null;

		var skinUrl = Url.Action(nameof(PublicSkinPng), "PonySkin",
			new { profileId = profile.Id.ToString("N"), v = updated?.Ticks ?? 0 },
			protocol: Request.Scheme)!;

		var vm = new PonySkinViewModel
		{
			ProfileId = profile.Id.ToString("N"),
			ProfileName = profile.Name,
			HasSkin = exists,
			UpdatedAtUtc = updated,
			SkinImageUrl = skinUrl
		};

		return View("Index", vm);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[RequestSizeLimit(MaxUploadBytes)]
	public async Task<IActionResult> Upload(IFormFile skin, CancellationToken ct)
	{
		var (userId, profile) = await GetMyProfileOrNotFound(ct);
		if (profile is null) return NotFound();

		if (skin is null || skin.Length <= 0)
		{
			TempData["Err"] = "Файл не выбран.";
			return RedirectToAction(nameof(Index));
		}

		if (skin.Length > MaxUploadBytes)
		{
			TempData["Err"] = $"Слишком большой файл. Лимит: {MaxUploadBytes / (1024 * 1024)} MB.";
			return RedirectToAction(nameof(Index));
		}

		if (!(skin.FileName?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ?? false))
		{
			TempData["Err"] = "Нужен PNG (.png).";
			return RedirectToAction(nameof(Index));
		}

		await using var ms = new MemoryStream();
		await skin.CopyToAsync(ms, ct);
		ms.Position = 0;

		int w, h;
		try
		{
			(w, h) = PngHeaderReader.ReadDimensions(ms);
		}
		catch (Exception ex)
		{
			TempData["Err"] = $"Невалидный PNG: {ex.Message}";
			return RedirectToAction(nameof(Index));
		}

		// MLP old/new + HD кратность
		var isOld = (w == 2 * h) && (w % 64 == 0) && (h % 32 == 0);
		var isNew = (w == h) && (w % 64 == 0);

		if (!isOld && !isNew)
		{
			TempData["Err"] = $"Размер {w}x{h} не похож на MLP skin. Жду old: 64x32 (HD 128x64/256x128/512x256...) или new: 64x64 (HD 128x128/256x256/512x512...).";
			return RedirectToAction(nameof(Index));
		}

		// 1) save PNG
		Directory.CreateDirectory(GetSkinDir());
		var finalPath = GetSkinPath(profile.Id);
		var tmpPath = finalPath + ".tmp";

		ms.Position = 0;
		await System.IO.File.WriteAllBytesAsync(tmpPath, ms.ToArray(), ct);
		System.IO.File.Move(tmpPath, finalPath, overwrite: true);

		// 2) update TexturesValue/TexturesSignature in DB
		var publicUrl = Url.Action(nameof(PublicSkinPng), "PonySkin",
			new { profileId = profile.Id.ToString("N") },
			protocol: Request.Scheme)!;

		var (value, signature) = _textures.BuildSkinTextures(
			profileIdN: profile.Id.ToString("N"),
			profileName: profile.Name,
			skinUrl: publicUrl
		);

		profile.TexturesValue = value;
		profile.TexturesSignature = signature;

		await _db.SaveChangesAsync(ct);

		TempData["Ok"] = $"Скин обновлён ({w}x{h}). TexturesValue/Signature обновлены.";
		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(CancellationToken ct)
	{
		var (userId, profile) = await GetMyProfileOrNotFound(ct);
		if (profile is null) return NotFound();

		var path = GetSkinPath(profile.Id);
		if (System.IO.File.Exists(path))
			System.IO.File.Delete(path);

		profile.TexturesValue = null;
		profile.TexturesSignature = null;

		await _db.SaveChangesAsync(ct);

		TempData["Ok"] = "Скин удалён. TexturesValue/Signature очищены.";
		return RedirectToAction(nameof(Index));
	}

	// ===========================
	// PUBLIC: skin PNG for clients
	// ===========================

	[AllowAnonymous]
	[HttpGet("{controller}/Textures/{profileId}.png")]
	public async Task<IActionResult> PublicSkinPng(string profileId, CancellationToken ct)
	{
		var pid = GuidHelper.ParseGuidAny(profileId);
		if (pid is null) return NotFound();

		// профиль должен существовать
		var existsProfile = await _db.Profiles.AsNoTracking().AnyAsync(p => p.Id == pid.Value, ct);
		if (!existsProfile) return NotFound();

		var path = GetSkinPath(pid.Value);
		if (!System.IO.File.Exists(path)) return NotFound();

		Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
		Response.Headers.Pragma = "no-cache";

		return File(System.IO.File.OpenRead(path), "image/png");
	}

	// ===========================
	// helpers
	// ===========================

	private async Task<(Guid userId, MinecraftProfileEntity? profile)> GetMyProfileOrNotFound(CancellationToken ct)
	{
		var userId = GetUserIdOrThrow();

		// Если у тебя 1 профиль на юзера — это ровно то, что нужно.
		// Если вдруг их несколько — берём самый первый созданный и ВСЁ.
		var profile = await _db.Profiles
			.FirstOrDefaultAsync(p => p.UserId == userId, ct);

		return (userId, profile);
	}

	private Guid GetUserIdOrThrow()
	{
		var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var g = GuidHelper.ParseGuidAny(s ?? "");
		if (g is null) throw new InvalidOperationException("Auth cookie has no user id.");
		return g.Value;
	}

	private string GetSkinDir()
		=> Path.Combine(_env.ContentRootPath, "Storage", "PonySkins");

	private string GetSkinPath(Guid profileId)
		=> Path.Combine(GetSkinDir(), $"{profileId:N}.png");
}