using LLama;
using LLama.Common;
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
        mModelPath = Path.Combine(CACHE_FOLDER, Path.GetFileName(mModelUrl));
        mModelParams = new ModelParams(mModelPath)
        {
            // TODO: Make this adjustable
            ContextSize = 8192,
            GpuLayerCount = 32,
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
        Directory.CreateDirectory(CACHE_FOLDER);
        var downloader = new ModelDownloader(mModelUrl, mModelPath);

        // Download model if not cached
        if (!downloader.ModelDownloaded)
        {
            Console.WriteLine("Downloading model...");
            await downloader.Load();
            Console.WriteLine("Download complete.");
        }
        else
        {
            Console.WriteLine("Model found in cache.");
        }

        // Load model
        mWeights = LLamaWeights.LoadFromFile(mModelParams);
    }
}
