using System.Text.Json.Serialization;

namespace Vibekiller.Settings;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(InferenceSettings))]
[JsonSerializable(typeof(SamplingSettings))]
[JsonSerializable(typeof(GitSettings))]
[JsonSerializable(typeof(ServerSettings))]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}
