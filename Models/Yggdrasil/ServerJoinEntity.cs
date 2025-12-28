namespace YggdrasilWebApp.Models.Yggdrasil;

public sealed class ServerJoinEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid ProfileId { get; set; }
	public MinecraftProfileEntity Profile { get; set; } = default!;

	public string ServerId { get; set; } = default!;
	public string? ClientIp { get; set; }

	public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}