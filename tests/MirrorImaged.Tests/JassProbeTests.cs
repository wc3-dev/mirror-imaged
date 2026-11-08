using System.Text;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using MirrorImaged.Probes;

namespace MirrorImaged.Tests;

/// <summary>
/// The JASS probes' maps run in MirrorImaged, without the game's blizzard.j, which they don't call: each gives the lines
/// the game gave (probes/expected; docs/facts.md, "JASS maps").
/// </summary>
public sealed class JassProbeTests : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    // The probe's lines, from the Preload file it writes when it calls done(), as the runner reads the game's
    internal static IReadOnlyList<string> Run(string name, string folder)
    {
        var path = Path.Combine(folder, name + ".w3x");
        ProbeMap.BuildJass(name, ProbeMap.JassSource(name), path);
        using var simulation = Simulation.Load(path, "");
        simulation.Advance(TimeSpan.FromSeconds(60));
        Assert.True(simulation.Ended, string.Join("\n", simulation.Log));
        return Preload.Lines(Encoding.UTF8.GetString(simulation.Documents.CustomMapData[$@"{ProbeMap.Folder}\{name}.pld"]))!;
    }

    static IEnumerable<string> Expected(string name) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "probes", "expected", name + ".txt")).Where(line => !line.StartsWith("# "));

    // The game's lines MirrorImaged gives otherwise, by a decision: the game's, then MirrorImaged's
    static readonly Dictionary<string, string> Differences = new()
    {
        // D135: the operation limit counts Lua instructions, which stand for JASS's operations by a ratio fitted to
        // these five loops
        ["limit Spin 428571"] = "limit Spin 412500",
        ["limit SpinCalling 300000"] = "limit SpinCalling 235714",
        ["limit SpinNative 300000"] = "limit SpinNative 235714",
        ["limit SpinLocal 333333"] = "limit SpinLocal 549999",
        ["limit SpinExit 230769"] = "limit SpinExit 235714",
        // D27: 1 - 0.001 keeps one guard bit of 0.001 and gives 0.99899995 (3f7fbe77) where the game cuts to 3f7fbe76, so
        // the margin of == holds there (W1-10)
        ["ladder 1 110000001000"] = "ladder 1 110000001100",
        ["ladder -1 110000001000"] = "ladder -1 110000001100",
        ["ladder 100 110000001000"] = "ladder 100 110000001100",
        ["ladder 1000 111000001000"] = "ladder 1000 111000001100",
        ["ladder 0.5 110000001000"] = "ladder 0.5 110000001100",
        ["minus 3f7fbe76 4479ffef 3f800000 3f800000 3f800000 4b800000"] = "minus 3f7fbe77 4479fff0 3f800000 3f800000 3f800000 4b800000",
        // D83: S2R is a float step off on these, and past the float range the model is extrapolated; the literals, read
        // through it, match where S2R does
        ["1.0000001 3f800000 3f800002"] = "1.0000001 3f800000 3f800001",
        ["1000000.0625 49742400 49742401"] = "1000000.0625 49742400 49742400",
        ["3.14159 40490fcf 40490fd1"] = "3.14159 40490fcf 40490fd0",
        ["literal 3dccccce 3e99999a 4b800000 00000000 3a83126f"] = "literal 3dccccce 3e99999a 4b800000 7f7fc98c 3a83126f",
    };

    [Theory]
    [InlineData("jass-numbers")]
    [InlineData("jass-reals")]
    [InlineData("jass-literals")]
    [InlineData("jass-equal")]
    [InlineData("jass-threads")]
    [InlineData("jass-debug")]
    [InlineData("jass-rawcodes")]
    [InlineData("jass-rawcode-quote")]
    [InlineData("jass-divide-integer")]
    [InlineData("jass-divide-real")]
    public void A_jass_probe_gives_the_games_lines(string name) =>
        Assert.Equal(Expected(name).Select(line => Differences.GetValueOrDefault(line, line)), Run(name, _folder));

    // the game closed (probe jass-divide-overflow)
    [Fact]
    public void Dividing_the_lowest_integer_by_minus_one_closes_the_game()
    {
        var path = Path.Combine(_folder, "overflow.w3x");
        ProbeMap.BuildJass("jass-divide-overflow", ProbeMap.JassSource("jass-divide-overflow"), path);
        using var simulation = Simulation.Load(path, "");

        Assert.Throws<LuaException>(() => simulation.Advance(TimeSpan.FromSeconds(1)));
        Assert.IsType<GameClosedException>(simulation.Stopped?.InnerException);
        Assert.StartsWith("war3map.lua:", simulation.Stopped.Message);
        Assert.Contains("The game closes: -2147483648 was divided by -1", simulation.Stopped.Message);
    }
}
