using System.CommandLine;

namespace Vibecheck;

internal abstract class CommandBase
{
    public static implicit operator Command(CommandBase command)
        => command.ToCommand();

    public abstract Command ToCommand();
}
