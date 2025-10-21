using System.Text.Json;
using System.Text.Json.Serialization;
using Vibecheck.Utility;

namespace Vibecheck.Inference.Tools;

public sealed class ToolInvocationConverter : JsonConverter<ToolInvocation>
{
    public override ToolInvocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var activity = Tracing.Start();

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var toolProp = root.GetProperty("tool", true) ?? root.GetProperty("Tool", true);
        var parametersProp = root.GetProperty("parameters", true) ?? root.GetProperty("Parameters", true);

        var toolName = toolProp?.GetString() ?? string.Empty;
        if (string.IsNullOrEmpty(toolName))
        {
            return null;
        }

        Dictionary<string, object>? parameters = null;
        if (parametersProp is { ValueKind: JsonValueKind.Object })
        {
            parameters = [];
            foreach (var property in parametersProp.Value.EnumerateObject())
            {
                parameters[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString()!,
                    JsonValueKind.Number => property.Value.TryGetInt64(out var l) ? l : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => property.Value.ToString()!
                };
            }
        }

        return new ToolInvocation
        {
            Tool = toolName,
            Parameters = parameters
        };
    }

    public override void Write(Utf8JsonWriter writer, ToolInvocation value, JsonSerializerOptions options)
    {
        using var activity = Tracing.Start();

        writer.WriteStartObject();

        writer.WriteString("tool", value.Tool);

        if (value.Parameters is not null)
        {
            writer.WritePropertyName("parameters");
            JsonSerializer.Serialize(writer, value.Parameters, options);
        }

        writer.WriteEndObject();
    }
}
