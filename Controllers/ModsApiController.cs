using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using YggdrasilWebApp.Contexts;

namespace YggdrasilWebApp.Controllers;

[ApiController]
[Route("api/mods")]
public sealed class ModsApiController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly IWebHostEnvironment _env;

	public ModsApiController(AppDbContext db, IWebHostEnvironment env)
	{
		_db = db;
		_env = env;
	}

	// GET /api/mods
	[HttpGet]
	public async Task<IActionResult> GetMods(CancellationToken ct)
	{
		var baseUrl = $"{Request.Scheme}://{Request.Host}";

		var mods = await _db.GameMods
			.OrderBy(m => m.Name)
			.ToListAsync(ct);

		var dto = mods.Select(m => new
		{
			id = m.Id,
			name = m.Name,
			description = m.Description,
			version = m.Version,
			// полный URL для скачивания
			url = baseUrl + "/" + m.FilePath,
			// относительный путь установки (по дефолту "mods")
			installPath = m.InstallPath,
			sha256 = m.Sha256
		});

		return Ok(dto);
	}

	// GET /mods/{fileName}
	[HttpGet("/mods/{fileName}")]
	public IActionResult GetModFile([FromRoute] string fileName)
	{
		fileName = SanitizeFileName(fileName);
		if (string.IsNullOrWhiteSpace(fileName))
			return BadRequest("Invalid file name.");

		// Storage/Mods рядом с проектом (ContentRoot)
		var modsDir = Path.Combine(_env.ContentRootPath, "Storage", "Mods");
		if (!Directory.Exists(modsDir))
			Directory.CreateDirectory(modsDir);

		var fullPath = Path.Combine(modsDir, fileName);

		if (!System.IO.File.Exists(fullPath))
			return NotFound();

		return PhysicalFile(
			fullPath,
			MediaTypeNames.Application.Octet,
			fileDownloadName: fileName,
			enableRangeProcessing: true);
	}

	private static string SanitizeFileName(string input)
	{
		input = Uri.UnescapeDataString(input);

		// выкидываем любые пути
		input = input.Replace('\\', '/');
		if (input.Contains('/'))
			input = input.Split('/').Last();

		// убираем мусор
		foreach (var c in Path.GetInvalidFileNameChars())
			input = input.Replace(c.ToString(), "");

		// режем попытки ../
		input = input.Replace("..", "");

		return input.Trim();
	}
}
