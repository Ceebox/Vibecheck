using LLama;
using LLama.Native;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

internal static class LlamaItemFactory
{
    static LlamaItemFactory()
    {
        // This code is inside the static constructor because we can't have any pre-created configurations

        // Use Vulkan
        // TODO: Allow other backends, e.g. CPU, CUDA
        NativeLibraryConfig.All.WithVulkan(true);

        NativeLibraryConfig.All.WithLogCallback((level, message) =>
        {
            if (level == LLamaLogLevel.Warning)
            {
                // We get a lot of useless warnings, wrap them behind this
                if (Tracing.DebugLevel == LogLevel.VERBOSE)
                {
                    // Write because these end with a newline
                    Tracing.Write(message, LogLevel.WARNING);
                }
            }
            else if (level == LLamaLogLevel.Error)
            {
                // Write because these end with a newline
                Tracing.Write(message, LogLevel.ERROR);
            }

            // This is important, it means we are on the CPU instead
            if (level == LLamaLogLevel.Warning && message.Contains("cannot be used with preferred buffer type Vulkan_Host"))
            {
                Tracing.WriteLine("Unable to run this model on the GPU - using CPU instead.", LogLevel.ERROR);
            }
        });
    }

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
