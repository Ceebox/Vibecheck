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
    private readonly ModelData mEmbedderData;
    private readonly string mRootFolder;
    private readonly string mDatabasePath;

    public VectorDatabaseInterface(string rootFolder, ModelData embedderData)
    {
        if (string.IsNullOrEmpty(rootFolder))
        {
            // Catastrophically fail before the user indexes their root drive (that would be suboptimal)
            throw new InvalidOperationException("Invalid git repository");
        }

        mEmbedderData = embedderData;
        mRootFolder = rootFolder;

        var databaseCacheDir = Path.Combine(AppContext.BaseDirectory, DATABASE_FOLDER);
        if (!Directory.Exists(databaseCacheDir))
        {
            Directory.CreateDirectory(databaseCacheDir);
        }

        mDatabasePath = Path.Combine(databaseCacheDir, Path.GetFileName(rootFolder) + ".json");
        mDatabase = new VectorDatabase(mDatabasePath);
    }

    public async Task VectoriseAsync()
    {
        using var activity = Tracing.Start();

        if (!Directory.Exists(mRootFolder))
            throw new DirectoryNotFoundException(mRootFolder);

        var files = Directory.EnumerateFiles(
            mRootFolder,
            "*.*",
            SearchOption.AllDirectories
        ).Where(f =>
        {
            var relative = Path.GetRelativePath(mRootFolder, f);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Skip if any folder in the path matches an excluded folder
            if (parts.Take(parts.Length - 1).Any(p => Configuration.Current.InferenceSettings.VectorDatabaseSettings.ExcludedFolders.Contains(p)))
                return false;

            // Optionally: filter by extension
            return Configuration.Current.InferenceSettings.VectorDatabaseSettings
                                .IncludedFileTypes.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase);
        }).ToList();

        // TODO: Maybe do some fancy loading indicator, with the count
        foreach (var file in files)
        {
            Tracing.WriteLine($"Indexing {file}", LogLevel.INFO);

            try
            {
                var text = await File.ReadAllTextAsync(file, Encoding.UTF8);

                // We need to recreate the context per-file
                var embedding = await InferenceFactory.CreateEmbedder(mEmbedderData).GetEmbeddings(text);
                if (!embedding.Any())
                {
                    // Store relative path as ID to ensure uniqueness
                    var relativePath = Path.GetRelativePath(mRootFolder, file);
                    var record = new VectorRecord(

                        relativePath,
                        file,
                        text,
                        // Pick the first, since in theory, we only have 1 (PoolingType.Mean)
                        embedding[0]
                    );

                    await mDatabase.AddAsync(record);
                }
            }
            catch (Exception ex)
            {
                activity.AddError(ex);
            }
        }

        Tracing.WriteLine($"Saving database to {mDatabasePath}.", LogLevel.INFO);
        mDatabase.Save();

        Tracing.WriteLine($"Indexing complete.", LogLevel.INFO);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
