using System.Text.Json.Serialization;

namespace Vibecheck.Inference.Tools;

[JsonConverter(typeof(ToolInvocationConverter))]
public sealed class ToolInvocation
{
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}
