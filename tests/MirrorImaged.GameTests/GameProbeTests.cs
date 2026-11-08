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

/// <summary>A theory that launches the installed game; skipped when it isn't installed.</summary>
public sealed class GameTheoryAttribute : TheoryAttribute
{
    public GameTheoryAttribute()
    {
        if (!File.Exists(GameRunner.Default.Game))
            Skip = $"No Warcraft III at {GameRunner.Default.Game}; set {GameInstall.Variable} to the game's folder to run it";
    }
}

[Trait("Needs", "Game")]
public sealed class GameProbeTests
{
    public static TheoryData<string> Probes => [.. ProbeComparison.Names];

    // Runs a probe in the game and in MirrorImaged and compares both with the game's lines in probes/expected (decisions
    // D120): the game must still give them, and MirrorImaged give them but for what probes/differences records. One
    // probe, from PowerShell: dotnet test tests/MirrorImaged.GameTests --filter 'Needs=Game&DisplayName~name: \"triggers\"'
    [GameTheory]
    [MemberData(nameof(Probes))]
    public void A_probe_gives_the_games_lines_in_the_game_and_in_MirrorImaged(string name)
    {
        var run = GameRunner.Default.Run(name, ProbeMap.Source(name), ProbeComparison.Longest, ProbeComparison.WithComputer(name), ProbeComparison.Change(name));
        Assert.True(run.State is ProbeState.Done or ProbeState.Closed, $"{run.State}: {run.Detail}");
        IReadOnlyList<string> lines = run.State == ProbeState.Closed ? [ProbeComparison.Closes] : run.Lines;
        Assert.True(File.Exists(ProbeComparison.Expected(name)), $"""
            {name} has no probes/expected/{name}.txt; the game gave:
            {string.Join('\n', lines)}
            """);
        var game = ProbeComparison.Compare(ProbeComparison.Expected(name), lines);
        Assert.True(game.Count == 0, $"""
            {name}: the game no longer gives probes/expected/{name}.txt's lines:
            {string.Join('\n', game.Select(d => d.Describe("this run")))}
            This run's lines:
            {string.Join('\n', lines)}
            {run.Detail}
            """);

        ProbeComparison.AssertMirror(name);
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

    [GameFact]
    public void A_line_too_long_for_Preload_ends_the_probe_with_its_error()
    {
        // 126 backslashes and a letter count 253, the most a line holds, and come back whole.
        var run = GameRunner.Default.Run("refused", "out(string.rep('\\\\', 126) .. 'a')\nout(string.rep('a', 254))\nout('never')\ndone()", TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal(new string('\\', 126) + "a", run.Lines[0]);
        Assert.StartsWith("error: ", run.Lines[1]);
        Assert.Contains("a line is at most 253 bytes, a backslash counting two, with no line break", run.Lines[1]);
        Assert.Equal(2, run.Lines.Count);
    }

    // EndGame's evidence (decisions D147): the events probe's code after it and a 0 s timer started before it write their
    // files, and the end-level event's action never writes its own. Its lines are the theory's.
    [GameFact]
    public void The_events_probe_writes_the_files_EndGame_lets_it_write()
    {
        var files = new[] { "events-after.pld", "events-timer.pld", "events-end.pld" }.Select(file => Path.Combine(GameRunner.Default.Data, file)).ToList();
        files.ForEach(File.Delete);

        var run = GameRunner.Default.Run("events", ProbeMap.Source("events"), ProbeComparison.Longest, withComputer: true);

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal([true, true, false], files.Select(File.Exists));
    }
}
