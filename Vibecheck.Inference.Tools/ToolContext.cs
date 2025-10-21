namespace Vibecheck.Inference.Tools;

public sealed class ToolContext
{
    public string? RepositoryPath { get; set; } = string.Empty;
    public IVectorSearchContext? VectorContext
        => VectorSearchContextHost.GetContext();
}
