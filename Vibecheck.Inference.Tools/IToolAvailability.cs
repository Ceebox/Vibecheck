namespace Vibecheck.Inference.Tools;

public interface IToolAvailability
{
    bool IsAvailable(ToolContext ctx);
}