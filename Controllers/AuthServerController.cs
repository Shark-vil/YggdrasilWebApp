using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Dtos;
using YggdrasilWebApp.Helpers;
using YggdrasilWebApp.Models.Yggdrasil;
using YggdrasilWebApp.Services;

namespace YggdrasilWebApp.Controllers;

[ApiController]
[Route("api/yggdrasil/authserver")]
public sealed class AuthServerController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly PasswordHasher _hasher;
	private readonly TokenGenerator _tokens;

	public AuthServerController(AppDbContext db, PasswordHasher hasher, TokenGenerator tokens)
	{
		_db = db;
		_hasher = hasher;
		_tokens = tokens;
	}

	[HttpPost("authenticate")]
	public async Task<ActionResult<AuthenticateResponse>> Authenticate([FromBody] AuthenticateRequest req, CancellationToken ct)
	{
		var login = req.Username.Trim();

		var user = await _db.Users
			.Include(x => x.Profiles)
			.FirstOrDefaultAsync(x => x.Username == login || (x.Email != null && x.Email == login), ct);

		if (user is null || !_hasher.Verify(req.Password, user.PasswordHash))
			return Unauthorized(new { error = "ForbiddenOperationException", errorMessage = "Invalid credentials." });

		var clientToken = string.IsNullOrWhiteSpace(req.ClientToken) ? _tokens.NewTokenHex(16) : req.ClientToken!;
		var accessToken = _tokens.NewTokenHex(32);

		// revoke old tokens (optional). Тут делаем жестко: 1 активный токен на clientToken
		var old = await _db.AccessTokens
			.Where(t => t.UserId == user.Id && t.ClientToken == clientToken && t.RevokedAtUtc == null)
			.ToListAsync(ct);
		foreach (var t in old) t.RevokedAtUtc = DateTime.UtcNow;

		var tokenEntity = new AccessTokenEntity
		{
			UserId = user.Id,
			Token = accessToken,
			ClientToken = clientToken,
			IssuedAtUtc = DateTime.UtcNow,
			ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
		};

		_db.AccessTokens.Add(tokenEntity);
		await _db.SaveChangesAsync(ct);

		var available = user.Profiles
			.OrderBy(p => p.CreatedAtUtc)
			.Select(p => new YggProfileRef(p.Id.ToString("N"), p.Name))
			.ToList();

		var selected = available.FirstOrDefault();

		YggUser? userObj = null;
		if (req.RequestUser == true)
		{
			userObj = new YggUser(user.Id.ToString("N"), new List<YggUserProperty>());
		}

		return new AuthenticateResponse(
			AccessToken: accessToken,
			ClientToken: clientToken,
			AvailableProfiles: available,
			SelectedProfile: selected,
			User: userObj
		);
	}

	[HttpPost("refresh")]
	public async Task<ActionResult<RefreshResponse>> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
	{
		var token = await _db.AccessTokens
			.Include(t => t.User)
			.ThenInclude(u => u.Profiles)
			.FirstOrDefaultAsync(t => t.Token == req.AccessToken, ct);

		if (token is null || !token.IsActive)
			return Unauthorized(new { error = "ForbiddenOperationException", errorMessage = "Invalid token." });

		if (!string.IsNullOrWhiteSpace(req.ClientToken) && token.ClientToken != req.ClientToken)
			return Unauthorized(new { error = "ForbiddenOperationException", errorMessage = "Invalid clientToken." });

		// issue new token, revoke old
		token.RevokedAtUtc = DateTime.UtcNow;

		var newAccess = _tokens.NewTokenHex(32);
		var newClient = string.IsNullOrWhiteSpace(req.ClientToken) ? (token.ClientToken ?? _tokens.NewTokenHex(16)) : req.ClientToken!;

		var newEntity = new AccessTokenEntity
		{
			UserId = token.UserId,
			Token = newAccess,
			ClientToken = newClient,
			IssuedAtUtc = DateTime.UtcNow,
			ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
		};

		_db.AccessTokens.Add(newEntity);
		await _db.SaveChangesAsync(ct);

		YggProfileRef? selected = null;
		if (req.SelectedProfile is not null)
		{
			// validate ownership
			var pid = GuidHelper.ParseGuidN(req.SelectedProfile.Id);
			if (pid != null)
			{
				var profile = token.User.Profiles.FirstOrDefault(p => p.Id == pid.Value);
				if (profile != null) selected = new YggProfileRef(profile.Id.ToString("N"), profile.Name);
			}
		}
		else
		{
			var p = token.User.Profiles.OrderBy(x => x.CreatedAtUtc).FirstOrDefault();
			if (p != null) selected = new YggProfileRef(p.Id.ToString("N"), p.Name);
		}

		YggUser? userObj = null;
		if (req.RequestUser == true)
			userObj = new YggUser(token.UserId.ToString("N"), new List<YggUserProperty>());

		return new RefreshResponse(
			AccessToken: newAccess,
			ClientToken: newClient,
			SelectedProfile: selected,
			User: userObj
		);
	}

	[HttpPost("validate")]
	public async Task<IActionResult> Validate([FromBody] ValidateRequest req, CancellationToken ct)
	{
		var token = await _db.AccessTokens
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Token == req.AccessToken, ct);
		if (token is null || !token.IsActive) return Forbid();

		if (!string.IsNullOrWhiteSpace(req.ClientToken) && token.ClientToken != req.ClientToken)
			return Forbid();

		return NoContent();
	}

	[HttpPost("invalidate")]
	public async Task<IActionResult> Invalidate([FromBody] InvalidateRequest req, CancellationToken ct)
	{
		var token = await _db.AccessTokens.FirstOrDefaultAsync(t => t.Token == req.AccessToken, ct);
		if (token is null) return NoContent();

		if (!string.IsNullOrWhiteSpace(req.ClientToken) && token.ClientToken != req.ClientToken)
			return NoContent(); // Mojang-like: молча

		token.RevokedAtUtc = DateTime.UtcNow;
		await _db.SaveChangesAsync(ct);
		return NoContent();
	}

	[HttpPost("signout")]
	public async Task<IActionResult> Signout([FromBody] SignoutRequest req, CancellationToken ct)
	{
		var login = req.Username.Trim();

		var user = await _db.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Username == login || (x.Email != null && x.Email == login), ct);
		if (user is null || !_hasher.Verify(req.Password, user.PasswordHash))
			return Unauthorized(new { error = "ForbiddenOperationException", errorMessage = "Invalid credentials." });

		var tokens = await _db.AccessTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(ct);
		foreach (var t in tokens) t.RevokedAtUtc = DateTime.UtcNow;

		await _db.SaveChangesAsync(ct);
		return NoContent();
	}
}