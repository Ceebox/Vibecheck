using LLama;
using LLama.Common;

namespace Vibecheck.Inference;

public sealed class ModelData : IDisposable
{
    private readonly ModelParams mParameters;
    private readonly LLamaWeights mWeights;

    internal ModelData(ModelParams parameters, LLamaWeights weights)
    {
        mParameters = parameters;
        mWeights = weights;
    }

    internal ModelParams Parameters => mParameters;
    internal LLamaWeights Weights => mWeights;

    public void Dispose()
    {
        this.Weights.Dispose();
    }
}
