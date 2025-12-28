using System.Security.Cryptography;

namespace YggdrasilWebApp.Services;

public sealed class PasswordHasher
{
	public string Hash(string password, int iterations = 120_000)
	{
		var salt = RandomNumberGenerator.GetBytes(16);
		using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
		var hash = pbkdf2.GetBytes(32);

		return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
	}

	public bool Verify(string password, string stored)
	{
		var parts = stored.Split('.', 3);
		if (parts.Length != 3) return false;

		if (!int.TryParse(parts[0], out var iterations)) return false;

		var salt = Convert.FromBase64String(parts[1]);
		var expected = Convert.FromBase64String(parts[2]);

		using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
		var actual = pbkdf2.GetBytes(expected.Length);

		return CryptographicOperations.FixedTimeEquals(actual, expected);
	}
}