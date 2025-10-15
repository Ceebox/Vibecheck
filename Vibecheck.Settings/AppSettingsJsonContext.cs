using System.Text.Json.Serialization;

namespace Vibecheck.Settings;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(InferenceSettings))]
[JsonSerializable(typeof(SamplingSettings))]
[JsonSerializable(typeof(GitSettings))]
[JsonSerializable(typeof(ToolSettings))]
[JsonSerializable(typeof(ReviewSettings))]
[JsonSerializable(typeof(ServerSettings))]
[JsonSerializable(typeof(WatcherSettings))]
[JsonSerializable(typeof(WatcherNotificationSettings))]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}
