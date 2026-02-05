using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Models.App;
using YggdrasilWebApp.Views;

namespace YggdrasilWebApp.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminModsController : Controller
{
	private readonly AppDbContext _db;
	private readonly IWebHostEnvironment _env;

	public AdminModsController(AppDbContext db, IWebHostEnvironment env)
	{
		_db = db;
		_env = env;
	}

	private string GetModsRoot()
	{
		var modsRoot = Path.Combine(_env.ContentRootPath, "Storage", "Mods");
		Directory.CreateDirectory(modsRoot);
		return modsRoot;
	}

	public async Task<IActionResult> Index(CancellationToken ct)
	{
		var mods = await _db.GameMods
			.OrderByDescending(m => m.UploadedAtUtc)
			.ToListAsync(ct);

		return View(mods);
	}

	[HttpGet]
	public IActionResult Upload()
	{
		return View(new UploadModViewModel());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Upload(UploadModViewModel model, CancellationToken ct)
	{
		if (!ModelState.IsValid)
			return View(model);

		if (model.File == null || model.File.Length == 0)
		{
			ModelState.AddModelError(nameof(model.File), "File is required.");
			return View(model);
		}

		var modsRoot = GetModsRoot();

		// генерим уникальное имя файла
		var safeName = Path.GetFileName(model.File.FileName);
		var fileName = $"{Guid.NewGuid():N}_{safeName}";
		var physicalPath = Path.Combine(modsRoot, fileName);

		// сохраняем файл и считаем SHA256
		string sha256Hex;
		await using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			await model.File.CopyToAsync(fs, ct);
		}

		await using (var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			using var sha = SHA256.Create();
			var hash = await sha.ComputeHashAsync(fs, ct);
			sha256Hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}

		// относительный путь для скачивания через статику
		var relativePath = Path.Combine("mods", fileName).Replace('\\', '/');

		var entity = new GameModEntity
		{
			Name = model.Name,
			Description = model.Description,
			Version = model.Version,
			InstallPath = string.IsNullOrWhiteSpace(model.InstallPath) ? "mods" : model.InstallPath.Trim(),
			FilePath = relativePath,
			Sha256 = sha256Hex,
			UploadedAtUtc = DateTime.UtcNow
		};

		_db.GameMods.Add(entity);
		await _db.SaveChangesAsync(ct);

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(long id, CancellationToken ct)
	{
		var mod = await _db.GameMods.FirstOrDefaultAsync(m => m.Id == id, ct);
		if (mod is null)
			return RedirectToAction(nameof(Index));

		// удаляем файл
		try
		{
			var modsRoot = GetModsRoot();
			var fileName = Path.GetFileName(mod.FilePath);
			var physicalPath = Path.Combine(modsRoot, fileName);
			if (System.IO.File.Exists(physicalPath))
				System.IO.File.Delete(physicalPath);
		}
		catch
		{
			// пофиг, не получилось удалить - ладно
		}

		_db.GameMods.Remove(mod);
		await _db.SaveChangesAsync(ct);

		return RedirectToAction(nameof(Index));
	}
}
