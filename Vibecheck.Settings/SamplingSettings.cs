namespace Vibecheck.Settings;

public sealed class SamplingSettings
{
    public float Temperature { get; set; } = 0.3f;
    public float TopP { get; set; } = 0.9f;
    public float MinP { get; set; } = 0.05f;
    public int TopK { get; set; } = 40;
    public float RepeatPenalty { get; set; } = 1.1f;
    public bool PenalizeNewline { get; set; } = true;
}
