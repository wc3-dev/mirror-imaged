using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using MirrorImaged.Probes;

namespace MirrorImaged.GameTests;

/// <summary>How a probe's run ended.</summary>
public enum ProbeState
{
    /// <summary>The probe ran and its lines were read.</summary>
    Done,

    /// <summary>No map loaded: the game didn't get that far, or no hub game is open. It waits for Sven.</summary>
    Queued,

    /// <summary>The map loaded, but the probe's Preload file didn't come in time.</summary>
    NoOutput,
}

/// <summary>A probe's run: how it ended, its lines, and what the runner saw on the way.</summary>
public sealed record ProbeRun(ProbeState State, IReadOnlyList<string> Lines, string Detail);

/// <summary>
/// Runs probe maps in the installed game, on a Windows desktop of its own that is never shown: builds the map, launches
/// the game on it, presses space for the game while a map waits on its loading screen, waits for the probe's Preload
/// file, stops the game and returns the lines (docs/facts.md, "The game runner").
/// </summary>
/// <param name="game">The game's executable.</param>
/// <param name="documents">The game's documents folder (<c>Documents\Warcraft III</c>).</param>
/// <param name="maps">Where the built maps go.</param>
public sealed class GameRunner(string game, string documents, string maps)
{
    /// <summary>The installed game (in <c>MIRROR_GAME</c>'s folder when it is set), its documents, and maps built beside the tests.</summary>
    public static GameRunner Default { get; } = new(
        Path.Combine(
            Environment.GetEnvironmentVariable("MIRROR_GAME") is { Length: > 0 } folder
                ? folder
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Warcraft III"),
            "_retail_", "x86_64", "Warcraft III.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Warcraft III"),
        Path.Combine(AppContext.BaseDirectory, "maps"));

    static readonly TimeSpan Patience = TimeSpan.FromSeconds(3);

    /// <summary>The game's executable.</summary>
    public string Game => game;

    string Data => Path.Combine(documents, "CustomMapData", ProbeMap.Folder);

    string Requests => Path.Combine(Data, "hub");

    string Loaded => Path.Combine(documents, "CustomMapData", ProbeMap.Loaded);

    string Started => Path.Combine(documents, "CustomMapData", ProbeMap.Started);

