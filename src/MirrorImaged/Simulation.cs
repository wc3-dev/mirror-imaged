using System.Reflection;
using MirrorImaged.Data;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using MirrorImaged.World;
using War3Net.Build.Info;

namespace MirrorImaged;

/// <summary>How a run is set up.</summary>
public sealed class SimulationOptions
{
    /// <summary>The seed of the game's random numbers: the same map, seed and input give the same run.</summary>
    public int Seed { get; init; }

    /// <summary>
    /// The slots people play, 0 to 23, the first of them the local player, whose game the run is; null for a
    /// single-player game in the map's first user slot.
    /// </summary>
    public IReadOnlyList<int>? Users { get; init; }

    /// <summary>The slots computers play; null for the map's computer slots that aren't among <see cref="Users"/>.</summary>
    public IReadOnlyList<int>? Computers { get; init; }

    /// <summary>
    /// The simulated <c>Documents\Warcraft III</c> the run reads and writes: its <c>CustomMapData</c> files and saved
    /// game caches. Give the same one to the next run to keep them.
    /// </summary>
    public Documents Documents { get; init; } = new();
}

/// <summary>
/// A map's script running on a simulated game, with no game open. <see cref="Load"/> sets <c>common.j</c>'s globals,
/// runs the installed game's <c>blizzard.j</c>, then the script, its <c>config</c> and <c>main</c>;
/// <see cref="Advance"/> moves the clock on and runs the timers that fall due. A Lua error goes in the log and the run
/// carries on, as the game drops it. A Missing native stops the run, and so does a call the game closes on:
/// <see cref="Stopped"/> is then a <see cref="LuaException"/> that starts with the calling line
/// (<c>war3map.lua:3: The native ...</c>), whose inner exception is the <see cref="MissingNativeException"/> or the
/// <see cref="GameClosedException"/>, and <see cref="Advance"/> throws it. The log keeps what came before.
/// </summary>
public sealed partial class Simulation : IDisposable
{
    readonly Game _game;
    readonly GameNatives _natives;
    readonly LuaState _lua = new() { Halts = e => e is MissingNativeException or GameClosedException };
    TimeSpan _time;

