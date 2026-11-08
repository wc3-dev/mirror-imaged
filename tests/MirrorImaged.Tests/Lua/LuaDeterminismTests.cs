using MirrorImaged.Lua;

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
                  tostring(a), string.format('%.9g %g %.3f %a %A', a, b, b, a, b),
                  tonumber(string.format('0x%x.%xp%d', x & 0xfffff, (x >> 12) & 0xfff, i % 40 - 20)),
                  os.date('!%Y-%m-%d %H:%M:%S', i * 86413)}) do
                add(v)
              end
            end
            return h
            """)[0];
        Assert.Equal(DeterminismHash, hash);
    }

    const int DeterminismHash = -1790714231;

    [Fact]
    public void SortGivesTheSameOrderEveryRun()
    {
        const string code = """
            local t = {}
            for i = 1, 10000 do t[i] = {key = i % 7, id = i} end
            table.sort(t, function(a, b) return a.key < b.key end)
            local h = 0
            for i = 1, #t do h = (h * 31 + t[i].id) & 0xffffff end
            return h
            """;
        using var first = new LuaState();
        using var second = new LuaState();
        Assert.Equal(SortHash, first.Run(code)[0]);
        Assert.Equal(SortHash, second.Run(code)[0]);
    }

    const int SortHash = 11734620;

    /// <summary>Objects sit at the same place in their state's arena every run: pairs over them and tostring agree.</summary>
    [Fact]
    public void AddressesAreTheSameEveryRun()
    {
        const string code = """
            local keys, order = {}, {}
            for i = 1, 200 do keys[{}] = i end
            keys[print], keys[function() end], keys[coroutine.create(print)] = 201, 202, 203
            for _, i in pairs(keys) do order[#order + 1] = i end
            return table.concat(order, ' '), tostring({}), tostring(print), tostring(function() end), tostring(coroutine.create(print))
            """;
        using var first = new LuaState();
        using var second = new LuaState();
        var a = first.Run(code);
        Assert.Equal(a, second.Run(code));
        Assert.Matches("^table: [0-9A-F]{16}$", (string)a[1]!);
    }
}
