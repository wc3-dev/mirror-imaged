using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>The random natives against the probes strings and strings-4 (docs/facts.md, "Strings, numbers and hashtables").</summary>
public sealed class RandomNativesTests
{
    readonly GameNatives _natives = new(new Game(1));

    List<int> Integers(int low, int high) => [.. Enumerable.Range(0, 1000).Select(_ => _natives.GetRandomInt(low, high))];

    List<float> Reals(float low, float high) => [.. Enumerable.Range(0, 1000).Select(_ => _natives.GetRandomReal(low, high))];

    [Fact]
    public void Bounds_that_meet_give_that_bound()
    {
        Assert.Equal(3, _natives.GetRandomInt(3, 3));
        Assert.Equal(1f, _natives.GetRandomReal(1, 1));
    }

    // The count of numbers wraps in 32 bits, as the game's: always the first bound when it wraps to 0, and past
    // int.MaxValue into the negatives from it.
    [Fact]
    public void The_widest_integer_bounds_wrap_as_the_games()
    {
        Assert.All(Integers(int.MinValue, int.MaxValue), v => Assert.Equal(int.MinValue, v));
        Assert.All(Integers(int.MaxValue, int.MinValue), v => Assert.Equal(int.MaxValue, v));
        Assert.All(Integers(int.MaxValue, 0), v => Assert.True(v < 0));
        Assert.All(Integers(0, int.MaxValue), v => Assert.True(v >= 0));
    }

    // The game gave numbers across (-3e38, 3e38) and from 4.7e35 to 3e38 for (0, 3e38); for (3e38, -3e38) it gave some
    // from about 0 to 3.4e38, 355 of 1,000 tiny and below 0, which the model doesn't follow: there it holds at the largest float.
    [Fact]
    public void Finite_bounds_give_finite_numbers()
    {
        var wide = Reals(-3e38f, 3e38f);
        Assert.All(wide, v => Assert.InRange(v, -3e38f, 3e38f));
        Assert.Contains(wide, v => v < -1e38f);
        Assert.Contains(wide, v => v > 1e38f);
        Assert.All(Reals(0, 3e38f), v => Assert.InRange(v, 0, 3e38f));
        Assert.All(Reals(3e38f, -3e38f), v => Assert.InRange(v, 3e38f, float.MaxValue));
    }

    // The game's generator isn't known (decisions D46): its numbers for seed 42 beside the model's.
    [Fact]
    public void A_seed_gives_the_models_numbers_not_the_games()
    {
        _natives.SetRandomSeed(42);
        var integers = Enumerable.Range(0, 5).Select(_ => _natives.GetRandomInt(0, 99)).ToArray();
        _natives.SetRandomSeed(42);
        var reals = Enumerable.Range(0, 3).Select(_ => BitConverter.SingleToUInt32Bits(_natives.GetRandomReal(0, 1))).ToArray();

        Assert.NotEqual([63, 11, 34, 15, 77], integers);
        Assert.Equal([74, 15, 27, 34, 3], integers);
        Assert.NotEqual([0x3f71d256u, 0x3e8b35bcu, 0x3e2bf0d8u], reals);
    }
}
