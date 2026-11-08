using System.Text;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

// Preload files in the simulated CustomMapData (decisions D100-D102), as the facts' "Files and Preload" and the files
// probe measure them: every Preload text is kept, cut to 259 bytes, until PreloadGenClear; PreloadGenEnd writes the
// file as the game does; Preloader runs it at once, and a path keeps what the game first read there.
public sealed partial class GameNatives
{
    const string FileStart = "function PreloadFiles takes nothing returns nothing\n\r\n\tcall PreloadStart()\r\n";
    const string FileEnd = "\tcall PreloadEnd( 0.0 )\r\n\nendfunction\n\n\r\n";

    readonly List<byte[]> _preloaded = [];
    readonly Dictionary<string, byte[]> _preloaderRead = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The simulated <c>Documents\Warcraft III</c> the game writes its files and saved game caches to.</summary>
    public Documents Documents { get; init; } = new();

    /// <summary>
    /// Runs a Preload file's JASS, given its path and its text, as the game does in <c>Preloader</c>; the simulation
    /// sets it. Without it a file is read and nothing runs.
    /// </summary>
    public Action<string, string>? RunPreloadFile { get; init; }

    /// <summary>Keeps the text for the next file <see cref="PreloadGenEnd"/> writes: its first 259 bytes.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Files and Preload: one Preload call keeps the first 259 bytes of its text (11-02, 11-03); docs/facts.md, Files and game cache: kept until PreloadGenClear")]
    public void Preload(string? filename)
    {
        var text = Encoding.UTF8.GetBytes(filename ?? "");
        _preloaded.Add(text.Length > 259 ? text[..259] : text);
    }

    [Native(Level.Exact, Fact = "docs/facts.md, Files and game cache: PreloadGenClear empties what Preload kept, nothing else does")]
    public void PreloadGenClear() => _preloaded.Clear();

    /// <summary>Does nothing here: the game starts the count it writes as <c>PreloadEnd</c>'s number, which isn't known.</summary>
    [Native(Level.Close)]
    public void PreloadGenStart() { }

    /// <summary>
    /// Writes what Preload kept to <paramref name="filename"/> in <c>CustomMapData</c>, as the game writes it, each text
    /// in a <c>call Preload( "..." )</c> with its backslashes doubled; nothing where the game writes nothing (D101).
    /// <c>PreloadEnd</c>'s number is 0.0, the game's for a file written in one call; it wrote 1.9 and 2.0 for one ended
    /// 2.5 game seconds after <c>PreloadGenStart</c>.
    /// </summary>
    [Native(Level.Close)]
    public void PreloadGenEnd(string? filename)
    {
        if (PreloadPath(filename, reading: false) is not { } path) return;
        var file = new MemoryStream();
        file.Write(Encoding.UTF8.GetBytes(FileStart));
        foreach (var text in _preloaded)
        {
            file.Write("\tcall Preload( \""u8);
            foreach (var b in text)
            {
                if (b == '\\') file.WriteByte(b);
                file.WriteByte(b);
            }
            file.Write("\" )\r\n"u8);
        }
        file.Write(Encoding.UTF8.GetBytes(FileEnd));
        Documents.CustomMapData[path] = file.ToArray();
    }

    /// <summary>
    /// Runs the file at <paramref name="filename"/> in <c>CustomMapData</c> at once, as the content the game first read
    /// at that path in this game; nothing for a missing file or a path the game reads nothing from (D101, D102).
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Files and Preload: read back at once, a missing file changes nothing, a path keeps its first content (11-03, 11-06); docs/facts.md, Files and game cache")]
    public void Preloader(string? filename)
    {
        if (PreloadPath(filename, reading: true) is not { } path) return;
        if (!_preloaderRead.TryGetValue(path, out var file) && Documents.CustomMapData.TryGetValue(path, out file)) _preloaderRead[path] = file;
        if (file is not null) RunPreloadFile?.Invoke(path, Encoding.UTF8.GetString(file));
    }

    // The natives a Preload file calls around its Preload calls: they show the game loading the files named.
    [Native(Level.Recorded, Reason = "Starts the game's loading of the files Preload names, which shows nothing a map reads; not in the run's log (D98)")]
    public void PreloadStart() { }

    [Native(Level.Recorded, Reason = "Ends the game's loading of the files Preload names, which shows nothing a map reads; not in the run's log (D98)")]
    public void PreloadEnd(float timeout) { }

    [Native(Level.Recorded, Reason = "Ends the game's loading of the files Preload names, which shows nothing a map reads; not in the run's log (D98)")]
    public void PreloadEndEx() { }

    [Native(Level.Recorded, Reason = "Goes on with the game's loading of the files Preload names, which shows nothing a map reads; not in the run's log (D98)")]
    public void PreloadRefresh() { }

    // The longest path the game wrote: 201 characters, where CustomMapData's own path is 58 (D101).
    const int LongestPath = 201;

    // Where a map's file name is in CustomMapData, folders split by \, or null where the game writes or reads nothing
    // (the files probe): an extension but .pld or .txt in lower case, an absolute path, more than 201 characters, or ..
    // when writing; a read goes through .., but never above CustomMapData.
    static string? PreloadPath(string? name, bool reading)
    {
        if (name is null || name.Length > LongestPath || !(name.EndsWith(".pld", StringComparison.Ordinal) || name.EndsWith(".txt", StringComparison.Ordinal))
            || name.Contains(':') || name.StartsWith('\\') || name.StartsWith('/'))
            return null;
        var folders = new List<string>();
        foreach (var part in name.Split('\\', '/'))
        {
            if (part is "" or ".") continue;
            if (part != "..") folders.Add(part);
            else if (!reading || folders.Count == 0) return null;
            else folders.RemoveAt(folders.Count - 1);
        }
        return string.Join('\\', folders);
    }
}
