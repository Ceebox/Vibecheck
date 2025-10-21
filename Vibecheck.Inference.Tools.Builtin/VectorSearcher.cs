namespace Vibecheck.Inference.Tools.Builtin;

[ToolClass]
public sealed class VectorSearcher
{
    [ToolMethod(
        Description = "Perform a vector search through an indexed portion of the codebase.",
        AvailabilityType = typeof(VectorSearchAvailability)
    )]
    public static string? VectorSearch(
        ToolContext toolContext,
        [ToolParameter(Description = "The file or file path to search for.")]
        string searchQuery
    )
    {
        var db = toolContext.VectorContext!;
        var results = db.Search(searchQuery);
        return results;
    }

    public sealed class VectorSearchAvailability : IToolAvailability
    {
        public bool IsAvailable(ToolContext ctx)
            => ctx.VectorContext is not null && ctx.VectorContext.IsIndexed();
    }
}
