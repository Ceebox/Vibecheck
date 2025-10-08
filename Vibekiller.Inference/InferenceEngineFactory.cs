using System.Collections.Concurrent;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Inference;

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

    public static async Task<DiffEngine> CreateDiffEngine(string modelUrl, string systemPrompt, IEnumerable<string> diffs)
        => await CreateEngineAsync(() => new DiffEngine(modelUrl, systemPrompt, diffs));

    public static async Task<ConsoleChatEngine> CreateChatEngine(string modelUrl, string systemPrompt)
        => await CreateEngineAsync(() => new ConsoleChatEngine(modelUrl, systemPrompt));

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
