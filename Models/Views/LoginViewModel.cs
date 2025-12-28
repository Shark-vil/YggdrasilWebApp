using System.ComponentModel.DataAnnotations;

namespace YggdrasilWebApp.Models.Views;

public sealed class LoginViewModel
{
	[Required]
	[MaxLength(128)]
	public string UsernameOrEmail { get; set; } = "";

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = "";

	public bool RememberMe { get; set; } = true;
}
