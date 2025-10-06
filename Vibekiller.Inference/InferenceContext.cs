using LLama;
using LLama.Common;
using LLama.Native;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Inference;

public sealed partial class InferenceContext
{
    private readonly string mModelUrl;
    private readonly string mSystemPrompt;
    private readonly IEnumerable<string> mDiffs;

    static InferenceContext()
    {
        // This code is inside the static constructor because we can't have any pre-created configurations

        // Use Vulkan
        // TODO: Allow other backends, e.g. CPU, CUDA
        NativeLibraryConfig.All.WithVulkan(true);

        NativeLibraryConfig.All.WithLogCallback((level, message) =>
        {
            using var activity = Tracing.Start("Llama Log");
            if (level == LLamaLogLevel.Warning)
            {
                activity.AddWarning(message);
            }
            else if (level == LLamaLogLevel.Error)
            {
                activity.AddError(message);
            }

            // This is important, it means we are on the CPU instead
            if (level == LLamaLogLevel.Warning && message.Contains("cannot be used with preferred buffer type Vulkan_Host"))
            {
                activity.Log("Unable to run this model on the GPU - using CPU instead.", LogLevel.ERROR);
            }
        });
    }

    public InferenceContext(string modelUrl, string systemPrompt, IEnumerable<string> diffs)
    {
        mModelUrl = modelUrl;
        mSystemPrompt = CleanSystemPrompt(systemPrompt);
        mDiffs = diffs;
    }

    public async IAsyncEnumerable<InferenceResult> Execute()
    {
        var modelLoader = new ModelLoader(mModelUrl);
        var model = await modelLoader.Fetch();
        using var context = model.CreateContext(modelLoader.ModelParams);
        var executor = new InteractiveExecutor(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.System, mSystemPrompt);

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
            AntiPrompts = Configuration.Current.InferenceSettings.AntiPrompts
        };

        if (!mDiffs.Any())
        {
            yield break;
        }

        foreach (var diffText in mDiffs)
        {
            var sb = new StringBuilder();

            // Give it the system prompt every time to keep it in the context window
            var message = new ChatHistory.Message(AuthorRole.User, this.GeneratePrompt(diffText));
            await foreach (var chunk in session.ChatAsync(message, inferenceParams))
            {
                sb.Append(chunk);
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
        return mSystemPrompt
            + '\n'
            + codeStylePrompt
            + (string.IsNullOrEmpty(codeStylePrompt) ? string.Empty : "\n")
            + diff
            + (string.IsNullOrEmpty(completionPrompt) ? string.Empty : "\n")
            + completionPrompt;
    }

    /// <summary>
    /// Our prompt can have escape characters in that confuse the AI. Fix it up a little.
    /// </summary>
    /// <param name="initialPrompt">The prompt to clean.</param>
    /// <returns></returns>
    private static string CleanSystemPrompt(string initialPrompt)
    {
        var newPrompt = initialPrompt.Replace("\r\n", "");
        newPrompt = newPrompt.Replace("\u2014", "");
        newPrompt = newPrompt.Replace("\u0022", "");
        newPrompt = newPrompt.Replace("\u201C", "");
        newPrompt = newPrompt.Replace("\u201D", "");
        newPrompt = newPrompt.Replace("\u0027", "");
        newPrompt = newPrompt.Replace("\u0060", "");
        return newPrompt;
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

    private static async Task Chat(InteractiveExecutor executor, ChatHistory chatHistory)
    {
        var session = new ChatSession(executor, chatHistory);
        Console.WriteLine("Chat session started. Type your query (type 'exit' to quit):");

        while (true)
        {
            Console.Write("User: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            try
            {
                Console.Write("Assistant: ");
                var responseBuilder = new StringBuilder();

                var inferenceParams = new InferenceParams()
                {
                    MaxTokens = 512,
                    AntiPrompts = ["User:", "\nUser:", "</s>", "<|eot_id|>"]
                };

                await foreach (var chunk in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, input), inferenceParams))
                {
                    // Remove trailing "User:" if model generated it
                    if (chunk.EndsWith("User:"))
                    {
                        Console.Write(chunk[..^"User:".Length]);
                    }
                    else
                    {
                        Console.Write(chunk);
                    }
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] {ex}");
            }
        }
    }
}
