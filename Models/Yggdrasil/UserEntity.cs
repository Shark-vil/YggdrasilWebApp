using System.ComponentModel.DataAnnotations;

namespace YggdrasilWebApp.Models.Yggdrasil;

public sealed class UserEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[MaxLength(128)]
	public string Username { get; set; } = default!;

	[MaxLength(256)]
	public string? Email { get; set; }

	// PBKDF2 format: {iterations}.{saltBase64}.{hashBase64}
	public string PasswordHash { get; set; } = default!;

	// простой флаг админа
	public bool IsAdmin { get; set; } = false;

	public List<MinecraftProfileEntity> Profiles { get; set; } = new();
}
