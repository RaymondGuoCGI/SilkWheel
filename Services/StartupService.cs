using Microsoft.Win32;
using System.Diagnostics;

namespace SilkWheel.Services;

public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SilkWheel";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null)
        {
            return;
        }

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            key.SetValue(ValueName, $"\"{exe}\" --background");
        }
    }
}
