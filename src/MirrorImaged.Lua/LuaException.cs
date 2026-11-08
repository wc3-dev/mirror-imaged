namespace MirrorImaged.Lua;

/// <summary>
/// A Lua error: its message, then Lua's stack traceback when it was raised while running. When the state halted, the
/// inner exception is what halted it.
/// </summary>
public sealed class LuaException(string message, Exception? innerException = null) : Exception(message, innerException);
