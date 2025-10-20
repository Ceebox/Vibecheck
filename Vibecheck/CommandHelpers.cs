using System.CommandLine;
using System.Reflection;

namespace Vibecheck;

internal static class CommandHelpers
{
    public static RootCommand CreatRootCommand()
    {
        var rootCommand = new RootCommand("Vibecheck CLI");
        var cmds = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(CommandBase).IsAssignableFrom(t))
            .Select(t => Activator.CreateInstance(t) as CommandBase)
            .Where(cmd => cmd != null)
            .OrderByDescending(cmd => cmd!.Precedence)
            .ToList();

        foreach (var cmd in cmds)
        {
            rootCommand.Subcommands.Add(cmd!);
        }

        return rootCommand;
    }
}
