namespace MirrorImaged.Lua;

/// <summary>
/// A Lua thread C# started (<see cref="LuaFunction.Start"/>, <see cref="LuaState.Start(string)"/>). While it
/// <see cref="Waiting"/>, it can be resumed, or disposed of to let it go without running on.
/// </summary>
public sealed class LuaThread : IDisposable
{
    readonly LuaState _state;
    readonly int _reference;

    internal LuaThread(LuaState state, int reference)
    {
        _state = state;
        _reference = reference;
    }

    /// <summary>Whether it yielded and waits to be resumed; false once its function has returned or raised.</summary>
    public bool Waiting { get; private set; } = true;

    /// <summary>What its function returned, once it has; a table or a function comes as a <see cref="LuaOpaque"/>.</summary>
    public object?[] Results { get; private set; } = [];

    /// <summary>Runs it on from where it yielded, until it returns or yields again. An error ends it and is thrown.</summary>
    public void Resume()
    {
        if (!Waiting) throw new InvalidOperationException("The Lua thread has ended.");
        Waiting = false;
        (Results, Waiting) = _state.Resume(_reference);
    }

    public void Dispose()
    {
        if (!Waiting) return;
        Waiting = false;
        _state.Release(_reference);
    }
}
