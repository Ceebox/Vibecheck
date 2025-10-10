using System.CommandLine;
using Vibecheck.Server;
using Vibecheck.Settings;
using Vibecheck.Utility;

namespace Vibecheck;

internal class ServerCommand : CommandBase
{
    public override Command ToCommand()
    {
        using var activity = Tracing.Start();

        var portOption = new Option<int>("--port")
        {
            Description = "The port to host the server on.",
            DefaultValueFactory = _ => Configuration.Current.ServerSettings.Port
        };

        var cmd = new Command("server", "Run the Vibecheck server.")
        {
            portOption
        };

        cmd.SetAction(async parsedArgs =>
        {
            var port = parsedArgs.GetValue(portOption);
            await RunServer(port);
        });

        return cmd;
    }

    private static async Task RunServer(int port)
    {
        using var activity = Tracing.Start();
        Console.WriteLine($"Starting Vibecheck server on port {port}...");

        var server = new ServerHost();
        await server.Run(port, null);
    }
}
