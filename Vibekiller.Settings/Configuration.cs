using System.Text.Json;

namespace Vibekiller.Settings;

public static class Configuration
{
    private const string SETTINGS_FILE_NAME = "appsettings.json";
    private static readonly AppSettings sSettings;

    public static AppSettings Current => sSettings;

    static Configuration()
    {
        var defaults = new AppSettings();

        if (!File.Exists(SETTINGS_FILE_NAME))
        {
            sSettings = defaults;
            Save();
            return;
        }

        var json = File.ReadAllText(SETTINGS_FILE_NAME);
        var existing = JsonSerializer.Deserialize(json, typeof(AppSettings), AppSettingsJsonContext.Default) ?? defaults;

        sSettings = Merge(defaults, (AppSettings)existing);

        Save();
    }

    private static void Save()
    {
        var json = JsonSerializer.Serialize(sSettings, AppSettingsJsonContext.Default.AppSettings);
        File.WriteAllText(SETTINGS_FILE_NAME, json);
    }

    private static T Merge<T>(T defaults, T overrides)
    {
        foreach (var prop in typeof(T).GetProperties())
        {
            var defVal = prop.GetValue(defaults);
            var overVal = prop.GetValue(overrides);

            // Handle nested objects recursively
            if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
            {
                if (overVal == null && defVal != null)
                {
                    prop.SetValue(overrides, defVal);
                }
                else if (overVal != null && defVal != null)
                {
                    var merged = Merge(defVal, overVal);
                    prop.SetValue(overrides, merged);
                }
            }
            else if (overVal == null && defVal != null)
            {
                prop.SetValue(overrides, defVal);
            }
        }

        return overrides;
    }
}
