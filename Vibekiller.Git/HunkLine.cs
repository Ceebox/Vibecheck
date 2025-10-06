namespace Vibekiller.Git;

public sealed record HunkLine
{
    public ChangeType Type { get; init; }
    public string Content { get; init; } = string.Empty;
}
