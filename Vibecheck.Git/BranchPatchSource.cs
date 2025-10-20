
using LibGit2Sharp;
using Vibecheck.Utility;

namespace Vibecheck.Git;

public sealed class BranchPatchSource : IPatchSource
{
    private readonly string mRepoPath;
    private readonly string mSourceBranch;
    private readonly string mTargetBranch;
    private readonly int mSourceOffset;
    private readonly int mTargetOffset;

    /// <summary>
    /// Creates a BranchDiffer for comparing the current branch to the target branch.
    /// </summary>
    /// <param name="repoFinder">The Git repository.</param>
    /// <param name="sourceBranch">The branch to use as the "current" branch (typically HEAD).</param>
    /// <param name="targetBranch">The branch or tag to diff against (e.g., main).</param>
    /// <param name="sourceOffset">Number of commits back from the source branch HEAD.</param>
    /// <param name="targetOffset">Number of commits back from the target branch HEAD.</param>
    public BranchPatchSource(RepositoryFinder repoFinder, string sourceBranch, string targetBranch, int sourceOffset = 0, int targetOffset = 0)
    {
        mRepoPath = repoFinder.GitRoot;
        mSourceBranch = sourceBranch;
        mTargetBranch = targetBranch;
        mSourceOffset = sourceOffset;
        mTargetOffset = targetOffset;
    }

    public string? PatchRootDirectory => Path.GetDirectoryName(mRepoPath.TrimEnd(Path.DirectorySeparatorChar))!;

    public IEnumerable<PatchInfo> GetPatchInfo()
    {
        using var activity = Tracing.Start();

        activity.AddTag("git.repo_path", mRepoPath);
        activity.AddTag("git.source_branch", mSourceBranch);
        activity.AddTag("git.target_branch", mTargetBranch);
        activity.AddTag("git.source_offset", mSourceOffset);
        activity.AddTag("git.target_offset", mTargetOffset);

        using var repo = new Repository(mRepoPath);
        var current = repo.Head;
        var target = repo.Branches[mTargetBranch];
        if (target == null)
        {
            activity.Log($@"Branch '{mTargetBranch}' not found.", Utility.LogLevel.ERROR);
            yield break;
        }

        var sourceCommit = ResolveCommit(repo, mSourceBranch, mSourceOffset);
        var targetCommit = ResolveCommit(repo, mTargetBranch, mTargetOffset);

        if (sourceCommit == null || targetCommit == null)
        {
            activity.Log($"Failed to resolve commits for diff.", Utility.LogLevel.ERROR);
            yield break;
        }

        Tracing.WriteLine($"Comparing commit {DescribeCommit(targetCommit)} with {DescribeCommit(sourceCommit)}", Utility.LogLevel.INFO);

        var patch = repo.Diff.Compare<Patch>(targetCommit!.Tree, sourceCommit!.Tree);
        foreach (var file in patch)
        {
            yield return new PatchInfo()
            {
                Path = file.Path,
                Contents = file.Patch
            };
        }
    }

    /// <summary>
    /// Resolve a branch, tag, or SHA into a specific commit, optionally offsetting by N commits back.
    /// </summary>
    private static Commit ResolveCommit(Repository repo, string reference, int offset)
    {
        Commit? commit;

        // Try as branch
        var branch = repo.Branches[reference];
        if (branch != null)
        {
            commit = branch.Commits.Skip(offset).FirstOrDefault();
            if (commit != null)
            {
                return commit;
            }
        }

        // Try as tag
        var tag = repo.Tags[reference];
        if (tag?.Target is Commit tagCommit)
        {
            return tagCommit;
        }

        // Try as SHA
        if (repo.Lookup(reference) is Commit shaCommit)
        {
            return shaCommit;
        }

        throw new InvalidOperationException();
    }

    private static string DescribeCommit(Commit commit)
        => $"{commit.Sha[..7]} ({commit.MessageShort})";
}
