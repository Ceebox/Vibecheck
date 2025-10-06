namespace Vibekiller.Engine;
public sealed record ReviewComment
{
    public string? Path { get; init; }
    public bool HasChange { get; init; }
    public string? SuggestedChange { get; init; }
    public string? Comment { get; init; }
    public double? AiProbability { get; init; }
}
