using System.Collections.Concurrent;
using System.CommandLine;
using System.Text;
using Vibecheck.Engine;
using Vibecheck.Git;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck;
internal sealed class ReviewCommand : CommandBase
{
    public override Command ToCommand()
    {
        using var activity = Tracing.Start();

        var pathOption = new Option<string>("--path")
        {
            Description = "The path of the target git repository.",
            DefaultValueFactory = _ => string.Empty
        };

        var diffOption = new Option<string>("--diff")
        {
            Description = "Directly provide a git diff string to review.",
            DefaultValueFactory= _ => string.Empty
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

        var sourceOffsetOption = new Option<int>("--source-offset")
        {
            Description = "Number of commits back from the source branch HEAD.",
            DefaultValueFactory = _ => 0
        };

        var targetOffsetOption = new Option<int>("--target-offset")
        {
            Description = "Number of commits back from the target branch HEAD.",
            DefaultValueFactory = _ => 0
        };

        targetOffsetOption.Aliases.Add("--back");

        var cmd = new Command("review", "Review code changes in a repository.")
        {
            pathOption,
            diffOption,
            sourceOption,
            targetOption,
            sourceOffsetOption,
            targetOffsetOption
        };

        cmd.Aliases.Add("r");

        cmd.SetAction(async parsedArgs =>
        {
            var repoPath = parsedArgs.GetValue(pathOption);
            var diffText = parsedArgs.GetValue(diffOption);
            var sourceBranch = parsedArgs.GetValue(sourceOption);
            var targetBranch = parsedArgs.GetValue(targetOption);
            var sourceOffset = parsedArgs.GetValue(sourceOffsetOption);
            var targetOffset = parsedArgs.GetValue(targetOffsetOption);

            await ExecuteAsync(repoPath, diffText, sourceBranch, targetBranch, sourceOffset, targetOffset);
        });

        return cmd;
    }

    private static async Task ExecuteAsync(
        string? repoPath,
        string? diffText,
        string? sourceBranch,
        string? targetBranch,
        int? sourceOffset,
        int? targetOffset
    )
    {
        using var activity = Tracing.Start();

        repoPath = string.IsNullOrEmpty(repoPath)
            ? string.Empty
            : repoPath;
        sourceBranch = string.IsNullOrEmpty(sourceBranch)
            ? Configuration.Current.GitSettings.GitSourceBranch
            : sourceBranch;
        targetBranch = string.IsNullOrEmpty(targetBranch)
            ? Configuration.Current.GitSettings.GitTargetBranch
            : targetBranch;
        sourceOffset = sourceOffset.HasValue
            ? sourceOffset!.Value
            : Configuration.Current.GitSettings.GitSourceCommitOffset;
        targetOffset = targetOffset.HasValue
            ? targetOffset!.Value
            : Configuration.Current.GitSettings.GitTargetCommitOffset;

        IPatchSource patchGenerator = string.IsNullOrEmpty(diffText)
            ? new BranchPatchSource(repoPath, sourceBranch, targetBranch, sourceOffset.Value, targetOffset.Value)
            : new TextPatchSource(diffText);

        using var engine = new ReviewEngine(null, patchGenerator);

        // TODO: Add some sort of loading indicator here
        var renderer = new ReviewCommentConsoleRenderer(false);
        await foreach (var comment in engine.Review())
        {
            Tracing.WriteLine($"New comment detected: {comment.Comment}", LogLevel.SUCCESS);
            renderer.QueueMessage(comment);
        }

        var hasResults = renderer.HasComments;
        if (hasResults)
        {
            Console.WriteLine("No review comments found.");
        }
        else
        {
            renderer.Flush();
        }

        activity.SetTag("review.hasResults", hasResults);
    }
}
