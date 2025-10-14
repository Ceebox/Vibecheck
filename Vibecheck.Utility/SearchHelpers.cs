namespace Vibecheck.Utility;

public static class SearchHelpers
{
    private const float SCORE_MATCH_THRESHOLD = 0.4f;

    public static string? FuzzySearch(string startingDirectory, string searchPath)
    {
        if (!Directory.Exists(startingDirectory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {startingDirectory}");
        }

        var allFiles = Directory.EnumerateFiles(startingDirectory, "*", SearchOption.AllDirectories);
        if (!allFiles.Any())
        {
            return null;
        }

        var searchTerm = Path.GetFileName(searchPath).ToLowerInvariant();

        // Rank all files by fuzzy distance
        var ranked = allFiles
            .Select(f => new
            {
                Path = f,
                Score = ComputeFuzzyScore(Path.GetFileName(f).ToLowerInvariant(), searchTerm)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var best = ranked.FirstOrDefault(r => r.Score > SCORE_MATCH_THRESHOLD);
        return best?.Path;
    }

    private static float ComputeFuzzyScore(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return 0;
        }

        // Exact or contains match gets top score
        if (a.Contains(b))
        {
            return 1.0f;
        }

        if (b.Contains(a))
        {
            return 1.0f;
        }

        var distance = CalculateLevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return 1.0f - ((float)distance / maxLen);
    }

    /// <summary>
    /// Compute Levenshtein edit distance.
    /// </summary>
    /// <remarks>
    /// Gee, I've never implemented this before:
    /// https://github.com/Ceebox/Launchbox/blob/644a0ad21234d946eff485a413431f505607475d/app/src/main/java/com/chadderbox/launchbox/search/SearchHelpers.java#L11
    /// But this time it is C# so I can do it better, whoo!
    /// </remarks>
    private static int CalculateLevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
        {
            return b.Length;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a.Length;
        }

        var lenA = a.Length;
        var lenB = b.Length;

        if (lenA > lenB)
        {
            (a, b) = (b, a);
            (lenA, lenB) = (lenB, lenA);
        }

        var previous = new int[lenA + 1];
        var current = new int[lenA + 1];

        for (var i = 0; i <= lenA; i++)
        {
            previous[i] = i;
        }

        for (var j = 1; j <= lenB; j++)
        {
            var bj = b[j - 1];
            current[0] = j;

            for (var i = 1; i <= lenA; i++)
            {
                var cost = a[i - 1] == bj ? 0 : 1;
                current[i] = Math.Min(Math.Min(current[i - 1] + 1, previous[i] + 1), previous[i - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[lenA];
    }
}
