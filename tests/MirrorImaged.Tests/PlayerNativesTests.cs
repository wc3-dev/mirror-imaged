using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

public sealed class PlayerNativesTests
{
    readonly Game _game = new(0);
    readonly GameNatives _natives;

    public PlayerNativesTests() => _natives = new GameNatives(_game);

    // facts: in single player the slots are 0 to 27 and GetLocalPlayer is slot 0
    [Fact]
    public void There_are_28_players_and_the_local_one_is_player_0()
    {
        var players = Enumerable.Range(0, 28).Select(_natives.Player).ToList();

        Assert.Equal(Enumerable.Range(0, 28), players.Select(_natives.GetPlayerId));
        Assert.Equal(28, players.Distinct().Count());
        Assert.Same(_natives.Player(0), _natives.GetLocalPlayer());
        Assert.Null(_natives.Player(28));
        Assert.Null(_natives.Player(-1));
    }

    [Fact]
    public void The_players_are_the_games_first_handles()
    {
        Assert.Equal(0x100000, _natives.Player(0)!.Id);
        Assert.Equal(0x10001B, _natives.Player(27)!.Id);
        Assert.Same(_natives.Player(5), _game.Handle(_natives.Player(5)!.Key));
    }

    [Fact]
    public void Text_shown_to_a_player_goes_in_the_log_with_the_player()
    {
        _natives.DisplayTextToPlayer(_natives.Player(0), 0, 0, "hello");
        _game.Advance(Game.Steps(2));
        _natives.DisplayTimedTextToPlayer(_natives.Player(3), 0.5f, 0.5f, 10, "there");
        _natives.DisplayTextToPlayer(null, 0, 0, "nobody");
        _natives.DisplayTextToPlayer(_natives.Player(1), 0, 0, null);

        Assert.Equal(["0.000 player 0: hello", "2.000 player 3: there", "2.000 player 1: "], _game.Log);
    }

    [Fact]
    public void The_random_natives_use_the_games_seeded_generator()
    {
        _natives.SetRandomSeed(42);
        var first = new[] { _natives.GetRandomInt(1, 100), _natives.GetRandomInt(1, 100) };
        var real = _natives.GetRandomReal(2, 3);

        _game.Seed(42);

        Assert.Equal(first, new[] { _game.RandomInt(1, 100), _game.RandomInt(1, 100) });
        Assert.Equal(real, _game.RandomReal(2, 3));
        Assert.InRange(real, 2, 3);
    }
}
