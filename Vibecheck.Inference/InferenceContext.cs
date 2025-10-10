using LLama;
using LLama.Native;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

public sealed partial class InferenceContext : IDisposable
{
    private readonly string mModelUrl;

    private LLamaContext? mContext;

    static InferenceContext()
    {
        // This code is inside the static constructor because we can't have any pre-created configurations

        // Use Vulkan
        // TODO: Allow other backends, e.g. CPU, CUDA
        NativeLibraryConfig.All.WithVulkan(true);

        NativeLibraryConfig.All.WithLogCallback((level, message) =>
        {
            if (level == LLamaLogLevel.Warning)
            {
                // Write because these end with a newline
                Tracing.Write(message, LogLevel.WARNING);
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

    public InferenceContext(string modelUrl)
    {
        mModelUrl = modelUrl;
    }

    public async Task<LLamaContext> GetContext()
    {
        if (mContext == null)
        {
            await this.Load();
        }

        return mContext!;
    }

    private async Task Load()
    {
        var modelLoader = new ModelLoader(mModelUrl);
        var model = await modelLoader.Fetch();
        mContext = model.CreateContext(modelLoader.ModelParams);
    }

    public void Dispose()
    {
        mContext?.Dispose();
    }
}
