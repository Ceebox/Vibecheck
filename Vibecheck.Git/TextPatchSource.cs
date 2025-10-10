using System.Text.RegularExpressions;

namespace Vibecheck.Git;

public sealed partial class TextPatchSource : IPatchSource
{
    private readonly string mPatchText;

    public TextPatchSource(string patchText)
    {
        mPatchText = patchText ?? throw new ArgumentNullException(nameof(patchText));
    }

    public IEnumerable<PatchInfo> GetPatchInfo()
    {
        if (string.IsNullOrWhiteSpace(mPatchText))
        {
            yield break;
        }

        var matches = FileDiffRegex().Matches(mPatchText);

        // If we have no file headers, it is probably a single file patch
        // This is a shame but it'll have to do (assuming there no file name)
        if (matches.Count == 0)
        {
            yield return new PatchInfo
            {
                Path = string.Empty,
                Contents = mPatchText
            };

            yield break;
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var startIndex = matches[i].Index;
            var endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : mPatchText.Length;

            var fileSection = mPatchText[startIndex..endIndex];
            var filePath = matches[i].Groups["new"].Value;

            yield return new PatchInfo
            {
                Path = filePath,
                Contents = fileSection.Trim()
            };
        }
    }

    /// <summary>
    /// Match each file diff header from git diff / patch.
    /// e.g. "diff --git a/Some/File.cs b/Some/File.cs"
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"--- a/(?<old>.+?)\r?\n\+\+\+ b/(?<new>.+?)\r?\n(?<content>(?:@@[\s\S]*?)(?=(?:--- a/|\z)))", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex FileDiffRegex();
}
