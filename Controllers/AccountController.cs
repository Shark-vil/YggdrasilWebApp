using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Models.Views;
using YggdrasilWebApp.Models.Yggdrasil;
using YggdrasilWebApp.Services;

namespace YggdrasilWebApp.Controllers;

public sealed class AccountController : Controller
{
	private readonly AppDbContext _db;
	private readonly PasswordHasher _hasher;

	public AccountController(AppDbContext db, PasswordHasher hasher)
	{
		_db = db;
		_hasher = hasher;
	}

	[HttpGet("/account/register")]
	public IActionResult Register(string? returnUrl = null)
	{
		ViewBag.ReturnUrl = returnUrl;
		return View(new RegisterViewModel());
	}

	[HttpPost("/account/register")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null, CancellationToken ct = default)
	{
		ViewBag.ReturnUrl = returnUrl;

		model.Username = (model.Username ?? "").Trim();
		model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
		model.ProfileName = string.IsNullOrWhiteSpace(model.ProfileName) ? null : model.ProfileName.Trim();

		if (!ModelState.IsValid)
			return View(model);

		// жёстко отсекаем пробелы/кривые логины
		if (model.Username.Any(char.IsWhiteSpace))
		{
			ModelState.AddModelError(nameof(model.Username), "Username must not contain spaces.");
			return View(model);
		}

		var existsUser = await _db.Users.AnyAsync(u => u.Username == model.Username, ct);
		if (existsUser)
		{
			ModelState.AddModelError(nameof(model.Username), "Username already taken.");
			return View(model);
		}

		if (model.Email != null)
		{
			var existsEmail = await _db.Users.AnyAsync(u => u.Email == model.Email, ct);
			if (existsEmail)
			{
				ModelState.AddModelError(nameof(model.Email), "Email already taken.");
				return View(model);
			}
		}

		var profileName = model.ProfileName ?? model.Username;
		if (profileName.Length > 16)
		{
			ModelState.AddModelError(nameof(model.ProfileName), "Profile name must be 16 chars max.");
			return View(model);
		}

		if (profileName.Any(char.IsWhiteSpace))
		{
			ModelState.AddModelError(nameof(model.ProfileName), "Profile name must not contain spaces.");
			return View(model);
		}

		var existsProfile = await _db.Profiles.AnyAsync(p => p.Name == profileName, ct);
		if (existsProfile)
		{
			ModelState.AddModelError(nameof(model.ProfileName), "Minecraft profile name already taken.");
			return View(model);
		}

		var user = new UserEntity
		{
			Username = model.Username,
			Email = model.Email,
			PasswordHash = _hasher.Hash(model.Password)
		};

		var profile = new MinecraftProfileEntity
		{
			UserId = user.Id,
			Name = profileName
		};

		_db.Users.Add(user);
		_db.Profiles.Add(profile);
		await _db.SaveChangesAsync(ct);

		await SignInUser(user, rememberMe: true);

		if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
			return Redirect(returnUrl);

		return RedirectToAction("Index", "Home");
	}

	[HttpGet("/account/login")]
	public IActionResult Login(string? returnUrl = null)
	{
		ViewBag.ReturnUrl = returnUrl;
		return View(new LoginViewModel());
	}

	[HttpPost("/account/login")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
	{
		ViewBag.ReturnUrl = returnUrl;

		model.UsernameOrEmail = (model.UsernameOrEmail ?? "").Trim();

		if (!ModelState.IsValid)
			return View(model);

		var login = model.UsernameOrEmail;

		var user = await _db.Users.FirstOrDefaultAsync(u =>
			u.Username == login || (u.Email != null && u.Email == login), ct);

		if (user is null || !_hasher.Verify(model.Password, user.PasswordHash))
		{
			ModelState.AddModelError(string.Empty, "Invalid credentials.");
			return View(model);
		}

		await SignInUser(user, model.RememberMe);

		if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
			return Redirect(returnUrl);

		return RedirectToAction("Index", "Home");
	}

	[HttpPost("/account/logout")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		return RedirectToAction("Index", "Home");
	}

	private async Task SignInUser(UserEntity user, bool rememberMe)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.Id.ToString("N")),
			new(ClaimTypes.Name, user.Username),
		};

		if (!string.IsNullOrWhiteSpace(user.Email))
			claims.Add(new Claim(ClaimTypes.Email, user.Email));

		var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		var principal = new ClaimsPrincipal(identity);

		await HttpContext.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme,
			principal,
			new AuthenticationProperties
			{
				IsPersistent = rememberMe,
				AllowRefresh = true,
				IssuedUtc = DateTimeOffset.UtcNow,
				ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(12)
			});
	}
}