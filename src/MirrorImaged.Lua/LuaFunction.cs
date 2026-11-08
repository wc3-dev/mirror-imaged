namespace MirrorImaged.Lua;

/// <summary>
/// A Lua function a C# function was given. It is let go of when that C# function returns, unless the C# function
/// calls <see cref="Keep"/>; a kept one is let go of by <see cref="Dispose"/>.
/// </summary>
public sealed class LuaFunction : IDisposable
{
    readonly LuaState _state;
    bool _disposed;

    internal LuaFunction(LuaState state, int reference)
    {
        _state = state;
        Reference = reference;
    }

    internal int Reference { get; }

    internal bool Kept { get; private set; }

    /// <summary>Keeps the function past the call that gave it to C#, until it is disposed.</summary>
    public LuaFunction Keep()
    {
        Kept = true;
        return this;
    }

    /// <summary>Calls the function and returns its results.</summary>
    public object?[] Call(params object?[] args)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _state.Call(this, args);
    }

    /// <summary>
    /// Calls the function in a new Lua thread, as the game runs a callback, until it returns or a C# function it calls
    /// makes it yield (<see cref="LuaState.Yield"/>). The script can't yield the thread itself: in it
    /// <c>coroutine.isyieldable()</c> is false and <c>coroutine.yield</c> raises, as in the game's. An error ends it and
    /// is thrown as a <see cref="LuaException"/>.
    /// </summary>
    public LuaThread Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _state.Start(null, Reference);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _state.Release(Reference);
    }
}
