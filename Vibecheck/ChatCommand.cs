using System.CommandLine;
using Vibecheck.Engine;
using Vibecheck.Inference;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck;

internal sealed class ChatCommand : CommandBase
{
    public override Command ToCommand()
    {
        using var activity = Tracing.Start();

        var cmd = new Command("chat", "Are you feeling a bit lonely? Chat with your chosen model.");

        cmd.SetAction(async parsedArgs =>
        {
            await ExecuteAsync();
        });

        return cmd;
    }

    private static async Task ExecuteAsync()
    {
        using var activity = Tracing.Start();
        using var engine = await InferenceEngineFactory.CreateChatEngine(
            Configuration.Current.InferenceSettings.ModelUrl,
            Configuration.Current.InferenceSettings.SystemPrompt
        );

        await engine.Execute();
    }
}
