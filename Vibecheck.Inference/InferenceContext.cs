using LLama;
using LLama.Common;
using LLama.Native;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

public sealed partial class InferenceContext : IDisposable
{
    private readonly string mModelUrl;

    private LLamaWeights? mModel;
    private ModelParams? mParams;
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
                // We get a lot of useless warnings, wrap them behind this
                if (Tracing.DebugLevel == LogLevel.EVERYTHING)
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

    public InferenceContext(string modelUrl)
    {
        mModelUrl = modelUrl;
    }

    public async Task<LLamaContext> GetContext()
    {
        using var activity = Tracing.Start();
        if (mContext == null)
        {
            await this.Load();
        }

        return mContext!;
    }

    public async Task Reset()
    {
        using var activity = Tracing.Start();
        this.DisposeContext();

        if (mModel == null || mParams == null)
        {
            await this.Load();
        }
        else
        {
            mContext = mModel.CreateContext(mParams);
        }
    }

    private async Task Load()
    {
        using var activity = Tracing.Start();
        var modelLoader = new ModelLoader(mModelUrl);
        mModel = await modelLoader.Fetch();
        mParams = modelLoader.ModelParams;
        mContext = mModel.CreateContext(mParams);
    }

    private void DisposeContext()
    {
        mContext?.Dispose();
        mContext = null;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        DisposeContext();
        mModel?.Dispose();
    }
}
