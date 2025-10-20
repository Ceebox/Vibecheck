using Vibecheck.Inference.Data;

namespace Vibecheck.Inference.Tools;

public sealed class ToolContext
{
    public string? RepositoryPath { get; set; } = string.Empty;
    public VectorDatabase? VectorDatabase { get; set; } = null;
}
