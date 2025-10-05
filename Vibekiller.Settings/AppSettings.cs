namespace Vibekiller.Settings;

public sealed class AppSettings
{
    public InferenceSettings InferenceSettings { get; set; } = new();
    public GitSettings GitSettings { get; set; } = new();
}
