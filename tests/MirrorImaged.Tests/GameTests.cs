using MirrorImaged.World;

namespace MirrorImaged.Tests;

public sealed class GameTests
{
    readonly Game _game = new(0);
    readonly List<string> _ran = [];

    [Fact]
    public void The_clock_moves_in_steps_of_a_1024th_of_a_second()
    {
        _game.Advance(Game.Steps(1.5));

        Assert.Equal(1536, _game.Step);
        Assert.Equal(4, Game.Steps(1 / 256.0));
        Assert.Equal(31, Game.Steps(0.03));
    }

    [Fact]
    public void A_time_past_the_clocks_range_is_its_end()
    {
        Assert.Equal(1L << 60, Game.Steps(1e30));
        Assert.Equal(1L << 60, Game.Steps(double.PositiveInfinity));
        Assert.Equal(-(1L << 60), Game.Steps(double.NegativeInfinity));
        Assert.Equal(0, Game.Steps(double.NaN));
    }

    [Fact]
    public void Text_of_several_lines_is_one_entry_with_every_line_marked()
    {
        _game.Write("print", "one\ntwo");

        Assert.Equal(["0.000 print: one\n0.000 print: two"], _game.Log);
    }

    [Fact]
    public void What_falls_due_runs_at_its_step_in_the_order_it_was_scheduled()
    {
        _game.At(10, () => _ran.Add($"b {_game.Step}"));
        _game.At(5, () => _ran.Add($"a {_game.Step}"));
        _game.At(10, () => _ran.Add($"c {_game.Step}"));

        _game.Advance(9);
        Assert.Equal(["a 5"], _ran);
        Assert.Equal(9, _game.Step);

        _game.Advance(1);
        Assert.Equal(["a 5", "b 10", "c 10"], _ran);
    }

    [Fact]
    public void What_is_scheduled_for_now_or_the_past_runs_in_this_step_after_the_rest()
    {
        _game.At(4, () =>
        {
            _ran.Add("a");
            _game.At(_game.Step, () => _ran.Add($"now {_game.Step}"));
            _game.At(0, () => _ran.Add($"past {_game.Step}"));
        });
        _game.At(4, () => _ran.Add("b"));

        _game.Advance(4);

        Assert.Equal(["a", "b", "now 4", "past 4"], _ran);
    }

    [Fact]
    public void Advancing_by_nothing_runs_what_is_due_now()
    {
        _game.At(0, () => _ran.Add("now"));

        _game.Advance(0);

        Assert.Equal(["now"], _ran);
    }

    [Fact]
    public void Ids_count_up_from_0x100000()
    {
        Assert.Equal(0x100000, _game.NewId());
        Assert.Equal(0x100001, _game.NewId());
    }

    [Fact]
    public void Handles_are_found_by_their_keys_until_removed_and_a_key_is_never_given_twice()
    {
        object first = new(), second = new();
        int firstKey = _game.Add(first), secondKey = _game.Add(second);

        Assert.Same(second, _game.Handle(secondKey));
        _game.Remove(firstKey);
        Assert.Null(_game.Handle(firstKey));
        Assert.Equal(3, new[] { firstKey, secondKey, _game.Add(new object()) }.Distinct().Count());
    }

    [Fact]
    public void The_same_seed_gives_the_same_numbers_and_another_seed_others()
    {
        int[] Draw(int seed)
        {
            var game = new Game(seed);
            return [.. Enumerable.Range(0, 20).Select(_ => game.RandomInt(0, 1000000))];
        }

        Assert.Equal(Draw(1), Draw(1));
        Assert.NotEqual(Draw(1), Draw(2));
        _game.Seed(1);
        Assert.Equal(Draw(1), Enumerable.Range(0, 20).Select(_ => _game.RandomInt(0, 1000000)));
    }

    [Fact]
    public void Random_integers_take_in_both_bounds_given_in_either_order()
    {
        var seen = Enumerable.Range(0, 1000).Select(i => i % 2 == 0 ? _game.RandomInt(1, 3) : _game.RandomInt(3, 1)).ToHashSet();

        Assert.Equal([1, 2, 3], seen.Order());
        Assert.Equal(5, _game.RandomInt(5, 5));
        _ = _game.RandomInt(int.MinValue, int.MaxValue);
    }

    [Fact]
    public void Random_reals_stay_between_the_bounds()
    {
        for (int i = 0; i < 1000; i++)
        {
            Assert.InRange(_game.RandomReal(0, 1), 0f, 0.99999994f);
            Assert.InRange(_game.RandomReal(-5, 5), -5f, 5f);
        }
    }

    [Fact]
    public void A_log_line_has_the_time_its_source_and_its_text()
    {
        _game.Advance(Game.Steps(1.5));
        _game.Write("print", "hello");

        Assert.Equal(["1.500 print: hello"], _game.Log);
    }
}
