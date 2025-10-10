namespace Vibecheck.Git;

public interface IPatchSource
{
    IEnumerable<PatchInfo> GetPatchInfo();
}
