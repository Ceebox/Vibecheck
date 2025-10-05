using LibGit2Sharp;
using System.Text.RegularExpressions;
using Vibekiller.Utility;

namespace Vibekiller.Git;

public sealed partial class BranchDiffer
{
    private static readonly Regex HunkHeaderRegex = HunkRegex();

    public static IEnumerable<LineChange> GetBranchDiffs(string repoPath, string targetBranch)
    {
        using var activity = Tracing.Start();

        repoPath = Repository.Discover(repoPath);
        using var repo = new Repository(repoPath);

        var current = repo.Head;
        var target = repo.Branches[targetBranch] ?? throw new ArgumentException($"Branch '{targetBranch}' not found.");

        // Find common ancestor (merge base) to diff from
        var mergeBase = repo.ObjectDatabase.FindMergeBase(current.Tip, target.Tip);
        var patch = repo.Diff.Compare<Patch>(mergeBase.Tree, current.Tip.Tree);

        foreach (var file in patch)
        {
            if (string.IsNullOrWhiteSpace(file.Patch))
            {
                continue;
            }

            int? oldLine = null;
            int? newLine = null;

            foreach (var line in file.Patch.Split('\n'))
            {
                var hunkMatch = HunkHeaderRegex.Match(line);
                if (hunkMatch.Success)
                {
                    oldLine = int.Parse(hunkMatch.Groups[1].Value) - 1;
                    newLine = int.Parse(hunkMatch.Groups[2].Value) - 1;
                    continue;
                }

                if (line.StartsWith('+') && !line.StartsWith("+++"))
                {
                    newLine++;
                    yield return new LineChange(file.Path, null, newLine, "Added", line[1..]);
                }
                else if (line.StartsWith('-') && !line.StartsWith("---"))
                {
                    oldLine++;
                    yield return new LineChange(file.Path, oldLine, null, "Deleted", line[1..]);
                }
                else
                {
                    oldLine++;
                    newLine++;
                    yield return new LineChange(file.Path, oldLine, newLine, "Unmodified", line[1..]);
                }
            }
        }
    }

    [GeneratedRegex(@"@@ -(\d+),\d+ \+(\d+),\d+ @@", RegexOptions.Compiled)]
    private static partial Regex HunkRegex();
}
