using System.Text.Json.Serialization;

namespace Vibekiller.Settings;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(InferenceSettings))]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}
