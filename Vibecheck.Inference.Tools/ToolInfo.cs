namespace Vibecheck.Inference.Tools;

///<remarks>I'm considering making Description required, otherwise I reckon the AI will struggle.</remarks>
public sealed record ToolInfo
{
    public List<ToolMethodInfo> Methods { get; set; } = [];
}
