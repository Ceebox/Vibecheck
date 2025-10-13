using System.Text.Json;
using System.Text.RegularExpressions;
using Vibecheck.Inference;
using Vibecheck.Utility;

namespace Vibecheck.Engine;

internal partial class ReviewResponseParser
{
    private static readonly JsonSerializerOptions sOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new ReviewCommentConverter() },
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

            foreach (var slice in JsonArrayExtractor.ExtractObjectsAsArray(candidate))
            {
                ReviewComment? parsedComment;
                try
                {
                    parsedComment = JsonSerializer.Deserialize<ReviewComment>(slice, sOptions);
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

                if (parsedComment is { HasChange: true } && !string.IsNullOrWhiteSpace(parsedComment.SuggestedChange))
                {
                    yield return parsedComment with { Path = result.Path };
                }
            }
        }
    }

    // Thank the lord for regex builder
    [GeneratedRegex(@"\s*(\{(?:[^{}""\\]|\\.|""(?:[^""\\]|\\.)*"")*\}|\[(?:[^\[\]""\\]|\\.|""(?:[^""\\]|\\.)*"")*\])", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex JsonExtractorRegex();
}

