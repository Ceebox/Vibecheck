using LLama;
using LLama.Common;
using System.Text;
using System.Text.RegularExpressions;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Inference;

public sealed partial class DiffEngine : InferenceEngineBase<IAsyncEnumerable<InferenceResult>>
{
    private readonly IEnumerable<string> mDiffs;

    public DiffEngine(string modelUrl, string systemPrompt, IEnumerable<string> diffs) : base(modelUrl, systemPrompt)
    {
        mDiffs = diffs;
    }

    public override async IAsyncEnumerable<InferenceResult> Execute()
    {
        var executor = new InteractiveExecutor(await this.GetContext());

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.System, this.SystemPrompt);
        await foreach (var chunk in ProcessDiffs(executor, chatHistory))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<InferenceResult> ProcessDiffs(InteractiveExecutor executor, ChatHistory chatHistory)
    {
        using var activity = Tracing.Start();
        activity.AddTag("diffs.count", mDiffs.Count());

        var session = new ChatSession(executor, chatHistory);
        var inferenceParams = new InferenceParams()
        {
            MaxTokens = Configuration.Current.InferenceSettings.MaxTokens,
            AntiPrompts = Configuration.Current.InferenceSettings.AntiPrompts,
            SamplingPipeline = new SettingBasedSamplingPipeline(Configuration.Current.InferenceSettings.SamplingSettings)
        };

        if (!mDiffs.Any())
        {
            Tracing.WriteLine("No diffs found.", LogLevel.INFO);
            yield break;
        }

        foreach (var diffText in mDiffs)
        {
            var sb = new StringBuilder();

            var prompt = this.GeneratePrompt(diffText);
            var message = new ChatHistory.Message(AuthorRole.User, prompt);
            await foreach (var chunk in session.ChatAsync(message, inferenceParams))
            {
                sb.Append(chunk);
                Tracing.Write(chunk, LogLevel.INFO);
            }

            var parsedHeader = ParseHunkHeader(diffText);
            var result = new InferenceResult()
            {
                Path = parsedHeader.Path,
                CodeStartLine = parsedHeader.NewStart,
                Contents = sb.ToString()
            };

            yield return result;
        }
    }

    private string GeneratePrompt(string diff)
    {
        var codeStylePrompt = Configuration.Current.InferenceSettings.CodeStylePrompt;
        var completionPrompt = Configuration.Current.InferenceSettings.CompletionPrompt;

        // Give it the system prompt every time to keep it in the context window
        return this.SystemPrompt
            + '\n'
            + codeStylePrompt
            + (string.IsNullOrEmpty(codeStylePrompt) ? string.Empty : "\n")
            + diff
            + (string.IsNullOrEmpty(completionPrompt) ? string.Empty : "\n")
            + completionPrompt;
    }

    #region Evil Git Header extraction

    // Just pretend you didn't see this section
    private static readonly Regex HunkHeaderExtractor = HunkHeaderRegex();

    [GeneratedRegex(@"^(?<path>[^\r\n]+)\r?\n@@ -(?<oldStart>\d+),(?<oldCount>\d+) \+(?<newStart>\d+),(?<newCount>\d+) @@", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HunkHeaderRegex();

    private static (string Path, int OldStart, int OldCount, int NewStart, int NewCount) ParseHunkHeader(string hunkText)
    {
        using var activity = Tracing.Start();

        // If this fails, we're screwed
        // But it shouldn't, hopefully
        var match = HunkHeaderExtractor.Match(hunkText);

        var path = match.Groups["path"].Value.Trim();
        var oldStart = int.Parse(match.Groups["oldStart"].Value);
        var oldCount = int.Parse(match.Groups["oldCount"].Value);
        var newStart = int.Parse(match.Groups["newStart"].Value);
        var newCount = int.Parse(match.Groups["newCount"].Value);

        return (path, oldStart, oldCount, newStart, newCount);
    }

    #endregion
}
