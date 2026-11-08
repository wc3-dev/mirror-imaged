using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using MirrorImaged.Data;
using MirrorImaged.Probes;

namespace MirrorImaged.GameTests;

/// <summary>How a probe's run ended.</summary>
public enum ProbeState
{
    /// <summary>The probe ran and its lines were read.</summary>
    Done,

    /// <summary>The map never loaded: the game didn't get that far. It waits for Sven.</summary>
    Queued,

    /// <summary>The map loaded, but the probe's Preload file didn't come in time.</summary>
    NoOutput,
}

/// <summary>A probe's run: how it ended, its lines, and what the runner saw on the way.</summary>
public sealed record ProbeRun(ProbeState State, IReadOnlyList<string> Lines, string Detail);

/// <summary>
/// Runs probe maps in the installed game, on a Windows desktop of its own that is never shown: builds the map, launches
/// the game on it, presses space for the game while the map waits on its loading screen, waits for the probe's Preload
/// file, ends the game and what it started, and returns the lines (docs/facts.md, "The game runner").
/// </summary>
/// <param name="game">The game's executable.</param>
/// <param name="documents">The game's documents folder (<c>Documents\Warcraft III</c>).</param>
/// <param name="maps">Where the built maps go.</param>
public sealed class GameRunner(string game, string documents, string maps)
{
    /// <summary>
    /// The installed game (<see cref="GameInstall.Place"/>), its documents, and maps built in a short folder of the local
    /// application data, as the game loads no map whose path is longer than <see cref="LongestMapPath"/>.
    /// </summary>
    public static GameRunner Default { get; } = new(
        Path.Combine(GameInstall.Place(), "_retail_", "x86_64", "Warcraft III.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Warcraft III"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MirrorImaged", "maps"));

    /// <summary>The longest map path the game loads from <c>-loadfile</c>; with a longer one it stalls (docs/facts.md).</summary>
    public const int LongestMapPath = 173;

    static readonly TimeSpan Patience = TimeSpan.FromSeconds(3);

    /// <summary>The game's executable.</summary>
    public string Game => game;

    string Data => Path.Combine(documents, "CustomMapData", ProbeMap.Folder);

    /// <summary>
    /// Runs a probe in a game started on its map with <c>-loadfile</c>, and ends that game. One run at a time on this
    /// computer: a second waits for the first.
    /// </summary>
    /// <exception cref="InvalidOperationException">A Warcraft III that this runner didn't start is running.</exception>
    /// <exception cref="ArgumentException">The name is not a probe's, or makes the map's path too long for the game.</exception>
    public ProbeRun Run(string name, string lua, TimeSpan timeout)
    {
        using var one = new Mutex(false, "MirrorImaged.GameRunner");
        try
        {
            one.WaitOne();
        }
        catch (AbandonedMutexException)
        {
            // A runner ended without letting go; the mutex is this one's now, and its game ended with its job.
        }

        try
        {
            if (Process.GetProcessesByName("Warcraft III").Length > 0)
                throw new InvalidOperationException("Warcraft III is already running; the runner uses only a game it starts itself");
            return RunAlone(name, lua, timeout);
        }
        finally
        {
            one.ReleaseMutex();
        }
    }

    ProbeRun RunAlone(string name, string lua, TimeSpan timeout)
    {
        var map = MapPath(maps, name);
        var output = Path.Combine(Data, name + ".pld");
        var loaded = Path.Combine(documents, "CustomMapData", ProbeMap.Loaded);
        var started = Path.Combine(documents, "CustomMapData", ProbeMap.Started);
        var events = new List<string>();
        IReadOnlyList<string>? lines = null;
        var kept = Keep(Kept());
        using var watch = new Watch();
        GameProcess? running = null;
        try
        {
            ProbeMap.Build(name, lua, map);
            Directory.CreateDirectory(Data);
            File.Delete(output);
            File.Delete(loaded);
            File.Delete(started);
            running = GameProcess.Start(game, $"-launch -windowmode windowed -loadfile \"{map}\"", Path.GetDirectoryName(Path.GetDirectoryName(game))!, watch.Desktop);
            lines = Wait(output, loaded, started, timeout, watch, running, events);
            events.Add($"in the game's job: {string.Join(", ", running.Ids().Select(Name))}");
        }
        finally
        {
            if (running is not null)
            {
                events.Add(running.Stop());
                running.Dispose();
            }

            events.AddRange(Restore(kept));
            if (File.Exists(map))
                File.Delete(map);
        }

        var state = lines is not null ? ProbeState.Done : File.Exists(loaded) ? ProbeState.NoOutput : ProbeState.Queued;
        return new ProbeRun(state, lines ?? [], $"""
            {string.Join('\n', events)}
            windows on the desktop: {string.Join("; ", watch.Windows)}
            """);
    }

    // Files of Sven's that the game rewrites: his preferences, and for each account the replays and campaign profiles.
    // Only LastReplay.w3g changed in a run (docs/facts.md), but all are put back as they were.
    List<string> Kept()
    {
        var files = new List<string> { Path.Combine(documents, "War3Preferences.txt") };
        var accounts = Path.Combine(documents, "BattleNet");
        if (Directory.Exists(accounts))
        {
            foreach (var account in Directory.GetDirectories(accounts))
            {
                files.Add(Path.Combine(account, "Replays", "LastReplay.w3g"));
                files.Add(Path.Combine(account, "Replays", "TempReplay.w3g"));
                files.Add(Path.Combine(account, "Campaigns", "Classic", "Classic.w3p"));
                files.Add(Path.Combine(account, "Campaigns", "Reforged", "Reforged.w3p"));
            }
        }

        return files;
    }

    /// <summary>The content of each file as it is, or null for one that doesn't exist.</summary>
    internal static List<(string File, byte[]? Content)> Keep(IEnumerable<string> files) =>
        files.Select(file => (file, File.Exists(file) ? File.ReadAllBytes(file) : null)).ToList();

    /// <summary>
    /// Puts each kept file back as it was, removing one that didn't exist, and says which it changed or couldn't. Never
    /// throws for a file, so it can run when a run fails.
    /// </summary>
    internal static List<string> Restore(List<(string File, byte[]? Content)> kept)
    {
        var changed = new List<string>();
        foreach (var (file, content) in kept)
        {
            try
            {
                if (content is null && File.Exists(file))
                    File.Delete(file);
                else if (content is not null && (!File.Exists(file) || !File.ReadAllBytes(file).AsSpan().SequenceEqual(content)))
                    File.WriteAllBytes(file, content);
                else
                    continue;
                changed.Add($"put back {file}");
            }
            catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
            {
                changed.Add($"could not put back {file}: {problem.Message}");
            }
        }

        return changed;
    }

    /// <summary>A new path for this run's map: the probe's name and a random suffix, so two runs never share one.</summary>
    /// <exception cref="ArgumentException">The path is longer than <see cref="LongestMapPath"/>.</exception>
    internal static string MapPath(string maps, string name)
    {
        var map = Path.Combine(Path.GetFullPath(maps), $"{name}-{Guid.NewGuid().ToString("N")[..8]}.w3x");
        if (map.Length > LongestMapPath)
            throw new ArgumentException($"The game loads no map whose path is longer than {LongestMapPath} characters: {map}", nameof(name));
        return map;
    }

    /// <summary>Whether the map has loaded (its <c>main</c> wrote the loaded file) and its game hasn't started.</summary>
    internal static bool OnLoadingScreen(string loaded, string started) => File.Exists(loaded) && !File.Exists(started);

    // Waits for the probe's lines. A loaded map waits on its loading screen for a key (Sven, 11-08), so while it does, a
    // space key is posted to the game's window every few seconds.
    static IReadOnlyList<string>? Wait(string output, string loaded, string started, TimeSpan timeout, Watch watch, GameProcess running, List<string> events)
    {
        var time = Stopwatch.StartNew();
        var posted = TimeSpan.Zero;
        var posts = 0;
        var waiting = false;
        while (time.Elapsed < timeout && !running.Process.HasExited)
        {
            Thread.Sleep(250);
            watch.Look(running.Ids());
            if (Preload.Lines(ReadShared(output) ?? "") is { } lines)
            {
                events.Add($"the probe's lines came at {time.Elapsed.TotalSeconds:0.0} s");
                return lines;
            }

            if (OnLoadingScreen(loaded, started) != waiting)
            {
                waiting = !waiting;
                events.Add(waiting
                    ? $"the map loaded at {time.Elapsed.TotalSeconds:0.0} s"
                    : $"its game started at {time.Elapsed.TotalSeconds:0.0} s, after {posts} space keys");
                posted = time.Elapsed;
            }

            if (waiting && time.Elapsed - posted >= Patience && watch.Main != 0)
            {
                Win32.PostMessage(watch.Main, Win32.KeyDown, Win32.Space, 0x00390001);
                Win32.PostMessage(watch.Main, Win32.KeyUp, Win32.Space, unchecked((int)0xC0390001));
                posted = time.Elapsed;
                posts++;
            }
        }

        // The file may have come as the wait ended.
        var last = Preload.Lines(ReadShared(output) ?? "");
        events.Add(last is null
            ? $"no lines after {time.Elapsed.TotalSeconds:0.0} s{(running.Process.HasExited ? "; the game had exited" : "")}"
            : $"the probe's lines came as the wait ended, at {time.Elapsed.TotalSeconds:0.0} s");
        return last;
    }

    static string Name(int id)
    {
        try
        {
            using var process = Process.GetProcessById(id);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return $"{id} (exited)";
        }
    }

    static string? ReadShared(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return new StreamReader(stream).ReadToEnd();
        }
        catch (IOException)
        {
            return null;
        }
    }

    // The hidden desktop, made on first use (or opened, when it exists), and what was on it: each look adds every window
    // on it as it was, with the process that owns it, and finds the game's own window.
    sealed class Watch : IDisposable
    {
        nint _desktop;

        public string Desktop
        {
            get
            {
                if (_desktop == 0 && (_desktop = Win32.CreateDesktop(ProbeMap.Folder, 0, 0, 0, Win32.DesktopRights, 0)) == 0)
                    throw new Win32Exception();
                return ProbeMap.Folder;
            }
        }

        public SortedSet<string> Windows { get; } = new(StringComparer.Ordinal);

        public nint Main { get; private set; }

        public void Look(IReadOnlyList<int> ours)
        {
            _ = Desktop;
            bool See(nint window, nint parameter)
            {
                var (name, title) = (Text(window, Win32.GetClassName), Text(window, Win32.GetWindowText));
                Win32.GetWindowThreadProcessId(window, out var owner);
                var shown = $"{name} \"{title}\"{(Win32.IsWindowVisible(window) ? "" : " hidden")} of {(ours.Contains(owner) ? GameRunner.Name(owner) : $"{owner}, not in the game's job")}";
                // A dialog (#32770) is the game, or its error reporter, asking something: what it says is in its child windows.
                if (name == "#32770")
                {
                    var said = new List<string>();
                    Win32.EnumChildWindows(window, (child, _) => { said.Add(Text(child, Win32.GetWindowText)); return true; }, 0);
                    shown += $" saying \"{string.Join(" / ", said.Where(text => text.Length > 0))}\"";
                }

                Windows.Add(shown);
                if (name == "OsWindow" && title == "Warcraft III")
                    Main = window;
                return true;
            }

            Win32.EnumDesktopWindows(_desktop, See, 0);
        }

        static string Text(nint window, Func<nint, StringBuilder, int, int> get)
        {
            var text = new StringBuilder(1024);
            get(window, text, text.Capacity);
            return text.ToString();
        }

        public void Dispose()
        {
            if (_desktop != 0)
                Win32.CloseDesktop(_desktop);
        }
    }
}
