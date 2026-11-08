namespace MirrorImaged.Lua;

/// <summary>A Lua error: its message, then Lua's stack traceback when it was raised while running.</summary>
public sealed class LuaException(string message) : Exception(message);
