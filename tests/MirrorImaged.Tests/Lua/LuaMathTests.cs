using System.Globalization;
using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>
/// The game's math library, from the "Lua runtime" facts and their probes (3.0.1.24371, docs/facts.md "Lua numbers").
/// Exact where the model reproduces the game. Where it doesn't (Close, docs/decisions.md "The game's Lua numbers"),
/// the tests give the game's value beside the model's and assert the model's, so the recorded gap can't grow unseen.
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
          if math.sqrt(f(0x3f800000 + k * 0x1000000)) ~= f(0x3f800000 + k * 0x800000) then return false end
        end
        return true
        """)[0]);

    /// <summary>
    /// The roots of every mantissa from 1 up to 1 + 263 float steps, and of every 128th from 1 up to 1 + 527 * 2^-16
    /// (probe numbers-fine): the game's root stays the same over runs of inputs and steps by 2.16e-5 (decisions D152).
    /// </summary>
    [Theory]
    [InlineData(0, 18, "1")]
    [InlineData(19, 263, "0x3f8000b5")]
    public void SquareRootsStayTheSameOverRunsOfInputs(int first, int last, string game) => Assert.Equal(true, _lua.Run($$"""
        for i = {{first}}, {{last}} do
          if bits(math.sqrt(f(0x3f800000 + i))) ~= {{Expected(game)}} then return false end
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
    [InlineData("1000000", "1000.00006")]
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
    [InlineData("math.cos(0)", "0.99999994")]
    [InlineData("math.sin(0)", "0")]
    [InlineData("math.sin(90 * f(0x3c8efa35))", "0.99999994")]
    [InlineData("1 - math.cos(0)", "5.96046448e-08")]
    [InlineData("math.sin(0.5)", "0.479430377")]
    [InlineData("math.cos(0.5)", "0.877579808")]
    [InlineData("math.tan(0.5)", "0.546309769")]
    [InlineData("math.sin(30 * f(0x3c8efa35))", "0.499999851")]
    [InlineData("math.cos(90 * f(0x3c8efa35))", "4.65661287e-10")]
    [InlineData("math.cos(-90 * f(0x3c8efa35))", "6.01541251e-06")]
    [InlineData("math.sin(math.pi)", "4.65661287e-10")]
    [InlineData("math.sin(1e6)", "0xbef15ae9")]
    [InlineData("math.sin(-1)", "0xbf576a9a")]
    [InlineData("math.asin(2)", "0x3fc90fdb")]
    [InlineData("math.asin(1)", "0x3fc90fdb")]
    [InlineData("math.acos(-1)", "0x40490fdb")]
    [InlineData("math.acos(-2)", "0x40490fdb")]
    [InlineData("math.acos(2)", "0")]
    [InlineData("math.atan(0, 0)", "0x3fc90fdb")]
    [InlineData("math.atan(0, -1)", "0x40490fdb")]
    [InlineData("math.atan(1, 0)", "0x3fc90fdb")]
    [InlineData("math.rad(180)", "0x40490fd9")]
    [InlineData("math.deg(1)", "0x42652ee3")]
    [InlineData("math.exp(-104)", "0x7f000000")]
    [InlineData("math.exp(100)", "0")]
    [InlineData("math.log(0)", "0xc2b00f33")]
    [InlineData("math.log(-1)", "0")]
    [InlineData("0^-1", "0")]
    [InlineData("1000^10", "0x7149f2c9")]
    [InlineData("(-8)^3", "-512")]
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

    /// <summary>
    /// The game's values the model doesn't reproduce (Close): the model's own value is asserted, so the gap to the
    /// game's stays what docs/decisions.md records.
    /// </summary>
    [Theory]
    [InlineData("math.atan(1, 1) * 4", "3.14159298", "3.1415925")]
    [InlineData("math.atan(1)", "0.785398245", "0.785398126")]
    [InlineData("math.exp(1)", "2.71828151", "2.71828175")]
    [InlineData("math.log(2)", "0.693147182", "0.693147123")]
    [InlineData("math.log(1000, 10)", "2.99999189", "3")]
    [InlineData("2^0.5", "0x3fb504f3", "0x3fb504f2")]
    [InlineData("4^0.5", "1.99999964", "0x3fffffff")]
    [InlineData("25^0.5", "4.99999523", "0x409fffff")]
    [InlineData("100^0.5", "9.99998856", "0x411ffffd")]
    [InlineData("10000^0.5", "99.9998856", "0x42c7fffa")]
    [InlineData("40000^0.5", "199.999756", "0x4347fffa")]
    [InlineData("16777216^0.5", "4095.99512", "0x457ffff0")]
    [InlineData("0.25^0.5", "0.500000119", "0x3f000001")]
    [InlineData("3^0.5", "1.73205006", "0x3fddb3d6")]
    [InlineData("1000000^0.5", "999.991516", "0x4479fff9")]
    [InlineData("0.0001^0.5", "0.00999999791", "0x3c23d70f")]
    [InlineData("(function(x) local s = math.sqrt(x) return (s + x / s) / 2 end)(3)", "1.7320509", "1.73205078")]
    public void CloseToTheGame(string expression, string game, string model)
    {
        Assert.NotEqual(Expected(game), Expected(model));
        Assert.Equal(Expected(model), _lua.Bits(expression));
    }

    /// <summary>How often a cast or Ceiling of a quotient goes wrong: as often as in the game (decisions D151).</summary>
    [Theory]
    [InlineData("for d = 2, 100 do for n = 1, 2000 do if math.floor(n / d) ~= n // d then c = c + 1 end end end", 268)]
    [InlineData("for d = 2, 100 do for n = 1, 2000 do if math.ceil(n / d) ~= -(-n // d) then c = c + 1 end end end", 893)]
    public void CastsGoWrongAsOftenAsInTheGame(string loop, int game) =>
        Assert.Equal(game, _lua.Value($"(function() local c = 0 {loop} return c end)()"));

    /// <summary>
    /// How often a cast of a root goes wrong, with the game's count beside the model's (probe numbers-squares, plain Lua):
    /// the game's integer root is a step higher for 6 of these squares, by a rule not found (D152).
    /// </summary>
    [Theory]
    [InlineData("for n = 1, 4096 do if math.floor(math.sqrt(n * n)) ~= n then c = c + 1 end end", 64, 70)]
    public void CastsGoWrongNearlyAsOftenAsInTheGame(string loop, int game, int model)
    {
        Assert.NotEqual(game, model);
        Assert.Equal(model, _lua.Value($"(function() local c = 0 {loop} return c end)()"));
    }

    /// <summary>
    /// The worst errors in parts in 100,000 over every 64th float of [1, 2) (an even octave) and [2, 4) (odd), with
    /// the game's from its sweep of 2^-6 to 2^26 beside them (Close). That sweep was coarser: the model, which gives
    /// 6,156 of the 6,162 roots measured since, finds worse ones, as the probe numbers-fine saw near 1 (D152).
    /// </summary>
    [Theory]
    [InlineData("math.sqrt(x)", 0x3f800000, -0.0773249492, 1.92854142, -0.11549491821181203, 2.0398912033154426)]
    [InlineData("math.sqrt(x)", 0x40000000, -0.087634936, 1.92324328, -0.1240212441026552, 2.034505208333333)]
    [InlineData("x ^ 0.5", 0x3f800000, -1.3516643, 0.067014955, -0.012199268590600682, 0)]
    [InlineData("x ^ 0.5", 0x40000000, -1.3516643, 0.067014955, -0.013059732154122535, 0)]
    [InlineData("(s + x / s) / 2", 0x3f800000, -0.0115596894, 0.00990830455, -0.014159728811543813, 0.016815356076030196)]
    [InlineData("(s + x / s) / 2", 0x40000000, -0.0115596894, 0.00990830455, -0.011459323429449726, 0.011142766349221933)]
    public void RootErrorsByOctave(string root, int start, double gameLow, double gameHigh, double low, double high)
    {
        var roots = _lua.Run($$"""
            local out = {}
            for i = 0, 2^23 - 1, 64 do
              local x = f({{start}} + i)
              local s = math.sqrt(x)
              out[#out + 1] = {{root}}
            end
            return table.unpack(out)
            """);
        double worstLow = 0, worstHigh = 0;
        for (int i = 0; i < roots.Length; i++)
        {
            double exact = Math.Sqrt(BitConverter.Int32BitsToSingle(start + i * 64));
            double error = ((float)roots[i]! - exact) / exact * 1e5;
            worstLow = Math.Min(worstLow, error);
            worstHigh = Math.Max(worstHigh, error);
        }
        // the gaps to the game's worst are the recorded ones
        Assert.Equal(gameLow - low, gameLow - worstLow, 9);
        Assert.Equal(gameHigh - high, gameHigh - worstHigh, 9);
    }

    /// <summary>
    /// Sine and cosine of every whole degree, the angle made as the game's probe made it (degrees times pi / 180):
    /// the game's 720 values (moonwell-maths-numbers.txt, 11-01). The model matches 716; the other four,
    /// sin 19 degrees and its kin, are a float step off.
    /// </summary>
    static float[] WholeDegreeValues() => string.Join(" ", Sines).Split(' ').Concat(string.Join(" ", Cosines).Split(' '))
        .Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();

    [Fact]
    public void WholeDegreesCloseToTheGame()
    {
        var game = WholeDegreeValues();
        var model = _lua.Run("""
            local rad, out = f(0x3c8efa35), {}
            for d = 0, 359 do out[#out + 1] = math.sin(d * rad) end
            for d = 0, 359 do out[#out + 1] = math.cos(d * rad) end
            return table.unpack(out)
            """).Cast<float>().ToArray();
        Assert.Equal(720, game.Length);
        Assert.Equal(716, game.Zip(model).Count(p => p.First == p.Second));
        Assert.InRange(game.Zip(model).Max(p => Math.Abs(p.First - p.Second)), 2.98e-8, 2.99e-8);
    }

    static readonly string[] Sines =
    [
        "0 0.017450396 0.0349014923 0.0523359478 0.0697544515 0.0871577263 0.104528435 0.121867314 0.139169112 0.156434417",
        "0.173652083 0.190805018 0.207911626 0.224954933 0.241917938 0.258818954 0.275635391 0.292373538 0.309016913 0.325566173",
        "0.342021912 0.35836792 0.374604613 0.390732855 0.406736612 0.422616303 0.43837285 0.4539904 0.469469637 0.484811336",
        "0.499999851 0.515041351 0.52991581 0.544638872 0.559196115 0.573572993 0.587785065 0.601818204 0.615662873 0.629320204",
        "0.642785966 0.656060338 0.669130504 0.681996703 0.694659591 0.707106769 0.719338179 0.731354892 0.743144691 0.754707992",
        "0.76604563 0.777145743 0.788009286 0.798633039 0.809016764 0.819154143 0.829035163 0.838670313 0.848050117 0.857164979",
        "0.86602515 0.874618649 0.882948279 0.891006351 0.898792982 0.906308353 0.91354537 0.920503795 0.927184343 0.933580339",
        "0.939691603 0.94551903 0.951056302 0.956303895 0.961262167 0.965925515 0.970296443 0.974369049 0.978147268 0.981627762",
        "0.984806836 0.987688005 0.990268588 0.992545366 0.994521618 0.996194363 0.997563839 0.998629391 0.999390483 0.999847412",
        "0.99999994 0.999847412 0.999390483 0.998629391 0.997563839 0.996194363 0.994521618 0.992546082 0.990268588 0.987688005",
        "0.984806836 0.981627762 0.978147268 0.974369049 0.970296443 0.965925515 0.961262167 0.956303895 0.951056302 0.94551903",
        "0.939691603 0.933580339 0.927184343 0.920503795 0.91354537 0.906308353 0.898792982 0.891006351 0.882948279 0.874618649",
        "0.86602515 0.857164979 0.848050117 0.838670313 0.829035163 0.819154143 0.809016764 0.798633039 0.788009286 0.777145743",
        "0.76604563 0.754707992 0.743144691 0.731354892 0.719338179 0.707106769 0.694659591 0.681996703 0.669130504 0.656060338",
        "0.642785966 0.629320204 0.615662873 0.601818204 0.587785065 0.573572993 0.559196115 0.544638872 0.52991581 0.515041351",
        "0.499999851 0.484811336 0.469469637 0.4539904 0.43837285 0.422616303 0.406736612 0.390732855 0.374604613 0.35836792",
        "0.342021912 0.325566202 0.309016913 0.292373538 0.275635391 0.258818954 0.241923779 0.224954933 0.207911626 0.190805018",
        "0.173652083 0.156434417 0.139169112 0.121873289 0.104528435 0.0871577263 0.0697544515 0.0523359478 0.0349014923 0.017450396",
        "4.65661287e-10 -0.017450396 -0.0349014923 -0.0523359478 -0.0697544515 -0.0871577263 -0.104528435 -0.121867314 -0.139169112 -0.156434417",
        "-0.173652083 -0.190805018 -0.207911626 -0.224954933 -0.241917938 -0.258818954 -0.275635391 -0.292373538 -0.309016913 -0.325566173",
        "-0.342021912 -0.35836792 -0.374604613 -0.390732855 -0.406736612 -0.422616303 -0.43837285 -0.4539904 -0.469469637 -0.484811336",
        "-0.499999851 -0.515041351 -0.52991581 -0.544638872 -0.559196115 -0.573572993 -0.587785065 -0.601818204 -0.615658104 -0.629320204",
        "-0.642785966 -0.656060338 -0.669130504 -0.681996703 -0.694659591 -0.707106769 -0.719338179 -0.731354892 -0.743144691 -0.754707992",
        "-0.76604563 -0.777145743 -0.788009286 -0.798633039 -0.809016764 -0.819154143 -0.829035163 -0.838670313 -0.848050117 -0.857164979",
        "-0.86602515 -0.874618649 -0.882948279 -0.891006351 -0.898792982 -0.906308353 -0.91354537 -0.920503795 -0.927184343 -0.933578193",
        "-0.939691603 -0.94551903 -0.951056302 -0.956303895 -0.961262167 -0.965925515 -0.970296443 -0.974369049 -0.978147268 -0.981627762",
        "-0.984806836 -0.987688005 -0.990268588 -0.992545366 -0.994521618 -0.996194363 -0.997563839 -0.998629391 -0.999390483 -0.999847412",
        "-0.99999994 -0.999847412 -0.999390483 -0.998629391 -0.997563839 -0.996194363 -0.994521618 -0.992546082 -0.990268588 -0.987688005",
        "-0.984807849 -0.981627762 -0.978147268 -0.974369049 -0.970296443 -0.965925515 -0.961262167 -0.956303895 -0.951056302 -0.94551903",
        "-0.939691603 -0.933580339 -0.927184343 -0.920503795 -0.913547814 -0.906308353 -0.898792982 -0.891006351 -0.882948279 -0.874618649",
        "-0.86602515 -0.857168078 -0.848050117 -0.838670313 -0.829035163 -0.819154143 -0.809016764 -0.798633039 -0.788012981 -0.777145743",
        "-0.76604563 -0.754707992 -0.743144691 -0.731354892 -0.719338179 -0.707106769 -0.694659591 -0.681996703 -0.669130504 -0.656060338",
        "-0.642785966 -0.629320204 -0.615662873 -0.601818204 -0.587785065 -0.573572993 -0.559196115 -0.544638872 -0.52991581 -0.515041351",
        "-0.499999851 -0.484811336 -0.469474941 -0.4539904 -0.43837285 -0.422616303 -0.406736612 -0.390732855 -0.374604613 -0.35836792",
        "-0.342021912 -0.325566202 -0.309016913 -0.292373538 -0.275635391 -0.258818954 -0.241923779 -0.224954933 -0.207911626 -0.190805018",
        "-0.173652083 -0.156434417 -0.139169112 -0.121873289 -0.104528435 -0.0871577263 -0.0697544515 -0.0523359478 -0.0349014923 -0.017450396",
    ];

    static readonly string[] Cosines =
    [
        "0.99999994 0.999847412 0.999390483 0.998629391 0.997563839 0.996194363 0.994521618 0.992546082 0.990268588 0.987688005",
        "0.984806836 0.981627762 0.978147268 0.974369049 0.970296443 0.965925515 0.961262167 0.956303895 0.951056302 0.94551903",
        "0.939691603 0.933580339 0.927184343 0.920503795 0.91354537 0.906308353 0.898792982 0.891006351 0.882948279 0.874618649",
        "0.86602515 0.857164979 0.848050117 0.838670313 0.829035163 0.819154143 0.809016764 0.798633039 0.788009286 0.777145743",
        "0.76604563 0.754707992 0.743144691 0.731354892 0.719338179 0.707106769 0.694659591 0.681996703 0.669130504 0.656060338",
        "0.642785966 0.629320204 0.615662873 0.601818204 0.587785065 0.573572993 0.559196115 0.544638872 0.52991581 0.515041351",
        "0.499999851 0.484811336 0.469469637 0.4539904 0.43837285 0.422616303 0.406736612 0.390732855 0.374604613 0.35836792",
        "0.342021912 0.325566202 0.309016913 0.292373538 0.275635391 0.258818954 0.241917938 0.224954933 0.207911626 0.190805018",
        "0.173652083 0.156434417 0.139169112 0.121873289 0.104528435 0.0871577263 0.0697544515 0.0523359478 0.0349014923 0.017450396",
        "4.65661287e-10 -0.017450396 -0.0349014923 -0.0523359478 -0.0697544515 -0.0871577263 -0.104528435 -0.121867314 -0.139169112 -0.156434417",
        "-0.173652083 -0.190805018 -0.207911626 -0.224954933 -0.241917938 -0.258818954 -0.275635391 -0.292373538 -0.309016913 -0.325566173",
        "-0.342021912 -0.35836792 -0.374604613 -0.390732855 -0.406736612 -0.422616303 -0.43837285 -0.4539904 -0.469469637 -0.484811336",
        "-0.499999851 -0.515041351 -0.52991581 -0.544638872 -0.559196115 -0.573572993 -0.587785065 -0.601818204 -0.615662873 -0.629320204",
        "-0.642785966 -0.656060338 -0.669130504 -0.681996703 -0.694659591 -0.707106769 -0.719338179 -0.731354892 -0.743144691 -0.754707992",
        "-0.76604563 -0.777145743 -0.788009286 -0.798633039 -0.809016764 -0.819154143 -0.829035163 -0.838670313 -0.848050117 -0.857164979",
        "-0.86602515 -0.874618649 -0.882948279 -0.891006351 -0.898792982 -0.906308353 -0.91354537 -0.920503795 -0.927184343 -0.933580339",
        "-0.939691603 -0.94551903 -0.951056302 -0.956303895 -0.961262167 -0.965925515 -0.970294952 -0.974369049 -0.978147268 -0.981627762",
        "-0.984806836 -0.987688005 -0.990268588 -0.992545366 -0.994521618 -0.996194363 -0.997563839 -0.998629391 -0.999390483 -0.999847412",
        "-0.99999994 -0.999847412 -0.999390483 -0.998629391 -0.997563839 -0.996194363 -0.994521618 -0.992546082 -0.990268588 -0.987688005",
        "-0.984806836 -0.981627762 -0.978147268 -0.974369049 -0.970296443 -0.965925515 -0.961262167 -0.956303895 -0.951056302 -0.94551903",
        "-0.939691603 -0.933580339 -0.927184343 -0.920503795 -0.91354537 -0.906308353 -0.898792982 -0.891006351 -0.882948279 -0.874618649",
        "-0.86602515 -0.857164979 -0.848050117 -0.838670313 -0.829035163 -0.819154143 -0.809016764 -0.798633039 -0.788012981 -0.777145743",
        "-0.76604563 -0.754707992 -0.743144691 -0.731354892 -0.719338179 -0.707106769 -0.694659591 -0.681996703 -0.669130504 -0.656060338",
        "-0.642785966 -0.629320204 -0.615662873 -0.601818204 -0.587785065 -0.573572993 -0.559196115 -0.544638872 -0.52991581 -0.515041351",
        "-0.499999851 -0.484811336 -0.469469637 -0.4539904 -0.43837285 -0.422616303 -0.406736612 -0.390732855 -0.374604613 -0.358373523",
        "-0.342021912 -0.325566202 -0.309016913 -0.292373538 -0.275635391 -0.258818954 -0.241917938 -0.224954933 -0.207911626 -0.190805018",
        "-0.173652083 -0.156434417 -0.139169112 -0.121873289 -0.104528435 -0.0871577263 -0.0697544515 -0.0523359478 -0.0349014923 -0.017450396",
        "-4.65661287e-10 0.017450396 0.0349014923 0.0523359478 0.0697544515 0.0871577263 0.104528435 0.121867314 0.139169112 0.156434417",
        "0.173646167 0.190805018 0.207911626 0.224954933 0.241917938 0.258818954 0.275635391 0.292373538 0.309016913 0.325566173",
        "0.342021912 0.35836792 0.374604613 0.390732855 0.406731099 0.422616303 0.43837285 0.4539904 0.469469637 0.484811336",
        "0.499999851 0.515036225 0.52991581 0.544638872 0.559196115 0.573572993 0.587785065 0.601818204 0.615658104 0.629320204",
        "0.642785966 0.656060338 0.669130504 0.681996703 0.694659591 0.707106769 0.719338179 0.731354892 0.743144691 0.754707992",
        "0.76604563 0.777145743 0.788009286 0.798633039 0.809016764 0.819154143 0.829035163 0.838670313 0.848050117 0.857164979",
        "0.86602515 0.874618649 0.882945418 0.891006351 0.898792982 0.906308353 0.91354537 0.920503795 0.927184343 0.933580339",
        "0.939691603 0.94551903 0.951056302 0.956303895 0.961262167 0.965925515 0.970294952 0.974369049 0.978147268 0.981627762",
        "0.984806836 0.987688005 0.990268588 0.992545366 0.994521618 0.996194363 0.997563839 0.998629391 0.999390483 0.999847412",
    ];
}