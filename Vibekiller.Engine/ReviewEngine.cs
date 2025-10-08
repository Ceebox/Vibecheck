using Vibekiller.Git;
using Vibekiller.Inference;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Engine
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

            var diffEngine = new PatchDiffer(mPatchSource);
            var diffs = diffEngine.GetDiffs().ToList();
            var inputCreator = new DiffParser(diffs);
            using var context = await InferenceEngineFactory.CreateDiffEngine(
                mModelUrl,
                Configuration.Current.InferenceSettings.SystemPrompt,
                [.. inputCreator.FormatDiffs()]
            );
            
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
