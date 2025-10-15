using System.CommandLine;
using System.Runtime.InteropServices;
using Vibecheck.Engine;
using Vibecheck.Git;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck;
internal sealed class WatchCommand : CommandBase
{
    public override Command ToCommand()
    {
        using var activity = Tracing.Start();

        var pathArgument = new Argument<string>("path")
        {
            Description = "The path of the repository to watch.",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory(),
        };

        var sourceOption = new Option<string>("--source")
        {
            Description = "The source branch to compare (defaults to current branch).",
            DefaultValueFactory = _ => string.Empty
        };

        var targetOption = new Option<string>("--target")
        {
            Description = "The branch or tag into which changes are intended to be merged.",
            DefaultValueFactory = _ => string.Empty
        };

        var cmd = new Command("watch", "Run the Vibecheck server.")
        {
            pathArgument,
            targetOption,
            sourceOption,
        };

        cmd.SetAction(async parsedArgs =>
        {
            var repositoryPath = parsedArgs.GetValue(pathArgument);
            if (string.IsNullOrEmpty(repositoryPath))
            {
                Tracing.WriteLine("The repository path cannot be empty", LogLevel.ERROR);
                return;
            }

            var sourceBranch = parsedArgs.GetValue(sourceOption);
            var targetBranch = parsedArgs.GetValue(targetOption);

            await RunAsync(repositoryPath, sourceBranch, targetBranch);
        });

        return cmd;
    }

    private static async Task RunAsync(
        string? repositoryPath,
        string? sourceBranch,
        string? targetBranch
    )
    {
        using var activity = Tracing.Start();

        sourceBranch = string.IsNullOrEmpty(sourceBranch)
            ? Configuration.Current.GitSettings.GitSourceBranch
            : sourceBranch;
        targetBranch = string.IsNullOrEmpty(targetBranch)
            ? Configuration.Current.GitSettings.GitTargetBranch
            : targetBranch;

        INotificationProvider? notificationProvider = null;
        if (Configuration.Current.WatcherSettings.NotificationSettings.NotificationsEnabled)
        {
            notificationProvider = NotificationProviderFactory.GetNotificationProvider();
        }

        using var watcher = new FolderCommitWatcher(repositoryPath!);

        watcher.CommitDetected += (_, e) => _ = HandleCommitAsync(e, notificationProvider, sourceBranch, targetBranch);

        await watcher.RunAsync();
    }

    private static async Task HandleCommitAsync(
        WatcherEventArgs e,
        INotificationProvider? notificationProvider,
        string sourceBranch,
        string targetBranch
    )
    {
        // TODO: This is deeply flawed, I have no idea what happens if you commit multiple times
        var patchGenerator = new BranchPatchSource(
            e.RepositoryPath,
            string.IsNullOrEmpty(e.Branch)
                ? sourceBranch
                : e.Branch,
            string.IsNullOrEmpty(targetBranch)
                ? Configuration.Current.GitSettings.GitTargetBranch
                : targetBranch,
            0,
            1
        );

        using var engine = new ReviewEngine(null, patchGenerator);

        var renderer = new ReviewCommentConsoleRenderer(true);
        await foreach (var comment in engine.Review())
        {
            Tracing.WriteLine($"New comment detected: {comment.Comment}", LogLevel.SUCCESS);
            renderer.QueueMessage(comment);
        }

        notificationProvider?.SendNotification("You have new feedback!");
    }
}
