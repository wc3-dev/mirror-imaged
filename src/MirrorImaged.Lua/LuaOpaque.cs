namespace MirrorImaged.Lua;

/// <summary>A value a C# function was given that C# can't read, such as a table or a coroutine: only its Lua type.</summary>
public readonly record struct LuaOpaque(string Type);
