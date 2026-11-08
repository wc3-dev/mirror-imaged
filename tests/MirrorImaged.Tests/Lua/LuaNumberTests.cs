using System.Globalization;
using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>
/// The game's numbers, from the "Lua runtime" facts and the probes behind them (3.0.1.24371, docs/facts.md "Lua
/// numbers"): each expected value is the game's, given as its float bits or as the game printed it with %.9g, except
/// in <see cref="NotMeasuredInTheGame"/>, which checks what the model or plain Lua gives where nothing was measured.
/// </summary>
public sealed class LuaNumberTests : IDisposable
{
    readonly LuaState _lua = LuaTestState.Create();

    public void Dispose() => _lua.Dispose();

    static uint Expected(string value) => value.StartsWith("0x")
        ? uint.Parse(value[2..], NumberStyles.HexNumber)
        : LuaTestState.BitsOf(float.Parse(value, CultureInfo.InvariantCulture));

    [Fact]
    public void IntegersAre32Bit()
    {
        Assert.Equal(int.MaxValue, _lua.Value("math.maxinteger"));
        Assert.Equal(int.MinValue, _lua.Value("math.mininteger"));
        Assert.Equal(true, _lua.Value("2^24 + 1 == 2^24"));
    }

    [Theory]
    // additions and subtractions: toward zero, and what is under two float steps is dropped
    [InlineData("0.1 + 0.2", "0x3e999999")]
    [InlineData("2^24 + 1", "0x4b800000")]
    [InlineData("2^24 + 3", "0x4b800000")]
    [InlineData("2^24 + 5", "0x4b800002")]
    [InlineData("1 + 2^-24", "0x3f800000")]
    [InlineData("1 + 3 * 2^-24", "0x3f800000")]
    [InlineData("1 - 2^-25", "0x3f800000")]
    [InlineData("1 - 3 * 2^-25", "0x3f800000")]
    [InlineData("-(2^24) - 3", "0xcb800000")]
    [InlineData("1 - 0.9", "0.100000024")]
    [InlineData("(1000 + 0.1) - 1000", "0.0999755859")]
    [InlineData("7200.017 + 0.0002", "0x45e10023")]
    [InlineData("7200.017 + 0.000244", "0x45e10023")]
    [InlineData("7200.017 + 0.00025", "0x45e10023")]
    [InlineData("7200.017 + 0.0003", "0x45e10023")]
    [InlineData("7200.017 + 0.000488", "0x45e10023")]
    [InlineData("7200.017 + 0.0005", "0x45e10023")]
    [InlineData("20000.75 + 0.0009", "0x469c4180")]
    [InlineData("20000.75 + 0.00097", "0x469c4180")]
    [InlineData("20000.75 + 0.00098", "0x469c4180")]
    [InlineData("20000.75 + 0.001", "0x469c4180")]
    [InlineData("20000.75 + 0.0015", "0x469c4180")]
    [InlineData("20000.75 + 0.0019", "0x469c4180")]
    [InlineData("20000.75 + 0.00195", "0x469c4180")]
    [InlineData("20000.75 + 0.002", "0x469c4180")]
    [InlineData("33000.01 + 0.001", "0x4700e803")]
    [InlineData("33000.01 + 0.0019", "0x4700e803")]
    [InlineData("33000.01 + 0.002", "0x4700e803")]
    [InlineData("33000.01 + 0.0039", "0x4700e803")]
    [InlineData("33000.01 + 0.004", "0x4700e803")]
    [InlineData("f(0x7f7fffff) + 1", "0x7f7fffff")]
    [InlineData("math.huge - math.huge", "0x00000000")]
    // products: toward zero
    [InlineData("3 * 0.1", "0x3e999999")]
    [InlineData("0.1 * 0.1", "0x3c23d70a")]
    [InlineData("20000.75 * 1000", "0x4b9897f7")]
    [InlineData("33000.01 * 1000", "0x4bfbc525")]
    [InlineData("10 * 0.7", "6.99999952")]
    [InlineData("100 * 0.07", "7")]
    [InlineData("100 * 0.29", "28.9999981")]
    [InlineData("100 * 0.57", "56.9999962")]
    [InlineData("1000 * 0.7", "699.999939")]
    [InlineData("3 * 0.1 * 10", "2.99999976")]
    [InlineData("f(0x40a0008c) * f(0x40a0008c)", "0x41c8015e")]
    [InlineData("f(0x4120008c) * f(0x4120008c)", "0x42c8015e")]
    [InlineData("f(0x42c800f3) * f(0x42c800f3)", "0x461c417b")]
    [InlineData("f(0x434800f3) * f(0x434800f3)", "0x471c417b")]
    [InlineData("f(0x3fb504f3) * f(0x3fb504f3)", "0x3fffffff")]
    [InlineData("f(0x3fddb3f5) * f(0x3fddb3f5)", "0x40400033")]
    [InlineData("f(0x447a00f6) * f(0x447a00f6)", "0x497425e0")]
    [InlineData("f(0x3c23d76d) * f(0x3c23d76d)", "0x38d1b814")]
    [InlineData("120 * 120 + 160 * 160", "0x471c4000")]
    // quotients
    [InlineData("1 / 3", "0x3eaaaaab")]
    [InlineData("2 / 3", "0x3f2aaaab")]
    [InlineData("1 / 5", "0x3e4cccce")]
    [InlineData("1 / 10", "0x3dccccce")]
    [InlineData("1 / 7", "0x3e124925")]
    [InlineData("7 / 7", "0x3f800000")]
    [InlineData("40000 / 200", "0x43480000")]
    [InlineData("10000 / 100", "0x42c80000")]
    [InlineData("1 / 3 * 3", "0x3f800000")]
    [InlineData("3 / 5", "0.600000024")]
    [InlineData("7.0 // 2", "0x40400000")]
    // no nan and no infinity, and past the range
    [InlineData("0 / 0", "0x3f800000")]
    [InlineData("1 / 0", "0x7f000000")]
    [InlineData("-1 / 0", "0xff000000")]
    [InlineData("100 / 0", "0x00000000")]
    [InlineData("0 * math.huge", "0x00000000")]
    [InlineData("(1 / 0) * 0", "0x00000000")]
    [InlineData("(1 / 0) * 2", "0x7f800000")]
    [InlineData("(1 / 0) * 4", "0x80000000")]
    [InlineData("1e30 * 1e30", "0x00000000")]
    [InlineData("-1e30 * 1e30", "0x00000000")]
    [InlineData("1e20 * 1e20", "0x00000000")]
    [InlineData("1e20 * 1e20 / 1e20", "0x00000000")]
    [InlineData("1e38 * 10", "-5.5173927e-39")]
    [InlineData("1e-30 * 1e-30", "0x00000000")]
    [InlineData("1e-20 * 1e-20", "0x00000000")]
    [InlineData("1e-38 / 10", "0x00000000")]
    [InlineData("1e-38 / 1000", "0x00000000")]
    [InlineData("math.huge", "0x7f800000")]
    [InlineData("math.pi", "0x40490fdb")]
    // -0
    [InlineData("-0.0", "0x80000000")]
    [InlineData("-(0.0)", "0x80000000")]
    [InlineData("tonumber('-0.0')", "0x80000000")]
    [InlineData("1 / -0.0", "0xff000000")]
    [InlineData("0.0 * -1.0", "0x00000000")]
    // probes numbers-ops and numbers-fine (decisions D151): a subtraction is exact, then cut; results are packed
    // without a range check; a product's range is checked before the carry; zeros divide as the game's
    [InlineData("f(0x3f82d6ea) - f(0x3eabf225)", "0x3f2fb4c1")]
    [InlineData("f(0x3fa72a3f) - f(0x3ef945af)", "0x3f51b1a6")]
    [InlineData("f(0x3fcc62ea) - f(0x3f3491a2)", "0x3f643432")]
    [InlineData("0.0 - 0.0", "0x80000000")]
    [InlineData("0.0 + -0.0", "0x80000000")]
    [InlineData("f(0x00800000) - f(0x00800001)", "0xf5000000")]
    [InlineData("f(0x01000000) - f(0x00800001)", "0x007ffffe")]
    [InlineData("math.huge + math.huge", "0x80000000")]
    [InlineData("f(0x7f400000) * 6", "0x80900000")]
    [InlineData("f(0x7f400000) * 12", "0x00000000")]
    [InlineData("f(0xff000000) * 4", "0x80000000")]
    [InlineData("f(0x7f000000) * 8", "0x00000000")]
    [InlineData("f(0x00800000) * f(0x3f7fffff)", "0x00000000")]
    [InlineData("0.0 / 0.0", "0x3f800000")]
    [InlineData("-0.0 / -0.0", "0x3f800000")]
    [InlineData("-0.0 / 0.0", "0x00000000")]
    [InlineData("0.0 / -0.0", "0x00000000")]
    [InlineData("f(1) / 0.0", "0x00000000")]
    [InlineData("1 / f(0x00400000)", "0x7eaaaaab")]
    [InlineData("f(0x7f7fffff) / f(0x3f7fffff)", "0x7f800000")]
    [InlineData("f(0x7f000000) / 0.125", "0x00000000")]
    [InlineData("f(0x3fffffff) / f(0x3fffffff)", "0x3f800000")]
    public void Arithmetic(string expression, string game) => Assert.Equal(Expected(game), _lua.Bits(expression));

