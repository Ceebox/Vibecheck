using LLama.Native;
using System.Collections.Concurrent;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

/// <summary>
/// Manages and creates different instances of <see cref="InferenceEngineBase{T}"/> via a queue.
/// </summary>
/// <remarks>
/// Remember to dispose of the created inference engines!
/// </remarks>
public static class InferenceEngineFactory
{
    private static readonly SemaphoreSlim sContextSemaphore =
        new(Configuration.Current.InferenceSettings.ContextLimit, Configuration.Current.InferenceSettings.ContextLimit);

    private static readonly ConcurrentQueue<TaskCompletionSource<InferenceEngineBase>> sQueue =
        new();

    static InferenceEngineFactory()
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
    /// Fetches weights and parameters from a model URL. Can be reused across contexts.
    /// </summary>
    public static async Task<ModelData> LoadModelDataAsync(string modelUrl)
    {
        var loader = new ModelLoader(modelUrl);
        return new ModelData(loader.ModelParams, await loader.Fetch());
    }

    public static async Task<DiffEngine> CreateDiffEngineAsync(ModelData modelData, string systemPrompt, IEnumerable<string> diffs)
        => await CreateEngineAsync(() => new DiffEngine(modelData, systemPrompt, diffs));

    public static async Task<ConsoleChatEngine> CreateChatEngineAsync(ModelData modelData, string systemPrompt)
        => await CreateEngineAsync(() => new ConsoleChatEngine(modelData, systemPrompt));

    private static async Task<T> CreateEngineAsync<T>(Func<T> factory, CancellationToken cancellationToken = default) where T : InferenceEngineBase
    {
        using var activity = Tracing.Start();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var baseTcs = new TaskCompletionSource<InferenceEngineBase>(TaskCreationOptions.RunContinuationsAsynchronously);
        sQueue.Enqueue(baseTcs);

        await sContextSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        if (sQueue.TryDequeue(out var nextBaseTcs))
        {
            try
            {
                var engine = factory();

                engine.Disposed += (_, _) => sContextSemaphore.Release();

                nextBaseTcs.SetResult(engine);
                tcs.SetResult((T)engine);
            }
            catch (Exception ex)
            {
                sContextSemaphore.Release();
                activity.AddException(ex);
                nextBaseTcs.SetException(ex);
                tcs.SetException(ex);
            }
        }

        return await tcs.Task.ConfigureAwait(false);
    }

}
