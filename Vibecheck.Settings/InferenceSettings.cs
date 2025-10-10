namespace Vibecheck.Settings;
public class InferenceSettings
{
    public string ModelUrl { get; set; } = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf";
    public string ModelApiToken { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = """
            You are an advanced senior software engineer performing automated code reviews.
            You must only output valid, compact JSON — nothing else. Do not include explanations, markdown, or additional text.
            Your task is to review small code diffs and produce zero or more structured review comments.
            Do not describe the changes. Only suggest improvements that could be made (if any are required) to the new code.

            Return all suggestions as a single JSON array.
            The JSON format must always be a list (array) of comment objects. Each object must follow this schema:

            [
              {
                "HasChange": true,
                "SuggestedChange": "The new code that should go in the place of the old code, or a short suggestion.",
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
            - Never include “Assistant:”, “User:”, or **any** text outside of JSON.
            - Be concise and only comment when necessary.
            - Be reasonable with the "ai_probability" estimation. AI code will probably be unlike other code around it.
            - Do not describe the changes. **Only suggest improvements to the new code**.
            - Stop (<|eot_id|>) at the end of the JSON "]".
            """;
    public string CodeStylePrompt { get; set; } = string.Empty;
    public string CompletionPrompt { get; set; } = "Complete the following JSON array describing code review comments, matching the schema.\r\n[";
    public int ContextWindowSize { get; set; } = 2048;
    public int GpuLayerCount { get; set; } = -1;
    public int MaxTokens { get; set; } = 512;

    /// <summary>
    /// The GPU to perform inference on.
    /// </summary>
    public int MainGpu { get; set; } = 0;

    /// <summary>
    /// The maximum amount of inference contexts permitted to exist at once.
    /// </summary>
    public int ContextLimit { get; set; } = 1;
    public string[] AntiPrompts { get; set; } = ["User:", "\nUser:", "</s>", "<|eot_id|>", "]User"];
    public SamplingSettings SamplingSettings { get; set; } = new();
}
