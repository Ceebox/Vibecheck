using Vibecheck.Utility;

namespace Vibecheck.Engine;

public abstract class WatcherBase : IDisposable
{
    public event EventHandler<WatcherEventArgs>? CommitDetected;

    public async Task RunAsync()
    {
        using var activity = Tracing.Start();
        await this.StartWatcherAsync();
    }

    protected abstract Task StartWatcherAsync();

    protected virtual void OnCommitDetected(WatcherEventArgs e)
    {
        using var activity = Tracing.Start();
        Tracing.WriteLine($"Commit detected - {e.RepositoryPath} - {e.Timestamp}", LogLevel.INFO);

        this.CommitDetected?.Invoke(this, e);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
