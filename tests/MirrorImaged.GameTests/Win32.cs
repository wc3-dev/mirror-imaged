using System.Runtime.InteropServices;
using System.Text;

namespace MirrorImaged.GameTests;

static class Win32
{
    // DESKTOP_READOBJECTS to list its windows, and DESKTOP_CREATEWINDOW, without which CreateDesktop fails with error 8.
    public const uint DesktopRights = 0x1 | 0x2;
    public const int CreateSuspended = 0x4, CreateUnicodeEnvironment = 0x400;
    public const uint TokenQueryAndDuplicate = 0x8 | 0x2;
    public const int JobObjectBasicProcessIdList = 3, JobObjectExtendedLimitInformation = 9;
    public const uint KillOnJobClose = 0x2000;
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

    // JOBOBJECT_EXTENDED_LIMIT_INFORMATION, with its basic limits and I/O counters laid out in place.
    [StructLayout(LayoutKind.Sequential)]
    public struct ExtendedLimitInformation
    {
        public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize, MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass, SchedulingClass;
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount;
        public nuint ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateDesktop(string name, nint device, nint mode, int flags, uint access, nint attributes);

    [DllImport("user32")]
    public static extern bool CloseDesktop(nint desktop);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateProcess(
        string application, StringBuilder commandLine, nint processAttributes, nint threadAttributes, bool inheritHandles,
        int flags, nint environment, string? directory, ref StartupInfo startup, out ProcessInformation information);

    [DllImport("kernel32", SetLastError = true)]
    public static extern uint ResumeThread(nint thread);

    [DllImport("kernel32")]
    public static extern nint GetCurrentProcess();

    [DllImport("advapi32", SetLastError = true)]
    public static extern bool OpenProcessToken(nint process, uint access, out nint token);

    [DllImport("userenv", SetLastError = true)]
    public static extern bool CreateEnvironmentBlock(out nint environment, nint token, bool inherit);

    [DllImport("userenv")]
    public static extern bool DestroyEnvironmentBlock(nint environment);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool TerminateProcess(nint process, uint exitCode);

    [DllImport("kernel32")]
    public static extern bool CloseHandle(nint handle);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateJobObject(nint attributes, string? name);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool SetInformationJobObject(nint job, int infoClass, ref ExtendedLimitInformation information, int length);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool AssignProcessToJobObject(nint job, nint process);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool QueryInformationJobObject(nint job, int infoClass, long[] information, int length, out int returned);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool TerminateJobObject(nint job, uint exitCode);

    [DllImport("user32")]
    public static extern bool EnumDesktopWindows(nint desktop, EnumWindowsProc callback, nint parameter);

    [DllImport("user32")]
    public static extern bool EnumChildWindows(nint parent, EnumWindowsProc callback, nint parameter);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(nint window, StringBuilder text, int capacity);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(nint window, StringBuilder name, int capacity);

    [DllImport("user32")]
    public static extern int GetWindowThreadProcessId(nint window, out int processId);

    [DllImport("user32")]
    public static extern bool IsWindowVisible(nint window);

    [DllImport("user32")]
    public static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
