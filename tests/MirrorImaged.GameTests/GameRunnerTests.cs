using System.Diagnostics;

namespace MirrorImaged.GameTests;

/// <summary>A test of the runner's Windows parts; skipped elsewhere.</summary>
public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows only";
    }
}

public sealed class GameRunnerTests : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    [Fact]
    public void A_map_is_on_its_loading_screen_from_its_loaded_mark_until_its_started_mark()
    {
        var loaded = Path.Combine(_folder, "loaded.pld");
        var started = Path.Combine(_folder, "started.pld");

        Assert.False(GameRunner.OnLoadingScreen(loaded, started));
        File.WriteAllText(loaded, "");
        Assert.True(GameRunner.OnLoadingScreen(loaded, started));
        File.WriteAllText(started, "");
        Assert.False(GameRunner.OnLoadingScreen(loaded, started));
    }

    [Theory]
    [InlineData(true, true, true, true, ProbeState.Done)]
    [InlineData(false, true, true, true, ProbeState.NoOutput)]
    [InlineData(false, true, true, false, ProbeState.NoOutput)]
    [InlineData(false, false, true, false, ProbeState.AtLogin)]
    [InlineData(false, false, true, true, ProbeState.Queued)]
    [InlineData(false, false, false, false, ProbeState.Queued)]
    public void A_run_ends_by_how_far_the_game_got(bool lines, bool loaded, bool stillRunning, bool loggedIn, ProbeState ending)
    {
        Assert.Equal(ending, GameRunner.Ending(lines, loaded, stillRunning, loggedIn));
    }

    [Fact]
    public void A_map_path_is_new_each_run_and_no_longer_than_the_game_loads()
    {
        var first = GameRunner.MapPath(_folder, "hello");
        var second = GameRunner.MapPath(_folder, "hello");
        var longest = new string('a', GameRunner.LongestMapPath - Path.GetFullPath(_folder).Length - 1 - 9 - 4);

        Assert.NotEqual(first, second);
        Assert.Matches(@"[\\/]hello-[0-9a-f]{8}\.w3x\z", first);
        Assert.Equal(GameRunner.LongestMapPath, GameRunner.MapPath(_folder, longest).Length);
        Assert.Throws<ArgumentException>(() => GameRunner.MapPath(_folder, longest + "a"));
    }

    [WindowsFact]
    public void A_run_is_refused_while_a_Warcraft_III_it_did_not_start_runs()
    {
        // Held while the fake runs, so a game test running at the same time doesn't see it.
        using var turn = new Mutex(false, GameRunner.OneRunAtATime);
        turn.WaitOne();
        var other = Path.Combine(_folder, "Warcraft III.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "PING.EXE"), other);
        using var running = Process.Start(new ProcessStartInfo(other, "-n 60 127.0.0.1") { CreateNoWindow = true, UseShellExecute = false })!;
        try
        {
            var runner = new GameRunner(Path.Combine(_folder, "game.exe"), Path.Combine(_folder, "Warcraft III"), Path.Combine(_folder, "maps"));

            Assert.Throws<InvalidOperationException>(() => runner.Run("hello", "done()", TimeSpan.FromSeconds(1)));
            Assert.False(Directory.Exists(Path.Combine(_folder, "maps")));
            Assert.False(running.HasExited);
        }
        finally
        {
            running.Kill();
            running.WaitForExit();
            turn.ReleaseMutex();
        }
    }
}
