using System.CommandLine;
using System.Text;
using Vibekiller.Engine;
using Vibekiller.Git;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller;
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

        var cmd = new Command("review", "Review code changes in a repository.")
        {
            pathOption,
            sourceOption,
            targetOption,
            sourceOffsetOption,
            targetOffsetOption
        };

        cmd.SetAction(async parsedArgs =>
        {
            var repoPath = parsedArgs.GetValue(pathOption);
            var sourceBranch = parsedArgs.GetValue(sourceOption);
            var targetBranch = parsedArgs.GetValue(targetOption);
            var sourceOffset = parsedArgs.GetValue(sourceOffsetOption);
            var targetOffset = parsedArgs.GetValue(targetOffsetOption);

            await ExecuteAsync(repoPath, sourceBranch, targetBranch, sourceOffset, targetOffset);
        });

        return cmd;
    }

    private static async Task ExecuteAsync(
        string? repoPath,
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

        var patchGenerator = new BranchPatchSource(repoPath, sourceBranch, targetBranch, sourceOffset.Value, targetOffset.Value);
        using var engine = new ReviewEngine(null, patchGenerator);

        var hasResults = false;
        await foreach (var comment in engine.Review())
        {
            hasResults = true;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Path: {comment.Path}");
            Console.ResetColor();

            Console.WriteLine($"Comment: {comment.Comment}");

            if (!string.IsNullOrWhiteSpace(comment.SuggestedChange))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Suggested Change: {comment.SuggestedChange}");
                Console.ResetColor();
            }

            if (comment.AiProbability.HasValue)
            {
                PrintProbabilityBar(comment.AiProbability.Value);
            }

            Console.WriteLine();
        }

        if (!hasResults)
        {
            Console.WriteLine("No review comments found.");
        }

        activity.SetTag("review.hasResults", hasResults);
    }

    private static void PrintProbabilityBar(double probability)
    {
        using var activity = Tracing.Start();

        probability = Math.Clamp(probability, 0, 1);

        var barWidth = 20;
        var filled = (int)Math.Round(probability * barWidth);

        var (r, g) = GetGradientColor(probability);

        Console.Write("AI Probability: ");
        Console.Write($"[{GradientBar(filled, barWidth, r, g)}] ");
        Console.WriteLine($"{probability:P0}");
    }

    private static string GradientBar(int filled, int total, int r, int g)
    {
        using var activity = Tracing.Start();

        var bar = new StringBuilder();
        bar.Append('|');

        for (var i = 0; i < total; i++)
        {
            if (i < filled)
            {
                bar.Append($"\u001b[38;2;{r};{g};0m=\u001b[0m");
            }
            else
            {
                bar.Append(' ');
            }
        }

        bar.Append('|');
        return bar.ToString();
    }

    private static (int r, int g) GetGradientColor(double p)
    {
        using var activity = Tracing.Start();
        activity.SetTag("gradient.progress", p);

        if (p < 0.5)
        {
            // Green -> Yellow
            var r = (int)(p * 2 * 255);
            return (r, 255);
        }
        else
        {
            // Yellow -> Red
            var g = (int)((1 - (p - 0.5) * 2) * 255);
            return (255, g);
        }
    }
}
