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
	private const string RouteSkinPngVersioned = "ponySkin_png_v";

	public PonySkinController(AppDbContext db, IWebHostEnvironment env, TexturesService textures)
	{
		_db = db;
		_env = env;
		_textures = textures;
	}

	[HttpGet]
	public async Task<IActionResult> Index(CancellationToken ct)
	{
		var (_, profile) = await GetMyProfileOrNotFound(ct);
		if (profile is null) return NotFound();

		var latestPath = GetSkinPathByProfileId(profile.Id);
		var exists = !string.IsNullOrWhiteSpace(latestPath) && System.IO.File.Exists(latestPath!);
		DateTime? updated = exists ? System.IO.File.GetLastWriteTimeUtc(latestPath!) : null;

		var uniqueId = exists ? ExtractUniqueIdFromSkinPath(latestPath!) : Guid.Empty;

		var skinUrl = Url.Link(RouteSkinPngVersioned, new
		{
			profileId = profile.Id.ToString("N"),
			uniqueId = uniqueId.ToString("N")
		})!;

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
		var (_, profile) = await GetMyProfileOrNotFound(ct);
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

		var isOld = (w == 2 * h) && (w % 64 == 0) && (h % 32 == 0);
		var isNew = (w == h) && (w % 64 == 0);

		if (!isOld && !isNew)
		{
			TempData["Err"] = $"Размер {w}x{h} не похож на MLP skin. Жду old: 64x32 (HD 128x64/256x128/512x256...) или new: 64x64 (HD 128x128/256x256/512x512...).";
			return RedirectToAction(nameof(Index));
		}

		var uniqueId = Guid.NewGuid();

		Directory.CreateDirectory(GetSkinDir());
		var finalPath = GetSkinPath(profile.Id, uniqueId);
		var tmpPath = finalPath + ".tmp";

		ms.Position = 0;
		await System.IO.File.WriteAllBytesAsync(tmpPath, ms.ToArray(), ct);
		System.IO.File.Move(tmpPath, finalPath, overwrite: true);

		var publicUrl = Url.Link(RouteSkinPngVersioned, new
		{
			profileId = profile.Id.ToString("N"),
			uniqueId = uniqueId.ToString("N")
		})!;

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
		var (_, profile) = await GetMyProfileOrNotFound(ct);
		if (profile is null) return NotFound();

		var dir = GetSkinDir();
		if (Directory.Exists(dir))
		{
			var pattern = $"{profile.Id:N}_*.png";
			foreach (var f in Directory.EnumerateFiles(dir, pattern))
			{
				try { System.IO.File.Delete(f); } catch { }
			}

			var legacy = GetLegacySkinPath(profile.Id);
			if (System.IO.File.Exists(legacy))
			{
				try { System.IO.File.Delete(legacy); } catch { }
			}
		}

		profile.TexturesValue = null;
		profile.TexturesSignature = null;

		await _db.SaveChangesAsync(ct);

		TempData["Ok"] = "Скин удалён. TexturesValue/Signature очищены.";
		return RedirectToAction(nameof(Index));
	}

	[AllowAnonymous]
	[HttpGet("PonySkin/Textures/{profileId}/{uniqueId}.png", Name = RouteSkinPngVersioned)]
	[HttpGet("PonySkin/Textures/{profileId}.png")]
	public async Task<IActionResult> PublicSkinPng(string profileId, string? uniqueId, CancellationToken ct)
	{
		var pid = GuidHelper.ParseGuidAny(profileId);
		if (pid is null) return NotFound();

		var existsProfile = await _db.Profiles.AsNoTracking().AnyAsync(p => p.Id == pid.Value, ct);
		if (!existsProfile) return NotFound();

		string? path;

		if (!string.IsNullOrWhiteSpace(uniqueId))
		{
			var uid = GuidHelper.ParseGuidAny(uniqueId);
			if (uid is null) return NotFound();

			path = GetSkinPath(pid.Value, uid.Value);
			if (!System.IO.File.Exists(path)) return NotFound();

			Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
			return File(System.IO.File.OpenRead(path), "image/png");
		}

		path = GetSkinPathByProfileId(pid.Value);
		if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return NotFound();

		Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
		Response.Headers["Pragma"] = "no-cache";

		return File(System.IO.File.OpenRead(path), "image/png");
	}

	private async Task<(Guid userId, MinecraftProfileEntity? profile)> GetMyProfileOrNotFound(CancellationToken ct)
	{
		var userId = GetUserIdOrThrow();
		var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
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

	private string GetLegacySkinPath(Guid profileId)
		=> Path.Combine(GetSkinDir(), $"{profileId:N}.png");

	private string GetSkinPath(Guid profileId, Guid uniqueId)
		=> Path.Combine(GetSkinDir(), $"{profileId:N}_{uniqueId:N}.png");

	private string? GetSkinPathByProfileId(Guid profileId)
	{
		try
		{
			var dir = GetSkinDir();
			if (!Directory.Exists(dir))
				return null;

			var pattern = $"{profileId:N}_*.png";
			var newest = Directory.EnumerateFiles(dir, pattern)
				.Select(p => new FileInfo(p))
				.OrderByDescending(f => f.LastWriteTimeUtc)
				.FirstOrDefault()
				?.FullName;

			if (!string.IsNullOrWhiteSpace(newest))
				return newest;

			var legacy = GetLegacySkinPath(profileId);
			return System.IO.File.Exists(legacy) ? legacy : null;
		}
		catch
		{
			return null;
		}
	}

	private static Guid ExtractUniqueIdFromSkinPath(string fullPath)
	{
		var name = Path.GetFileNameWithoutExtension(fullPath);
		var parts = name.Split('_');
		if (parts.Length == 2 && Guid.TryParseExact(parts[1], "N", out var uid))
			return uid;
		return Guid.Empty;
	}
}
