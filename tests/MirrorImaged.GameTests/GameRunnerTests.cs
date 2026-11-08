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

    [Fact]
    public void A_replay_no_probe_run_made_is_copied_aside_once_by_its_write_time_and_never_written_over()
    {
        var replays = Directory.CreateDirectory(Path.Combine(_folder, "BattleNet", "1", "Replays")).FullName;
        var replay = Path.Combine(replays, "LastReplay.w3g");
        File.WriteAllText(replay, "Sven's game");
        File.SetLastWriteTime(replay, new DateTime(2026, 11, 8, 1, 17, 15));
        var list = Path.Combine(_folder, "probe-replays.txt");
        var copy = Path.Combine(replays, "Saved by MirrorImaged", "LastReplay 2026-11-08 011715.w3g");

        Assert.Equal([copy], GameRunner.SaveReplays([replay], list));
        Assert.Equal("Sven's game", File.ReadAllText(copy));

        // Another replay with the same write time leaves the copy as it is.
        File.WriteAllText(replay, "another game");
        File.SetLastWriteTime(replay, new DateTime(2026, 11, 8, 1, 17, 15));
        Assert.Empty(GameRunner.SaveReplays([replay], list));
        Assert.Equal("Sven's game", File.ReadAllText(copy));
    }

    [Fact]
    public void A_replay_a_probe_run_made_is_noted_and_not_copied_and_one_that_did_not_change_is_not_noted()
    {
        var replays = Directory.CreateDirectory(Path.Combine(_folder, "Replays")).FullName;
        var probes = Path.Combine(replays, "LastReplay.w3g");
        var untouched = Path.Combine(replays, "Other.w3g");
        var missing = Path.Combine(replays, "Missing.w3g");
        File.WriteAllText(probes, "Sven's game");
        File.WriteAllText(untouched, "a game");
        var list = Path.Combine(_folder, "probe-replays.txt");
        var before = new List<(string, string?)> { (probes, "BEFORE"), (untouched, Hash(untouched)), (missing, null) };
        File.WriteAllText(probes, "the probe's game");

        GameRunner.NoteProbeReplays(before, list);

        Assert.Equal([Hash(probes)], File.ReadAllLines(list));
        Assert.Empty(GameRunner.SaveReplays([probes, missing], list));
        Assert.False(Directory.Exists(Path.Combine(replays, "Saved by MirrorImaged")));
    }

    static string Hash(string file) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)));

    [WindowsFact]
    public void A_run_is_refused_while_a_Warcraft_III_it_did_not_start_runs()
    {
        // Held while the fake runs, so a game test running at the same time doesn't see it.
        using var turn = new Mutex(false, GameRunner.OneRunAtATime);
        try
        {
            turn.WaitOne();
        }
        catch (AbandonedMutexException)
        {
            // A run ended without letting go; the mutex is this test's now.
        }

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
