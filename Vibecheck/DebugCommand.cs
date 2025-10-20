using System.CommandLine;
using Vibecheck.Engine;
using Vibecheck.Utility;

namespace Vibecheck;

internal sealed class DebugCommand : CommandBase
{
    public override int Precedence => -1;

    public override Command ToCommand()
    {
        var debugCommand = new Command("debug", "Enter development mode.");
        debugCommand.Aliases.Add("d");
        debugCommand.SetAction(async _ =>
        {
            Tracing.SetDebug();

            await CommandHelpers.CreatRootCommand().Parse(["-h"]).InvokeAsync();
            Console.Write("\nEnter command: ");
            var newLine = Console.ReadLine();
            if (string.IsNullOrEmpty(newLine))
            {
                return;
            }

            var newArgs = newLine.Split(" ");
            if (newArgs.Length >= 2)
            {
                // We're really debugging now!
                if (newArgs[0] == "notification")
                {
                    var notificationProvider = NotificationProviderFactory.GetNotificationProvider();
                    notificationProvider?.SendNotification(string.Join(' ', newArgs[1..]));
                    return;
                }
            }

            await Program.Run(newArgs);
        });

        var levelCommand = new Command("level", "Sets the tracing log level.");
        var levelArg = new Argument<LogLevel>("level")
        {
            Description = "The log level to use."
        };

        levelCommand.Add(levelArg);
        levelCommand.SetAction(async parsedArgs =>
        {
            var level = parsedArgs.GetValue(levelArg);
            Tracing.SetDebug(level);

            Tracing.WriteLine($"Log level set to: {level}", level);
            await Task.CompletedTask;
        });

        debugCommand.Add(levelCommand);

        return debugCommand;
    }
}
