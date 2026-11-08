using System.Diagnostics;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

public sealed class CoverageTests
{
    sealed class SomeNatives
    {
        [Native(Level.Missing)] public void Zeta() { }
        [Native(Level.Exact, Fact = "game-facts.md, Timers")] public void Alpha() { }
        [Native(Level.Missing)] public void Beta() { }
        [Native(Level.Recorded, Reason = "Plays a sound")] public void Gamma() { }
        [Native(Level.Close)] public void Delta() { }
        public void Helper() { }
    }

    [Fact]
    public void Counts_the_natives_at_each_level_and_lists_the_missing_ones_sorted()
    {
        var coverage = Coverage.Of(typeof(SomeNatives));

        Assert.Equal(1, coverage.Counts[Level.Exact]);
        Assert.Equal(1, coverage.Counts[Level.Close]);
        Assert.Equal(1, coverage.Counts[Level.Recorded]);
        Assert.Equal(2, coverage.Counts[Level.Missing]);
        Assert.Equal(["Beta", "Zeta"], coverage.Missing);
    }

    [Fact]
    public void A_level_with_no_natives_counts_zero()
    {
        Assert.Equal(0, Coverage.Of(typeof(object)).Counts[Level.Exact]);
    }

    [Fact]
    public void The_report_gives_the_counts_then_the_missing_natives()
    {
        Assert.Equal("""
            Exact       1
            Close       1
            Recorded    1
            Missing     2
            All         5

            Missing:
            Beta
            Zeta

            """.ReplaceLineEndings("\n"), Coverage.Of(typeof(SomeNatives)).ToString());
    }

    [Fact]
    public void Mirror_coverage_prints_the_report_of_the_natives()
    {
        var (exit, output, _) = Mirror("coverage");

        Assert.Equal(0, exit);
        Assert.Equal(Coverage.Of(typeof(GameNatives)).ToString(), output);
    }

    [Fact]
    public void Mirror_with_no_command_prints_its_usage()
    {
        var (exit, _, error) = Mirror();

        Assert.Equal(2, exit);
        Assert.Contains("mirror coverage", error);
    }

    internal static (int Exit, string Output, string Error) Mirror(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet", [Path.Combine(AppContext.BaseDirectory, "MirrorImaged.Cli.dll"), .. arguments])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error.Result);
    }
}
