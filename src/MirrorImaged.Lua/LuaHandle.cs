namespace MirrorImaged.Lua;

/// <summary>
/// A game handle as Lua holds it: a userdata whose metatable is named <paramref name="Type"/>, so <c>tostring</c>
/// writes <c>timer: 0000000000012340</c>. Pushing the same handle as the same type gives the same userdata while Lua
/// holds it; as another type, another userdata, as the game does.
/// </summary>
public readonly record struct LuaHandle(string Type, int Id);
