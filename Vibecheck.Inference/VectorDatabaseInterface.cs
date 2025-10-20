using LLama;
using System.Text;
using Vibecheck.Inference.Data;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck.Inference;

public sealed class VectorDatabaseInterface : IDisposable
{
    private const string DATABASE_FOLDER = "database_cache";

    private readonly VectorDatabase mDatabase;
    private readonly LLamaEmbedder mEmbedder;
    private readonly string mRootFolder;
    private readonly string mDatabasePath;

    public VectorDatabaseInterface(string rootFolder, LLamaEmbedder embedder)
    {
        if (string.IsNullOrEmpty(rootFolder))
        {
            // Catastrophically fail before the user indexes their root drive (that would be suboptimal)
            throw new InvalidOperationException("Invalid git repository");
        }

        mEmbedder = embedder;
        mRootFolder = rootFolder;
        mDatabasePath = Path.Combine(DATABASE_FOLDER, Path.GetFullPath(rootFolder) + ".json");
        mDatabase = new VectorDatabase(mDatabasePath);
    }

    public async Task VectoriseAsync()
    {
        using var activity = Tracing.Start();

        if (!Directory.Exists(mRootFolder))
        {
            throw new DirectoryNotFoundException(mRootFolder);
        }

        var files = Directory.EnumerateFiles(mRootFolder, "*.*", SearchOption.AllDirectories)
                             .Where(f => Configuration.Current.InferenceSettings.VectorDatabaseSettings.IncludedFileTypes.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            Tracing.WriteLine($"Indexing {file}", LogLevel.INFO);

            try
            {
                var text = await File.ReadAllTextAsync(file, Encoding.UTF8);
                var embedding = await mEmbedder.GetEmbeddings(text);

                if (!embedding.Any())
                {
                    continue;
                }

                // Store relative path as ID to ensure uniqueness
                var relativePath = Path.GetRelativePath(mRootFolder, file);
                var record = new VectorRecord(

                    relativePath,
                    file,
                    text,
                    // Pick the first, since in theory, we only have 1 (PoolingType.Mean)
                    embedding[0]
                );

                mDatabase.Add(record);
            }
            catch (Exception ex)
            {
                activity.AddError(ex);
            }
        }

        mDatabase.Save();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
