namespace Vibecheck.Settings;
public class ToolSettings
{
    public bool ToolsEnabled { get; set; } = true;
    public string ToolPrompt { get; set; } = """
        You can invoke tool methods if needed. Use this JSON format exactly:
        {
          "tool": "<tool_method_name>",
          "parameters": {
            "<parameter_name>": "<parameter_value>",
          }
        }

        - Only invoke one tool method per response.
        - Do not add extra text outside the JSON.
        - If no tool is needed, respond normally without JSON.
        - Tool and parameter names are case-sensitive.
        - You MUST invoke a tool method by it's Method name.

        Here are the available tools:

        """;
}
