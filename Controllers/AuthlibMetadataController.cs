using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Services;

namespace YggdrasilWebApp.Controllers;

[ApiController]
[Route("api/yggdrasil")]
public sealed class AuthlibMetadataController : ControllerBase
{
	private readonly TexturesService _textures;
	private readonly AppDbContext _db;

	public AuthlibMetadataController(TexturesService textures, AppDbContext db)
	{
		_textures = textures;
		_db = db;
	}

	[HttpGet]
	[AllowAnonymous] // чтобы authlib-injector мог дергать без куки
	public IActionResult Get()
	{
		var baseUrl = $"{Request.Scheme}://{Request.Host}";

		var publicKeyPem = _textures.GetPublicKeyPem();
		if (string.IsNullOrWhiteSpace(publicKeyPem))
		{
			// Ключ не сконфигурен — это твоя ошибка конфигурации, не клиента
			return StatusCode(500, new
			{
				error = "TexturesPublicKeyNotConfigured",
				errorMessage = "Yggdrasil:TexturesPrivateKeyPem / TexturesPrivateKeyPath не заданы."
			});
		}

		var payload = new
		{
			meta = new
			{
				serverName = "Pipbuck Auth",
				implementationName = "Pipbuck YggdrasilWebApp",
				implementationVersion = "1.0.0",
				links = new
				{
					homepage = baseUrl,
					register = baseUrl + "/account/register"
				}
			},
			skinDomains = new[]
			{
				"mc.pipbuck.ru",
				".mc.pipbuck.ru"
			},
			// ВАЖНО: имя поля должно быть именно таким
			signaturePublickey = publicKeyPem
		};

		return Ok(payload);
	}

	[HttpGet("debug/verify-texture")]
	public IActionResult VerifyTextureSig()
	{
		var profile = _db.Profiles
			.First(p => p.Id == Guid.Parse("499bb6fc-8ab4-438e-b69c-a9b7c0b9948e"));

		var ok = _textures.VerifyExistingTextures(
			profile.TexturesValue ?? "",
			profile.TexturesSignature ?? "");

		return Ok(new { ok });
	}
}