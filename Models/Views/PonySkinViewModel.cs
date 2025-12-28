namespace YggdrasilWebApp.Models.Views;

public sealed class PonySkinViewModel
{
    public string ProfileId { get; set; } = default!;
    public string ProfileName { get; set; } = default!;

    public bool HasSkin { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public string SkinImageUrl { get; set; } = default!;
}