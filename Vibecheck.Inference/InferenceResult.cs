namespace Vibecheck.Inference;

public sealed record InferenceResult
{
    public required string Path { get; init; }
    public required int CodeStartLine { get; init; }
    public required string Contents { get; init; }
}
