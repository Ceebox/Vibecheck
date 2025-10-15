using System.Text;
using Vibecheck.Engine;
using Vibecheck.Utility;

namespace Vibecheck;

internal sealed class ReviewCommentConsoleRenderer
{
    private readonly bool mImmediate;
    private readonly Queue<ReviewComment> mQueue = [];

    public ReviewCommentConsoleRenderer(bool immediate)
    {
        mImmediate = immediate;
    }

    public bool HasComments => mQueue.Count > 0;

    public void QueueMessage(ReviewComment comment)
    {
        if (mImmediate)
        {
            Render(comment);
        }
        else
        {
            mQueue.Enqueue(comment);
        }
    }

    public void Flush()
    {
        while (mQueue.Count > 0)
        {
            Render(mQueue.Dequeue());
        }
    }

    private static void Render(ReviewComment comment)
    {
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

    #region Console Helpers

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

    #endregion
}
