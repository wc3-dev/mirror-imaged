using System.Globalization;
using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>
/// The game's math library, from the "Lua runtime" facts and their probes (3.0.1.24371). Exact where the model
/// reproduces the game; the cases it doesn't are in <see cref="CloseToTheGame"/>, with the game's value and how
/// far off the model may be (docs/decisions.md, "The game's Lua numbers").
/// </summary>
public sealed class LuaMathTests : IDisposable
{
    readonly LuaState _lua = LuaTestState.Create();

    public void Dispose() => _lua.Dispose();

    static uint Expected(string value) => value.StartsWith("0x")
        ? uint.Parse(value[2..], NumberStyles.HexNumber)
        : LuaTestState.BitsOf(float.Parse(value, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("25", "0x40a0008c")]
    [InlineData("100", "0x4120008c")]
    [InlineData("10000", "0x42c800f3")]
    [InlineData("40000", "0x434800f3")]
    [InlineData("120 * 120 + 160 * 160", "0x434800f3")]
    [InlineData("2", "0x3fb504f3")]
    [InlineData("3", "0x3fddb3f5")]
    [InlineData("1000000", "0x447a00f6")]
    [InlineData("0.0001", "0x3c23d76d")]
    [InlineData("0.01", "0.100000285")]
    [InlineData("9", "3.0000577")]
    [InlineData("36", "6.00011539")]
    [InlineData("49", "7.00011635")]
    [InlineData("81", "9.00006676")]
    [InlineData("144", "12.0002308")]
    [InlineData("625", "25.0004635")]
    [InlineData("2500", "50.000927")]
    [InlineData("1", "1")]
    [InlineData("4", "2")]
    [InlineData("16", "4")]
    [InlineData("64", "8")]
    [InlineData("1024", "32")]
    [InlineData("0.25", "0.5")]
    [InlineData("16777216", "4096")]
    [InlineData("-1", "0")]
    [InlineData("-4", "0")]
    [InlineData("1e30 * 1e30 + 0 * 0", "0")]
    public void SquareRoots(string x, string game) => Assert.Equal(Expected(game), _lua.Bits($"math.sqrt({x})"));

    [Fact]
    public void SquareRootsOfPowersOfFourAreExact() => Assert.Equal(true, _lua.Run("""
        for k = -20, 20 do
          if math.sqrt(4.0^k) ~= 2.0^k then return false end
        end
        return true
        """)[0]);

    [Fact]
    public void ALargerValueNeverGivesASmallerRoot() => Assert.Equal(true, _lua.Run("""
        local start, last = bits(16384.0), 0
        for i = 0, 32767 do
          local r = math.sqrt(f(start + i))
          if r < last then return false end
          last = r
        end
        last = 0
        for i = 0, 2^25 - 1, 61 do  -- two octaves
          local r = math.sqrt(f(0x3f800000 + i))
          if r < last then return false end
          last = r
        end
        return true
        """)[0]);

    // One correction step (s + x / s) / 2, as Moonwell's Trig.Sqrt takes it.
    [Theory]
    [InlineData("1", "1")]
    [InlineData("2", "1.41421366")]
    [InlineData("4", "2")]
    [InlineData("9", "3")]
    [InlineData("16", "4")]
    [InlineData("25", "5")]
    [InlineData("36", "6")]
    [InlineData("49", "7")]
    [InlineData("64", "8")]
    [InlineData("81", "9")]
    [InlineData("100", "10")]
    [InlineData("144", "12")]
    [InlineData("625", "25.0000019")]
    [InlineData("1024", "32")]
    [InlineData("2500", "50.0000038")]
    [InlineData("10000", "100.000008")]
    [InlineData("40000", "200.000015")]
    [InlineData("16777216", "4096")]
    [InlineData("0.25", "0.5")]
    [InlineData("0.01", "0.100000001")]
    [InlineData("0.0001", "0.00999999978")]
    public void ACorrectionStepAfterTheRoot(string x, string game) =>
        Assert.Equal(Expected(game), _lua.Bits($"(function(x) local s = math.sqrt(x) return (s + x / s) / 2 end)({x})"));

    [Theory]
    [InlineData("math.log(10)", "2.30258489")]
    [InlineData("math.asin(0.5)", "0.523598731")]
    [InlineData("math.acos(0.5)", "1.04719746")]
    [InlineData("2^10", "1024")]
    [InlineData("10^2", "100")]
    [InlineData("10^3", "1000")]
    [InlineData("1.5^2", "2.25")]
    [InlineData("2^0.5", "0x3fb504f3")]
    [InlineData("1^0.5", "1")]
    [InlineData("math.cos(0)", "0.99999994")]
    [InlineData("math.sin(0)", "0")]
    [InlineData("math.sin(math.pi / 2)", "0.99999994")]
    [InlineData("1 - math.cos(0)", "5.96046448e-08")]
    public void Functions(string expression, string game) => Assert.Equal(Expected(game), _lua.Bits(expression));

    [Fact]
    public void SineSquaredAndCosineSquaredStayInTheGamesRange() => Assert.Equal(true, _lua.Run("""
        local rad = f(0x3c8efa35)  -- pi / 180 as a float
        for d = 0, 359 do
          local s, c = math.sin(d * rad), math.cos(d * rad)
          local sum = s * s + c * c
          if sum < 0.99999923 or sum > 1 then return false end
        end
        return true
        """)[0]);

    /// <summary>The game's values the model doesn't reproduce, and how far the model is from each.</summary>
    [Theory]
    [InlineData("math.sin(0.5)", "0.479430377", 1e-5)]
    [InlineData("math.cos(0.5)", "0.877579808", 1e-5)]
    [InlineData("math.tan(0.5)", "0.546309769", 1e-5)]
    [InlineData("math.atan(1, 1) * 4", "3.14159298", 6e-7)]
    [InlineData("math.atan(1)", "0.785398245", 2e-7)]
    [InlineData("math.exp(1)", "2.71828151", 3e-7)]
    [InlineData("math.log(2)", "0.693147182", 1e-7)]
    [InlineData("math.log(1000, 10)", "2.99999189", 1e-5)]
    [InlineData("math.sin(30 * f(0x3c8efa35))", "0.499999851", 5e-7)]
    [InlineData("math.cos(90 * f(0x3c8efa35))", "4.7e-10", 1e-7)]
    [InlineData("math.cos(-90 * f(0x3c8efa35))", "6.01541251e-06", 7e-6)]
    [InlineData("math.sin(math.pi)", "4.65661287e-10", 1e-7)]
    [InlineData("4^0.5", "1.99999964", 1e-6)]
    [InlineData("25^0.5", "4.99999523", 1e-5)]
    [InlineData("100^0.5", "9.99998856", 2e-5)]
    [InlineData("10000^0.5", "99.9998856", 2e-4)]
    [InlineData("40000^0.5", "199.999756", 3e-4)]
    [InlineData("16777216^0.5", "4095.99512", 6e-3)]
    [InlineData("0.25^0.5", "0.500000119", 2e-7)]
    [InlineData("3^0.5", "1.73205006", 1e-6)]
    [InlineData("1000000^0.5", "999.991516", 1e-2)]
    [InlineData("0.0001^0.5", "0.00999999791", 1e-8)]
    [InlineData("(function(x) local s = math.sqrt(x) return (s + x / s) / 2 end)(3)", "1.7320509", 2e-7)]
    [InlineData("(function(x) local s = math.sqrt(x) return (s + x / s) / 2 end)(1000000)", "1000.00006", 1e-4)]
    public void CloseToTheGame(string expression, string game, double within)
    {
        var value = (float)_lua.Value(expression)!;
        Assert.InRange(value, float.Parse(game, CultureInfo.InvariantCulture) - within, float.Parse(game, CultureInfo.InvariantCulture) + within);
    }
}
