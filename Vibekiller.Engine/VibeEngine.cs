using Vibekiller.Inference;
using Vibekiller.Utility;

namespace Vibekiller.Engine
{
    public sealed class VibeEngine : IDisposable
    {
        private const string DEFAULT_SYSTEM_PROMPT = """
            You are an advanced developer, tasked with providing insightful and critical, yet brief code review comments for snippets of code.
            Unfortunately, you have to work with a lot of bad, AI generated code.
            You can only reply in the JSON format. You may only return one response per message. Keep it brief if you can.
            Here is an example of the JSON format, do not deviate from it or add anythings:
            {
              "has_comment": true,
              "comment": "Feedback here",
              "suggested_change": "Fix the formatting here",
              "ai_probability": 0.78
            }
            The only thing required here is "has_comment", if this is false, nothing else is needed.
            "ai_probability" is a lenient 0-1 float chance of the generated code being AI.
            "suggested_change" is ideally a code change, or architectural change.
            Remember, only return that JSON, not even "Assistant: or User:"
            """;

        private readonly string mModelUrl;

        public VibeEngine(string modelUrl)
        {
            mModelUrl = modelUrl;
        }

        public async Task Run()
        {
            using var activity = Tracing.Start();

            // Prepare system prompt
            var context = new InferenceContext(mModelUrl, DEFAULT_SYSTEM_PROMPT);
            await context.Load();
        }

        public void Dispose()
        {
        }
    }
}
