using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using YggdrasilWebApp.Services;

namespace YggdrasilWebApp.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/yggdrasil/minecraftservices")]
public sealed class MinecraftServicesController : ControllerBase
{
	private readonly TexturesService _textures;

	public MinecraftServicesController(TexturesService textures)
	{
		_textures = textures;
	}

	// GET /minecraftservices/publickeys
	[HttpGet("publickeys")]
	public IActionResult PublicKeys()
	{
		var publicKeyPem = _textures.GetPublicKeyPem();
		if (string.IsNullOrWhiteSpace(publicKeyPem))
		{
			return StatusCode(500, new
			{
				error = "TexturesPublicKeyNotConfigured",
				errorMessage = "No public key configured for textures signing."
			});
		}

		// Mojang-style: base64 DER (SubjectPublicKeyInfo)
		var publicKeyBase64 = PemToSubjectPublicKeyInfoBase64(publicKeyPem);

		var keys = new[]
		{
			new { publicKey = publicKeyBase64 }
		};

		return Ok(new
		{
			profilePropertyKeys = keys,
			playerCertificateKeys = keys,
			authenticationKeys = keys
		});
	}

	private static string PemToSubjectPublicKeyInfoBase64(string pem)
	{
		using var rsa = RSA.Create();
		rsa.ImportFromPem(pem);

		// DER SubjectPublicKeyInfo -> base64 (без PEM-хедеров)
		var der = rsa.ExportSubjectPublicKeyInfo();
		return Convert.ToBase64String(der);
	}
}