    [Fact]
    public void ValuesInVariablesGiveTheSame()
    {
        Assert.Equal(0x3e999999u, _lua.Bits("(function(a, b) return a + b end)(0.1, 0.2)"));
        Assert.Equal(0x3e4cccceu, _lua.Bits("(function(a, b) return a / b end)(1, 5)"));
        Assert.Equal(0u, _lua.Bits("(function(a, b) return a * b end)(1e30, 1e30)"));
    }

    [Theory]
    [InlineData("tostring(0 / 0)", "1.0")]
    [InlineData("tostring(math.sqrt(-1))", "0.0")]
    [InlineData("tostring(1 / 0)", "1.701412e+38")]
    [InlineData("tostring(0.1)", "0.1")]
    [InlineData("string.format('%.9g', 1 / 0)", "1.70141183e+38")]
    [InlineData("string.format('%.9g', math.huge)", "inf")]
    [InlineData("string.format('%.9g', f(0x7f7fffff) * 2)", "nan")]
    [InlineData("string.format('%.9g', (1 / 0) * 2)", "inf")]
    [InlineData("string.format('%.9g', (1 / 0) * 4)", "-0")]
    [InlineData("string.format('%.9g', 1e38 * 10)", "-5.5173927e-39")]
    [InlineData("string.format('%g', -0.0)", "-0")]
    [InlineData("string.format('%.9g', 0.1 + 0.2)", "0.299999982")]
    [InlineData("string.format('%.9g', f(0x7f7fffff))", "3.40282347e+38")]
    [InlineData("string.format('%.9g', f(1))", "1.40129846e-45")]
    public void Text(string expression, string game) => Assert.Equal(game, _lua.Value(expression));

