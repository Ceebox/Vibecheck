using System.Text.RegularExpressions;

namespace Vibecheck.Utility;

/// <summary>
/// A lot of models will run away and keep generating tokens following completing a valid array.
/// This class is intended to detect a valid array and put a stop to that.
/// </summary>
/// <devnote>I really need to refactor this logic.</devnote>
public static partial class JsonArrayExtractor
{
    /// <summary>
    /// Extracts the first complete array or object from the text.
    /// If the first complete object is a single object, wraps it in an array.
    /// </summary>
    public static string? ExtractFirstCompleteArray(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var arrayDepth = 0;
        var objectDepth = 0;
        var inString = false;
        var escaped = false;
        var startIndex = -1;
        bool wrapObject = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"' && !escaped)
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '[')
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                    }

                    arrayDepth++;
                }
                else if (c == '{')
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                        wrapObject = true;
                    }

                    objectDepth++;
                }
                else if ((c == ']' && arrayDepth > 0) || (c == '}' && objectDepth > 0))
                {
                    if (c == ']')
                    {
                        arrayDepth--;
                    }

                    if (c == '}')
                    {
                        objectDepth--;
                    }

                    if (arrayDepth == 0 && objectDepth == 0 && startIndex >= 0)
                    {
                        var slice = TrimTrailingInput(text[startIndex..(i + 1)]);
                        return wrapObject ? $"[{slice}]" : slice;
                    }
                }
            }
        }

        // Fallback
        return ExtractLargestCompleteSlice(text);
    }

    /// <summary>
    /// Extracts all complete arrays or objects from the text in order.
    /// Wraps single objects in an array if necessary.
    /// </summary>
    public static IEnumerable<string> ExtractAllCompleteSlices(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var arrayDepth = 0;
        var objectDepth = 0;
        var inString = false;
        var escape = false;
        var startIndex = -1;
        bool wrapObject = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"' && !escape)
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '[')
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                    }

                    arrayDepth++;
                }
                else if (c == '{')
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                        wrapObject = true;
                    }

                    objectDepth++;
                }
                else if ((c == ']' && arrayDepth > 0) || (c == '}' && objectDepth > 0))
                {
                    if (c == ']')
                    {
                        arrayDepth--;
                    }

                    if (c == '}')
                    {
                        objectDepth--;
                    }

                    if (arrayDepth == 0 && objectDepth == 0 && startIndex >= 0)
                    {
                        var slice = TrimTrailingInput(text[startIndex..(i + 1)]);
                        yield return wrapObject ? $"[{slice}]" : slice;

                        // Reset for next slice
                        startIndex = -1;
                        wrapObject = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the largest valid array or object in the text.
    /// </summary>
    public static string? ExtractLargestCompleteSlice(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var largestSlice = string.Empty;
        var arrayDepth = 0;
        var objectDepth = 0;
        var inString = false;
        var escape = false;
        var startIndex = -1;
        bool wrapObject = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"' && !escape)
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '[')
                {
                    if (startIndex == -1) startIndex = i;
                    arrayDepth++;
                }
                else if (c == '{')
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                        wrapObject = true;
                    }
                    objectDepth++;
                }
                else if ((c == ']' && arrayDepth > 0) || (c == '}' && objectDepth > 0))
                {
                    if (c == ']') arrayDepth--;
                    if (c == '}') objectDepth--;

                    if (arrayDepth == 0 && objectDepth == 0 && startIndex >= 0)
                    {
                        var slice = TrimTrailingInput(text[startIndex..(i + 1)]);
                        if (slice.Length > largestSlice.Length)
                        {
                            largestSlice = wrapObject ? $"[{slice}]" : slice;
                        }
                        startIndex = -1;
                        wrapObject = false;
                    }
                }
            }
        }

        return string.IsNullOrEmpty(largestSlice) ? null : largestSlice;
    }

    /// <summary>
    /// Removes any trailing characters after the last closing bracket (non-whitespace).
    /// </summary>
    private static string TrimTrailingInput(string input)
    {
        // Match up to the last closing bracket, ignore trailing junk
        var match = TrailingInputRegex().Match(input);
        return match.Success ? match.Value : input;
    }

    [GeneratedRegex(@"^.*[\]\}]")]
    private static partial Regex TrailingInputRegex();
}
