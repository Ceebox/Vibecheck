namespace Vibekiller.Inference;

/// <summary>
/// A lot of models will run away and keep generating tokens following completing a valid array.
/// This class is intended to detect a valid array and put a stop to that.
/// </summary>
internal static class JsonArrayExtractor
{
    /// <summary>
    /// Since our AI will never output proper valid JSON, try to extract it from text.
    /// </summary>
    public static string? ExtractFirstCompleteArray(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        var escape = false;
        var startIndex = -1;
        bool usingObjectWrap = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
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

                    depth++;
                }
                else if (c == '{' && startIndex == -1)
                {
                    startIndex = i;
                    depth = 1;
                    usingObjectWrap = true;
                }
                else if (c == ']' && depth > 0)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text[startIndex..(i + 1)];
                    }
                }
                else if (c == '}' && depth > 0)
                {
                    depth--;
                    if (depth == 0)
                    {
                        var slice = text[startIndex..(i + 1)];
                        return usingObjectWrap ? $"[{slice}]" : slice;
                    }
                }
                else if (c == '{')
                {
                    depth++;
                }
            }
        }

        // Fallback
        return null;
    }
}
