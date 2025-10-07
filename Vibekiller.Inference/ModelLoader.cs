using LLama;
using LLama.Common;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Inference;

public sealed class ModelLoader : IFetcher<LLamaWeights>, IDisposable
{
    private const string CACHE_FOLDER = "model_cache";
    private readonly string mModelUrl;
    private readonly string mModelPath;
    private readonly ModelParams mModelParams;

    private LLamaWeights? mWeights = null;

    public ModelLoader(string modelUrl)
    {
        mModelUrl = modelUrl;
        mModelPath = Path.Combine(OutputDirectory, Path.GetFileName(mModelUrl));
        mModelParams = new ModelParams(mModelPath)
        {
            ContextSize = Convert.ToUInt32(Configuration.Current.InferenceSettings.ContextWindowSize),
            GpuLayerCount = Configuration.Current.InferenceSettings.GpuLayerCount,
            MainGpu = Configuration.Current.InferenceSettings.MainGpu,
        };
    }

    public bool Loaded
        => mWeights != null;

    public ModelParams ModelParams
        => mModelParams;

    public void Dispose()
        => mWeights?.Dispose();

    public async Task<LLamaWeights> Fetch()
    {
        await Load();
        return mWeights!;
    }

    private async Task Load()
    {
        using var activity = Tracing.Start();

        var loaded = mWeights != null;
        activity.AddTag("modelloader.loaded", loaded);
        if (loaded)
        {
            return;
        }

        // Ensure cache directory exists
        Directory.CreateDirectory(OutputDirectory);
        var downloader = new ModelDownloader(mModelUrl, mModelPath);

        // Download model if not cached
        if (!downloader.ModelDownloaded)
        {
            await downloader.Load();
        }
        else
        {
            activity.Log("Model found in cache.", LogLevel.INFO);
        }

        // Load model
        mWeights = LLamaWeights.LoadFromFile(mModelParams);
    }

    private static string OutputDirectory
        => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + CACHE_FOLDER;
}
