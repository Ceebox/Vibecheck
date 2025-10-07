using Vibekiller.Git;
using Vibekiller.Inference;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Engine
{
    public sealed class ReviewEngine : IDisposable
    {
        private readonly string mRepoPath;
        private readonly string mSourceBranch;
        private readonly string mTargetBranch;
        private readonly int mSourceOffset;
        private readonly int mTargetOffset;
        private readonly string mModelUrl;

        public ReviewEngine(
            string? repoPath,
            string? modelUrl,
            string? sourceBranch,
            string? targetBranch,
            int? sourceOffset,
            int? targetOffset
        )
        {
            mRepoPath = string.IsNullOrEmpty(repoPath)
                ? string.Empty
                : repoPath;
            mModelUrl = string.IsNullOrEmpty(modelUrl)
                ? Configuration.Current.InferenceSettings.ModelUrl
                : modelUrl;
            mSourceBranch = string.IsNullOrEmpty(sourceBranch)
                ? Configuration.Current.GitSettings.GitSourceBranch
                : sourceBranch;
            mTargetBranch = string.IsNullOrEmpty(targetBranch)
                ? Configuration.Current.GitSettings.GitTargetBranch
                : targetBranch;
            mSourceOffset = sourceOffset.HasValue
                ? sourceOffset!.Value
                : Configuration.Current.GitSettings.GitSourceCommitOffset;
            mTargetOffset = targetOffset.HasValue
                ? targetOffset!.Value
                : Configuration.Current.GitSettings.GitTargetCommitOffset;
        }

        public async IAsyncEnumerable<ReviewComment> Review()
        {
            using var activity = Tracing.Start();

            var diffEngine = new BranchDiffer(
                mRepoPath,
                mSourceBranch,
                mTargetBranch,
                mSourceOffset,
                mTargetOffset
            );

            var diffs = diffEngine.GetBranchDiffs().ToList();
            var inputCreator = new DiffParser(diffs);
            var inferenceResultParser = new ReviewResponseParser();
            var context = new DiffEngine(
                mModelUrl,
                Configuration.Current.InferenceSettings.SystemPrompt,
                [.. inputCreator.FormatDiffs()]
            );
            
            await foreach (var response in context.Execute())
            {
                foreach (var comment in inferenceResultParser.ParseResponse(response))
                {
                    yield return comment;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
