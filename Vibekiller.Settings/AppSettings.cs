namespace Vibekiller.Settings;

public sealed class AppSettings
{
    public InferenceSettings InferenceSettings { get; init; } = new();
    public GitSettings GitSettings { get; init; } = new();
}
