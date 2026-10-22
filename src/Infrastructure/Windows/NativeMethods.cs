using System;
using System.Runtime.InteropServices;
namespace AniimoAffinities
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr window, int message, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", SetLastError = true)] private static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("kernel32.dll")] private static extern void SetLastError(uint error);
        internal static bool TrySetWindowStyle(IntPtr window, int style)
        {
            SetLastError(0);
            return SetWindowLong(window, -20, style) != 0 || Marshal.GetLastWin32Error() == 0;
        }
        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
    }
}
