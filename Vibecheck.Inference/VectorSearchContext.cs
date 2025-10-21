using LLama;
using System.Text.Json;
using Vibecheck.Inference.Data;
using Vibecheck.Inference.Tools;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

/// <summary>
/// Provides the ability for tools to search an indexed vector database.
/// </summary>
public sealed class VectorSearchContext : IVectorSearchContext, IDisposable
{
    private static readonly JsonSerializerOptions sOptions = new()
    {
        WriteIndented = true
    };

    private readonly VectorDatabase mDatabase;
    private readonly Lazy<LLamaEmbedder> mLazyEmbedder;

    public VectorSearchContext(string rootFolder, ModelData modelData)
    {
        if (string.IsNullOrEmpty(rootFolder))
        {
            // Don't do this, aye
            throw new InvalidOperationException("Invalid Git repo");
        }

        mDatabase = new(VectorDatabase.GetDatabasePath(rootFolder));
        mLazyEmbedder = new Lazy<LLamaEmbedder>(
            () => LlamaItemFactory.CreateEmbedder(modelData)
        );
    }

    public bool IsIndexed() => mDatabase.IsIndexed;

    public string Search(string query, int amount = 3)
    {
        using var activity = Tracing.Start();

        // This is synchronous so I guess we have to do this
        var embedding = mLazyEmbedder.Value.GetEmbeddings(query)
            .GetAwaiter()
            .GetResult();

        if (embedding is null)
        {
            return string.Empty;
        }

        // Use the first embedding, since this should be the mean result
        var results = mDatabase
            .Search(embedding[0], amount)
            .Select(r => r.FilePath);

        return JsonSerializer.Serialize(results, sOptions);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (mLazyEmbedder.IsValueCreated)
        {
            mLazyEmbedder.Value.Dispose();
        }
    }
}
