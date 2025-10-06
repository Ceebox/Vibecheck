namespace Vibekiller.Engine;
public sealed record ReviewComment
{
    public bool HasComment { get; init; }
    public string? Comment { get; init; }
    public string? SuggestedChange { get; init; }
    public double? AiProbability { get; init; }
}
