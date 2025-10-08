using System.CommandLine;
using Vibekiller.Server;
using Vibekiller.Settings;
using Vibekiller.Utility;

namespace Vibekiller;

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

        var cmd = new Command("server", "Run the Vibekiller server.")
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
        Console.WriteLine($"Starting Vibekiller server on port {port}...");

        var server = new ServerHost();
        await server.Run(port, null);
    }
}
