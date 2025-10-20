namespace Vibecheck.Utility;

public static class FileHelper
{
    public static IEnumerable<string> GetCodeFilePaths(string folderPath, IEnumerable<string> extensions)
    {
        foreach (var file in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories))
        {
            if (extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }
}