    [Theory]
    [InlineData("local x = 0 / 0 return x == x and not (x ~= x) and x == 1 and x > 0")]
    [InlineData("local x = math.sqrt(-1) return x == 0 and not (x < 0)")]
    [InlineData("local x = -0.0 return x == 0 and not (x < 0) and math.type(x) == 'float'")]
    [InlineData("local m = f(0x7f7fffff) return 1e30 < m and not (m < m) and m + 1 == m")]
    [InlineData("return math.type(tonumber('-0')) == 'integer'")]
    public void Comparisons(string code) => Assert.Equal(true, _lua.Run(code)[0]);

    [Theory]
    [InlineData(1, "0.1", 11, 11)]
    [InlineData(1, "0.2", 6, 6)]
    [InlineData(1, "0.05", 21, 21)]
    [InlineData(1, "0.01", 101, 101)]
    [InlineData(2, "0.1", 21, 21)]
    [InlineData(3, "0.3", 11, 11)]
    [InlineData(10, "0.1", 101, 101)]
    [InlineData(100, "0.1", 1001, 1001)]
    [InlineData(1, "0.25", 4, 5)]
    [InlineData(5, "0.03125", 160, 161)]
    public void LoopsOnAFloatRunAsInTheGame(int limit, string step, int runs, int runsWithEqual)
    {
        var counts = _lua.Run($$"""
            local function count(less)
              local t, n = 0, 0
              while (less and t < {{limit}}) or (not less and t <= {{limit}}) do t = t + {{step}}; n = n + 1 end
              return n
            end
            return count(true), count(false)
            """);
        Assert.Equal([runs, runsWithEqual], counts);
    }

    [Fact]
    public void TenthsAddUpLow()
    {
        Assert.Equal(LuaTestState.BitsOf(0.999999821f), _lua.Bits("(function() local t = 0 for i = 1, 10 do t = t + 0.1 end return t end)()"));
    }

