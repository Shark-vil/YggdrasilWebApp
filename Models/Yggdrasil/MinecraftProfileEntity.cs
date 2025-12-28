using System.ComponentModel.DataAnnotations;

namespace YggdrasilWebApp.Models.Yggdrasil;

public sealed class MinecraftProfileEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid UserId { get; set; }
	public UserEntity User { get; set; } = default!;

	[MaxLength(16)]
	public string Name { get; set; } = default!;

	// Mojang-like "textures" property payload (base64 JSON)
	public string? TexturesValue { get; set; }
	public string? TexturesSignature { get; set; }

	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}