using LLama;
using Vibecheck.Inference.Tools;

namespace Vibecheck.Inference;

public abstract class InferenceEngineBase : IDisposable
{
    public event EventHandler? Disposed;

    private protected readonly InferenceContext mContext;
    private readonly string mSystemPrompt;

    public InferenceEngineBase(string modelUrl, string systemPrompt)
    {
        mContext = new InferenceContext(modelUrl);
        mSystemPrompt = CleanSystemPrompt(systemPrompt);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        mContext.Dispose();
        this.Disposed?.Invoke(this, EventArgs.Empty);
    }

    public ToolContext? ToolContext
        => mContext?.ToolContext;

    internal async Task<LLamaContext> GetLlamaContext()
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

public abstract class InferenceEngineBase<T> : InferenceEngineBase
{
    public InferenceEngineBase(string modelUrl, string systemPrompt) : base(modelUrl, systemPrompt) { }

    public abstract T Execute();

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
    }
}
