namespace Vibecheck.Engine;

public class WatcherEventArgs : EventArgs
{
    public string RepositoryPath { get; }
    public string Branch { get; }
    public string ChangedFile { get; }
    public DateTimeOffset Timestamp { get; }

    public WatcherEventArgs(string repoPath, string branchName, string changedFile, DateTimeOffset timestamp)
    {
        RepositoryPath = repoPath;
        Branch = branchName;
        ChangedFile = changedFile;
        Timestamp = timestamp;
    }
}
