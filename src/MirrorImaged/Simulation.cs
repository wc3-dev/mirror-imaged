using System.Reflection;
using MirrorImaged.Data;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged;

/// <summary>How a run is set up.</summary>
public sealed class SimulationOptions
{
    /// <summary>The seed of the game's random numbers: the same map, seed and input give the same run.</summary>
    public int Seed { get; init; }
}

/// <summary>
/// A map's script running on a simulated game, with no game open. <see cref="Load"/> sets <c>common.j</c>'s globals,
/// runs the installed game's <c>blizzard.j</c>, then the script, its <c>config</c> and <c>main</c>;
/// <see cref="Advance"/> moves the clock on and runs the timers that fall due. A Lua error goes in the log and the run
/// carries on, as the game drops it. A Missing native stops the run:
/// <see cref="Stopped"/> is then a <see cref="LuaException"/> that starts with the calling line
/// (<c>war3map.lua:3: The native ...</c>), whose inner exception is the <see cref="MissingNativeException"/>, and
/// <see cref="Advance"/> throws it. The log keeps what came before.
/// </summary>
public sealed partial class Simulation : IDisposable
{
    readonly Game _game;
    readonly GameNatives _natives;
    readonly LuaState _lua = new() { Halts = e => e is MissingNativeException };
    TimeSpan _time;

    Simulation(byte[] script, string blizzard, SimulationOptions options)
    {
        _game = new Game(options.Seed);
        _natives = new GameNatives(_game);
        try
        {
            foreach (var native in typeof(GameNatives).GetMethods()
                         .Where(method => method.GetCustomAttribute<NativeAttribute>() is not null)
                         .OrderBy(method => method.Name, StringComparer.Ordinal))
                _lua.Register(native.Name, Bind(native));
            foreach (var (name, value) in _natives.Globals())
                _lua[name] = value is handle h ? new LuaHandle(h.GetType().Name, h.Key) : value;
            AddLuaExtras();
            _lua.Run(blizzard, "blizzard.lua");
            Protect(() => _lua.Run(script, "war3map.lua"));
            Protect(() => _lua.Call("config"));
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
    /// Loads the map at <paramref name="path"/> and runs <c>blizzard.j</c>, its script, <c>config</c> and
    /// <c>main</c>; if a Missing native stops the run there, the simulation comes back <see cref="Stopped"/>.
    /// </summary>
    /// <exception cref="FileNotFoundException">The map has no <c>war3map.lua</c>.</exception>
    /// <exception cref="DirectoryNotFoundException">No Warcraft III is installed, whose <c>blizzard.j</c> every run needs.</exception>
    /// <exception cref="LuaException"><c>blizzard.j</c>'s Lua doesn't load or run (<c>blizzard.lua:N: ...</c>), so no map can.</exception>
    public static Simulation Load(string path, SimulationOptions? options = null)
    {
        var script = MapArchive.ReadLua(path); // first, so a missing map says so whether or not the game is installed
        return new(script, Blizzard.Lua(), options ?? new SimulationOptions());
    }

    /// <summary>
    /// As <see cref="Load(string, SimulationOptions?)"/>, with <paramref name="blizzard"/> run in place of the
    /// installed game's <c>blizzard.j</c>: <c>""</c> for the tests of the runtime itself, which run without the game.
    /// </summary>
    internal static Simulation Load(string path, string blizzard, SimulationOptions? options = null) =>
        new(MapArchive.ReadLua(path), blizzard, options ?? new SimulationOptions());

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
    /// all the time asked for so far, so many short advances add up as one long one.
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

    /// <summary>A Lua function a native keeps, such as a timer's: it runs as the game runs a callback.</summary>
    sealed class LuaCode(Simulation simulation, LuaFunction function) : IDisposable
    {
        public void Run() => simulation.Protect(() => function.Call());

        public void Dispose() => function.Dispose();
    }
}
