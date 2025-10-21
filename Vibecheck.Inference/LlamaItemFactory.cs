using LLama;
using LLama.Native;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

internal static class LlamaItemFactory
{
    /// <summary>
    /// Creates a new LLamaContext using the given model URL and parameters.
    /// Dispose the context after use.
    /// </summary>
    public static LLamaContext CreateContext(ModelData modelData)
    {
        var context = modelData.Weights.CreateContext(modelData.Parameters);
        return context;
    }

    /// <summary>
    /// Creates a new LlamaEmbedder using the given model URL.
    /// Dispose the embedder after use.
    /// NOTE: This will override the model data parameters' PoolingType.
    /// </summary>
    public static LLamaEmbedder CreateEmbedder(ModelData modelData)
    {
        // Set this to mean so that we only get a 1 dimensional vector
        // And yes, I know I am overriding this. Deal with it!
        modelData.Parameters.PoolingType = LLamaPoolingType.Mean;
        var embedder = new LLamaEmbedder(modelData.Weights, modelData.Parameters);

        return embedder;
    }
}
