using System.Text.Json;
using System.Text.RegularExpressions;
using Vibecheck.Inference;
using Vibecheck.Utility;

namespace Vibecheck.Engine;

internal partial class ReviewResponseParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = new SnakeCaseNamingPolicy()
    };

    public ReviewResponseParser() { }

    internal static IEnumerable<ReviewComment> ParseResponse(InferenceResult result)
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

        var matches = JsonExtractorRegex().Matches(json);
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

            var slices = JsonArrayExtractor.ExtractAllCompleteSlices(candidate);
            foreach (var slice in slices)
            {
                List<ReviewComment>? parsedComment;
                try
                {
                    parsedComment = JsonSerializer.Deserialize<List<ReviewComment>>(slice, Options);
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
    }

    // Thank the lord for regex builder
    [GeneratedRegex(@"\s*(\{(?:[^{}""\\]|\\.|""(?:[^""\\]|\\.)*"")*\}|\[(?:[^\[\]""\\]|\\.|""(?:[^""\\]|\\.)*"")*\])", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex JsonExtractorRegex();
}

