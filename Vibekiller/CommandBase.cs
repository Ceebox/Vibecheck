using System.CommandLine;

namespace Vibekiller;

internal abstract class CommandBase
{
    public static implicit operator Command(CommandBase command)
        => command.ToCommand();

    public abstract Command ToCommand();
}
