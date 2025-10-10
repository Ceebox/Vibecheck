namespace Vibecheck.Git;

public sealed record PatchInfo
{
    public required string Path { get; init; }
    public required string Contents { get; init; }
}