    [Theory]
    [InlineData("0.001", 10000, "9.99838257")]
    [InlineData("0.001", 100000, "99.9489441")]
    [InlineData("0.001", 1000000, "981.466797")]
    [InlineData("0.01", 10000, "99.971489")]
    [InlineData("0.01", 100000, "996.854736")]
    [InlineData("0.01", 1000000, "9799.05859")]
    [InlineData("0.03", 10000, "299.988312")]
    [InlineData("0.03", 100000, "2989.46997")]
    [InlineData("0.03", 120000, "3585.1731")]
    [InlineData("0.03", 1000000, "29441.9434")]
    [InlineData("1/32", 115200, "3600")]
    [InlineData("0.1", 10000, "999.804871")]
    [InlineData("0.1", 100000, "9973.49512")]
    [InlineData("0.1", 1000000, "97000.0703")]
    [InlineData("0.37", 10000, "3699.40674")]
    [InlineData("0.37", 100000, "36906.2461")]
    [InlineData("0.37", 1000000, "357990.594")]
    [InlineData("1.7", 10000, "16995.1406")]
    [InlineData("1.7", 100000, "169506.906")]
    [InlineData("1.7", 1000000, "1664565")]
    public void SumsRunLow(string step, int times, string game)
    {
        var sum = _lua.Bits($"(function() local s, c = 0, {step} for i = 1, {times} do s = s + c end return s end)()");
        Assert.Equal(Expected(game), sum);
    }

    [Theory]
    [InlineData("0.001", 8192f, 8383586)]
    [InlineData("0.01", 65536f, 7546324)]
    [InlineData("0.1", 524288f, 5557738)]
    [InlineData("1.0", 8388608f, 8388608)]
    public void AddingStopsWhereTheGamesDoes(string step, float stopsAt, int additions)
    {
        var result = _lua.Run($$"""
            local s, c, n = 0, {{step}}, 0
            while true do
              local t = s + c
              if t == s then return s, n end
              s, n = t, n + 1
            end
            """);
        Assert.Equal([stopsAt, additions], result);
    }

    [Theory]
    [InlineData("16384", 1, 0x46800000u)]
    [InlineData("16384 + 1 step", 1, 0x46800001u)]
    [InlineData("16386", -1, 0x46800400u)]
    [InlineData("-16384", 1, 0xc6800000u)]
    [InlineData("-16384", -1, 0xc6800000u)]
    [InlineData("1", 1, 0x3f800000u)]
    [InlineData("1 + 1 step", 1, 0x3f800001u)]
    [InlineData("1.5", -1, 0x3fc00000u)]
    [InlineData("1000.1", 1, 0x447a0666u)]
    [InlineData("1000.1", -1, 0x447a0666u)]
    public void AddingFloatSteps(string name, int sign, uint baseBits)
    {
        _ = name;
        var x = BitConverter.UInt32BitsToSingle(baseBits);
        double step = Math.Pow(2, (int)((baseBits >> 23) & 0xff) - 127 - 23);
        var moved = new List<double>();
        for (int k = 1; k <= 8; k++)
        {
            var r = (float)_lua.Value($"f({(int)baseBits}) + {(float)(sign * k * step):R}")!;
            moved.Add((r - (double)x) / step);
        }
        Assert.Equal([0, 2 * sign, 3 * sign, 4 * sign, 5 * sign, 6 * sign, 7 * sign, 8 * sign], moved);
    }

    [Theory]
    [InlineData("10 * 0.7", 6)]
    [InlineData("100 * 0.07", 7)]
    [InlineData("100 * 0.29", 28)]
    [InlineData("100 * 0.57", 56)]
    [InlineData("1000 * 0.7", 699)]
    [InlineData("3 * 0.1 * 10", 2)]
    public void WholeProductsCastLow(string expression, int game) => Assert.Equal(game, _lua.Value($"math.floor({expression})"));

    [Fact]
    public void PercentagesCastWrongAsOftenAsInTheGame()
    {
        var counts = _lua.Run("""
            local wrong, whole, nudged = 0, 0, 0
            for k = 1, 99 do
              local q = k / 100
              for n = 1, 1000 do
                local v, exact = n * q, n * k // 100
                if n * k % 100 == 0 then whole = whole + 1 end
                if math.floor(v) ~= exact then wrong = wrong + 1 end
                if math.floor(v + 0.001) ~= exact then nudged = nudged + 1 end
              end
            end
            return wrong, whole, nudged
            """);
        Assert.Equal([140, 4200, 0], counts);
    }

