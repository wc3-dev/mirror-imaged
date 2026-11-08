namespace MirrorImaged.Tests.Lua;

/// <summary>
/// Every number operation on every kind of float (nan and infinity patterns, -0, the smallest ones) hashed into
/// one value: CI runs this on win-x64, linux-x64 and osx-arm64, so a platform that gives other bits fails here.
/// The value is the library's own, not the game's.
/// </summary>
public sealed class LuaDeterminismTests
{
    [Fact]
    public void TheSameBitsOnEveryPlatform()
    {
        using var lua = LuaTestState.Create();
        var hash = lua.Run("""
            local h, x = -2128831035, 7
            local function add(v)
              local s = math.type(v) == 'float' and string.pack('<f', v) or tostring(v)
              for i = 1, #s do h = (h ~ s:byte(i)) * 16777619 end
            end
            for i = 1, 20000 do
              x = x * 1103515245 + 12345
              local a = f(x)
              x = x * 1103515245 + 12345
              local b = i % 4 == 0 and f(x) or (x >> 8) / 1000
              for _, v in ipairs({a + b, a - b, a * b, a / b, a % b, a // b, -a, a < b, a == b, a ^ b, math.sqrt(a),
                  math.sin(a), math.cos(a), math.tan(a), math.asin(b), math.acos(b), math.atan(a, b), math.exp(b),
                  math.log(a), math.log(a, 2), math.log(a, 10), math.log(a, b), math.floor(a), math.ceil(b),
                  math.fmod(a, b), (math.modf(a)), math.rad(a), math.deg(b), math.abs(a), math.tointeger(b) or 0,
                  tostring(a), string.format('%.9g %g %.3f', a, b, b)}) do
                add(v)
              end
            end
            return h
            """)[0];
        Assert.Equal(DeterminismHash, hash);
    }

    const int DeterminismHash = -1502114234;
}
