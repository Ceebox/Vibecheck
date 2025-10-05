using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vibekiller.Settings;

public static class Configuration
{
    private const string SETTINGS_FILE_NAME = "appsettings.json";
    private static AppSettings sSettings = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static Configuration()
    {
        // Static constructor so we don't have to create it wherever we are
        Load();
    }

    public static AppSettings Current => sSettings;

    private static void Load()
    {
        var defaults = new AppSettings();

        if (!File.Exists(SETTINGS_FILE_NAME))
        {
            // Create one from defaults
            sSettings = defaults;
            Save();
            return;
        }

        var json = File.ReadAllText(SETTINGS_FILE_NAME);
        var existing = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? defaults;

        sSettings = Merge(defaults, existing);

        // Save again to ensure new fields are written back
        Save();
    }

    private static void Save()
    {
        var json = JsonSerializer.Serialize(sSettings, JsonOptions);
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
