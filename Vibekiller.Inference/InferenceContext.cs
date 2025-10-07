using LLama;
using LLama.Native;
using Vibekiller.Utility;

namespace Vibekiller.Inference;

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
            using var activity = Tracing.Start("Llama Log");
            if (level == LLamaLogLevel.Warning)
            {
                activity.AddWarning(message);
            }
            else if (level == LLamaLogLevel.Error)
            {
                activity.AddError(message);
            }

            // This is important, it means we are on the CPU instead
            if (level == LLamaLogLevel.Warning && message.Contains("cannot be used with preferred buffer type Vulkan_Host"))
            {
                activity.Log("Unable to run this model on the GPU - using CPU instead.", LogLevel.ERROR);
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
