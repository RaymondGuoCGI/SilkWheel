using System.Text.Json;
using System.IO;

namespace SilkWheel.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public SettingsStore() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SilkWheel",
        "settings.json"))
    {
    }

    internal SettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new ArgumentException("A settings directory is required.", nameof(settingsPath));
        Directory.CreateDirectory(directory);
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

        AppSettings settings;
        bool settingsMigrated;
        try
        {
            settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath), Options)
                ?? AppSettings.CreateDefault();
            settings.EnsureProfiles();
            settingsMigrated = settings.EnsureExcludedProcesses();
            settingsMigrated |= settings.NormalizeScrollRanges();
        }
        catch
        {
            var defaults = AppSettings.CreateDefault();
            defaults.EnsureProfiles();
            defaults.EnsureExcludedProcesses();
            return defaults;
        }

        if (settingsMigrated)
        {
            try
            {
                Save(settings);
            }
            catch
            {
                // Loading succeeded, so keep the normalized in-memory settings
                // even when a read-only or temporarily locked file cannot be migrated.
            }
        }

        return settings;
    }

    public void Save(AppSettings settings)
    {
        var temporaryPath = $"{_settingsPath}.{Environment.ProcessId}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, Options));
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch
            {
            }
        }
    }
}
