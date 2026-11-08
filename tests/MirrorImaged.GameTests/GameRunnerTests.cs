using MirrorImaged.Probes;
using War3Net.Build;

namespace MirrorImaged.GameTests;

public sealed class GameRunnerTests : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    readonly GameRunner _runner;

    public GameRunnerTests() =>
        _runner = new GameRunner(Path.Combine(_folder, "game.exe"), Path.Combine(_folder, "Warcraft III"), Path.Combine(_folder, "maps"));

    public void Dispose() => Directory.Delete(_folder, true);

    // The shape of War3Log.txt on 3.0.1.24371: at start the game opens every map under Documents\Warcraft III\Maps, then
    // activates the WebUI, then opens the map it was started on.
    const string Log =
        "11/8 01:16:27.685  GameMain Started\n" +
        "11/8 01:16:27.685  Command Line: \"C:\\Program Files (x86)\\Warcraft III\\_retail_\\x86_64\\Warcraft III.exe\" -launch -windowmode windowed -loadfile C:\\maps\\hello-1.w3x\n" +
        "11/8 01:16:28.175  Opening mod - War3.w3mod\n" +
        "11/8 01:16:30.012  Opening map - C:/Users/x/Documents/Warcraft III/Maps/scan.w3x\n" +
        "11/8 01:16:34.192  Activating WebUI\n";

    [Fact]
    public void A_map_counts_as_opened_only_after_the_WebUI()
    {
        Assert.False(GameRunner.Opened(Log, @"C:\maps\hello-1.w3x"));
        Assert.False(GameRunner.Opened(Log, @"C:\Users\x\Documents\Warcraft III\Maps\scan.w3x"));
        Assert.True(GameRunner.Opened(Log + "11/8 01:16:37.087  Opening map - C:/maps/hello-1.w3x\n", @"C:\maps\hello-1.w3x"));
        Assert.True(GameRunner.Opened(Log + "11/8 01:16:37.087  Opening map - c:/MAPS/hello-1.w3x\r\n", @"C:\maps\hello-1.w3x"));
        Assert.False(GameRunner.Opened(Log + "11/8 01:16:37.087  Opening map - C:/maps/hello-10.w3x\n", @"C:\maps\hello-1.w3x"));
    }

    [Fact]
    public void A_map_is_on_its_loading_screen_from_its_loaded_mark_until_a_newer_started_mark()
    {
        var loaded = Path.Combine(_folder, "loaded.pld");
        var started = Path.Combine(_folder, "started.pld");
        var time = new DateTime(2026, 11, 8, 3, 0, 0, DateTimeKind.Utc);

        Assert.False(GameRunner.OnLoadingScreen(loaded, started));
        File.WriteAllText(loaded, "");
        Assert.True(GameRunner.OnLoadingScreen(loaded, started));
        File.WriteAllText(started, "");
        File.SetLastWriteTimeUtc(loaded, time);
        File.SetLastWriteTimeUtc(started, time.AddSeconds(5));
        Assert.False(GameRunner.OnLoadingScreen(loaded, started));
        // The next map (a hub after a probe) has loaded since the last start.
        File.SetLastWriteTimeUtc(loaded, time.AddSeconds(9));
        Assert.True(GameRunner.OnLoadingScreen(loaded, started));
    }

    [Fact]
    public void A_hub_run_queues_a_numbered_request_naming_a_probe_that_returns_to_the_next_hub()
    {
        var first = _runner.RunInHub("hello", "done()", TimeSpan.Zero);
        var second = _runner.RunInHub("hello", "done()", TimeSpan.Zero);

        Assert.Equal(ProbeState.Queued, first.State);
        Assert.Equal(ProbeState.Queued, second.State);
        for (var k = 1; k <= 2; k++)
        {
            var probe = Path.Combine(_folder, "maps", $"{k}-hello.w3x");
            var hub = Path.Combine(_folder, "maps", $"hub-{k + 1}.w3x");
            Assert.Equal(Preload.Request(probe), File.ReadAllText(Path.Combine(_folder, "Warcraft III", "CustomMapData", "MirrorImaged", "hub", $"{k}.pld")));
            Assert.Contains($"ChangeLevel(\"{hub.Replace("\\", "\\\\")}\", false)", Map.Open(probe).Script);
            Assert.Contains($"Preloader(\"MirrorImaged\\\\hub\\\\{k + 1}.pld\")", Map.Open(hub).Script);
        }
    }

    [Fact]
    public void A_hub_run_removes_the_probes_last_output_first()
    {
        var output = Path.Combine(_folder, "Warcraft III", "CustomMapData", "MirrorImaged", "hello.pld");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, "old");

        _runner.RunInHub("hello", "done()", TimeSpan.Zero);

        Assert.False(File.Exists(output));
    }

    [Fact]
    public void A_new_hub_starts_at_request_one()
    {
        _runner.RunInHub("hello", "done()", TimeSpan.Zero);
        _runner.RunInHub("hello", "done()", TimeSpan.Zero);

        var hub = _runner.ResetHub();

        Assert.False(Directory.Exists(Path.Combine(_folder, "Warcraft III", "CustomMapData", "MirrorImaged", "hub")));
        Assert.Equal(Path.Combine(_folder, "maps", "hub-1.w3x"), hub);
        Assert.Contains("Preloader(\"MirrorImaged\\\\hub\\\\1.pld\")", Map.Open(hub).Script);
        Assert.Equal(ProbeState.Queued, _runner.RunInHub("hello", "done()", TimeSpan.Zero).State);
        Assert.True(File.Exists(Path.Combine(_folder, "Warcraft III", "CustomMapData", "MirrorImaged", "hub", "1.pld")));
    }
}