    Simulation(GameData data, string blizzard, SimulationOptions options)
    {
        var info = data.Map!.Files.Info;
        _game = new Game(options.Seed);
        _natives = new GameNatives(_game) { Threads = new ScriptThreads(this), Documents = options.Documents, RunPreloadFile = RunPreloadFile, Data = data };
        try
        {
            var (users, computers, others) = Lineup(info, options);
            foreach (var file in data.Map.Unread) _game.Write("map", "left out, as it can't be read: " + file);
            foreach (var native in NativeMethods.OrderBy(method => method.Name, StringComparer.Ordinal))
                _lua.Register(native.Name, Bind(native));
            foreach (var (name, value) in _natives.Globals())
                _lua[name] = value is handle h ? ToLua(h, h.GetType()) : value;
            AddLuaExtras();
            AddPreloader();
            _lua.Run(blizzard, "blizzard.lua");
            Protect(() => _lua.Run(data.Map.Script!, "war3map.lua"));
            Protect(() => _lua.Call("config"));
            var flags = info?.MapFlags ?? 0;
            _natives.Seat(users, computers, others, flags.HasFlag(MapFlags.UseCustomForces), flags.HasFlag(MapFlags.FixedPlayerSettingsForCustomForces),
                info?.Players.Where(p => p.Controller == PlayerController.Computer).DistinctBy(p => p.Id).ToDictionary(p => p.Id, p => data.Map.Resolve(p.Name)));
            Protect(() => _lua.Call("main"));
        }
        catch (LuaException e) when (_lua.Halted is not null)
        {
            Stopped = e;
        }
        catch
        {
            _lua.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Loads the map at <paramref name="path"/> and runs <c>blizzard.j</c>, its script and <c>config</c>, seats the
    /// players as the lobby does (<see cref="SimulationOptions.Users"/>, from the map's <c>war3map.w3i</c> unless the
    /// options say), and runs <c>main</c>; if a Missing native stops the run there, the simulation comes back
    /// <see cref="Stopped"/>.
    /// </summary>
    /// <exception cref="FileNotFoundException">The map has no <c>war3map.lua</c>.</exception>
    /// <exception cref="InvalidDataException">
    /// A file of the map can't be read, other than the terrain, doodad and placed-unit files, which are left out
    /// (<see cref="Unread"/>); the message names it.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">No Warcraft III is installed, whose <c>blizzard.j</c> and standard data every run needs.</exception>
    /// <exception cref="LuaException"><c>blizzard.j</c>'s Lua doesn't load or run (<c>blizzard.lua:N: ...</c>), so no map can.</exception>
    /// <exception cref="ArgumentException">
    /// The options seat no person, a slot twice, a slot the map's <c>war3map.w3i</c> doesn't have, a person in a slot
    /// the map gives a computer, or anyone in its neutral or rescuable slots; checked before any of the map's Lua runs.
    /// </exception>
    public static Simulation Load(string path, SimulationOptions? options = null)
    {
        var map = Read(path); // first, so a missing map says so whether or not the game is installed
        var blizzard = Blizzard.Lua();
        return new(new GameData(StandardData.Installed(GameInstall.Require(), map.Files.Info), map), blizzard, options ?? new SimulationOptions());
    }

    /// <summary>
    /// As <see cref="Load(string, SimulationOptions?)"/>, with <paramref name="blizzard"/> run in place of the
    /// installed game's <c>blizzard.j</c> and <paramref name="data"/> in place of its standard data (none when null):
    /// <c>""</c> and the data a test gives for the tests of the runtime itself, which run without the game.
    /// </summary>
    internal static Simulation Load(string path, string blizzard, SimulationOptions? options = null, StandardData? data = null)
    {
        var map = Read(path);
        return new(new GameData(data ?? new StandardData(), map), blizzard, options ?? new SimulationOptions());
    }

    static MapArchive Read(string path) =>
        MapArchive.Read(path) is { Script: not null } map ? map : throw new FileNotFoundException($"{path} has no war3map.lua; only Lua maps run so far", path);

    // Who the lobby seats: the people and computers the options name, else the map's first user and its computers, and
    // the map's neutral and rescuable slots, which always play. Checked against war3map.w3i's slots before any Lua runs:
    // a person only where the map has a user, a computer where it has a user or a computer.
    static (IReadOnlyList<int> Users, IReadOnlyList<int> Computers, IReadOnlyList<int> Others) Lineup(MapInfo? info, SimulationOptions options)
    {
        var players = info?.Players ?? [];
        var users = options.Users ?? [players.FirstOrDefault(p => p.Controller == PlayerController.User)?.Id ?? 0];
        var computers = options.Computers ?? [.. players.Where(p => p.Controller == PlayerController.Computer).Select(p => p.Id).Except(users)];
        GameNatives.CheckSeats(users, computers);
        if (info is not null)
            foreach (var (slot, person) in users.Select(slot => (slot, true)).Concat(computers.Select(slot => (slot, false))))
            {
                var controller = players.FirstOrDefault(p => p.Id == slot)?.Controller;
                if (controller is null or PlayerController.None)
                    throw new ArgumentException($"Slot {slot} isn't one of the map's players in war3map.w3i ({string.Join(", ", players.Select(p => p.Id))})");
                if (controller != PlayerController.User && (person || controller != PlayerController.Computer))
                    throw new ArgumentException($"A {(person ? "person" : "computer")} can't play slot {slot}: the map gives it to a {controller.ToString()!.ToLowerInvariant()} player");
            }
        return (users, computers, [.. players.Where(p => p.Controller is PlayerController.Neutral or PlayerController.Rescuable).Select(p => p.Id)]);
    }

    /// <summary>
    /// Whether the map has paused the game (<c>PauseGame</c>): its clock doesn't move, and only the map can go on, as in
    /// the game; until dialogs and input exist (waves 5 and 6), only what was due in the step it paused in still runs.
    /// </summary>
    public bool Paused => _game.Paused;

    /// <summary>
    /// The map's files left out as they can't be read (<see cref="MapArchive.Unread"/>), each also the run's first log
    /// entries: <c>0.000 map: left out, as it can't be read: war3map.w3e: ...</c>.
    /// </summary>
    public IReadOnlyList<string> Unread => _natives.Data.Map!.Unread;

    /// <summary>
    /// Whether the map has ended the game (<c>EndGame</c>): the rest of that step ran, and the clock doesn't move again.
    /// </summary>
    public bool Ended => _game.Ended;

    /// <summary>The documents the run reads and writes, <see cref="SimulationOptions.Documents"/>.</summary>
    public Documents Documents => _natives.Documents;

    /// <summary>The natives the run's script calls.</summary>
    internal GameNatives Natives => _natives;

    /// <summary>
    /// What the run showed, an entry each: the time in seconds, where it came from (<c>print</c>, <c>player 0</c> for
    /// text shown to a player, <c>error</c> for a Lua error) and the text, every line of it so marked.
    /// </summary>
    public IReadOnlyList<string> Log => _game.Log;

    /// <summary>What stopped the run, if something has: no more of it can run.</summary>
    public LuaException? Stopped { get; private set; }

    /// <summary>
    /// Moves the clock on by <paramref name="time"/>. The clock moves in whole steps of 1/1024 s, to the step nearest
    /// all the time asked for so far, so many short advances add up as one long one. A paused game's clock doesn't
    /// move, and once the game goes on it moves from where it stopped; an ended game's never moves again.
    /// </summary>
    /// <exception cref="LuaException">The run has stopped, or a Missing native stopped it now: <see cref="Stopped"/>.</exception>
    public void Advance(TimeSpan time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, TimeSpan.Zero);
        if (Stopped is not null) throw Stopped;
        _time += time;
        try { _game.Advance(Game.Steps(_time.TotalSeconds) - _game.Step); }
        catch (LuaException e) when (_lua.Halted is not null)
        {
            Stopped = e;
            throw;
        }
        // Paused time isn't the game's, so the next advance goes on from where it stopped.
        if (_game.Paused || _game.Ended) _time = TimeSpan.FromSeconds((double)_game.Step / Game.StepsPerSecond);
    }

    public void Dispose() => _lua.Dispose();

    // Runs Lua with os.clock and os.time on the game's clock; an error goes in the log, unless it stops the run.
    void Protect(Action run)
    {
        _lua.Clock = TimeSpan.FromMilliseconds(_game.Step * 1000 / Game.StepsPerSecond);
        try { run(); }
        catch (LuaException e) when (_lua.Halted is null)
        {
            _game.Write("error", e.Message.Split("\nstack traceback:")[0]);
        }
    }

    // Runs a Lua thread as Protect runs Lua: null when it failed
    LuaThread? Started(Func<LuaThread> start)
    {
        LuaThread? thread = null;
        Protect(() => thread = start());
        return thread;
    }

    /// <summary>
    /// A Lua function a native keeps, such as a timer's or a condition's. It runs in a thread of its own, as the game
    /// runs a callback, where a wait never ends, unless a native starts it to run it on (decisions D70).
    /// </summary>
    sealed class LuaCode(Simulation simulation, LuaFunction function) : IScriptFunction, IDisposable
    {
        public void Run() => simulation.Started(function.Start)?.Dispose();

        // A condition passes on true or a number other than 0, and fails on anything else (probes triggers, triggers-2)
        public bool Evaluate()
        {
            using var thread = simulation.Started(function.Start);
            return thread is { Waiting: false, Results: [true or int and not 0 or float and not 0f, ..] };
        }

        public IScriptThread? Start() => Waiting.Of(simulation, simulation.Started(function.Start));

        public void Dispose() => function.Dispose();
    }

    /// <summary>A Lua thread waiting, which runs on as Protect runs Lua.</summary>
    sealed class Waiting(Simulation simulation, LuaThread thread) : IScriptThread
    {
        public static Waiting? Of(Simulation simulation, LuaThread? thread) => thread is { Waiting: true } ? new(simulation, thread) : null;

        public bool Resume()
        {
            simulation.Protect(thread.Resume);
            return thread.Waiting;
        }

        public void Dispose() => thread.Dispose();
    }

    /// <summary>The threads ExecuteFunc's function runs in, and the wait of any of the script's threads.</summary>
    sealed class ScriptThreads(Simulation simulation) : IScriptThreads
    {
        public IScriptThread? Start(string name) => Waiting.Of(simulation, simulation.Started(() => simulation._lua.Start(name)));

        public void Yield() => simulation._lua.Yield();
    }
}
