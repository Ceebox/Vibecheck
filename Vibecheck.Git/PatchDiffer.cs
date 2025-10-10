using System.Text.RegularExpressions;
using Vibecheck.Utility;

namespace Vibecheck.Git;

public sealed partial class PatchDiffer
{
    private static readonly Regex HunkHeaderRegex = HunkRegex();
    private readonly IPatchSource mPatchSource;
    private readonly bool mOnlyShowChanges = false;

    public PatchDiffer(IPatchSource patchSource, bool onlyShowChanges)
    {
        mPatchSource = patchSource;
        mOnlyShowChanges = onlyShowChanges;
    }

    public IEnumerable<HunkChange> GetDiffs()
    {
        using var activity = Tracing.Start();
        foreach (var file in mPatchSource.GetPatchInfo())
        {
            Tracing.WriteLine($"Parsing {file.Path}", LogLevel.INFO);
            if (string.IsNullOrWhiteSpace(file.Contents))
            {
                continue;
            }

            foreach (var hunk in ParsePatchToHunks(file.Path, file.Contents, mOnlyShowChanges))
            {
                yield return hunk;
            }
        }
    }

    internal static IEnumerable<HunkChange> ParsePatchToHunks(string filePath, string patchText, bool onlyShowChanges = false)
    {
        if (string.IsNullOrWhiteSpace(patchText))
        {
            yield break;
        }

        var currentHunk = default(HunkChange);
        var pendingDeletion = default(HunkLine);
        foreach (var rawLine in patchText.Split('\n'))
        {
            var line = rawLine.Replace("\r", "").TrimStart();

            var hunkMatch = HunkHeaderRegex.Match(line);
            if (hunkMatch.Success)
            {
                // If there was a previous hunk, yield it now
                if (currentHunk != null && currentHunk.Lines.Count > 0)
                {
                    yield return currentHunk;
                }

                currentHunk = new HunkChange
                {
                    Path = filePath,
                    OldStart = int.Parse(hunkMatch.Groups[1].Value),
                    OldCount = int.Parse(hunkMatch.Groups[2].Value),
                    NewStart = int.Parse(hunkMatch.Groups[3].Value),
                    NewCount = int.Parse(hunkMatch.Groups[4].Value)
                };

                pendingDeletion = null;
                continue;
            }

            // Skip file header lines (--- and +++)
            if (line.StartsWith("---") || line.StartsWith("+++"))
            {
                continue;
            }

            // Ignore lines before the first hunk header
            if (currentHunk == null)
            {
                continue;
            }

            if (onlyShowChanges)
            {
                if (line.StartsWith('+'))
                {
                    currentHunk.Lines.Add(new HunkLine
                    {
                        Type = ChangeType.ADDED,
                        Content = line[1..]
                    });

                    // Replacement detected
                    pendingDeletion = null;
                }
                else if (line.StartsWith('-'))
                {
                    // Tentatively store deletion, may be replaced by + next
                    pendingDeletion = new HunkLine
                    {
                        Type = ChangeType.DELETED,
                        Content = line[1..]
                    };
                }
                else
                {
                    // Context line: yield pending deletion if any
                    // Skip the context line itself
                    if (pendingDeletion != null)
                    {
                        currentHunk.Lines.Add(pendingDeletion);
                        pendingDeletion = null;
                    }
                }
            }
            else
            {
                if (line.StartsWith('+'))
                {
                    currentHunk.Lines.Add(new HunkLine { Type = ChangeType.ADDED, Content = line[1..] });
                }
                else if (line.StartsWith('-'))
                {
                    currentHunk.Lines.Add(new HunkLine { Type = ChangeType.DELETED, Content = line[1..] });
                }
                else
                {
                    // Context or unmodified lines (strip leading space or backslash)
                    var content = line.Length > 0 && (line[0] == ' ' || line[0] == '\\') ? line[1..] : line;
                    currentHunk.Lines.Add(new HunkLine { Type = ChangeType.UNMODIFIED, Content = content });
                }
            }
        }

        // Yield any remaining pending deletion at the end of the hunk
        if (onlyShowChanges && currentHunk != null && pendingDeletion != null)
        {
            currentHunk.Lines.Add(pendingDeletion);
        }

        // Yield the last hunk if present
        if (currentHunk != null)
        {
            yield return currentHunk;
        }
    }

    [GeneratedRegex(@"@@ -(\d+),(\d+) \+(\d+),(\d+) @@", RegexOptions.Compiled)]
    private static partial Regex HunkRegex();
}
