using System.Text.Json;

namespace Vibecheck.Engine;

internal sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    /// <summary>
    /// Convert snake_case to PascalCase to work with <see cref="ReviewComment"/>'s properties.
    /// </summary>
    /// <param name="name">The name to convert to PascalCase.</param>
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var pascal = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
        return pascal;
    }
}