    // C#'s % through CSharp.lua's System.modf: Lua's % with the sign put right
    [Theory]
    [InlineData("7.2", "0.1", "0x00000000")]
    [InlineData("5.5", "2", "1.5")]
    [InlineData("-5.5", "2", "-1.5")]
    public void Remainders(string x, string y, string game) => Assert.Equal(Expected(game), _lua.Bits(
        $"(function(x, y) local v = x % y if v ~= 0 and x * y < 0 then return v - y end return v end)({x}, {y})"));


    [Theory]
    [InlineData("math.ceil(0.0)", 1)]
    [InlineData("math.ceil(-0.0)", 1)]
    [InlineData("math.ceil(0)", 0)]
    [InlineData("math.ceil(0.5)", 1)]
    [InlineData("math.ceil(-0.5)", 0)]
    [InlineData("math.ceil(1e-30)", 1)]
    [InlineData("math.ceil(-1.5)", -1)]
    [InlineData("math.floor(0.0)", 0)]
    [InlineData("math.floor(-1.5)", -2)]
    [InlineData("math.tointeger(0.0)", 0)]
    [InlineData("math.modf(0.0)", 0)]
    [InlineData("7 // 2", 3)]
    public void WholeNumbers(string expression, int game) => Assert.Equal(game, _lua.Value(expression));

    [Theory]
    [InlineData("math.maxinteger + 1 == math.mininteger")]
    [InlineData("46341 * 46341 == -2147479015")]
    [InlineData("1 << 31 == math.mininteger")]
    [InlineData("-1 >> 1 == 2147483647")]
    [InlineData("-8 >> 1 == 2147483644")]
    [InlineData("math.type(-2147483648) == 'float'")]
    [InlineData("math.type(-2147483647 - 1) == 'integer'")]
    [InlineData("math.type(tonumber('-2147483648')) == 'integer'")]
    [InlineData("math.type(math.floor(3e9)) == 'float' and bits(math.floor(3e9)) == 0x4f32d05e")]
    [InlineData("not pcall(function() return -(-2147483648 + 1) & 63 end)")]
    [InlineData("select(2, pcall(function() return -(-2147483648 + 1) & 63 end)):find('number has no integer representation') ~= nil")]
    public void IntegersWrap(string condition) => Assert.Equal(true, _lua.Value(condition));

    [Fact]
    public void ATimeInMillisecondsIsAFloat() =>
        Assert.Equal("float", _lua.Value("math.type(os.time() * 1000 % 2147483648)"));

    /// <summary>Not measured in the game: what the model gives (docs/decisions.md), or plain Lua 5.3.</summary>
    [Theory]
    [InlineData("bits(f(0x7f7fffff) * 2) == 0x7fffffff")]  // the game printed nan; its bits weren't read
    [InlineData("math.fmod(7.2, 0.1) == 0")]  // the game's 7.2 % 0.1 was measured, its math.fmod not
    [InlineData("5.5 % -2 == -0.5")]
    [InlineData("math.ceil(1.0) == 1")]
    [InlineData("tostring(1 / 3) == '0.3333333'")]
    [InlineData("tostring(12345678.0) == '1.234568e+07'")]
    [InlineData("tostring(0.000012) == '1.2e-05'")]
    [InlineData("tostring(math.huge) == 'inf' and tostring(-math.huge) == '-inf'")]
    [InlineData("tostring(f(0x7fffffff)) == 'nan' and tostring(f(-1)) == '-nan'")]
    // inf and nan honour the format's flags, width and case, as the C library writes them
    [InlineData("string.format('%10.3f|%-6g|%+g|% g|%010f', math.huge, math.huge, math.huge, math.huge, math.huge) == '       inf|inf   |+inf| inf|       inf'")]
    [InlineData("string.format('%G|%E|%5.1f|%-5g|', math.huge, -math.huge, f(0x7fffffff), f(-1)) == 'INF|-INF|  nan|-nan |'")]
    public void NotMeasuredInTheGame(string condition) => Assert.Equal(true, _lua.Value(condition));

    [Theory]
    [InlineData("0")]
    [InlineData("-0.5")]
    [InlineData("0.1")]
    [InlineData("3.14159")]
    [InlineData("1e30")]
    [InlineData("-1e-30")]
    public void FloatsPackAndUnpack(string value) =>
        Assert.Equal(true, _lua.Value($"string.unpack('<f', string.pack('<f', {value})) == {value}"));

    [Fact]
    public void TheCollectorRuns() => Assert.Equal(true, _lua.Run("""
        local cache = setmetatable({}, {__mode = 'v'})
        cache[1] = {}
        for i = 1, 1000000 do local t = {} end
        return cache[1] == nil
        """)[0]);
}
