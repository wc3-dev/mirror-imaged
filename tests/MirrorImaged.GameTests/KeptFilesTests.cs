namespace MirrorImaged.GameTests;

public sealed class KeptFilesTests : IDisposable
{
    static readonly DateTime Then = new(2026, 11, 8, 1, 17, 0, DateTimeKind.Utc);

    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    string Keep => Path.Combine(_folder, "kept");

    public void Dispose() => Directory.Delete(_folder, true);

    [Fact]
    public void Kept_files_are_put_back_as_they_were_with_their_write_times_and_the_keep_goes()
    {
        var changed = Write("LastReplay.w3g", "Sven's replay");
        var rewritten = Write("Classic.w3p", "profile");
        var made = Path.Combine(_folder, "TempReplay.w3g");
        var removed = Write("War3Preferences.txt", "preferences");
        var kept = KeptFiles.Keep(Keep, [changed, rewritten, made, removed]);

        File.WriteAllText(changed, "the probe's replay");
        File.WriteAllText(rewritten, "profile");
        File.WriteAllText(made, "");
        File.Delete(removed);
        var report = kept.PutBack();

        Assert.Equal("Sven's replay", File.ReadAllText(changed));
        Assert.Equal("profile", File.ReadAllText(rewritten));
        Assert.False(File.Exists(made));
        Assert.Equal("preferences", File.ReadAllText(removed));
        Assert.All([changed, rewritten, removed], file => Assert.Equal(Then, File.GetLastWriteTimeUtc(file)));
        Assert.Equal([$"put back {changed}", $"put back {rewritten}", $"put back {made}", $"put back {removed}"], report);
        Assert.False(Directory.Exists(Keep));
    }

    [Fact]
    public void A_keep_a_run_left_is_put_back_by_the_next_run_but_not_a_file_written_after_it()
    {
        var replay = Write("LastReplay.w3g", "Sven's replay");
        var preferences = Write("War3Preferences.txt", "preferences");
        var kept = KeptFiles.Keep(Keep, [replay, preferences]);
        kept.Mark();
        File.WriteAllText(replay, "the probe's replay");
        File.SetLastWriteTimeUtc(replay, DateTime.UtcNow.AddSeconds(-1));
        File.WriteAllText(preferences, "Sven's new preferences");
        File.SetLastWriteTimeUtc(preferences, DateTime.UtcNow.AddMinutes(5));

        var left = KeptFiles.Left(Keep)!;
        var report = left.PutBack(left.LastMark + TimeSpan.FromSeconds(5));

        Assert.Equal("Sven's replay", File.ReadAllText(replay));
        Assert.Equal("Sven's new preferences", File.ReadAllText(preferences));
        Assert.Equal([$"put back {replay}", $"left {preferences}: written after that run"], report);
        Assert.Null(KeptFiles.Left(Keep));
    }

    [Fact]
    public void A_keep_is_forgotten_when_another_game_ran_saying_which_files_differ()
    {
        var replay = Write("LastReplay.w3g", "Sven's replay");
        var preferences = Write("War3Preferences.txt", "preferences");
        var kept = KeptFiles.Keep(Keep, [replay, preferences]);
        File.WriteAllText(replay, "his new replay");

        Assert.Equal([replay], kept.Forget());
        Assert.Equal("his new replay", File.ReadAllText(replay));
        Assert.False(Directory.Exists(Keep));
    }

    string Write(string name, string text)
    {
        var file = Path.Combine(_folder, name);
        File.WriteAllText(file, text);
        File.SetLastWriteTimeUtc(file, Then);
        return file;
    }
}
