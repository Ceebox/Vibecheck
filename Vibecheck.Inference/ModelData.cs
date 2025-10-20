using LLama;
using LLama.Common;

namespace Vibecheck.Inference;

public sealed class ModelData : IDisposable
{
    public required ModelParams Parameters { get; init; }
    public required LLamaWeights Weights { get; init; }

    public void Dispose()
    {
        this.Weights.Dispose();
    }
}
