using Vibekiller.Git;
using Vibekiller.Inference;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Engine
{
    public sealed class ReviewEngine : IDisposable
    {
        private readonly string mRepoPath;
        private readonly string mTargetBranch;
        private readonly string mModelUrl;

        public ReviewEngine(string? repoPath, string? targetBranch, string? modelUrl)
        {
            mRepoPath = string.IsNullOrEmpty(repoPath)
                ? string.Empty
                : repoPath;
            mTargetBranch = string.IsNullOrEmpty(targetBranch)
                ? Configuration.Current.GitSettings.GitTargetBranch
                : targetBranch;
            mModelUrl = string.IsNullOrEmpty(modelUrl)
                ? Configuration.Current.InferenceSettings.ModelUrl
                : modelUrl;
        }

        public async IAsyncEnumerable<ReviewComment> Review()
        {
            using var activity = Tracing.Start();

            var diffEngine = new BranchDiffer(mRepoPath, mTargetBranch);
            var diffs = diffEngine.GetBranchDiffs();
            var inputCreator = new DiffParser(diffs);
            var inferenceResultParser = new ReviewResponseParser();
            var context = new DiffEngine(
                mModelUrl,
                Configuration.Current.InferenceSettings.SystemPrompt,
                inputCreator.FormatDiffs()
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
