using System.CommandLine;

namespace Vibecheck;

internal abstract class CommandBase
{
    public static implicit operator Command(CommandBase command)
        => command.ToCommand();

    public virtual int Precedence { get; } = 0;

    public abstract Command ToCommand();
}
