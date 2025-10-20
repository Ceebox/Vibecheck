using System.Text.Json;

namespace Vibecheck.Utility;

public static class JsonElementExtensions
{
    public static bool GetBoolean(this JsonElement? element) => element?.GetBoolean() ?? false;
    public static string? GetString(this JsonElement? element) => element?.GetString();
    public static double? GetDouble(this JsonElement? element) => element?.GetDouble();

    public static JsonElement? GetProperty(this JsonElement element, string name, bool optional = false)
    {
        using var activity = Tracing.Start();
        activity.SetTag("property.name", name);

        try
        {
            if (element.TryGetProperty(name, out var prop))
            {
                return prop;
            }
        }
        catch (Exception ex)
        {
            activity.AddError(ex);
        }

        if (optional)
        {
            return default;
        }    

        throw new JsonException($"Property '{name}' not found.");
    }
}
