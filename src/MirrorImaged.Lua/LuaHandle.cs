namespace MirrorImaged.Lua;

/// <summary>
/// A game handle as Lua holds it: a userdata whose metatable is named <paramref name="Type"/>, the type the handle is
/// seen as, so <c>tostring</c> writes <c>timer: 0000000000012340</c>, and which holds <paramref name="Key"/>, the
/// handle's own, by which C# finds it whatever type it is seen as. Pushing the same key as the same type gives the
/// same userdata while Lua holds it; as another type, another userdata, as the game does. Lua's <c>==</c> compares
/// <paramref name="Id"/>, the id the game shows, whatever the types; when <paramref name="Agent"/> says the type is an
/// agent, its <c>==</c> refuses a second operand that isn't one, as the game's does.
/// </summary>
public readonly record struct LuaHandle(string Type, int Key, int Id, bool Agent);
