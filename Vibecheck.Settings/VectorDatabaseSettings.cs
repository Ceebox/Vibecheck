namespace Vibecheck.Settings;

public sealed class VectorDatabaseSettings
{
    public string[] IncludedFileTypes { get; set; } =
    [
        ".c", ".cpp", ".cc", ".cxx", ".h", ".hpp", ".hh", ".hxx",
        ".cs", ".m", ".mm", ".swift", ".go", ".rs", ".ml", ".hs", ".lhs",
        ".vb", ".fs", ".fsi", ".fsx", ".fsscript",
        ".java", ".kt", ".kts", ".xml", ".xaml", ".json",
        ".js", ".mjs", ".cjs", ".jsx", ".ts", ".mts", ".cts", ".tsx",
        ".py", ".pyi", ".pyc", ".r", ".rb", ".pl", ".pm",
        ".html", ".htm", ".xhtml", ".css", ".scss", ".less", ".vue", ".svelte",
        ".sh", ".bash", ".zsh", ".bat", ".cmd", ".ps1", ".psm1",
        ".sql", ".psql", ".ddl", ".dml",
        ".json", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".sqf",
        ".lua", ".rake", ".make", ".mk", ".gradle", ".dockerfile", ".gitignore"
    ];

    public string[] ExcludedFolders { get; set; } = [
        "bin",
        "obj",
        ".git"
    ];
}
