namespace Vibekiller.Git;

public record LineChange
{
    public string FilePath { get; init; }
    public int? OldLine { get; init; }
    public int? NewLine { get; init; }
    public string ChangeType { get; init; }
    public string Content { get; init; }

    public LineChange(string filePath, int? oldLine, int? newLine, string changeType, string content)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        OldLine = oldLine;
        NewLine = newLine;
        ChangeType = changeType ?? throw new ArgumentNullException(nameof(changeType));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}

