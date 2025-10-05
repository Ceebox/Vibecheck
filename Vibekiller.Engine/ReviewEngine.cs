using Vibekiller.Inference;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller.Engine
{
    public sealed class ReviewEngine : IDisposable
    {
        private readonly string mModelUrl;

        public ReviewEngine(string modelUrl)
        {
            mModelUrl = modelUrl;
        }

        public async Task Run()
        {
            using var activity = Tracing.Start();

            // Prepare system prompt
            var context = new InferenceContext(mModelUrl, Configuration.Current.InferenceSettings.SystemPrompt);
            await context.Load();
        }

        public void Dispose()
        {
        }
    }
}
