using System.Text.Json;
using System.Text.Json.Serialization;
using Vibecheck.Utility;

namespace Vibecheck.Engine;

internal sealed class ReviewCommentConverter : JsonConverter<ReviewComment>
{
    public override ReviewComment? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var hasChange = root.GetProperty("HasChange", true) ?? root.GetProperty("has_change", true);
        var suggestedChange = root.GetProperty("SuggestedChange", true) ?? root.GetProperty("suggested_change", true);
        var comment = root.GetProperty("Comment", true) ?? root.GetProperty("comment", true);
        var aiProbability = root.GetProperty("AiProbability", true) ?? root.GetProperty("ai_probability", true);

        return new ReviewComment()
        {
            HasChange = hasChange.GetBoolean(),
            SuggestedChange = suggestedChange.GetString(),
            Comment = comment.GetString(),
            AiProbability = aiProbability.GetDouble() ?? 0d
        };
    }

    public override void Write(Utf8JsonWriter writer, ReviewComment value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options); // default write
    }
}
