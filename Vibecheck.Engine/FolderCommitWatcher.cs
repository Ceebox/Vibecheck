using LibGit2Sharp;
using Vibecheck.Utility;

namespace Vibecheck.Engine;

public sealed class FolderCommitWatcher : WatcherBase
{
    private readonly string mRootPath;
    private readonly List<FileSystemWatcher> mWatchers = [];
    private readonly CancellationTokenSource mCancellationSource = new();

    public FolderCommitWatcher(string rootPath)
    {
        mRootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
    }

    protected override async Task StartWatcherAsync()
    {
        using var activity = Tracing.Start();
        Tracing.WriteLine($"Searching for repositories in: {mRootPath}", Utility.LogLevel.INFO);

        foreach (var dir in Directory.GetDirectories(mRootPath, ".git", SearchOption.AllDirectories))
        {
            var repoPath = Path.GetDirectoryName(dir)!;
            Tracing.WriteLine($"Found repo: {repoPath}", Utility.LogLevel.INFO);
            this.StartWatcher(repoPath);
        }

        // Keep us alive!
        await Task.Delay(-1, mCancellationSource.Token).ContinueWith(_ => { });
    }

    private void StartWatcher(string repoPath)
    {
        using var activity = Tracing.Start();

        var gitDir = Path.Combine(repoPath, ".git");
        var logsDir = Path.Combine(gitDir, "logs");

        if (!Directory.Exists(logsDir))
        {
            return;
        }

        // Basically, see if we have any activity in any of the git files that we care about
        var watcher = new FileSystemWatcher
        {
            Path = logsDir,
            Filter = "*",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, e) =>
        {
            if (e.FullPath.Contains("HEAD", StringComparison.OrdinalIgnoreCase) ||
                e.FullPath.Contains(Path.Combine("refs", "heads"), StringComparison.OrdinalIgnoreCase))
            {
                var branch = TryGetBranchName(repoPath, e.FullPath);
                this.OnCommitDetected(new WatcherEventArgs(repoPath, branch, e.FullPath, DateTime.Now));
            }
        };

        mWatchers.Add(watcher);
    }

    private static string TryGetBranchName(string repoPath, string changedFile)
    {
        try
        {
            using var repo = new Repository(repoPath);

            // HEAD was changed (commit on current branch)
            if (changedFile.EndsWith("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                return repo.Head.FriendlyName; // e.g. "main"
            }

            // Logs for a specific branch were changed (e.g. refs/heads/dev)
            var gitDir = Path.Combine(repoPath, ".git");
            var headsDir = Path.Combine(gitDir, "logs", "refs", "heads");

            if (changedFile.StartsWith(headsDir, StringComparison.OrdinalIgnoreCase))
            {
                var branchPath = changedFile[(headsDir.Length + 1)..];
                return branchPath.Replace('\\', '/');
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Tracing.WriteLine($"Failed to get branch name for {repoPath}: {ex}", Utility.LogLevel.WARNING);
            return string.Empty;
        }
    }

    public override void Dispose()
    {
        using var activity = Tracing.Start();

        mCancellationSource.Cancel();
        foreach (var watcher in mWatchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        mWatchers.Clear();
        mCancellationSource.Dispose();

        base.Dispose();
    }
}
