namespace Vibecheck.Settings;

public sealed class AppSettings
{
    public InferenceSettings InferenceSettings { get; init; } = new();
    public GitSettings GitSettings { get; init; } = new();
    public ToolSettings ToolSettings { get; init; } = new();
    public ReviewSettings ReviewSettings { get; init; } = new();
    public ServerSettings ServerSettings { get; init; } = new();
}
