using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
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

    /// <summary>
    /// The game never got past its Battle.net login: it didn't open the account (rewrite <c>War3Preferences.txt</c>). It
    /// needs Sven to look at the game; the detail says what it is known to show then.
    /// </summary>
    AtLogin,

    /// <summary>The map loaded, but the probe's Preload file didn't come in time.</summary>
    NoOutput,

    /// <summary>The probe's game started and then exited by itself before its lines came: it closed, as on some calls (docs/facts.md).</summary>
    Closed,
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

    /// <summary>The game version the runner's facts were measured on.</summary>
    public const string MeasuredOn = "3.0.1.24371";

    /// <summary>The name of the mutex a run holds, so runs on one computer take turns.</summary>
    public const string OneRunAtATime = "MirrorImaged.GameRunner";

    static readonly TimeSpan Patience = TimeSpan.FromSeconds(3);

    // Well inside the 10 minutes after which the test host's blame collector ends a run (tests/Tests.runsettings).
    static readonly TimeSpan LongestTurnWait = TimeSpan.FromMinutes(5);

    const string AtLoginSays =
        "needs Sven: the game never got past its Battle.net login (it didn't open the account). Seen every time the runner " +
        "ran under Claude Code's Bash tool, where the game's browser loads web pages from outside as a login page would, " +
        "and never from PowerShell (docs/facts.md): run the game tests from PowerShell or a normal terminal; if it happens " +
        "there, Sven looks at the game in a normal window (nobody else types a password into it)";

    /// <summary>The game's executable.</summary>
    public string Game => game;

    /// <summary>The folder the probes write to, <c>CustomMapData\MirrorImaged</c> in the game's documents.</summary>
    public string Data => Path.Combine(documents, "CustomMapData", ProbeMap.Folder);

    // Beside the maps' folder: the hashes of the account's files that probe runs made, one a line (named when it held
    // only replays).
    string ProbeFiles => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(maps))!, "probe-replays.txt");

    /// <summary>
    /// Runs a probe in a game started on its map with <c>-loadfile</c>, and ends that game. One run at a time on this
    /// computer: a second waits for the first.
    /// </summary>
    /// <exception cref="InvalidOperationException">A Warcraft III that this runner didn't start is running.</exception>
    /// <exception cref="ArgumentException">The name is not a probe's, or makes the map's path too long for the game.</exception>
    /// <exception cref="TimeoutException">Another run kept the game longer than five minutes.</exception>
    /// <param name="withComputer">The probe map with a computer and named slots (<see cref="ProbeMap.Build"/>).</param>
    /// <param name="change">Changes the probe's map before it is written (<see cref="ProbeMap.Build"/>).</param>
    /// <param name="jass">The probe is JASS (<see cref="ProbeMap.BuildJass"/>), <paramref name="lua"/> its script.</param>
    public ProbeRun Run(string name, string lua, TimeSpan timeout, bool withComputer = false, Action<War3Net.Build.Map>? change = null, bool jass = false)
    {
        using var one = new Mutex(false, OneRunAtATime);
        bool mine;
        try
        {
            mine = one.WaitOne(LongestTurnWait);
        }
        catch (AbandonedMutexException)
        {
            // A runner ended without letting go; the mutex is this one's now, and its game ended with its job.
            mine = true;
        }

        if (!mine)
            throw new TimeoutException($"Another run has held the game for {LongestTurnWait.TotalMinutes} minutes (mutex {OneRunAtATime}); wait for it, or end the process that holds it");

        try
        {
            if (Process.GetProcessesByName("Warcraft III").Length > 0)
                throw new InvalidOperationException("Warcraft III is already running; the runner uses only a game it starts itself");
            return RunAlone(name, lua, timeout, withComputer, change, jass);
        }
        finally
        {
            one.ReleaseMutex();
        }
    }

    ProbeRun RunAlone(string name, string lua, TimeSpan timeout, bool withComputer, Action<War3Net.Build.Map>? change, bool jass)
    {
        var map = MapPath(maps, name);
        var output = Path.Combine(Data, name + ".pld");
        var loaded = Path.Combine(documents, "CustomMapData", ProbeMap.Loaded);
        var started = Path.Combine(documents, "CustomMapData", ProbeMap.Started);
        var events = new List<string> { Version() };
        IReadOnlyList<string>? lines = null;

        // The probe's game replaces the account's last replay and rewrites its campaigns' profiles, and SaveGameCache
        // rewrites a Campaigns.w3v (facts): Sven's are copied aside first. Nothing is ever put back.
        var files = AccountFiles(documents);
        events.AddRange(SaveFiles(files, ProbeFiles).Select(copy => $"saved Sven's file as {copy}"));

        // The game rewrites the preferences once it has logged in to Battle.net and opened the account (facts).
        var preferences = Path.Combine(documents, "War3Preferences.txt");
        var preferencesBefore = File.Exists(preferences) ? File.GetLastWriteTimeUtc(preferences) : DateTime.MinValue;
        bool LoggedIn() => File.Exists(preferences) && File.GetLastWriteTimeUtc(preferences) != preferencesBefore;
        var loggedIn = false;
        var stillRunning = false;
        var gameStarted = false;

        using var watch = new Watch();
        GameProcess? running = null;
        try
        {
            if (jass) ProbeMap.BuildJass(name, lua, map);
            else ProbeMap.Build(name, lua, map, withComputer, change);
            Directory.CreateDirectory(Data);
            File.Delete(output);
            File.Delete(loaded);
            File.Delete(started);
            running = GameProcess.Start(game, $"-launch -windowmode windowed -loadfile \"{map}\"", Path.GetDirectoryName(Path.GetDirectoryName(game))!, watch.Desktop);
            lines = Wait(output, loaded, started, timeout, watch, running, LoggedIn, events);
            loggedIn = LoggedIn();
            stillRunning = !running.Process.HasExited;
            gameStarted = File.Exists(started);
            events.Add($"in the game's job: {string.Join(", ", running.Ids().Select(Name))}");
        }
        finally
        {
            if (running is not null)
            {
                events.Add(running.Stop());
                running.Dispose();
            }

            // A file that changed during the run is the probe's, unless another Warcraft III ran: then it may be Sven's,
            // and the next run saves it.
            if (watch.Foreign)
                events.Add("another Warcraft III ran, so no file was noted as a probe's");
            else
                NoteProbeFiles(files, ProbeFiles);
            if (File.Exists(map))
                File.Delete(map);
        }

        var state = Ending(lines is not null, File.Exists(loaded), gameStarted, stillRunning, loggedIn);
        if (state == ProbeState.AtLogin)
            events.Insert(0, AtLoginSays);
        return new ProbeRun(state, lines ?? [], $"""
            {string.Join('\n', events)}
            windows on the desktop: {string.Join("; ", watch.Windows)}
            """);
    }

    // The installed build, with a warning when it isn't the one the facts were measured on.
    string Version()
    {
        string version;
        try
        {
            version = GameInstall.Build(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(game)))!).Version;
        }
        catch (Exception problem) when (problem is IOException or InvalidDataException)
        {
            version = $"unknown ({problem.Message})";
        }

        return version == MeasuredOn
            ? $"game {version}"
            : $"game {version}; warning: the facts this runner relies on were measured on {MeasuredOn} (docs/facts.md)";
    }

    // Each account's LastReplay.w3g and each of its campaigns' Campaigns.w3v and profile (named after the campaign's folder:
    // Classic\Classic.w3p), with each one's hash before the run (null when there is none).
    internal static List<(string File, string? Hash)> AccountFiles(string documents)
    {
        var accounts = Path.Combine(documents, "BattleNet");
        if (!Directory.Exists(accounts))
            return [];
        return Directory.GetDirectories(accounts).SelectMany(account =>
            {
                var campaigns = Path.Combine(account, "Campaigns");
                var saves = Directory.Exists(campaigns)
                    ? Directory.GetDirectories(campaigns).SelectMany(campaign => new[] { Path.Combine(campaign, "Campaigns.w3v"), Path.Combine(campaign, Path.GetFileName(campaign) + ".w3p") })
                    : [];
                return saves.Prepend(Path.Combine(account, "Replays", "LastReplay.w3g"));
            })
            .Select(file => (file, Hash(file))).ToList();
    }

    /// <summary>
    /// Copies each file, by its hash before the run, that no probe run made (the hash isn't in <paramref name="probeFiles"/>) to
    /// <c>Saved by MirrorImaged\&lt;its name&gt; &lt;its write time&gt;&lt;its extension&gt;</c> beside it
    /// (<c>LastReplay 2026-11-08 011715.w3g</c>), or with the start of its hash added when another file holds that name;
    /// never over a file, and not again when a copy there has its content, whatever its name. Says which copies it made.
    /// </summary>
    internal static List<string> SaveFiles(IEnumerable<(string File, string? Hash)> files, string probeFiles)
    {
        var probes = File.Exists(probeFiles) ? File.ReadAllLines(probeFiles).ToHashSet(StringComparer.Ordinal) : [];
        var saved = new List<string>();
        var copied = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (file, hash) in files)
        {
            if (hash is null || probes.Contains(hash))
                continue;
            var folder = Path.Combine(Path.GetDirectoryName(file)!, "Saved by MirrorImaged");
            // Every game rewrites the profiles with the same bytes (facts), so their write time alone would copy them each run.
            if (!copied.TryGetValue(folder, out var there))
                copied[folder] = there = Directory.Exists(folder) ? Directory.EnumerateFiles(folder).Select(copy => Hash(copy)!).ToHashSet(StringComparer.Ordinal) : [];
            if (!there.Add(hash))
                continue;
            var name = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(file)} {File.GetLastWriteTime(file).ToString("yyyy-MM-dd HHmmss", CultureInfo.InvariantCulture)}");
            var extension = Path.GetExtension(file);
            Directory.CreateDirectory(folder);

            // Under its write time, or, when another file holds that name, with the start of its hash too.
            foreach (var copy in new[] { name + extension, $"{name} {hash[..8]}{extension}" })
            {
                if (File.Exists(copy))
                    continue;

                try
                {
                    File.Copy(file, copy, overwrite: false);
                    saved.Add(copy);
                    break;
                }
                catch (IOException) when (File.Exists(copy))
                {
                    // Made by another run in the meantime: never written over.
                }
            }
        }

        return saved;
    }

    /// <summary>Adds the hash of each file that is no longer what it was before the run to <paramref name="probeFiles"/>.</summary>
    internal static void NoteProbeFiles(IEnumerable<(string File, string? Hash)> before, string probeFiles)
    {
        var made = before.Select(file => (Before: file.Hash, Now: Hash(file.File)))
            .Where(file => file.Now is not null && file.Now != file.Before)
            .Select(file => file.Now!)
            .ToList();
        if (made.Count > 0)
            File.AppendAllLines(probeFiles, made);
    }

    static string? Hash(string file) => File.Exists(file) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) : null;

    /// <summary>A new path for this run's map: the probe's name and a random suffix, so two runs never share one.</summary>
    /// <exception cref="ArgumentException">The path is longer than <see cref="LongestMapPath"/>.</exception>
    internal static string MapPath(string maps, string name)
    {
        var map = Path.Combine(Path.GetFullPath(maps), $"{name}-{Guid.NewGuid().ToString("N")[..8]}.w3x");
        if (map.Length > LongestMapPath)
            throw new ArgumentException($"The game loads no map whose path is longer than {LongestMapPath} characters: {map}", nameof(name));
        return map;
    }

    /// <summary>
    /// How a run ended: with its lines; with the game exited by itself after the probe's game started (its
    /// <c>started.pld</c> came, which the probes closing the game write before they close it); with the map loaded but
    /// no lines; with the game still running and never logged in; or otherwise without the map loaded.
    /// </summary>
    internal static ProbeState Ending(bool lines, bool loaded, bool started, bool stillRunning, bool loggedIn) =>
        lines ? ProbeState.Done
        : started && !stillRunning ? ProbeState.Closed
        : loaded ? ProbeState.NoOutput
        : stillRunning && !loggedIn ? ProbeState.AtLogin
        : ProbeState.Queued;

    /// <summary>Whether the map has loaded (its <c>main</c> wrote the loaded file) and its game hasn't started.</summary>
    internal static bool OnLoadingScreen(string loaded, string started) => File.Exists(loaded) && !File.Exists(started);

    // Waits for the probe's lines. A loaded map waits on its loading screen for a key (Sven, 11-08), so while it does, a
    // space key is posted to the game's window every few seconds.
    static IReadOnlyList<string>? Wait(
        string output, string loaded, string started, TimeSpan timeout, Watch watch, GameProcess running, Func<bool> loggedIn,
        List<string> events)
    {
        var time = Stopwatch.StartNew();
        var posted = TimeSpan.Zero;
        var posts = 0;
        var waiting = false;
        var account = false;
        while (time.Elapsed < timeout && !running.Process.HasExited)
        {
            Thread.Sleep(250);
            watch.Look(running.Ids());
            if (!account && loggedIn())
            {
                account = true;
                events.Add($"the game logged in and opened the account at {time.Elapsed.TotalSeconds:0.0} s");
            }

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
        catch (Exception problem) when (problem is ArgumentException or InvalidOperationException)
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

        /// <summary>Whether a Warcraft III outside the game's job ran at a look: Sven's own game, or World Editor's test.</summary>
        public bool Foreign { get; private set; }

        public void Look(IReadOnlyList<int> ours)
        {
            _ = Desktop;
            foreach (var other in Process.GetProcessesByName("Warcraft III"))
            {
                Foreign |= !ours.Contains(other.Id);
                other.Dispose();
            }


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
