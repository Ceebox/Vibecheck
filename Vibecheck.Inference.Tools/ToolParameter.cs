namespace Vibecheck.Inference.Tools;

[AttributeUsage(AttributeTargets.Parameter)]
public class ToolParameter : Attribute
{
    public required string Description { get; init; }
}
