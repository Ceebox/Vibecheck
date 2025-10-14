using LLama;
using LLama.Common;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Vibecheck.Inference.Tools;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

public sealed partial class DiffEngine : InferenceEngineBase<IAsyncEnumerable<InferenceResult>>
{
    private static readonly JsonSerializerOptions sOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new ToolInvocationConverter() },
    };

    private readonly IEnumerable<string> mDiffs;

    internal DiffEngine(string modelUrl, string systemPrompt, IEnumerable<string> diffs) : base(modelUrl, systemPrompt)
    {
        mDiffs = diffs;
    }

    public override async IAsyncEnumerable<InferenceResult> Execute()
    {
        await foreach (var chunk in this.ProcessDiffs())
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<InferenceResult> ProcessDiffs()
    {
        using var activity = Tracing.Start();
        activity.AddTag("diffs.count", mDiffs.Count());

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = Configuration.Current.InferenceSettings.MaxTokens,
            AntiPrompts = Configuration.Current.InferenceSettings.AntiPrompts,
            SamplingPipeline = new SettingBasedSamplingPipeline(Configuration.Current.InferenceSettings.SamplingSettings)
        };

        // Begin spaghetti arrow code:
        var hasDiffs = false;
        foreach (var diffText in mDiffs)
        {
            await mContext.Reset();

            var executor = new InteractiveExecutor(await this.GetLlamaContext());
            var chatHistory = new ChatHistory();
            chatHistory.AddMessage(AuthorRole.System, this.SystemPrompt);

            var session = new ChatSession(executor, chatHistory);

            hasDiffs = true;
            
            var parsedHeader = ParseHunkHeader(diffText);
            var prompt = this.GeneratePrompt(diffText);
            var userMessage = new ChatHistory.Message(AuthorRole.User, prompt);

            var continueConversation = true;
            while (continueConversation)
            {
                var buffer = new StringBuilder();
                object? invocationResult = null;

                await foreach (var chunk in session.ChatAsync(userMessage, inferenceParams))
                {
                    buffer.Append(chunk);
                    Tracing.Write(chunk, LogLevel.INFO);

                    while (true)
                    {
                        var bufferText = buffer.ToString().TrimStart(',', '\n', '\r', ' ');

                        // TODO: Investigate refactoring this into ExtractObjectsAsArray
                        // Check if we have a complete array and stop before the AI runs away from us!
                        var jsonCandidate = JsonArrayExtractor.ExtractFirstCompleteArray(bufferText);
                        if (jsonCandidate == null)
                        {
                            break;
                        }

                        if (this.TryExecuteInvocation(jsonCandidate, ref buffer, out invocationResult))
                        {
                            break;
                        }

                        yield return new InferenceResult
                        {
                            Path = parsedHeader.Path + $" @ {parsedHeader.NewStart}",
                            CodeStartLine = parsedHeader.NewStart,
                            Contents = jsonCandidate
                        };

                        // Remove the portion we just processed from the buffer
                        var idx = buffer.ToString().IndexOf(jsonCandidate, StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            buffer.Remove(0, idx + jsonCandidate.Length);
                        }
                        else
                        {
                            buffer.Clear();
                        }
                    }

                    if (invocationResult != null)
                    {
                        // Can't have multiple user messages in a row, add the assistant respose because it's all we've got
                        // TODO: Investigate if having this empty improves the context window
                        var assistantMessage = new ChatHistory.Message(AuthorRole.Assistant, buffer.ToString());
                        chatHistory.AddMessage(assistantMessage.AuthorRole, assistantMessage.Content);

                        var continuationText = GenerateContinuedPrompt(diffText, invocationResult.ToString() ?? string.Empty);
                        userMessage = new ChatHistory.Message(AuthorRole.User, continuationText);
                        chatHistory.AddMessage(userMessage.AuthorRole, userMessage.Content);

                        buffer.Clear();
                        invocationResult = null;

                        continue;
                    }

                    // no more continuations
                    continueConversation = false;
                }
            }


            // New line, otherwise everything is smushed
            Tracing.Write("\n", LogLevel.INFO);
        }

        if (!hasDiffs)
        {
            Tracing.WriteLine("No diffs found.", LogLevel.INFO);
        }
    }

    private string GeneratePrompt(string diff)
    {
        var codeStylePrompt = Configuration.Current.InferenceSettings.CodeStylePrompt;
        var completionPrompt = Configuration.Current.InferenceSettings.CompletionPrompt;
        var toolPromptBase = Configuration.Current.ToolSettings.ToolPrompt;
        var toolPrompt = Configuration.Current.ToolSettings.ToolsEnabled ? toolPromptBase + mContext.GetToolInfo() + "\n" : string.Empty;

        // Give it the system prompt every time to keep it in the context window
        return this.SystemPrompt
            + '\n'
            + codeStylePrompt
            + (string.IsNullOrEmpty(codeStylePrompt) ? string.Empty : "\n")
            + toolPrompt
            + diff
            + (string.IsNullOrEmpty(completionPrompt) ? string.Empty : "\n")
            + completionPrompt;
    }

    private string GenerateContinuedPrompt(string diff, string continuationResult)
    {
        var completionPrompt = Configuration.Current.InferenceSettings.CompletionPrompt;

        // Give it the system prompt every time to keep it in the context window
        return continuationResult
            + this.SystemPrompt
            + '\n'
            + diff
            + (string.IsNullOrEmpty(completionPrompt) ? string.Empty : "\n")
            + completionPrompt;
    }

    private bool TryExecuteInvocation(string jsonCandidate, ref StringBuilder buffer, out object? invocationResult)
    {
        using var activity = Tracing.Start();

        ToolInvocation? invocation = null;
        foreach (var candidate in JsonArrayExtractor.ExtractObjectsAsArray(jsonCandidate))
        {
            try
            {
                invocation = JsonSerializer.Deserialize<ToolInvocation>(candidate, sOptions);
                if (invocation != null)
                {
                    break;
                }
            }
            catch (JsonException)
            {
                // Continue, try InferenceResult elsewhere
            }
        }

        if (invocation != null)
        {
            Tracing.WriteLine($"Invoking '{invocation.Tool}'.", LogLevel.INFO);

            // Stop current response, invoke the tool
            try
            {
                invocationResult = mContext.InvokeTool(invocation);
                Tracing.WriteLine($"Invoked tool '{invocation.Tool}' successfully.", LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Tracing.WriteLine($"Error invoking tool '{invocation.Tool}': {ex}", LogLevel.ERROR);
                invocationResult = $"Error invoking tool: {ex}";
                return false;
            }

            // Clear the buffer for the AI to continue after the tool call
            buffer.Clear();
            return true;
        }

        invocationResult = null;
        return false;
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
        if (!match.Success)
        {
            return (string.Empty, 0, 0, 0, 0);
        }

        var path = match.Groups["path"].Value.Trim();
        var oldStart = int.Parse(match.Groups["oldStart"].Value);
        var oldCount = int.Parse(match.Groups["oldCount"].Value);
        var newStart = int.Parse(match.Groups["newStart"].Value);
        var newCount = int.Parse(match.Groups["newCount"].Value);

        return (path, oldStart, oldCount, newStart, newCount);
    }

    #endregion
}