    /// <summary>Runs a probe in a game started on its map with <c>-loadfile</c>, and stops that game.</summary>
    /// <exception cref="InvalidOperationException">Warcraft III is already running.</exception>
    public ProbeRun Run(string name, string lua, TimeSpan timeout)
    {
        RefuseWhenRunning();

        // A new path each run, so an older War3Log.txt never shows this run's map opening.
        var map = Path.Combine(maps, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.w3x");
        ProbeMap.Build(name, lua, map);
        var output = Path.Combine(Data, name + ".pld");
        Directory.CreateDirectory(Data);
        File.Delete(output);
        File.Delete(Loaded);
        File.Delete(Started);

        var events = new List<string>();
        IReadOnlyList<string>? lines;
        using var watch = new Watch();
        using var process = Launch(map, watch.Desktop);
        try
        {
            lines = Wait(output, timeout, watch, events, () => process.HasExited);
        }
        finally
        {
            events.Add(Stop(process));
            File.Delete(map);
        }

        // The game holds War3Log.txt back while it runs and loses what it held when it is stopped, so the log seldom
        // shows the opening; the map's own loaded file is what says it loaded.
        var log = ReadShared(Path.Combine(documents, "Logs", "War3Log.txt")) ?? "";
        var state = lines is not null ? ProbeState.Done : File.Exists(Loaded) ? ProbeState.NoOutput : ProbeState.Queued;
        return new ProbeRun(state, lines ?? [], $"""
            War3Log.txt shows the map opened: {Opened(log, map)}
            {string.Join('\n', events)}
            windows on the desktop: {string.Join("; ", watch.Windows)}
            """);
    }

    /// <summary>
    /// Starts the game on a new hub, on the hidden desktop, and leaves it running for <see cref="RunInHub"/>; the caller
    /// stops the process. On 3.0.1.24371 the hub reads its request but <c>ChangeLevel</c> doesn't load the probe from a
    /// <c>-loadfile</c> game (docs/facts.md, "The game runner"), so the hub doesn't work yet.
    /// </summary>
    /// <exception cref="InvalidOperationException">Warcraft III is already running.</exception>
    public Process StartHub()
    {
        RefuseWhenRunning();
        Directory.CreateDirectory(Data);
        File.Delete(Loaded);
        File.Delete(Started);
        using var watch = new Watch();
        var process = Launch(ResetHub(), watch.Desktop);

        // The desktop goes when its last handle closes; once the game's window is on it, the game holds it.
        var time = Stopwatch.StartNew();
        while (watch.Main == 0 && !process.HasExited && time.Elapsed < TimeSpan.FromMinutes(1))
        {
            Thread.Sleep(250);
            watch.Look();
        }

        return process;
    }

    /// <summary>
    /// Runs a probe through the hub: the probe is the next request, and it returns to a hub that waits for the one after.
    /// When no hub picks it up in time the request stays queued, and a hub that opens later runs it.
    /// </summary>
    public ProbeRun RunInHub(string name, string lua, TimeSpan timeout)
    {
        Directory.CreateDirectory(Requests);
        var request = Directory.GetFiles(Requests, "*.pld").Length + 1;
        var hub = Path.Combine(maps, $"hub-{request + 1}.w3x");
        var map = Path.Combine(maps, $"{request}-{name}.w3x");
        ProbeMap.BuildHub(request + 1, hub);
        ProbeMap.Build(name, lua, map, hub);
        var output = Path.Combine(Data, name + ".pld");
        File.Delete(output);

        // Written whole before the hub can see it: the game keeps a file's first content (facts, "Files and Preload").
        var written = Path.Combine(Requests, $"{request}.tmp");
        File.WriteAllText(written, Preload.Request(map));
        File.Move(written, Path.ChangeExtension(written, ".pld"));

        var events = new List<string> { $"request {request}" };
        using var watch = new Watch();
        var lines = Wait(output, timeout, watch, events, () => Process.GetProcessesByName("Warcraft III").Length == 0);
        return lines is null
            ? new ProbeRun(ProbeState.Queued, [], $"{string.Join('\n', events)}\nthe request waits in {Requests}: is the hub's game open?")
            : new ProbeRun(ProbeState.Done, lines, string.Join('\n', events));
    }

    /// <summary>Empties the hub's requests and builds the hub that waits for request 1.</summary>
    internal string ResetHub()
    {
        if (Directory.Exists(Requests))
            Directory.Delete(Requests, true);
        var hub = Path.Combine(maps, "hub-1.w3x");
        ProbeMap.BuildHub(1, hub);
        return hub;
    }

    /// <summary>Whether <c>War3Log.txt</c> shows the game opening the map after it activated the WebUI.</summary>
    /// <remarks>At start the game opens every map under <c>Documents\Warcraft III\Maps</c>, before the WebUI.</remarks>
    internal static bool Opened(string log, string map)
    {
        const string Opening = "Opening map - ";
        var webUi = false;
        foreach (var line in log.Split('\n'))
        {
            if (line.Contains("Activating WebUI", StringComparison.Ordinal))
                webUi = true;
            else if (webUi && line.IndexOf(Opening, StringComparison.Ordinal) is var at and >= 0
                && line[(at + Opening.Length)..].TrimEnd().Replace('\\', '/').Equals(map.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Whether a map has loaded (its <c>main</c> wrote the loaded file) and its game hasn't started since.</summary>
    internal static bool OnLoadingScreen(string loaded, string started) =>
        File.Exists(loaded) && (!File.Exists(started) || File.GetLastWriteTimeUtc(started) < File.GetLastWriteTimeUtc(loaded));

    // Waits for the probe's lines. A loaded map waits on its loading screen for a key (Sven, 11-08), so while one does,
    // a space key is posted to the game's window every few seconds.
    IReadOnlyList<string>? Wait(string output, TimeSpan timeout, Watch watch, List<string> events, Func<bool> gone)
    {
        var time = Stopwatch.StartNew();
        var posted = TimeSpan.Zero;
        var posts = 0;
        var waiting = false;
        while (time.Elapsed < timeout && !gone())
        {
            Thread.Sleep(250);
            watch.Look();
            if (Preload.Lines(ReadShared(output) ?? "") is { } lines)
            {
                events.Add($"the probe's lines came at {time.Elapsed.TotalSeconds:0.0} s");
                return lines;
            }

            if (OnLoadingScreen(Loaded, Started) != waiting)
            {
                waiting = !waiting;
                events.Add(waiting
                    ? $"a map loaded at {time.Elapsed.TotalSeconds:0.0} s"
                    : $"its game started at {time.Elapsed.TotalSeconds:0.0} s, after {posts} space keys");
                posted = time.Elapsed;
                posts = 0;
            }

            if (waiting && time.Elapsed - posted >= Patience && watch.Main != 0)
            {
                Win32.PostMessage(watch.Main, Win32.KeyDown, Win32.Space, 0x00390001);
                Win32.PostMessage(watch.Main, Win32.KeyUp, Win32.Space, unchecked((int)0xC0390001));
                posted = time.Elapsed;
                posts++;
            }
        }

        events.Add($"no lines after {time.Elapsed.TotalSeconds:0.0} s{(gone() ? "; no game was running" : "")}");
        return null;
    }

    static void RefuseWhenRunning()
    {
        if (Process.GetProcessesByName("Warcraft III").Length > 0)
            throw new InvalidOperationException("Warcraft III is already running; the runner uses only a game it starts itself");
    }

    Process Launch(string map, string desktop)
    {
        var startup = new Win32.StartupInfo { Size = Marshal.SizeOf<Win32.StartupInfo>(), Desktop = desktop };
        var command = new StringBuilder($"\"{game}\" -launch -windowmode windowed -loadfile \"{map}\"");
        if (!Win32.CreateProcess(game, command, 0, 0, false, 0, 0, Path.GetDirectoryName(Path.GetDirectoryName(game)), ref startup, out var started))
            throw new Win32Exception();

        try
        {
            var process = Process.GetProcessById(started.ProcessId);
            _ = process.Handle; // held from now on, so the exit code can be read after the game exits
            return process;
        }
        finally
        {
            Win32.CloseHandle(started.Thread);
            Win32.CloseHandle(started.Process);
        }
    }

    // Killed with the BlizzardBrowser processes it started: closing its window opens the game's quit dialog, which waits
    // for a person (docs/facts.md).
    static string Stop(Process process)
    {
        var exited = process.HasExited;
        if (!exited)
            process.Kill(entireProcessTree: true);

        // A game that exits by itself leaves its WebUI browsers running, and one that crashes leaves its error reporter
        // showing; only a game starts them, and only this one runs.
        foreach (var helper in Process.GetProcessesByName("BlizzardBrowser").Concat(Process.GetProcessesByName("BlizzardError")))
        {
            try
            {
                if (helper.StartTime >= process.StartTime)
                    helper.Kill();
            }
            catch (InvalidOperationException)
            {
                // It had exited.
            }
        }

        process.WaitForExit(10_000);
        return exited ? $"the game had exited by itself with 0x{process.ExitCode:X8}" : "the game was stopped";
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

    // The hidden desktop, opened on first use (it is made when it doesn't exist), and what was on it: each look adds its
    // windows as they were and finds the game's own.
    sealed class Watch : IDisposable
    {
        nint _desktop;

        public string Desktop
        {
            get
            {
                if (_desktop == 0 && (_desktop = Win32.CreateDesktop(ProbeMap.Folder, 0, 0, 0, Win32.GenericAll, 0)) == 0)
                    throw new Win32Exception();
                return ProbeMap.Folder;
            }
        }

        public SortedSet<string> Windows { get; } = new(StringComparer.Ordinal);

        public nint Main { get; private set; }

        public void Look()
        {
            _ = Desktop;
            bool See(nint window, nint parameter)
            {
                var (name, title) = (Text(window, Win32.GetClassName), Text(window, Win32.GetWindowText));
                var shown = $"{name} \"{title}\"{(Win32.IsWindowVisible(window) ? "" : " hidden")}";
                // A message box (#32770) is the game reporting an error: what it says is in its child windows.
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
