using LLama;
using LLama.Common;
using LLama.Native;
using Vibecheck.Inference.Tools;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

public sealed partial class InferenceContext : IDisposable
{
    private readonly string mModelUrl;

    private readonly ToolHost? mToolHost;
    private ModelData? mModelData;
    private LLamaContext? mContext;

    public InferenceContext(string modelUrl)
    {
        mModelUrl = modelUrl;
        if (Configuration.Current.ToolSettings.ToolsEnabled)
        {
            mToolHost = new ToolHost();
            mToolHost.Load();
        }
    }

    public ToolContext? ToolContext
        => mToolHost?.ToolContext;

    public async Task<LLamaContext> GetContext()
    {
        using var activity = Tracing.Start();
        if (mContext == null)
        {
            await this.LoadModel();
        }

        return mContext!;
    }

    public async Task Reset()
    {
        using var activity = Tracing.Start();
        this.DisposeContext();

        if (mModelData is null)
        {
            await this.LoadModel();
        }
        else
        {
            mContext = InferenceFactory.CreateContext(mModelData);
        }
    }

    public string GetToolInfo()
        => mToolHost?.GetToolContextJson() ?? string.Empty;

    public object? InvokeTool(ToolInvocation invocation)
        => mToolHost?.InvokeTool(invocation);

    private async Task LoadModel()
    {
        using var activity = Tracing.Start();

        mModelData = await InferenceFactory.LoadModelDataAsync(mModelUrl);
        mContext = InferenceFactory.CreateContext(mModelData);
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
        mModelData?.Dispose();
    }
}
