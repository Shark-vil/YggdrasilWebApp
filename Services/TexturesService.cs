using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace YggdrasilWebApp.Services;

public sealed class TexturesService
{
	private readonly RSA? _privateRsa;
	private readonly RSA? _publicRsa;

	public TexturesService(IConfiguration cfg)
	{
		// PRIVATE
		var privPem = cfg["Yggdrasil:TexturesPrivateKeyPem"];
		var privPath = cfg["Yggdrasil:TexturesPrivateKeyPath"];

		if (!string.IsNullOrWhiteSpace(privPath) && File.Exists(privPath))
			privPem = File.ReadAllText(privPath);

		if (!string.IsNullOrWhiteSpace(privPem))
		{
			_privateRsa = RSA.Create();
			ImportPem(_privateRsa, privPem, isPrivate: true);
		}

		// PUBLIC
		var pubPem = cfg["Yggdrasil:TexturesPublicKeyPem"];
		var pubPath = cfg["Yggdrasil:TexturesPublicKeyPath"];

		if (!string.IsNullOrWhiteSpace(pubPath) && File.Exists(pubPath))
			pubPem = File.ReadAllText(pubPath);

		if (!string.IsNullOrWhiteSpace(pubPem))
		{
			_publicRsa = RSA.Create();
			ImportPem(_publicRsa, pubPem, isPrivate: false);
		}
		else if (_privateRsa is not null)
		{
			// Если публичник явно не задан — вытаскиваем из приватника
			_publicRsa = RSA.Create();
			_publicRsa.ImportSubjectPublicKeyInfo(_privateRsa.ExportSubjectPublicKeyInfo(), out _);
		}
	}

	/// <summary>
	/// Подпись текстур как у Mojang: SHA1withRSA (PKCS#1 v1.5), данные — UTF8(valueBase64)
	/// </summary>
	public (string value, string? signature) BuildSkinTextures(string profileIdN, string profileName, string skinUrl)
	{
		var payload = new
		{
			timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			profileId = profileIdN,   // без дефисов — ок
			profileName = profileName,
			textures = new
			{
				SKIN = new { url = skinUrl }
			}
		};

		var json = JsonSerializer.Serialize(payload);
		var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

		if (_privateRsa is null)
			return (value, null);

		var data = Encoding.UTF8.GetBytes(value);

		// MUST: SHA1 + PKCS#1 v1.5
		var signed = _privateRsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
		var sig = Convert.ToBase64String(signed);

		return (value, sig);
	}

	/// <summary>
	/// Проверка подписи textures.signature публичным ключом.
	/// </summary>
	public bool VerifyExistingTextures(string valueBase64, string signatureBase64)
	{
		if (_publicRsa is null)
			throw new InvalidOperationException("Public RSA key is not configured.");

		var data = Encoding.UTF8.GetBytes(valueBase64);
		var sig = Convert.FromBase64String(signatureBase64);

		return _publicRsa.VerifyData(data, sig, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
	}

	/// <summary>
	/// Возвращает PEM публичного ключа (BEGIN PUBLIC KEY).
	/// </summary>
	public string? GetPublicKeyPem()
	{
		if (_publicRsa is null && _privateRsa is null)
			return null;

		var rsa = _publicRsa ?? _privateRsa!;
		return rsa.ExportSubjectPublicKeyInfoPem(); // .NET 7+
	}

	public string SignTexturesValue(string valueBase64)
	{
		if (_privateRsa is null)
			throw new InvalidOperationException("Private RSA key is not configured.");

		var data = Encoding.UTF8.GetBytes(valueBase64.Trim());
		var signed = _privateRsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
		return Convert.ToBase64String(signed);
	}


	private static void ImportPem(RSA rsa, string pem, bool isPrivate)
	{
		try
		{
			rsa.ImportFromPem(pem);
		}
		catch (Exception ex)
		{
			var kind = isPrivate ? "private" : "public";
			throw new InvalidOperationException(
				$"Failed to import {kind} RSA key from PEM. Make sure it is valid PEM.", ex);
		}
	}
}
