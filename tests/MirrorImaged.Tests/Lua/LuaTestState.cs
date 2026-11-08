using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>A Lua state with two helpers: <c>f(bits)</c> makes a float from its bits and <c>bits(x)</c> reads them.</summary>
public static class LuaTestState
{
    public static LuaState Create()
    {
        var lua = new LuaState();
        lua.Run("""
            function f(b) return (string.unpack('<f', string.pack('<i4', b))) end
            function bits(x) return (string.unpack('<i4', string.pack('<f', x))) end
            """);
        return lua;
    }

    public static uint Bits(this LuaState lua, string expression) => (uint)(int)lua.Run($"return bits({expression})")[0]!;

    public static uint BitsOf(float value) => BitConverter.SingleToUInt32Bits(value);

    public static object? Value(this LuaState lua, string expression) => lua.Run($"return {expression}")[0];
}
