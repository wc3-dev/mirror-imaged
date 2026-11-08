using System.Text.RegularExpressions;
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

    // docs/facts.md, "Players and the game" and "Player and game events"
    [GameFact]
    public void The_lobby_probe_gives_the_lines_measured() => Measured("lobby", withComputer: true);

    [GameFact]
    public void The_events_probe_gives_the_lines_measured() => Measured("events", withComputer: true);

    [GameFact]
    public void The_forforce_wait_probe_gives_the_lines_measured() => Measured("forforce-wait");

    static void Measured(string name, bool withComputer = false)
    {
        var run = GameRunner.Default.Run(name, ProbeMap.Source(name), TimeSpan.FromMinutes(2), withComputer);

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        ExpectedLines.Assert(Path.Combine(AppContext.BaseDirectory, "probes", "expected", name + ".txt"), run.Lines);
    }

}

// A probe's lines as probes/expected/<name>.txt has them (decisions D71): a line starting "# " is a note; one starting
// "~ " varies from run to run, each {t} in it standing for a number and the rest exact, and the run gives it once,
// anywhere, since its time decides its place; every other line comes in order, exactly. A "~ " line without a {t} is
// refused.
public static class ExpectedLines
{
    public static void Assert(string file, IReadOnlyList<string> lines)
    {
        var expected = File.ReadAllLines(file).Select((line, i) => (Line: line, Number: i + 1))
            .Where(e => !e.Line.StartsWith("# ")).ToList();
        var varying = expected.Where(e => e.Line.StartsWith("~ ")).Select(e => e.Line.Contains("{t}")
            ? new Regex("^" + Regex.Escape(e.Line[2..]).Replace(@"\{t}", @"-?[0-9]+(\.[0-9]+)?") + "$")
            : throw new InvalidDataException(
                $"{file}:{e.Number}: a line starting \"~ \" varies, and needs a {{t}} for each number that does")).ToList();

        var rest = new List<string>();
        foreach (var line in lines)
        {
            var match = varying.FindIndex(pattern => pattern.IsMatch(line));
            if (match < 0) rest.Add(line);
            else varying.RemoveAt(match);
        }

        Xunit.Assert.Equal(expected.Where(e => !e.Line.StartsWith("~ ")).Select(e => e.Line), rest);
        Xunit.Assert.True(varying.Count == 0, $"no line of the run matched: {string.Join("; ", varying)}");
    }
}

public sealed class ExpectedLinesTests : IDisposable
{
    readonly string _file = Path.GetTempFileName();

    public void Dispose() => File.Delete(_file);

    void Check(string expected, params string[] run)
    {
        File.WriteAllText(_file, expected);
        ExpectedLines.Assert(_file, run);
    }

    [Fact]
    public void A_varying_line_matches_any_number_anywhere_and_the_other_lines_come_in_order()
    {
        const string expected = "# a note\na\n~ w {t} true\nb\n";

        Check(expected, "a", "b", "w 1.5 true");
        Check(expected, "w -2 true", "a", "b");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Check(expected, "a", "b", "w 1.5 false"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Check(expected, "b", "a", "w 1 true"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Check(expected, "a", "b"));
    }

    [Fact]
    public void A_varying_line_without_a_number_to_vary_is_refused()
    {
        var error = Assert.Throws<InvalidDataException>(() => Check("a\n~ w done\n", "a", "w done"));

        Assert.Equal($"{_file}:2: a line starting \"~ \" varies, and needs a {{t}} for each number that does", error.Message);
    }
}
