namespace Vibecheck.Inference.Data;

public sealed class VectorRecord
{
    public string Id { get; }
    public string FilePath { get; }
    public string Text { get; }
    public float[] Embedding { get; }

    public VectorRecord(string id, string filePath, string text, float[] embedding)
    {
        Id = id;
        FilePath = filePath;
        Text = text;
        Embedding = embedding;
    }
}
