namespace Vibekiller.Git;

public sealed record HunkChange
{
    public required string Path { get; init; }
    public int OldStart { get; init; }
    public int OldCount { get; init; }
    public int NewStart { get; init; }
    public int NewCount { get; init; }
    public List<HunkLine> Lines { get; init; } = [];

    public override string ToString()
        => $"{Path}: -{OldStart},{OldCount} +{NewStart},{NewCount} ({Lines.Count} lines)";
}
