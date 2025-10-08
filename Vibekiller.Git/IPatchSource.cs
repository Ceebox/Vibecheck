namespace Vibekiller.Git;

public interface IPatchSource
{
    IEnumerable<PatchInfo> GetPatchInfo();
}
