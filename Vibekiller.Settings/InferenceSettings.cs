namespace Vibekiller.Settings;
public class InferenceSettings
{
    public string ModelUrl { get; set; } = "https://huggingface.co/ibm-granite/granite-4.0-micro-GGUF/resolve/main/granite-4.0-micro-Q4_K_M.gguf";
    public string ModelApiToken { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = """
            You are an advanced senior software engineer performing automated code reviews.
            You must only output valid, compact JSON — nothing else. Do not include explanations, markdown, or additional text.
            Your task is to review small code diffs and produce zero or more structured comments.

            The JSON format must always be a list (array) of comment objects. Each object must follow this schema:

            [
              {
                "HasChange": true,
                "SuggestedChange": "Preferably a code suggestion, or a short, specific suggested change.",
                "Comment": "Brief feedback about the issue or improvement.",
                "AiProbability": float
              }
            ]

            Rules:
            - Always output a **JSON array** (`[]`) — never an object or text.
            - If there are **no issues**, return an **empty array**: `[]`.
            - Each array element represents **one review comment** for the diff.
            - `"has_change"` is required in each object.
            - `"suggested_change"` can be omitted if not relevant.
            - `"comment"` can also be omitted if not relevant or if the suggested change is trivial.
            - `"ai_probability"` is a float between 0 and 1 estimating whether the code appears AI-generated.
            - Never include “Assistant:”, “User:”, or any text outside of JSON.
            - Be concise and only comment when necessary.
            """;
    public string CodeStylePrompt { get; set; } = string.Empty;
    public string CompletionPrompt { get; set; } = "Complete the following JSON array describing code review comments, matching the schema.\r\n[";
    public int ContextWindowSize { get; set; } = 2048;
    public int GpuLayerCount { get; set; } = -1;
    public int MaxTokens { get; set; } = 512;
    public int MainGpu { get; set; } = 0;
    public string[] AntiPrompts { get; set; } = ["User:", "\nUser:", "</s>", "<|eot_id|>"];
    public SamplingSettings SamplingSettings { get; set; } = new();
}
