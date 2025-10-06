using LLama.Sampling;
using Vibekiller.Settings;

namespace Vibekiller.Inference;

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
