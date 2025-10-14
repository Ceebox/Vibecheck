using Vibecheck.Utility;

namespace Vibecheck.Inference.Tools.Builtin;

[ToolClass]
public sealed class FuzzySearcher
{
    [ToolMethod(Description = "Try to search for a file.")]
    public static string? FuzzySearch(
        ToolContext toolContext,
        [ToolParameter(Description = "The file or file path to search for.")]
        string searchPath
    )
    {
        var repoPath = toolContext.RepositoryPath;
        if (repoPath == null)
        {
            return null;
        }

        return SearchHelpers.FuzzySearch(repoPath, searchPath);
    }
}
