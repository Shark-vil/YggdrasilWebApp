using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace YggdrasilWebApp.Services;

public sealed class TexturesService
{
	private readonly RSA? _rsa;

	// Если ключ не задан — signature будет null, но TexturesValue всё равно обновится
	public TexturesService(IConfiguration cfg)
	{
		// appsettings.json:
		// "Yggdrasil": { "TexturesPrivateKeyPem": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----" }
		// либо "TexturesPrivateKeyPath": "C:\\keys\\ygg.pem"
		var pem = cfg["Yggdrasil:TexturesPrivateKeyPem"];
		var path = cfg["Yggdrasil:TexturesPrivateKeyPath"];

		if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
			pem = File.ReadAllText(path);

		if (!string.IsNullOrWhiteSpace(pem))
		{
			var rsa = RSA.Create();
			rsa.ImportFromPem(pem);
			_rsa = rsa;
		}
	}

	public (string value, string? signature) BuildSkinTextures(string profileIdN, string profileName, string skinUrl)
	{
		// Mojang-like payload: timestamp/profileId/profileName/textures.SKIN.url
		var payload = new
		{
			timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			profileId = profileIdN,
			profileName = profileName,
			textures = new
			{
				SKIN = new
				{
					url = skinUrl
				}
			}
		};

		var json = JsonSerializer.Serialize(payload);
		var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

		string? sig = null;
		if (_rsa != null)
		{
			// Mojang historically использует SHA1withRSA (PKCS#1)
			var data = Encoding.UTF8.GetBytes(value);
			var signed = _rsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
			sig = Convert.ToBase64String(signed);
		}

		return (value, sig);
	}
}