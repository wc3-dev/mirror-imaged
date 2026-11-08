using MirrorImaged.Data;
using MirrorImaged.Probes;

namespace MirrorImaged.GameTests;

/// <summary>A test that launches the installed game; skipped when it isn't installed.</summary>
public sealed class GameFactAttribute : FactAttribute
{
    public GameFactAttribute()
    {
        if (!File.Exists(GameRunner.Default.Game))
            Skip = $"No Warcraft III at {GameRunner.Default.Game}; set {GameInstall.Variable} to the game's folder to run it";
    }
}

[Trait("Needs", "Game")]
public sealed class GameProbeTests
{
    [GameFact]
    public void The_hello_probe_prints_its_lines_in_the_game()
    {
        var run = GameRunner.Default.Run("hello", ProbeMap.Source("hello"), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal(["hello from the game", "3"], run.Lines);
    }

    [GameFact]
    public void Game_time_runs_at_full_speed_on_the_hidden_desktop()
    {
        var run = GameRunner.Default.Run("game-time", ProbeMap.Source("game-time"), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        var last = run.Lines[^1].Split(' ');
        Assert.Equal("20", last[0]);
        Assert.InRange(int.Parse(last[2]), 19, 21);
    }
}
