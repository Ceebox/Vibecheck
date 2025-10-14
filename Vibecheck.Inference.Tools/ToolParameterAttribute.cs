namespace Vibecheck.Inference.Tools;

[AttributeUsage(AttributeTargets.Parameter)]
public class ToolParameterAttribute : Attribute
{
    public required string Description { get; init; }
}
