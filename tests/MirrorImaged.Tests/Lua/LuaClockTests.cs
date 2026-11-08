using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>
/// os.clock, os.time, os.date and os.difftime read the state's simulated clock: a run starts at 2026-01-01 00:00:00
/// and local time is UTC (decisions D44). The game's read the PC's clock (facts, "Lua runtime").
/// </summary>
public sealed class LuaClockTests : IDisposable
{
    const int Start = 1767225600;  // 2026-01-01 00:00:00 UTC, a Thursday

    readonly LuaState _lua = new();

    public void Dispose() => _lua.Dispose();

    [Fact]
    public void ANewStateIsAtTheStart()
    {
        Assert.Equal([0f, Start, "2026-01-01 00:00:00"], _lua.Run("return os.clock(), os.time(), os.date('%Y-%m-%d %H:%M:%S')"));
        Assert.Equal(TimeSpan.Zero, _lua.Clock);
    }

    // facts: os.clock counts in 1 ms steps, os.time is whole seconds
    [Fact]
    public void TheClockIsInWholeMilliseconds()
    {
        _lua.Clock = TimeSpan.FromSeconds(61.2349);
        Assert.Equal(TimeSpan.FromMilliseconds(61234), _lua.Clock);
        Assert.Equal([61.234f, "integer", Start + 61], _lua.Run("return os.clock(), math.type(os.time()), os.time()"));
    }

    [Fact]
    public void ACoroutineReadsTheSameClock()
    {
        _lua.Clock = TimeSpan.FromSeconds(5);
        Assert.Equal([5f], _lua.Run("return coroutine.wrap(function() return os.clock() end)()"));
    }

    [Fact]
    public void LocalTimeIsUtc()
    {
        _lua.Clock = TimeSpan.FromHours(13.5);
        Assert.Equal(["13:30:00", "13:30:00", 0, false], _lua.Run("""
            return os.date('%H:%M:%S'), os.date('!%H:%M:%S'), os.time(os.date('*t')) - os.time(os.date('!*t')),
              os.date('*t').isdst
            """));
    }

    [Fact]
    public void ADateTableHasEveryField() =>
        Assert.Equal("2026 1 1 12 0 0 5 1", _lua.Run("""
            local t = os.date('*t', os.time() + 12 * 3600)
            return table.concat({t.year, t.month, t.day, t.hour, t.min, t.sec, t.wday, t.yday}, ' ')
            """)[0]);

    [Fact]
    public void TimeOfATableCarriesFieldsOverAndPutsThemRight() =>
        Assert.Equal([1798761600, "2027 1 1 6"], _lua.Run("""
            local t = {year = 2026, month = 13, day = 1, hour = 0}
            local time = os.time(t)
            return time, table.concat({t.year, t.month, t.day, t.wday}, ' ')
            """));

    [Fact]
    public void TimeOfATableWithoutAHourIsNoon() =>
        Assert.Equal([Start + 12 * 3600], _lua.Run("return os.time({year = 2026, month = 1, day = 1})"));

    [Fact]
    public void DiffTimeIsAFloat() => Assert.Equal([6f], _lua.Run("return os.difftime(10, 4)"));

    // Windows's C library in the "C" locale (decisions D33), not measured in the game
    [Theory]
    [InlineData("%c", "01/01/26 13:05:09")]
    [InlineData("%#c", "Thursday, January 01, 2026, 13:05:09")]
    [InlineData("%x %X", "01/01/26 13:05:09")]
    [InlineData("%#x", "Thursday, January 01, 2026")]
    [InlineData("%a %A %b %B", "Thu Thursday Jan January")]
    [InlineData("%I %p %j %#j %U %W %w %y %#d", "01 PM 001 1 00 00 4 26 1")]
    [InlineData("%z %Z %%", "+0000 UTC %")]
    public void DateFormatsAsWindowsDoes(string format, string text)
    {
        _lua.Clock = new TimeSpan(13, 5, 9);
        Assert.Equal([text], _lua.Run($"return os.date('{format}')"));
    }

    [Fact]
    public void WeeksCountFromTheFirstSundayAndMonday() =>
        Assert.Equal(["01 00", "01 01"], _lua.Run("""
            local sunday, monday = os.time({year = 2026, month = 1, day = 4}), os.time({year = 2026, month = 1, day = 5})
            return os.date('%U %W', sunday), os.date('%U %W', monday)
            """));
}
