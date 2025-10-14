namespace Vibecheck.Inference.Tools;

public sealed record ToolMethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ToolParameterInfo> Parameters { get; set; } = [];
}
