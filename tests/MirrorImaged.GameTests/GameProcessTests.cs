using System.Diagnostics;

namespace MirrorImaged.GameTests;

public sealed class GameProcessTests
{
    static readonly string System = Environment.SystemDirectory;

    [WindowsFact]
    public void Stop_ends_the_process_and_what_it_started_and_nothing_else()
    {
        using var outside = Process.Start(new ProcessStartInfo(Path.Combine(System, "PING.EXE"), "-n 60 127.0.0.1") { CreateNoWindow = true, UseShellExecute = false })!;
        try
        {
            using var started = StartPingThroughCmd();
            var held = started.Ids().Select(Process.GetProcessById).ToList();

            started.Stop();

            Assert.All(held, process => Assert.True(process.WaitForExit(5_000), $"process {process.Id} still runs"));
            Assert.Empty(started.Ids());
            Assert.False(outside.HasExited);
        }
        finally
        {
            outside.Kill();
            outside.WaitForExit();
        }
    }

    [WindowsFact]
    public void Closing_the_job_ends_what_it_holds()
    {
        var started = StartPingThroughCmd();
        var held = started.Ids().Select(Process.GetProcessById).ToList();

        started.Dispose();

        Assert.All(held, process => Assert.True(process.WaitForExit(5_000), $"process {process.Id} still runs"));
    }

    // cmd starts ping, so the job holds a process the started one started.
    static GameProcess StartPingThroughCmd()
    {
        var started = GameProcess.Start(Path.Combine(System, "cmd.exe"), "/c ping -n 60 127.0.0.1 >nul", System, null);
        var time = Stopwatch.StartNew();
        while (started.Ids().Count < 2 && time.Elapsed < TimeSpan.FromSeconds(10))
            Thread.Sleep(50);
        Assert.True(started.Ids().Count >= 2, "cmd didn't start ping");
        return started;
    }
}
