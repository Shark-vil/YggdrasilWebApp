using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Dtos;
using YggdrasilWebApp.Models.Yggdrasil;

namespace YggdrasilWebApp.Controllers;

[ApiController]
[Route("api/yggdrasil/sessionserver/session/minecraft")]
public sealed class SessionServerController : ControllerBase
{
	private readonly AppDbContext _db;

	public SessionServerController(AppDbContext db)
	{
		_db = db;
	}

	[HttpPost("join")]
	public async Task<IActionResult> Join([FromBody] JoinRequest req, CancellationToken ct)
	{
		var token = await _db.AccessTokens
			.Include(t => t.User)
			.FirstOrDefaultAsync(t => t.Token == req.AccessToken, ct);

		if (token is null || !token.IsActive) return Forbid();

		var profileId = ParseGuidN(req.SelectedProfile);
		if (profileId == null) return Forbid();

		var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == profileId.Value && p.UserId == token.UserId, ct);
		if (profile is null) return Forbid();

		var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

		// Упростим: 1 запись join на (serverId, profile)
		var existing = await _db.ServerJoins
			.FirstOrDefaultAsync(j => j.ServerId == req.ServerId && j.ProfileId == profile.Id, ct);

		if (existing is null)
		{
			_db.ServerJoins.Add(new ServerJoinEntity
			{
				ProfileId = profile.Id,
				ServerId = req.ServerId,
				ClientIp = ip,
				JoinedAtUtc = DateTime.UtcNow
			});
		}
		else
		{
			existing.JoinedAtUtc = DateTime.UtcNow;
			existing.ClientIp = ip;
		}

		await _db.SaveChangesAsync(ct);
		return NoContent();
	}

	[HttpGet("hasJoined")]
	public async Task<ActionResult<SessionProfileResponse>> HasJoined(
		[FromQuery] string username,
		[FromQuery] string serverId,
		[FromQuery] bool? unsigned,
		CancellationToken ct)
	{
		var name = username.Trim();

		// Ищем последнюю join-запись для этого serverId и профиля с таким ником
		var join = await _db.ServerJoins
			.Include(j => j.Profile)
			.Where(j => j.ServerId == serverId && j.Profile.Name == name)
			.OrderByDescending(j => j.JoinedAtUtc)
			.FirstOrDefaultAsync(ct);

		if (join is null) return NoContent();

		var props = BuildTexturesProperties(join.Profile, unsigned);

		return new SessionProfileResponse(
			Id: join.Profile.Id.ToString("N"),
			Name: join.Profile.Name,
			Properties: props
		);
	}

	[HttpGet("profile/{uuid}")]
	public async Task<ActionResult<SessionProfileResponse>> ProfileByUuid(
		[FromRoute] string uuid,
		[FromQuery] bool? unsigned,
		CancellationToken ct)
	{
		var id = ParseGuidN(uuid);
		if (id == null) return NoContent();

		var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == id.Value, ct);
		if (profile is null) return NoContent();

		var props = BuildTexturesProperties(profile, unsigned);

		return new SessionProfileResponse(
			Id: profile.Id.ToString("N"),
			Name: profile.Name,
			Properties: props
		);
	}

	private static List<ProfileProperty> BuildTexturesProperties(MinecraftProfileEntity profile, bool? unsigned)
	{
		// null -> false
		var isUnsigned = unsigned == true;
		var includeSig = !isUnsigned;

		var list = new List<ProfileProperty>();
		if (!string.IsNullOrWhiteSpace(profile.TexturesValue))
		{
			list.Add(new ProfileProperty(
				Name: "textures",
				Value: profile.TexturesValue!,
				Signature: includeSig ? profile.TexturesSignature : null
			));
		}
		return list;
	}

	private static Guid? ParseGuidN(string s)
	{
		if (Guid.TryParseExact(s, "N", out var g)) return g;
		if (Guid.TryParse(s, out g)) return g;
		return null;
	}
}
