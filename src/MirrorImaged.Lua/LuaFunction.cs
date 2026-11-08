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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _state.Release(Reference);
    }
}
