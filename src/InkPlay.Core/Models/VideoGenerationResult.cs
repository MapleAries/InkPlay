namespace InkPlay.Core.Models;

public class VideoGenerationResult
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
