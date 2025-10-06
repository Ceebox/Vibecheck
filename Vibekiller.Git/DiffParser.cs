using System.Text;
using Vibekiller.Utility;

namespace Vibekiller.Git;

public sealed class DiffParser
{
    private readonly IEnumerable<HunkChange> mDiffs;

    public DiffParser(IEnumerable<HunkChange> diffs)
    {
        mDiffs = diffs;
    }

    public IEnumerable<string> FormatDiffs()
    {
        using var activity = Tracing.Start();

        if (!mDiffs.Any())
        {
            yield break;
        }

        foreach (var hunk in mDiffs)
        {
            yield return FormatHunk(hunk);
        }
    }

    private static string FormatHunk(HunkChange hunk)
    {
        using var activity = Tracing.Start();

        var sb = new StringBuilder();
        sb.AppendLine($"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@");
        foreach (var line in hunk.Lines)
        {
            var prefix = line.Type switch
            {
                ChangeType.ADDED => "+",
                ChangeType.DELETED => "-",
                _ => " "
            };

            sb.AppendLine($"{prefix}{line.Content}");
        }

        return sb.ToString();
    }
}
