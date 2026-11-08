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
    public void Kept_files_are_put_back_as_they_were()
    {
        var changed = Path.Combine(_folder, "LastReplay.w3g");
        var same = Path.Combine(_folder, "Classic.w3p");
        var made = Path.Combine(_folder, "TempReplay.w3g");
        var removed = Path.Combine(_folder, "War3Preferences.txt");
        File.WriteAllText(changed, "Sven's replay");
        File.WriteAllText(same, "profile");
        File.WriteAllText(removed, "preferences");
        var kept = GameRunner.Keep([changed, same, made, removed]);

        File.WriteAllText(changed, "the probe's replay");
        File.WriteAllText(same, "profile");
        File.WriteAllText(made, "");
        File.Delete(removed);
        var report = GameRunner.Restore(kept);

        Assert.Equal("Sven's replay", File.ReadAllText(changed));
        Assert.Equal("profile", File.ReadAllText(same));
        Assert.False(File.Exists(made));
        Assert.Equal("preferences", File.ReadAllText(removed));
        Assert.Equal([$"put back {changed}", $"put back {made}", $"put back {removed}"], report);
    }

    [WindowsFact]
    public void A_run_is_refused_while_a_Warcraft_III_it_did_not_start_runs()
    {
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
        }
    }
}
