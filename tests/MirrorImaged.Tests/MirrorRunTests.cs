using MirrorImaged.Data;
using static MirrorImaged.Tests.CoverageTests;

namespace MirrorImaged.Tests;

public sealed class MirrorRunTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    string Map(string main) => _maps.Build($"function config() end\nfunction main()\n{main}\nend\n");

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Mirror_run_runs_a_map_for_the_seconds_given_with_the_seed_and_prints_its_log()
    {
        var map = Map("print(GetRandomInt(1, 1000000)) TimerStart(CreateTimer(), 1, true, function() print('tick') end)");

        var (exit, output, _) = Mirror("run", map, "--seconds", "2.5", "--seed", "3");

        Assert.Equal(0, exit);
        using var simulation = Simulation.Load(map, new SimulationOptions { Seed = 3 });
        simulation.Advance(TimeSpan.FromSeconds(2.5));
        Assert.Equal(string.Concat(simulation.Log.Select(line => line + "\n")), output);
        Assert.EndsWith("2.000 print: tick\n", output);
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Mirror_run_prints_the_log_then_what_stopped_the_run()
    {
        var map = Map("print('before') TimerStart(CreateTimer(), 1, false, function() ClearTextMessages() end)");

        var (exit, output, error) = Mirror("run", map);

        Assert.Equal(1, exit);
        Assert.Equal("0.000 print: before\n", output);
        Assert.StartsWith("war3map.lua:3: The native ClearTextMessages is Missing", error);
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Mirror_run_prints_the_log_of_config_and_main_before_a_stop_in_main()
    {
        var map = _maps.Build("function config() print('config') end\nfunction main() print('main\\nagain') ClearTextMessages() end\n");

        var (exit, output, error) = Mirror("run", map);

        Assert.Equal(1, exit);
        Assert.Equal("0.000 print: config\n0.000 print: main\n0.000 print: again\n", output);
        Assert.StartsWith("war3map.lua:2: The native ClearTextMessages is Missing", error);
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Mirror_run_runs_a_JASS_map()
    {
        var map = _maps.Build("""
            function Tick takes nothing returns nothing
                call BJDebugMsg("tick " + I2S(7 / -2))
            endfunction

            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                call TimerStart(CreateTimer(), 1, false, function Tick)
            endfunction
            """, War3Net.Build.Info.ScriptLanguage.Jass);

        var (exit, output, _) = Mirror("run", map, "--seconds", "2");

        Assert.Equal(0, exit);
        Assert.Equal(string.Concat(Enumerable.Range(0, 24).Select(player => $"1.000 player {player}: tick -3\n")), output);
    }

    [Theory]
    [InlineData("--seconds")]
    [InlineData("--seconds", "-1")]
    [InlineData("--seed", "x")]
    [InlineData("--speed", "2")]
    public void Mirror_run_with_a_wrong_option_prints_its_usage(params string[] options)
    {
        var (exit, _, error) = Mirror(["run", "map.w3x", .. options]);

        Assert.Equal(2, exit);
        Assert.Contains("mirror run <map>", error);
    }

    [Fact]
    public void Mirror_run_without_the_game_says_a_run_needs_its_blizzard_j()
    {
        var nowhere = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var (exit, output, error) = Mirror(new Dictionary<string, string?> { [GameInstall.Variable] = nowhere }, "run", Map("print('never')"));

        Assert.Equal(1, exit);
        Assert.Empty(output);
        Assert.Equal($"A run needs blizzard.j from an installed Warcraft III, and there is none in {nowhere}. Set {GameInstall.Variable} to the game's folder.\n", error);
    }

    [Fact]
    public void Mirror_run_of_a_missing_map_says_so()
    {
        var (exit, _, error) = Mirror("run", Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".w3x"));

        Assert.Equal(1, exit);
        Assert.Contains(".w3x", error);
    }
}
