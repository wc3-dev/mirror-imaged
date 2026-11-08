using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MirrorImaged.GameTests;

/// <summary>
/// A process started in a job of its own, which holds everything the process starts. Ending the job ends them all, and
/// so does closing it, which Windows does when the test host ends however it ends.
/// </summary>
sealed class GameProcess : IDisposable
{
    readonly nint _job;

    GameProcess(nint job, Process process)
    {
        _job = job;
        Process = process;
    }

    public Process Process { get; }

    /// <summary>Starts <paramref name="application"/> in a new job, on <paramref name="desktop"/> when one is named.</summary>
    /// <exception cref="Win32Exception">The job or the process could not be made.</exception>
    public static GameProcess Start(string application, string arguments, string directory, string? desktop)
    {
        var job = Win32.CreateJobObject(0, null);
        if (job == 0)
            throw new Win32Exception();

        try
        {
            var limits = new Win32.ExtendedLimitInformation { LimitFlags = Win32.KillOnJobClose };
            if (!Win32.SetInformationJobObject(job, Win32.JobObjectExtendedLimitInformation, ref limits, Marshal.SizeOf<Win32.ExtendedLimitInformation>()))
                throw new Win32Exception();

            var startup = new Win32.StartupInfo { Size = Marshal.SizeOf<Win32.StartupInfo>(), Desktop = desktop };
            var command = new StringBuilder($"\"{application}\" {arguments}");
            var environment = UsersOwnEnvironment();
            Win32.ProcessInformation started;
            try
            {
                if (!Win32.CreateProcess(
                        application, command, 0, 0, false, Win32.CreateSuspended | Win32.CreateUnicodeEnvironment, environment, directory, ref startup, out started))
                    throw new Win32Exception();
            }
            finally
            {
                Win32.DestroyEnvironmentBlock(environment);
            }

            try
            {
                // Put in the job before it runs, so nothing it starts is outside it.
                if (!Win32.AssignProcessToJobObject(job, started.Process))
                {
                    var error = new Win32Exception();
                    Win32.TerminateProcess(started.Process, 1);
                    throw error;
                }

                var process = Process.GetProcessById(started.ProcessId);
                _ = process.Handle; // held from now on, so the exit code can be read after the process exits
                if (Win32.ResumeThread(started.Thread) == uint.MaxValue)
                {
                    // Closing the job below ends the suspended process.
                    var error = new Win32Exception();
                    process.Dispose();
                    throw error;
                }

                return new GameProcess(job, process);
            }
            finally
            {
                Win32.CloseHandle(started.Thread);
                Win32.CloseHandle(started.Process);
            }
        }
        catch
        {
            Win32.CloseHandle(job);
            throw;
        }
    }

    // The user's environment as a new logon gets it, not the caller's: started from Git Bash's, the game never logs in
    // (docs/facts.md).
    static nint UsersOwnEnvironment()
    {
        if (!Win32.OpenProcessToken(Win32.GetCurrentProcess(), Win32.TokenQueryAndDuplicate, out var token))
            throw new Win32Exception();
        try
        {
            if (!Win32.CreateEnvironmentBlock(out var environment, token, false))
                throw new Win32Exception();
            return environment;
        }
        finally
        {
            Win32.CloseHandle(token);
        }
    }

    /// <summary>The ids of the processes in the job: the process and those it started that still run.</summary>
    public IReadOnlyList<int> Ids()
    {
        var list = new long[256];
        if (!Win32.QueryInformationJobObject(_job, Win32.JobObjectBasicProcessIdList, list, list.Length * sizeof(long), out _))
            return [];
        // JOBOBJECT_BASIC_PROCESS_ID_LIST: two counts in the first eight bytes, the listed one high, then the ids.
        return list[1..(1 + (int)(list[0] >> 32))].Select(id => (int)id).ToList();
    }

    /// <summary>Ends the process and everything in its job, and says what it ended. Never throws.</summary>
    public string Stop()
    {
        try
        {
            var exited = Process.HasExited;
            var running = Ids().Count;
            if (!Win32.TerminateJobObject(_job, 1))
                throw new Win32Exception();
            Process.WaitForExit(10_000);
            return exited
                ? $"the game had exited by itself with 0x{Process.ExitCode:X8}; ended the {running} processes it left"
                : $"ended the game and the {running - 1} processes it started";
        }
        catch (Exception problem) when (problem is Win32Exception or InvalidOperationException)
        {
            return $"could not end the game's job: {problem.Message}";
        }
    }

    public void Dispose()
    {
        Win32.CloseHandle(_job);
        Process.Dispose();
    }
}
