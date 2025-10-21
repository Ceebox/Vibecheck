using Vibecheck.Git;
using Vibecheck.Inference;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck.Engine
{
    public sealed class ReviewEngine : IDisposable
    {
        private readonly string mModelUrl;
        private readonly IPatchSource mPatchSource;

        public ReviewEngine(
            string? modelUrl,
            IPatchSource patchSource
        )
        {
            mModelUrl = string.IsNullOrEmpty(modelUrl)
                ? Configuration.Current.InferenceSettings.ModelUrl
                : modelUrl;

            mPatchSource = patchSource;
        }

        public async IAsyncEnumerable<ReviewComment> Review()
        {
            using var activity = Tracing.Start();

            var diffEngine = new PatchDiffer(mPatchSource, Configuration.Current.ReviewSettings.OnlyNewCode);
            var diffs = diffEngine.GetDiffs().ToList();
            var inputCreator = new DiffParser(diffs);
            using var data = await InferenceEngineFactory.LoadModelDataAsync(Configuration.Current.InferenceSettings.ModelUrl);
            using var context = await InferenceEngineFactory.CreateDiffEngineAsync(
                data,
                Configuration.Current.InferenceSettings.SystemPrompt,
                [.. inputCreator.FormatDiffs()]
            );

            var toolContext = context.ToolContext;
            if (toolContext != null)
            {
                toolContext.RepositoryPath = mPatchSource?.PatchRootDirectory;
            }

            await foreach (var response in context.Execute())
            {
                foreach (var comment in ReviewResponseParser.ParseResponse(response))
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
