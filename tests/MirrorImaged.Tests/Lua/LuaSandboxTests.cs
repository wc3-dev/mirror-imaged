using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>The game's Lua libraries and limits (facts, "Lua runtime").</summary>
public sealed class LuaSandboxTests : IDisposable
{
    readonly LuaState _lua = new();

    public void Dispose() => _lua.Dispose();

    [Theory]
    [InlineData("io")]
    [InlineData("debug")]
    [InlineData("package")]
    [InlineData("require")]
    [InlineData("dofile")]
    [InlineData("loadfile")]
    [InlineData("collectgarbage")]
    [InlineData("math.random")]
    [InlineData("math.randomseed")]
    public void Missing(string name) => Assert.Null(_lua.Run($"return {name}")[0]);

    [Theory]
    [InlineData("load")]
    [InlineData("pcall")]
    [InlineData("xpcall")]
    [InlineData("print")]
    [InlineData("setmetatable")]
    [InlineData("coroutine.wrap")]
    [InlineData("string.pack")]
    [InlineData("table.unpack")]
    [InlineData("utf8.char")]
    [InlineData("math.sqrt")]
    public void Present(string name) => Assert.Equal("function", _lua.Run($"return type({name})")[0]);

    [Fact]
    public void OsHasOnlyTheClockAndDates() => Assert.Equal("clock date difftime time", _lua.Run("""
        local names = {}
        for name in pairs(os) do names[#names + 1] = name end
        table.sort(names)
        return table.concat(names, ' ')
        """)[0]);

    [Fact]
    public void OsTimeIsAnInteger() => Assert.Equal("integer", _lua.Run("return math.type(os.time())")[0]);

    [Fact]
    public void VersionIsLua53() => Assert.Equal("Lua 5.3", _lua.Run("return _VERSION")[0]);

    [Theory]
    [InlineData(0, 200, true)]
    [InlineData(0, 201, false)]
    [InlineData(1, 199, true)]
    [InlineData(1, 200, false)]
    public void LoadAllows200Locals(int parameters, int locals, bool loads)
    {
        var declared = string.Join("\n", Enumerable.Range(0, locals).Select(i => $"local v{i} = {i}"));
        var code = parameters == 0 ? declared : $"return function(p)\n{declared}\nend";
        var result = _lua.Run($"local f, message = load([==[{code}]==]) return f ~= nil, message");
        Assert.Equal(loads, result[0]);
        if (!loads) Assert.Contains("too many local variables (limit is 200)", (string)result[1]!);
    }
}
