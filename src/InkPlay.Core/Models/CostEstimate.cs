namespace InkPlay.Core.Models;

public class CostEstimate
{
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public string ModelId { get; set; } = string.Empty;
}
