namespace InkPlay.Core.Models;

public class VideoGenerationRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Duration { get; set; } = 5;
    public string Resolution { get; set; } = "1080p";
}
