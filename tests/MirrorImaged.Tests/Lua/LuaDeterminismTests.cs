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

    const int DeterminismHash = 1229561353;

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

    /// <summary>
    /// Objects sit at the same place in their state's arena every run: pairs over them and tostring agree. The
    /// library's own C functions are left out: they are known by their place in the library, which differs by
    /// platform (decisions D39).
    /// </summary>
    [Fact]
    public void AddressesAreTheSameEveryRun()
    {
        const string code = """
            local keys, order = {}, {}
            for i = 1, 200 do keys[{}] = i end
            keys[function() end], keys[coroutine.create(function() end)] = 201, 202
            for _, i in pairs(keys) do order[#order + 1] = i end
            return table.concat(order, ' '), tostring({}), tostring(function() end), tostring(coroutine.create(function() end))
            """;
        using var first = new LuaState();
        using var second = new LuaState();
        var a = first.Run(code);
        Assert.Equal(a, second.Run(code));
        Assert.Matches("^table: [0-9A-F]{16}$", (string)a[1]!);
    }

    /// <summary>
    /// After tables, closures, coroutines, long strings, growing tables and collection cycles with finalizers and weak
    /// tables, the order pairs visits objects in and the text of a new table: the same on every platform, as CI checks.
    /// </summary>
    [Fact]
    public void AddressesAreTheSameOnEveryPlatform()
    {
        using var lua = new LuaState();
        var result = lua.Run("""
            local weak, keep, id, finalized = setmetatable({}, {__mode = 'k'}), {}, {}, 0
            for i = 1, 3000 do
              local t = setmetatable({i}, {__gc = function() finalized = finalized + 1 end})
              local f = function() return i end
              local co = coroutine.create(function(x) coroutine.yield(x) end)
              coroutine.resume(co, i)
              local s = string.rep('s', 40 + i % 100) .. i
              weak[t] = s
              local key = i % 3 == 0 and t or i % 3 == 1 and f or co
              keep[key], id[key] = s, i
              local grow = {}
              for j = 1, i % 60 do grow[j], grow['k' .. j] = j, s end
            end
            for i = 1, 200000 do local junk = {i} end
            local h = 0
            for key in pairs(keep) do h = (h * 31 + id[key]) & 0xffffff end
            for key in pairs(weak) do h = (h * 31 + key[1]) & 0xffffff end
            return h, finalized, tostring({})
            """);
        Assert.Equal([PairsHash, Finalized, NewTable], result);
    }

    const int PairsHash = 6593288, Finalized = 2000;
    const string NewTable = "table: 00000000005592E0";
}