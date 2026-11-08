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

    // docs/facts.md, "Triggers and threads": each trigger probe gives the lines in probes/expected
    [GameFact]
    public void The_triggers_probe_gives_the_lines_measured() => Measured("triggers");

    [GameFact]
    public void The_second_triggers_probe_gives_the_lines_measured() => Measured("triggers-2");

    [GameFact]
    public void The_third_triggers_probe_gives_the_lines_measured() => Measured("triggers-3");

    [GameFact]
    public void The_fourth_triggers_probe_gives_the_lines_measured() => Measured("triggers-4");

    static void Measured(string name)
    {
        var run = GameRunner.Default.Run(name, ProbeMap.Source(name), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        AssertExpected(name, run.Lines);
    }

    // probes/expected/<name>.txt: "# " lines are notes, and "~ label | line" a line that varies from run to run, which
    // the run must give once, starting with its label, anywhere; the other lines come in order.
    static void AssertExpected(string name, IReadOnlyList<string> lines)
    {
        var expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "probes", "expected", name + ".txt")).Where(line => !line.StartsWith("# ")).ToList();
        var labels = expected.Where(line => line.StartsWith("~ ")).Select(line => line[2..line.IndexOf(" | ")]).ToList();
        string? Label(string line) => labels.Where(label => line == label || line.StartsWith(label + " ")).MaxBy(label => label.Length);

        Assert.Equal(expected.Where(line => !line.StartsWith("~ ")), lines.Where(line => Label(line) is null));
        Assert.Equal(labels.Order(StringComparer.Ordinal), lines.Select(Label).OfType<string>().Order(StringComparer.Ordinal));
    }
}
