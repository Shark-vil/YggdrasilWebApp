using System.ComponentModel.DataAnnotations;

namespace YggdrasilWebApp.Models.App
{
	public sealed class GameModEntity
	{
		[Key]
		public long Id { get; set; }

		[MaxLength(128)]
		public string Name { get; set; } = default!;

		[MaxLength(256)]
		public string? Description { get; set; }

		[MaxLength(64)]
		public string Version { get; set; } = "1.0.0";

		// относительный путь/имя файла под wwwroot (например, "mods/xxxxx.jar")
		[MaxLength(512)]
		public string FilePath { get; set; } = default!;

		// SHA-256 хеш файла (под "сигнатуру")
		[MaxLength(128)]
		public string Sha256 { get; set; } = default!;

		// куда ставить (по умолчанию: "mods")
		[MaxLength(256)]
		public string InstallPath { get; set; } = "mods";

		public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
	}
}
