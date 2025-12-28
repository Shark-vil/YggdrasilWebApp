namespace YggdrasilWebApp.Helpers;

public static class GuidHelper
{
	public static Guid? ParseGuidN(string s)
	{
		if (Guid.TryParseExact(s, "N", out var g)) return g;
		if (Guid.TryParse(s, out g)) return g;
		return null;
	}

	public static Guid? ParseGuidAny(string s)
	{
		if (string.IsNullOrWhiteSpace(s)) return null;
		if (Guid.TryParseExact(s, "N", out var n)) return n;
		if (Guid.TryParse(s, out var g)) return g;
		return null;
	}
}
