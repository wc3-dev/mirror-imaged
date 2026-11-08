using System.Diagnostics;
using System.Text;

namespace MirrorImaged.Tests;

// The fixtures are git repositories made here, not tracked files: a tracked fixture would fail the check on this repository.
public sealed class NoGameFilesCheckTests : IDisposable
{
    static readonly string Script = Path.Combine(RepoRoot(), "tools", "check-no-game-files.ps1");

    readonly string _repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public NoGameFilesCheckTests() => Git("init", "-q");

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
        Git("add", "-A");
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
    [InlineData("sounds/a.wav")]
    [InlineData("a.mp3")]
    [InlineData("a.flac")]
    [InlineData("a.ogg")]
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
    public void Fails_on_a_git_lfs_pointer()
    {
        Write("data/renamed.bin", "version https://git-lfs.github.com/spec/v1\noid sha256:0123\nsize 2048\n");

        AssertFailsOn("data/renamed.bin");
    }

    [Fact]
    public void Reads_the_signature_of_a_file_with_a_non_ascii_name()
    {
        Write("data/räv.bin", "MPQ\u001a");
        Git("add", "-A");

        var (exit, output) = Check();

        Assert.Equal(1, exit);
        Assert.Contains("(game file signature)", output);
    }

    [Fact]
    public void Reads_a_name_with_a_newline_whole()
    {
        Write("blob.bin", "MPQ\u001a");
        var blob = Git("hash-object", "-w", "blob.bin").Trim();
        // On Windows git refuses control characters in paths unless NTFS protection is off.
        Git("-c", "core.protectNTFS=false", "update-index", "--add", "--cacheinfo", $"100644,{blob},data/one\ntwo.bin");

        var (exit, output) = Check();

        Assert.Equal(1, exit);
        Assert.Contains("one\ntwo.bin (game file signature)", output);
    }

    [Fact]
    public void Checks_a_file_deleted_on_disk_as_git_stores_it()
    {
        Write("data/renamed.bin", "MPQ\u001a");
        Git("add", "-A");
        File.Delete(Path.Combine(_repo, "data/renamed.bin"));

        AssertFailsOn("data/renamed.bin", add: false);
    }

    [Fact]
    public void Checks_a_file_changed_on_disk_as_git_stores_it()
    {
        Write("data/renamed.bin", "MPQ\u001a");
        Git("add", "-A");
        Write("data/renamed.bin", "harmless");

        AssertFailsOn("data/renamed.bin", add: false);
    }

    [Fact]
    public void Fails_on_a_tracked_path_git_cannot_read()
    {
        Git("update-index", "--add", "--cacheinfo", "160000,1234567890123456789012345678901234567890,vendor/sub");

        AssertFailsOn("vendor/sub", add: false);
    }

    [Fact]
    public void Resolves_a_relative_path_against_the_powershell_location()
    {
        Write("src/Program.cs", "return 0;");
        Git("add", "-A");
        var command = $"Set-Location -LiteralPath '{Path.GetDirectoryName(_repo)}'; & '{Script}' -Path '{Path.GetFileName(_repo)}'; exit $LASTEXITCODE";

        // The process starts elsewhere, so .NET's current directory is not PowerShell's location.
        var (exit, output) = Run("pwsh", ["-NoProfile", "-Command", command], RepoRoot());

        Assert.True(exit == 0, output);
    }

    void AssertFailsOn(string path, bool add = true)
    {
        if (add) Git("add", "-A");

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

    (int Exit, string Output) Check() => Run("pwsh", ["-NoProfile", "-File", Script, "-Path", _repo]);

    string Git(params string[] arguments)
    {
        var (exit, output) = Run("git", arguments);
        if (exit != 0) throw new InvalidOperationException($"git {string.Join(' ', arguments)} exited {exit}: {output}");
        return output;
    }

    (int Exit, string Output) Run(string file, string[] arguments, string? workingDirectory = null)
    {
        var start = new ProcessStartInfo(file, arguments)
        {
            WorkingDirectory = workingDirectory ?? _repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd() + error.Result;
        process.WaitForExit();
        return (process.ExitCode, output);
    }

    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "MirrorImaged.slnx"))) dir = dir.Parent!;
        return dir.FullName;
    }
}
