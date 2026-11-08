namespace MirrorImaged.GameTests;

/// <summary>
/// Copies of files the game rewrites, with their write times, kept in a folder until they are put back, so the next run
/// puts back what a run that ended early (its test host killed) left.
/// </summary>
sealed class KeptFiles
{
    const string ListName = "kept.txt";

    readonly string _folder;
    readonly List<(string File, DateTime? Time)> _files;

    KeptFiles(string folder, List<(string File, DateTime? Time)> files)
    {
        _folder = folder;
        _files = files;
    }

    string List => Path.Combine(_folder, ListName);

    /// <summary>When the keep was last marked in use: what was written later was written by someone else.</summary>
    public DateTime LastMark => File.GetLastWriteTimeUtc(List);

    /// <summary>Keeps each file's content and write time (a null time for one that doesn't exist) in the folder.</summary>
    public static KeptFiles Keep(string folder, IEnumerable<string> files)
    {
        Directory.CreateDirectory(folder);
        var kept = files.Select(file => (file, File.Exists(file) ? File.GetLastWriteTimeUtc(file) : (DateTime?)null)).ToList();
        for (var i = 0; i < kept.Count; i++)
        {
            if (kept[i].Item2 is not null)
                File.Copy(kept[i].file, Copy(folder, i), true);
        }

        // Written last, so a folder with a list holds a whole keep.
        File.WriteAllLines(Path.Combine(folder, ListName), kept.Select(entry => $"{entry.Item2?.Ticks ?? -1}\t{entry.file}"));
        return new KeptFiles(folder, kept);
    }

    /// <summary>The keep a run left without putting it back, or null.</summary>
    public static KeptFiles? Left(string folder)
    {
        var list = Path.Combine(folder, ListName);
        if (!File.Exists(list))
            return null;
        var files = File.ReadAllLines(list)
            .Select(line => line.Split('\t', 2))
            .Select(parts => (parts[1], parts[0] == "-1" ? (DateTime?)null : new DateTime(long.Parse(parts[0]), DateTimeKind.Utc)))
            .ToList();
        return new KeptFiles(folder, files);
    }

    /// <summary>Marks the keep in use now.</summary>
    public void Mark() => File.SetLastWriteTimeUtc(List, DateTime.UtcNow);

    /// <summary>
    /// Puts back each file that differs from its copy in content or write time, and removes one that didn't exist; with
    /// <paramref name="notAfter"/>, leaves a file written after it. Then deletes the keep, unless a file couldn't be put
    /// back. Says what it did; never throws for a file.
    /// </summary>
    public List<string> PutBack(DateTime? notAfter = null)
    {
        var report = new List<string>();
        var whole = true;
        for (var i = 0; i < _files.Count; i++)
        {
            var (file, time) = _files[i];
            try
            {
                if (notAfter is { } limit && File.Exists(file) && File.GetLastWriteTimeUtc(file) > limit)
                {
                    report.Add($"left {file}: written after that run");
                    continue;
                }

                if (time is null)
                {
                    if (!File.Exists(file))
                        continue;
                    File.Delete(file);
                }
                else if (!Same(file, Copy(_folder, i)) || File.GetLastWriteTimeUtc(file) != time)
                {
                    File.Copy(Copy(_folder, i), file, true);
                    File.SetLastWriteTimeUtc(file, time.Value);
                }
                else
                {
                    continue;
                }

                report.Add($"put back {file}");
            }
            catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
            {
                report.Add($"could not put back {file}: {problem.Message}");
                whole = false;
            }
        }

        if (whole)
            Directory.Delete(_folder, true);
        return report;
    }

    /// <summary>Says which files differ from their copies, and deletes the keep without putting anything back.</summary>
    public List<string> Forget()
    {
        var differ = _files
            .Where((entry, i) => entry.Time is null ? File.Exists(entry.File) : !Same(entry.File, Copy(_folder, i)))
            .Select(entry => entry.File)
            .ToList();
        Directory.Delete(_folder, true);
        return differ;
    }

    static string Copy(string folder, int index) => Path.Combine(folder, $"{index}.kept");

    static bool Same(string file, string copy) =>
        File.Exists(file) && File.ReadAllBytes(file).AsSpan().SequenceEqual(File.ReadAllBytes(copy));
}
