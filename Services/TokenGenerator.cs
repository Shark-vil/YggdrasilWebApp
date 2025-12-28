using System.Security.Cryptography;
using System.Text;

namespace YggdrasilWebApp.Services;

public sealed class TokenGenerator
{
	public string NewTokenHex(int bytes = 32)
	{
		var b = RandomNumberGenerator.GetBytes(bytes);
		var sb = new StringBuilder(bytes * 2);
		foreach (var x in b) sb.Append(x.ToString("x2"));
		return sb.ToString();
	}
}
