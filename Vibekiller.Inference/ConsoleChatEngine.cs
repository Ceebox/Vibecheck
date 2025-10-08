using LLama;
using LLama.Common;
using System.Text;

namespace Vibekiller.Inference;

public sealed class ConsoleChatEngine : InferenceEngineBase<Task>
{
    internal ConsoleChatEngine(string modelUrl, string systemPrompt)
    : base(modelUrl, systemPrompt)
    {
    }

    public override async Task Execute()
    {
        // TODO: Probably refactor out the input logic from here to make it more pure?
        // In the future we can send requests to this with a chat, ya know, stuff like that
        var executor = new InteractiveExecutor(await this.GetContext());
        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.System, this.SystemPrompt);

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
