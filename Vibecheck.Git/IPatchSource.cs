namespace Vibecheck.Git;

public interface IPatchSource
{
    IEnumerable<PatchInfo> GetPatchInfo();
    string? PatchRootDirectory { get; }
}
