using LLama;

namespace Vibekiller.Inference;

public abstract class InferenceEngineBase : IDisposable
{
    public event EventHandler? Disposed;

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        this.Disposed?.Invoke(this, EventArgs.Empty);
    }
}

public abstract class InferenceEngineBase<T> : InferenceEngineBase
{
    private protected readonly InferenceContext mContext;
    private readonly string mSystemPrompt;

    public InferenceEngineBase(string modelUrl, string systemPrompt)
    {
        mContext = new InferenceContext(modelUrl);
        mSystemPrompt = CleanSystemPrompt(systemPrompt);
    }

    public abstract T Execute();

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        mContext.Dispose();
        base.Dispose();
    }

    internal async Task<LLamaContext> GetContext()
    {
        return await mContext.GetContext();
    }

    internal string SystemPrompt
        => mSystemPrompt;

    /// <summary>
    /// Our prompt can have escape characters in that confuse the AI. Fix it up a little.
    /// </summary>
    /// <param name="initialPrompt">The prompt to clean.</param>
    /// <returns></returns>
    private static string CleanSystemPrompt(string initialPrompt)
    {
        var newPrompt = initialPrompt.Replace("\r\n", string.Empty);
        newPrompt = newPrompt.Replace("\u2014", string.Empty);
        newPrompt = newPrompt.Replace("\u0022", string.Empty);
        newPrompt = newPrompt.Replace("\u201C", string.Empty);
        newPrompt = newPrompt.Replace("\u201D", string.Empty);
        newPrompt = newPrompt.Replace("\u0027", string.Empty);
        newPrompt = newPrompt.Replace("\u0060", string.Empty);
        return newPrompt;
    }
}
