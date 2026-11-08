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
/// A map's script running on a simulated game, with no game open. <see cref="Load"/> runs the script, then its
/// <c>config</c> and <c>main</c>; <see cref="Advance"/> moves the clock on and runs the timers that fall due. A Lua
/// error goes in the log and the run carries on, as the game drops it. A Missing native stops the run: the call
/// throws a <see cref="LuaException"/> that starts with the calling line (<c>war3map.lua:3: The native ...</c>), whose
/// inner exception is the <see cref="MissingNativeException"/>, and the simulation can't run on.
/// </summary>
public sealed partial class Simulation : IDisposable
{
    readonly Game _game;
    readonly GameNatives _natives;
    readonly LuaState _lua = new() { Halts = e => e is MissingNativeException };

    Simulation(byte[] script, SimulationOptions options)
    {
        _game = new Game(options.Seed);
        _natives = new GameNatives(_game);
        try
        {
            foreach (var native in typeof(GameNatives).GetMethods()
                         .Where(method => method.GetCustomAttribute<NativeAttribute>() is not null)
                         .OrderBy(method => method.Name, StringComparer.Ordinal))
                _lua.Register(native.Name, Bind(native));
            AddLuaExtras();
            Protect(() => _lua.Run(script, "war3map.lua"));
            Protect(() => _lua.Call("config"));
            Protect(() => _lua.Call("main"));
        }
        catch
        {
            _lua.Dispose();
            throw;
        }
    }

    /// <summary>Loads the map at <paramref name="path"/> and runs its script, <c>config</c> and <c>main</c>.</summary>
    /// <exception cref="FileNotFoundException">The map has no <c>war3map.lua</c>.</exception>
    /// <exception cref="LuaException">A Missing native stopped the run.</exception>
    public static Simulation Load(string path, SimulationOptions? options = null) =>
        new(MapArchive.ReadLua(path), options ?? new SimulationOptions());

    /// <summary>
    /// What the run showed, a line each: the time in seconds, where the line came from (<c>print</c>,
    /// <c>player 0</c> for text shown to a player, <c>error</c> for a Lua error) and the text.
    /// </summary>
    public IReadOnlyList<string> Log => _game.Log;

    /// <summary>Moves the clock on by <paramref name="time"/>, in whole steps of 1/1024 s.</summary>
    /// <exception cref="LuaException">A Missing native stopped the run.</exception>
    public void Advance(TimeSpan time) => _game.Advance(Game.Steps(time.TotalSeconds));

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
