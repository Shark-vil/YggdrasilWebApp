using System.ComponentModel.DataAnnotations;

namespace YggdrasilWebApp.Views;

public sealed class UploadModViewModel
{
	[Required]
	[MaxLength(128)]
	public string Name { get; set; } = default!;

	[MaxLength(256)]
	public string? Description { get; set; }

	[Required]
	[MaxLength(64)]
	public string Version { get; set; } = "1.0.0";

	// по умолчанию всё в папку mods
	[MaxLength(256)]
	public string InstallPath { get; set; } = "mods";

	[Required]
	public IFormFile File { get; set; } = default!;
}
