using System.Runtime.InteropServices;

namespace SilkWheel.Services;

public static class InputInjector
{
    private const int INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_HWHEEL = 0x01000;

    public static void SendWheel(int delta, bool horizontal)
    {
        if (delta == 0)
        {
            return;
        }

        var input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                mouseData = delta,
                dwFlags = horizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
