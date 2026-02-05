using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Models.App;

namespace YggdrasilWebApp.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminModsController : Controller
{
	private readonly AppDbContext _db;
	private readonly IWebHostEnvironment _env;

	// чтобы параллельные чанки не устроили кашу в одном файле
	private static readonly ConcurrentDictionary<string, SemaphoreSlim> _uploadLocks = new();

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

	private string GetTmpRoot()
	{
		var tmpRoot = Path.Combine(_env.ContentRootPath, "Storage", "Mods", "_tmp");
		Directory.CreateDirectory(tmpRoot);
		return tmpRoot;
	}

	public async Task<IActionResult> Index(CancellationToken ct)
	{
		var mods = await _db.GameMods
			.OrderByDescending(m => m.UploadedAtUtc)
			.ToListAsync(ct);

		return View(mods);
	}

	[HttpGet]
	public IActionResult Upload() => View(new Views.UploadModViewModel());

	// ---------- CHUNKED UPLOAD API ----------

	public sealed class StartUploadRequest
	{
		public string Name { get; set; } = "";
		public string? Description { get; set; }
		public string Version { get; set; } = "";
		public string? InstallPath { get; set; }
		public string OriginalFileName { get; set; } = "";
		public long TotalBytes { get; set; }
	}

	public sealed class StartUploadResponse
	{
		public string UploadId { get; set; } = "";
		public string FileName { get; set; } = "";
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[RequestSizeLimit(1_000_000)] // тут только метаданные, файл не идёт
	public IActionResult StartUpload([FromBody] StartUploadRequest req)
	{
		if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Version))
			return BadRequest("Name and Version are required.");

		if (string.IsNullOrWhiteSpace(req.OriginalFileName))
			return BadRequest("OriginalFileName is required.");

		var safeName = Path.GetFileName(req.OriginalFileName);
		var fileName = $"{Guid.NewGuid():N}_{safeName}";

		var uploadId = Guid.NewGuid().ToString("N");
		var tmpRoot = GetTmpRoot();
		var tmpPath = Path.Combine(tmpRoot, $"{uploadId}.part");

		// создаём пустой .part (если уже есть — перезатираем)
		System.IO.File.WriteAllBytes(tmpPath, Array.Empty<byte>());

		// сохраним метаданные рядом, чтобы finish мог их взять
		var metaPath = Path.Combine(tmpRoot, $"{uploadId}.json");
		var meta = System.Text.Json.JsonSerializer.Serialize(req);
		System.IO.File.WriteAllText(metaPath, meta);

		return Ok(new StartUploadResponse { UploadId = uploadId, FileName = fileName });
	}

	[HttpPost]
	[IgnoreAntiforgeryToken] // чанки проще без токена (или можешь проверять заголовком, см. ниже)
							 // лимит на один чанк (пример: 20MB)
	[RequestSizeLimit(20 * 1024 * 1024)]
	public async Task<IActionResult> UploadChunk(
		[FromQuery] string uploadId,
		[FromQuery] int index,
		[FromQuery] long offset,
		CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(uploadId))
			return BadRequest("uploadId required.");
		if (index < 0)
			return BadRequest("index invalid.");
		if (offset < 0)
			return BadRequest("offset invalid.");

		var tmpRoot = GetTmpRoot();
		var tmpPath = Path.Combine(tmpRoot, $"{uploadId}.part");
		if (!System.IO.File.Exists(tmpPath))
			return NotFound("Unknown uploadId.");

		var sem = _uploadLocks.GetOrAdd(uploadId, _ => new SemaphoreSlim(1, 1));
		await sem.WaitAsync(ct);
		try
		{
			// дописываем строго по offset: можно поддержать ретраи/дозагрузку
			await using var fs = new FileStream(tmpPath, FileMode.Open, FileAccess.Write, FileShare.Read);
			if (fs.Length != offset)
				return Conflict($"Server length={fs.Length}, but client offset={offset}.");

			fs.Seek(0, SeekOrigin.End);
			await Request.Body.CopyToAsync(fs, ct);
			await fs.FlushAsync(ct);
		}
		finally
		{
			sem.Release();
		}

		return Ok(new { ok = true });
	}

	public sealed class FinishUploadRequest
	{
		public string UploadId { get; set; } = "";
		public string FileName { get; set; } = "";
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[RequestSizeLimit(1_000_000)]
	public async Task<IActionResult> FinishUpload([FromBody] FinishUploadRequest req, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(req.UploadId) || string.IsNullOrWhiteSpace(req.FileName))
			return BadRequest("UploadId and FileName are required.");

		var tmpRoot = GetTmpRoot();
		var tmpPath = Path.Combine(tmpRoot, $"{req.UploadId}.part");
		var metaPath = Path.Combine(tmpRoot, $"{req.UploadId}.json");

		if (!System.IO.File.Exists(tmpPath) || !System.IO.File.Exists(metaPath))
			return NotFound("Upload not found.");

		var metaJson = await System.IO.File.ReadAllTextAsync(metaPath, ct);
		var meta = System.Text.Json.JsonSerializer.Deserialize<StartUploadRequest>(metaJson);
		if (meta is null)
			return BadRequest("Bad upload metadata.");

		var modsRoot = GetModsRoot();
		var physicalPath = Path.Combine(modsRoot, req.FileName);

		// атомарно “коммитим” .part в финальный файл
		System.IO.File.Move(tmpPath, physicalPath, overwrite: true);

		// считаем SHA256 потоково (без RAM-помойки)
		string sha256Hex;
		await using (var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			using var sha = SHA256.Create();
			var hash = await sha.ComputeHashAsync(fs, ct);
			sha256Hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}

		// относительный путь
		var relativePath = Path.Combine("mods", req.FileName).Replace('\\', '/');

		var entity = new GameModEntity
		{
			Name = meta.Name,
			Description = meta.Description,
			Version = meta.Version,
			InstallPath = string.IsNullOrWhiteSpace(meta.InstallPath) ? "mods" : meta.InstallPath.Trim(),
			FilePath = relativePath,
			Sha256 = sha256Hex,
			UploadedAtUtc = DateTime.UtcNow
		};

		_db.GameMods.Add(entity);
		await _db.SaveChangesAsync(ct);

		// чистим мету
		try { System.IO.File.Delete(metaPath); } catch { }

		_uploadLocks.TryRemove(req.UploadId, out _);

		return Ok(new { ok = true, redirectUrl = Url.Action(nameof(Index)) });
	}

	// ---------- delete (как было) ----------
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(long id, CancellationToken ct)
	{
		var mod = await _db.GameMods.FirstOrDefaultAsync(m => m.Id == id, ct);
		if (mod is null)
			return RedirectToAction(nameof(Index));

		try
		{
			var modsRoot = GetModsRoot();
			var fileName = Path.GetFileName(mod.FilePath);
			var physicalPath = Path.Combine(modsRoot, fileName);
			if (System.IO.File.Exists(physicalPath))
				System.IO.File.Delete(physicalPath);
		}
		catch { }

		_db.GameMods.Remove(mod);
		await _db.SaveChangesAsync(ct);

		return RedirectToAction(nameof(Index));
	}
}
