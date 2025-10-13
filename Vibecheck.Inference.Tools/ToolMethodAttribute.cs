namespace Vibecheck.Inference.Tools;

[AttributeUsage(AttributeTargets.Method)]
public class ToolMethodAttribute : Attribute
{
    public required string Description { get; init; }
}
