using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>
/// The maths natives against the probes strings and strings-2 (docs/facts.md, "Strings, numbers and hashtables"). In
/// the game they give Lua's math.* numbers, so they share its model and are Close where it is (decisions D31, D32):
/// there the game's value is given beside the model's and the model's is asserted.
/// </summary>
public sealed class MathNativesTests : IDisposable
{
    readonly GameNatives _natives = new(new Game(0));
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    uint Call(string native, uint x, uint y)
    {
        float a = BitConverter.UInt32BitsToSingle(x), b = BitConverter.UInt32BitsToSingle(y);
        return BitConverter.SingleToUInt32Bits(native switch
        {
            "SquareRoot" => _natives.SquareRoot(a),
            "Sin" => _natives.Sin(a),
            "Cos" => _natives.Cos(a),
            "Tan" => _natives.Tan(a),
            "Asin" => _natives.Asin(a),
            "Acos" => _natives.Acos(a),
            "Atan" => _natives.Atan(a),
            "Atan2" => _natives.Atan2(a, b),
            "Pow" => _natives.Pow(a, b),
            "Deg2Rad" => _natives.Deg2Rad(a),
            "Rad2Deg" => _natives.Rad2Deg(a),
            _ => throw new ArgumentException(native),
        });
    }

    // SquareRoot is 0 up to 3a83126e and a root from 3a83126f, the float nearest 0.001 (a bisection in the game)
    [Theory]
    [InlineData("SquareRoot", 0x00000000u, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x3e800000u, 0u, 0x3f000000u)]
    [InlineData("SquareRoot", 0x40000000u, 0u, 0x3fb504f3u)]
    [InlineData("SquareRoot", 0x40400000u, 0u, 0x3fddb3f5u)]
    [InlineData("SquareRoot", 0x41c80000u, 0u, 0x40a0008cu)]
    [InlineData("SquareRoot", 0x42c80000u, 0u, 0x4120008cu)]
    [InlineData("SquareRoot", 0x49742400u, 0u, 0x447a00f6u)]
    [InlineData("SquareRoot", 0x38d1b717u, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0xbf800000u, 0u, 0x00000000u)]
    [InlineData("Sin", 0x00000000u, 0u, 0x00000000u)]
    [InlineData("Cos", 0x00000000u, 0u, 0x3f7fffffu)]
    [InlineData("Tan", 0x00000000u, 0u, 0x00000000u)]
    [InlineData("Cos", 0x40490fdbu, 0u, 0xbf7fffffu)]
    [InlineData("Asin", 0x00000000u, 0u, 0x00000000u)]
    [InlineData("Acos", 0x00000000u, 0u, 0x3fc90fdau)]
    [InlineData("Asin", 0x3f000000u, 0u, 0x3f060a91u)]
    [InlineData("Acos", 0x3f000000u, 0u, 0x3f860a91u)]
    [InlineData("Asin", 0xbf000000u, 0u, 0xbf060a91u)]
    [InlineData("Acos", 0xbf000000u, 0u, 0x40060a91u)]
    [InlineData("Acos", 0x3f800000u, 0u, 0x00000000u)]
    [InlineData("Asin", 0x40000000u, 0u, 0x00000000u)]
    [InlineData("Acos", 0x40000000u, 0u, 0x00000000u)]
    [InlineData("Atan", 0x00000000u, 0u, 0x00000000u)]
    [InlineData("Atan", 0x42c80000u, 0u, 0x3fc7c82fu)]
    [InlineData("Atan2", 0x00000000u, 0x00000000u, 0x00000000u)]
    [InlineData("Pow", 0x40000000u, 0x41200000u, 0x44800000u)]
    [InlineData("Pow", 0x41200000u, 0x40400000u, 0x447a0000u)]
    [InlineData("Pow", 0x3fc00000u, 0x40000000u, 0x40100000u)]
    [InlineData("Pow", 0xc0000000u, 0x40400000u, 0xc1000000u)]
    [InlineData("Pow", 0x00000000u, 0x00000000u, 0x3f800000u)]
    [InlineData("Deg2Rad", 0x43340000u, 0u, 0x40490fdau)]
    [InlineData("Deg2Rad", 0x42b40000u, 0u, 0x3fc90fdau)]
    [InlineData("Deg2Rad", 0x3f800000u, 0u, 0x3c8efa35u)]
    [InlineData("Deg2Rad", 0x42340000u, 0u, 0x3f490fdau)]
    [InlineData("Deg2Rad", 0xc1f00000u, 0u, 0xbf060a91u)]
    [InlineData("Deg2Rad", 0x43b40000u, 0u, 0x40c90fdau)]
    [InlineData("Rad2Deg", 0x40490fdbu, 0u, 0x43340000u)]
    [InlineData("Rad2Deg", 0x3f800000u, 0u, 0x42652ee1u)]
    [InlineData("Rad2Deg", 0x3f000000u, 0u, 0x41e52ee1u)]
    [InlineData("Rad2Deg", 0xc0000000u, 0u, 0xc2e52ee1u)]
    [InlineData("SquareRoot", 0x3c23d70au, 0u, 0x3dccccf3u)]
    [InlineData("SquareRoot", 0x3a83126eu, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x3a03126fu, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x3951b717u, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x391d4952u, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x38e6afcdu, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x38d3cff6u, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x38d1bc76u, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x38bcbe62u, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x3727c5acu, 0u, 0x00000000u)]
    [InlineData("SquareRoot", 0x2edbe6ffu, 0u, 0x00000000u)]
    [InlineData("Asin", 0x3f800001u, 0u, 0x00000000u)]
    [InlineData("Acos", 0x3f800001u, 0u, 0x00000000u)]
    [InlineData("Asin", 0xbf800001u, 0u, 0x00000000u)]
    [InlineData("Acos", 0xbf800001u, 0u, 0x00000000u)]
    [InlineData("Asin", 0xc0000000u, 0u, 0x00000000u)]
    [InlineData("Acos", 0xc0000000u, 0u, 0x00000000u)]
    [InlineData("Sin", 0x3f000000u, 0u, 0x3ef577e6u)]
    [InlineData("Cos", 0x3f000000u, 0u, 0x3f60a912u)]
    [InlineData("Tan", 0x3f000000u, 0u, 0x3f0bdaf5u)]
    [InlineData("Sin", 0x3f800000u, 0u, 0x3f576ad1u)]
    [InlineData("Cos", 0x3f800000u, 0u, 0x3f0a50f7u)]
    [InlineData("Tan", 0x3f800000u, 0u, 0x3fc759b9u)]
    [InlineData("Sin", 0xbf800000u, 0u, 0xbf576a9au)]
    [InlineData("Cos", 0xbf800000u, 0u, 0x3f0a514cu)]
    [InlineData("Tan", 0xbf800000u, 0u, 0xbfc7590cu)]
    [InlineData("Sin", 0x40490fdbu, 0u, 0x30000000u)]
    [InlineData("Tan", 0x40490fdbu, 0u, 0xb0000001u)]
    [InlineData("Sin", 0x42c80000u, 0u, 0xbf01a157u)]
    [InlineData("Cos", 0x42c80000u, 0u, 0x3f5cc0d2u)]
    [InlineData("Tan", 0x42c80000u, 0u, 0xbf1653e9u)]
    [InlineData("Sin", 0x49742400u, 0u, 0xbef15ae9u)]
    [InlineData("Cos", 0x49742400u, 0u, 0x3f61c597u)]
    [InlineData("Tan", 0x49742400u, 0u, 0xbf08d5b9u)]
    [InlineData("Asin", 0x3f800000u, 0u, 0x3fc90fdbu)]
    [InlineData("Asin", 0xbf800000u, 0u, 0xbfc90fdbu)]
    [InlineData("Acos", 0xbf800000u, 0u, 0x40490fdbu)]
    [InlineData("Atan2", 0x00000000u, 0xbf800000u, 0x40490fdbu)]
    [InlineData("Atan2", 0x3f800000u, 0x00000000u, 0x3fc90fdbu)]
    [InlineData("Pow", 0x00000000u, 0xbf800000u, 0x00000000u)]
    [InlineData("SquareRoot", 0x3a83126fu, 0u, 0x3d018707u)]
    public void The_maths_natives_give_the_games_numbers(string native, uint x, uint y, uint game) =>
        Assert.Equal(game, Call(native, x, y));

