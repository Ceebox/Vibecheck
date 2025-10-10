using LLama.Sampling;
using Vibecheck.Settings;

namespace Vibecheck.Inference;

internal class SettingBasedSamplingPipeline : DefaultSamplingPipeline
{
    public SettingBasedSamplingPipeline(SamplingSettings settings)
    {
        Temperature = settings.Temperature;
        TopP = settings.TopP;
        MinP = settings.MinP;
        TopK = settings.TopK;
        RepeatPenalty = settings.RepeatPenalty;
        PenalizeNewline = settings.PenalizeNewline;
    }
}
