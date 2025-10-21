using LLama;
using Vibecheck.Inference.Tools;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

public sealed partial class InferenceContext : IDisposable
{
    private readonly ModelData mModelData;
    private readonly ToolHost? mToolHost;
    private LLamaContext? mContext;

    public InferenceContext(ModelData modelData)
    {
        mModelData = modelData;
        if (Configuration.Current.ToolSettings.ToolsEnabled)
        {
            mToolHost = new ToolHost();
            mToolHost.Load();
        }
    }

    public ToolContext? ToolContext
        => mToolHost?.ToolContext;

    public LLamaContext GetContext()
    {
        using var activity = Tracing.Start();
        if (mContext == null)
        {
            this.LoadContext();
        }

        return mContext!;
    }

    public void Reset()
    {
        using var activity = Tracing.Start();
        this.LoadContext();
    }

    public string GetToolInfo()
        => mToolHost?.GetToolContextJson() ?? string.Empty;

    public object? InvokeTool(ToolInvocation invocation)
        => mToolHost?.InvokeTool(invocation);

    private void LoadContext()
    {
        mContext = LlamaItemFactory.CreateContext(mModelData);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
