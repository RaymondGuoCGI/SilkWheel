using System.Text.Json;
using System.IO;

namespace SilkWheel.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public SettingsStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SilkWheel");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = AppSettings.CreateDefault();
            defaults.EnsureProfiles();
            defaults.EnsureExcludedProcesses();
            return defaults;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath), Options);
            settings ??= AppSettings.CreateDefault();
            settings.EnsureProfiles();
            if (settings.EnsureExcludedProcesses())
            {
                Save(settings);
            }
            return settings;
        }
        catch
        {
            var defaults = AppSettings.CreateDefault();
            defaults.EnsureProfiles();
            defaults.EnsureExcludedProcesses();
            return defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, Options));
    }
}
