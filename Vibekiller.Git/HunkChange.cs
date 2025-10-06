namespace Vibekiller.Git;

public sealed record HunkChange
{
    public string Path { get; init; } = string.Empty;
    public int OldStart { get; init; }
    public int OldCount { get; init; }
    public int NewStart { get; init; }
    public int NewCount { get; init; }
    public List<HunkLine> Lines { get; init; } = [];

    public override string ToString()
        => $"{Path}: -{OldStart},{OldCount} +{NewStart},{NewCount} ({Lines.Count} lines)";
}
