using System.ComponentModel.DataAnnotations;

namespace YggdrasilWebApp.Models.Views;

public sealed class RegisterViewModel
{
	[Required]
	[MinLength(3)]
	[MaxLength(128)]
	public string Username { get; set; } = "";

	[EmailAddress]
	[MaxLength(256)]
	public string? Email { get; set; }

	[Required]
	[MinLength(6)]
	[MaxLength(200)]
	[DataType(DataType.Password)]
	public string Password { get; set; } = "";

	[Required]
	[DataType(DataType.Password)]
	[Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
	public string ConfirmPassword { get; set; } = "";

	// опционально: ник для Minecraft-профиля (если пусто — берём Username)
	[MaxLength(16)]
	public string? ProfileName { get; set; }
}