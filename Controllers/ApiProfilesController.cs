using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Dtos;

namespace YggdrasilWebApp.Controllers;

[ApiController]
[Route("api/profiles")]
public sealed class ApiProfilesController : ControllerBase
{
	private readonly AppDbContext _db;

	public ApiProfilesController(AppDbContext db)
	{
		_db = db;
	}

	// POST /api/profiles/minecraft
	// Body: ["Notch","jeb_"] (как у Mojang)
	[HttpPost("minecraft")]
	public async Task<ActionResult<List<SimpleProfileResponse>>> ProfilesByNames([FromBody] string[] names, CancellationToken ct)
	{
		if (names is null || names.Length == 0) return new List<SimpleProfileResponse>();

		var cleaned = names
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (cleaned.Length == 0) return new List<SimpleProfileResponse>();

		var profiles = await _db.Profiles
			.Where(p => cleaned.Contains(p.Name))
			.Select(p => new SimpleProfileResponse(p.Id.ToString("N"), p.Name))
			.ToListAsync(ct);

		// Mojang возвращает в том порядке, как найдет, можно и так.
		return profiles;
	}
}
