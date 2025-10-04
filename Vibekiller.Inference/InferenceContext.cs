using LLama;
using LLama.Common;
using LLama.Native;
using System.Text;
using Vibekiller.Utility;

namespace Vibekiller.Inference;

public sealed class InferenceContext
{
    private readonly string mModelUrl;
    private readonly string mSystemPrompt;

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

    public InferenceContext(string modelUrl, string systemPrompt)
    {
        mModelUrl = modelUrl;
        mSystemPrompt = systemPrompt;
    }

    public async Task Load()
    {
        var modelLoader = new ModelLoader(mModelUrl);
        var model = await modelLoader.Fetch();
        using var context = model.CreateContext(modelLoader.ModelParams);
        var executor = new InteractiveExecutor(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.System, mSystemPrompt);

        await Chat(executor, chatHistory);
    }

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
