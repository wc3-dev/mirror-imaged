using System.Runtime.InteropServices;
using System.Text;

namespace MirrorImaged.GameTests;

static class Win32
{
    public const uint GenericAll = 0x10000000;
    public const uint KeyDown = 0x100, KeyUp = 0x101;
    public const int Space = 0x20;

    public delegate bool EnumWindowsProc(nint window, nint parameter);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct StartupInfo
    {
        public int Size;
        public string? Reserved;
        public string? Desktop;
        public string? Title;
        public int X, Y, Width, Height, CountCharsX, CountCharsY, FillAttribute, Flags;
        public short ShowWindow, Reserved2Size;
        public nint Reserved2, StdInput, StdOutput, StdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessInformation
    {
        public nint Process, Thread;
        public int ProcessId, ThreadId;
    }

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateDesktop(string name, nint device, nint mode, int flags, uint access, nint attributes);

    [DllImport("user32")]
    public static extern bool CloseDesktop(nint desktop);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateProcess(
        string application, StringBuilder commandLine, nint processAttributes, nint threadAttributes, bool inheritHandles,
        int flags, nint environment, string? directory, ref StartupInfo startup, out ProcessInformation information);

    [DllImport("kernel32")]
    public static extern bool CloseHandle(nint handle);

    [DllImport("user32")]
    public static extern bool EnumDesktopWindows(nint desktop, EnumWindowsProc callback, nint parameter);

    [DllImport("user32")]
    public static extern bool EnumChildWindows(nint parent, EnumWindowsProc callback, nint parameter);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(nint window, StringBuilder text, int capacity);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(nint window, StringBuilder name, int capacity);

    [DllImport("user32")]
    public static extern bool IsWindowVisible(nint window);

    [DllImport("user32")]
    public static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
