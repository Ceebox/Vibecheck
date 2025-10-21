namespace Vibecheck.Inference.Tools;

/// <summary>
/// Used to provide a vector search context for tools.
/// </summary>
public interface IVectorSearchContext
{
    /// <summary>
    /// Return the results of a search query as JSON.
    /// </summary>
    /// <param name="query">The string to search.</param>
    /// <param name="amount">The amount of results to return.</param>
    /// <returns>Search results as JSON.</returns>
    public string Search(string query, int amount = 3);

    /// <summary>
    /// If the database has actually been indexed. If this is false, <see cref="Search(string, int)"/> will not work.
    /// </summary>
    /// <returns>True if the database is indexed, otherwise false.</returns>
    public bool IsIndexed();
}
