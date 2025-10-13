namespace Vibecheck.Inference.Tools;

public sealed record ToolInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}
