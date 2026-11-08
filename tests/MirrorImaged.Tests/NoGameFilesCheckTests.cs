using System.Diagnostics;
using System.Text;

namespace MirrorImaged.Tests;

// The fixtures are git repositories made here, not tracked files: a tracked fixture would fail the check on this repository.
public sealed class NoGameFilesCheckTests : IDisposable
{
    readonly string _repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public NoGameFilesCheckTests() => Run("git", "init -q");

    public void Dispose()
    {
        // git makes its objects read-only, and Windows won't delete read-only files.
        foreach (var file in Directory.EnumerateFiles(_repo, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_repo, true);
    }

    [Fact]
    public void Passes_on_ordinary_files_and_ignores_untracked_game_files()
    {
        Write("src/Program.cs", "return 0;");
        Write("docs/common.j.md", "Notes about common.j");
        Run("git", "add -A");
        Write("probes/out/probe.w3x", "MPQ\u001a");

        var (exit, output) = Check();

        Assert.True(exit == 0, output);
    }

    [Theory]
    [InlineData("maps/a.w3x")]
    [InlineData("maps/Big.W3X")]
    [InlineData("a.w3m")]
    [InlineData("a.w3n")]
    [InlineData("a.mpq")]
    [InlineData("a.mdx")]
    [InlineData("a.mdl")]
    [InlineData("a.blp")]
    [InlineData("a.dds")]
    [InlineData("a.slk")]
    [InlineData("a.w3e")]
    [InlineData("a.doo")]
    [InlineData("scripts/common.j")]
    [InlineData("Blizzard.j")]
    [InlineData("COMMON.AI")]
    public void Fails_on_a_game_file_name(string path)
    {
        Write(path, "text");

        AssertFailsOn(path);
    }

    [Theory]
    [InlineData("MPQ\u001a")]
    [InlineData("MPQ\u001b")]
    [InlineData("HM3W")]
    [InlineData("BLP0")]
    [InlineData("BLP1")]
    [InlineData("BLP2")]
    [InlineData("MDLX")]
    public void Fails_on_a_game_file_signature(string signature)
    {
        Write("data/renamed.bin", signature + "rest of the file");

        AssertFailsOn("data/renamed.bin");
    }

    [Fact]
    public void Reads_the_signature_of_a_file_with_a_non_ascii_name()
    {
        Write("data/räv.bin", "MPQ\u001a");
        Run("git", "add -A");

        var (exit, output) = Check();

        Assert.Equal(1, exit);
        Assert.Contains("(game file signature)", output);
    }

    void AssertFailsOn(string path)
    {
        Run("git", "add -A");

        var (exit, output) = Check();

        Assert.Equal(1, exit);
        Assert.Contains(path, output);
    }

    void Write(string path, string content)
    {
        var full = Path.Combine(_repo, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, Encoding.Latin1);
    }

    (int Exit, string Output) Check()
    {
        var script = Path.Combine(RepoRoot(), "tools", "check-no-game-files.ps1");
        return Run("pwsh", $"-NoProfile -File \"{script}\" -Path \"{_repo}\"", check: false);
    }

    (int Exit, string Output) Run(string file, string arguments, bool check = true)
    {
        var start = new ProcessStartInfo(file, arguments)
        {
            WorkingDirectory = _repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd() + error.Result;
        process.WaitForExit();
        if (check && process.ExitCode != 0) throw new InvalidOperationException($"{file} {arguments} exited {process.ExitCode}: {output}");
        return (process.ExitCode, output);
    }

    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "MirrorImaged.slnx"))) dir = dir.Parent!;
        return dir.FullName;
    }
}