    [Theory]
    [InlineData("Atan", 0x3f800000u, 0u, 0x3f490fdcu, 0x3f490fdau)]
    [InlineData("Atan", 0xbf800000u, 0u, 0xbf490fdcu, 0xbf490fdau)]
    [InlineData("Atan", 0x3f000000u, 0u, 0x3eed6339u, 0x3eed6338u)]
    [InlineData("Atan2", 0x3f800000u, 0x3f800000u, 0x3f490fdcu, 0x3f490fdau)]
    [InlineData("Atan2", 0xbf800000u, 0xbf800000u, 0xc016cbe4u, 0xc016cbe3u)]
    [InlineData("Atan2", 0x40400000u, 0x40800000u, 0x3f24bc7eu, 0x3f24bc7du)]
    [InlineData("Pow", 0x40000000u, 0x3f000000u, 0x3fb504f3u, 0x3fb504f2u)]
    [InlineData("Pow", 0x41c80000u, 0x3f000000u, 0x409ffff6u, 0x409fffffu)]
    [InlineData("Pow", 0xc1000000u, 0x3eaaaaabu, 0x3ffffffdu, 0x3fffffffu)]
    [InlineData("Pow", 0x40000000u, 0xbf800000u, 0x3f000002u, 0x3f000001u)]
    [InlineData("Pow", 0x3f000000u, 0x3f000000u, 0x3f3504f5u, 0x3f3504f6u)]
    public void The_maths_natives_are_off_the_game_where_the_math_model_is(string native, uint x, uint y, uint game, uint model)
    {
        Assert.NotEqual(game, model);
        Assert.Equal(model, Call(native, x, y));
    }

    // In the game the natives gave math.*'s numbers but for SquareRoot below 0.001, Asin and Acos past 1 and Atan2(0, 0)
    [Fact]
    public void The_maths_natives_give_math_s_numbers_but_where_the_game_differs()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
              local differ = {}
              for x = -7, 7, 0.37 do
                local checks = {SquareRoot(x * x + 0.001) == math.sqrt(x * x + 0.001), Sin(x) == math.sin(x), Cos(x) == math.cos(x),
                  Tan(x) == math.tan(x), Atan(x) == math.atan(x), Atan2(x, 2) == math.atan(x, 2), Pow(2, x) == 2 ^ x,
                  Asin(x / 7) == math.asin(x / 7), Acos(x / 7) == math.acos(x / 7)}
                for i, same in ipairs(checks) do
                  if not same then differ[#differ + 1] = i .. '@' .. x end
                end
              end
              print(#differ, SquareRoot(0.0009), math.sqrt(0.0009) > 0, Asin(2), Acos(-2), Atan2(0, 0))
            end
            """), "");

        Assert.Equal(["0.000 print: 0\t0.0\ttrue\t0.0\t0.0\t0.0"], simulation.Log);
    }
}
