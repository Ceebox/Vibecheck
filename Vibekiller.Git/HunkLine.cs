namespace Vibekiller.Git;

public sealed record HunkLine
{
    public ChangeType Type { get; init; }
    public required string Content { get; init; }
}
