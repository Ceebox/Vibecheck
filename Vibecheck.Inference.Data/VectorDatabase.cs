using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text.Json;
using Vibecheck.Utility;

namespace Vibecheck.Inference.Data;

/// <summary>
/// A basic, naiive implementation of a hybrid vector database.
/// </summary>
public sealed class VectorDatabase
{
    private static readonly JsonSerializerOptions sOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string mJsonPath;
    private readonly string mVecsPath;
    private readonly List<VectorRecord> mRecords = [];
    private readonly Dictionary<string, (long Offset, int Length)> mIndex = [];

    public VectorDatabase(string path)
    {
        mJsonPath = Path.ChangeExtension(path, ".json");
        mVecsPath = Path.ChangeExtension(path, ".vecs");

        if (File.Exists(mJsonPath))
        {
            var json = File.ReadAllText(mJsonPath);
            var data = JsonSerializer.Deserialize<List<VectorRecord>>(json);
            if (data is not null)
            {
                mRecords = data;
                this.BuildIndex();
            }
        }
    }

    public async Task AddAsync(VectorRecord record)
    {
        using var activity = Tracing.Start();
        activity.AddTag("vector.filepath", record.FilePath);
        activity.AddTag("vector.id", record.Id);

        var existing = mRecords.FindIndex(r => r.Id == record.Id);

        if (existing >= 0)
        {
            mRecords[existing] = record;

            if (mIndex.TryGetValue(record.Id, out var idx))
            {
                await using var fs = new FileStream(
                    mVecsPath,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true
                );

                fs.Position = idx.Offset;
                var buffer = MemoryMarshal.Cast<float, byte>(record.Embedding.AsSpan()).ToArray();
                await fs.WriteAsync(buffer.AsMemory());
                return;
            }
        }
        else
        {
            mRecords.Add(record);

            await using var fs = new FileStream(
                mVecsPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true
            );

            var offset = fs.Position;
            var buffer = MemoryMarshal.Cast<float, byte>(record.Embedding.AsSpan()).ToArray();
            await fs.WriteAsync(buffer.AsMemory());

            mIndex[record.Id] = (offset, buffer.Length);
        }
    }

    public void Save()
    {
        using var activity = Tracing.Start();

        var json = JsonSerializer.Serialize(mRecords, sOptions);
        File.WriteAllText(mJsonPath, json);
    }

    public VectorRecord? Get(string id)
        => mRecords.FirstOrDefault(r => r.Id == id);

    // TODO: This might be reeeeeally dumb
    public unsafe IEnumerable<VectorRecord> Search(ReadOnlySpan<float> queryEmbedding, int topK = 3)
    {
        using var activity = Tracing.Start();

        var scored = new List<(VectorRecord Record, float Score)>(mRecords.Count);

        using var mmf = MemoryMappedFile.CreateFromFile(mVecsPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        var handle = accessor.SafeMemoryMappedViewHandle;

        foreach (var r in mRecords)
        {
            if (!mIndex.TryGetValue(r.Id, out var idx))
            {
                continue;
            }

            var floatsLength = idx.Length / sizeof(float);

            byte* ptrBase = null;
            handle.AcquirePointer(ref ptrBase);

            try
            {
                // Pointer to the start of the embedding
                float* vectorPtr = (float*)(ptrBase + idx.Offset);
                var embSpan = new Span<float>(vectorPtr, floatsLength);

                var score = CosineSimilarity(queryEmbedding, embSpan);
                scored.Add((r, score));
            }
            finally
            {
                handle.ReleasePointer();
            }
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Record);
    }

    private void BuildIndex()
    {
        using var activity = Tracing.Start();

        mIndex.Clear();
        var offset = 0L;
        if (File.Exists(mVecsPath))
        {
            foreach (var r in mRecords)
            {
                var length = r.Embedding.Length * sizeof(float);
                mIndex[r.Id] = (offset, length);
                offset += length;
            }
        }
    }

    // TODO: Allow choosing between different algorithms with an enum
    private static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
        {
            return 0f;
        }

        var dot = 0f;
        var magA = 0f;
        var magB = 0f;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB) + 1e-8f);
    }
}