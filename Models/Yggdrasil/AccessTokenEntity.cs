namespace YggdrasilWebApp.Models.Yggdrasil;

public sealed class AccessTokenEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid UserId { get; set; }
	public UserEntity User { get; set; } = default!;

	public string Token { get; set; } = default!;
	public string? ClientToken { get; set; }

	public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
	public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);

	public DateTime? RevokedAtUtc { get; set; }

	public bool IsRevoked => RevokedAtUtc != null;
	public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
	public bool IsActive => !IsRevoked && !IsExpired;
}
