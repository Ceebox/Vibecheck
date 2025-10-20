using LibGit2Sharp;

namespace Vibecheck.Git;

public sealed class RepositoryFinder
{
    private readonly string mStartPath;
    private readonly Lazy<string> mRepoRoot;

    private RepositoryFinder(string startPath)
    {
        mStartPath = startPath;
        mRepoRoot = new Lazy<string>(() => DiscoverRepoRoot(mStartPath) ?? string.Empty);
    }

    /// <summary>
    /// Returns the root folder of the git repository containing startPath, or null if not found.
    /// </summary>
    public string Root => mRepoRoot.Value;

    /// <summary>
    /// Factory method to create a RepositoryFinder starting from a folder.
    /// </summary>
    public static RepositoryFinder Discover(string path)
        => new(path);

    /// <summary>
    /// Recursively searches for a .git folder upwards from the starting path.
    /// </summary>
    private static string? DiscoverRepoRoot(string path)
        => Repository.Discover(path);

    public override string ToString() => mRepoRoot.Value;
}
