namespace Vibecheck.Inference.Tools;

/// <summary>
/// This is a temporary hack intended to get the basic vector search at least working.
/// </summary>
public static class VectorSearchContextHost
{
    private static IVectorSearchContext? sContext;

    public static void SetContext(IVectorSearchContext context)
    {
        sContext = context;
    }

    public static IVectorSearchContext? GetContext()
    {
        return sContext;
    }
}
