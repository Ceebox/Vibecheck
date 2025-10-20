namespace Vibecheck.Inference.Tools;

[AttributeUsage(AttributeTargets.Method)]
public class ToolMethodAttribute : Attribute
{
    /// <summary>
    /// Description of the tool method for the AI's context.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Optional user-defined decorator to determine if the method should be shown to the AI.
    /// Takes the current ToolContext and returns true if the method should be included.
    /// Must implement <see cref="IToolAvailability"/>.
    /// </summary>
    public Type? AvailabilityType { get; init; }
}
