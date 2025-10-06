using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using Vibekiller.Inference;
using Vibekiller.Utility;

namespace Vibekiller.Engine;

internal partial class ReviewResponseParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly Regex JsonExtractor = JsonExtractorRegex();

    public ReviewResponseParser()
    {

    }

    internal IEnumerable<ReviewComment> ParseResponse(InferenceResult result)
    {
        using var activity = Tracing.Start();

        // I hate working with AI, we could literally have anything here
        // So, I guess I'll just try and predict the future
        // Decode by vibes, there are no rules!
        var response = result.Contents;
        if (string.IsNullOrWhiteSpace(response))
        {
            yield break;
        }

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

        var matches = JsonExtractor.Matches(json);
        if (matches.Count == 0)
        {
            activity.AddError("No JSON found in response:\n" + response);
            yield break;
        }

        foreach (var match in matches)
        {
            // If the AI somehow returns a single object, wrap it to be valid JSON
            var candidate = ((Match)match).Value.Trim();
            if (!candidate.StartsWith('['))
            {
                candidate = $"[{candidate}]";
            }

            candidate = ExtractJson(candidate);

            List<ReviewComment>? parsedComment;
            try
            {
                parsedComment = JsonSerializer.Deserialize<List<ReviewComment>>(candidate, Options);
            }
            catch (JsonException)
            {
                activity.AddError("Error parsing:\n" + response);
                continue;
            }

            if (parsedComment is null)
            {
                continue;
            }

            foreach (var comment in parsedComment)
            {
                if (comment is { HasChange: true } && !string.IsNullOrWhiteSpace(comment.SuggestedChange))
                {
                    yield return comment with { Path = result.Path };
                }
            }
        }
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];
        return "[]";
    }

    [GeneratedRegex(@"(\[[\s\S]*?\]|\{[\s\S]*?\})", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex JsonExtractorRegex();
}

