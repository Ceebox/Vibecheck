namespace Vibecheck.Inference.Tools;

public sealed record ToolParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(object);
}
