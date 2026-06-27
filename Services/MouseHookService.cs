using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace SilkWheel.Services;

public sealed class MouseHookService : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const int HC_ACTION = 0;
    private const int LLMHF_INJECTED = 0x00000001;

    private readonly AppSettings _settings;
    private readonly ScrollEngine _engine;
    private readonly LowLevelMouseProc _proc;
    private IntPtr _hook;

    public MouseHookService(AppSettings settings, ScrollEngine engine)
    {
        _settings = settings;
        _engine = engine;
        _proc = HookCallback;
    }

    public void Start()
    {
        using var current = Process.GetCurrentProcess();
        using var module = current.MainModule;
        _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(module?.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == HC_ACTION && _settings.Enabled)
        {
            var message = wParam.ToInt32();
            if (message is WM_MOUSEWHEEL or WM_MOUSEHWHEEL)
            {
                var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                if ((info.flags & LLMHF_INJECTED) == 0 && !IsExcludedForegroundProcess())
                {
                    var horizontal = message == WM_MOUSEHWHEEL;
                    if (message == WM_MOUSEWHEEL && _settings.HorizontalShiftKey && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        horizontal = true;
                    }

                    if (horizontal && !_settings.HorizontalSmoothing)
                    {
                        return CallNextHookEx(_hook, nCode, wParam, lParam);
                    }

                    var delta = unchecked((short)((info.mouseData >> 16) & 0xffff));
                    _engine.Enqueue(delta, horizontal);
                    return new IntPtr(1);
                }
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool IsExcludedForegroundProcess()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(foreground, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName + ".exe";
            var path = SafeGetPath(process);
            return _settings.ExcludedProcesses.Any(rule =>
                string.Equals(Path.GetFileName(rule), processName, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(path) && string.Equals(rule, path, StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            return false;
        }
    }

    private static string SafeGetPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
