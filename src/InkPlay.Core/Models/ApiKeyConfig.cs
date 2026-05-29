namespace InkPlay.Core.Models;

public class ApiKeyConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public ApiKeyCategory Category { get; set; } = ApiKeyCategory.Text;
    public bool IsDefault { get; set; }
}

public enum ApiKeyCategory
{
    Text,
    Video,
    Voice
}
