namespace Vibekiller.Settings;

public sealed class AppSettings
{
    public InferenceSettings InferenceSettings { get; set; } = new();
}
