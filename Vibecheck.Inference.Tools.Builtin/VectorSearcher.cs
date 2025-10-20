using Microsoft.Extensions.Options;
using System.Text.Json;
using Vibecheck.Inference.Tools;
using Vibecheck.Utility;

namespace Vibecheck.Inference.Tools.Builtin;

[ToolClass]
public sealed class VectorSearcher
{
    private static readonly JsonSerializerOptions sOptions = new JsonSerializerOptions()
    {
        WriteIndented = true
    };

    [ToolMethod(
        Description = "Perform a vector search through an indexed portion of the codebase.",
        AvailabilityType = typeof(VectorSearchAvailability)
    )]
    public static string? VectorSearch(
        ToolContext toolContext,
        [ToolParameter(Description = "The file or file path to search for.")]
        string searchPath
    )
    {
        var db = toolContext.VectorDatabase;

        // TODO: Get embeddings
        var results = db.Search([]);
        return JsonSerializer.Serialize(results, sOptions);
    }

    public sealed class VectorSearchAvailability : IToolAvailability
    {
        public bool IsAvailable(ToolContext ctx)
            => ctx.VectorDatabase is not null;
    }
}
