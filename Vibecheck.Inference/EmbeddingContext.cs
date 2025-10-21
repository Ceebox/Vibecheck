namespace Vibecheck.Inference;
public sealed class EmbeddingContext : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
