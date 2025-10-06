using System.Text.Json;
using Vibekiller.Utility;

namespace Vibekiller.Engine;

internal class ReviewResponseParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public ReviewResponseParser()
    {

    }

    internal IEnumerable<ReviewComment> ParseResponse(string response)
    {
        using var activity = Tracing.Start();

        // I hate working with AI, we could literally have anything here
        // So, I guess I'll just try and predict the future
        // Decode by vibes, there are no rules!
        if (string.IsNullOrWhiteSpace(response))
        {
            yield break;
        }

        List<ReviewComment>? result;

        try
        {
            // Ensure it starts with '[' to match the enforced array format
            var json = response.Trim();
            var user = "User:";
            if (json.EndsWith(user))
            {
                json = json[..^user.Length].TrimEnd();
            }

            if (string.IsNullOrEmpty(json))
            {
                yield break;
            }    

            if (!json.StartsWith('['))
            {
                // If the AI somehow returns a single object, wrap it to be valid JSON
                json = $"[{json}]";
            }

            result = JsonSerializer.Deserialize<List<ReviewComment>>(json, Options);
        }
        catch (JsonException)
        {
            activity.AddError("Error parsing:\n" + response);
            yield break;
        }

        if (result is null)
        {
            yield break;
        }

        foreach (var comment in result)
        {
            if (comment is { HasComment: true } && !string.IsNullOrWhiteSpace(comment.Comment))
            {
                yield return comment;
            }
        }
    }
}

