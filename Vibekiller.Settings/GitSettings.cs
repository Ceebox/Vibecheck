namespace Vibekiller.Settings;
public sealed class GitSettings
{
    public string GitSourceBranch { get; set; } = "HEAD";
    public string GitTargetBranch { get; set; } = "main";
    public int GitSourceCommitOffset { get; set; } = 0;
    public int GitTargetCommitOffset { get; set; } = 0;
}
