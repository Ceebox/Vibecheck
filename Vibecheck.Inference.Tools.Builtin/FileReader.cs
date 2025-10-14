using Vibecheck.Utility;

namespace Vibecheck.Inference.Tools.Builtin;

[ToolClass]
public sealed class FileReader
{
    [ToolMethod(Description = "Find a file using a fuzzy search and return its contents.")]
    public static string? ReadFileContents(
        ToolContext toolContext,
        [ToolParameter(Description = "The file or file path to search for within the repository.")]
        string searchPath
    )
    {
        var repoPath = toolContext.RepositoryPath;
        if (repoPath == null)
        {
            return null;
        }

        var filePath = SearchHelpers.FuzzySearch(repoPath, searchPath);
        if (filePath == null)
        {
            return null;
        }

        // Then, read and return its contents
        return File.ReadAllText(filePath);
    }
}
