using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>Forces, against the players probe (docs/facts.md, "Players and the game").</summary>
public sealed class ForceNativesTests
{
    readonly Game _game = new(0);
    readonly GameNatives _natives;

    public ForceNativesTests() => _natives = new GameNatives(_game);

    player P(int number) => _natives.Player(number)!;

    int[] Members(force? force) => [.. Enumerable.Range(0, 28).Where(slot => _natives.IsPlayerInForce(P(slot), force))];

    [Fact]
    public void Players_are_added_and_removed_and_a_cleared_force_is_empty()
    {
        var force = _natives.CreateForce();
        _natives.ForceAddPlayer(force, P(3));
        _natives.ForceAddPlayer(force, P(27));
        _natives.ForceAddPlayer(force, P(3));

        Assert.Equal([3, 27], Members(force));
        Assert.True(_natives.BlzForceHasPlayer(force, P(27)));

        _natives.ForceRemovePlayer(force, P(3));
        Assert.Equal([27], Members(force));
        _natives.ForceClear(force);
        Assert.Empty(Members(force));
    }

    // the probe: ForForce runs over 3, 1 and 2 in slot order, and GetEnumPlayer is nil after it
    [Fact]
    public void ForForce_runs_in_slot_order_with_the_enum_player_set()
    {
        var force = _natives.CreateForce();
        foreach (var slot in new[] { 3, 1, 2 }) _natives.ForceAddPlayer(force, P(slot));
        var order = new List<int>();

        _natives.ForForce(force, () => order.Add(_natives.GetPlayerId(_natives.GetEnumPlayer())));

        Assert.Equal([1, 2, 3], order);
        Assert.Null(_natives.GetEnumPlayer());
    }

    // decisions D94: the players the force holds as ForForce starts (not measured)
    [Fact]
    public void ForForce_runs_over_the_players_the_force_held_as_it_started()
    {
        var force = _natives.CreateForce();
        _natives.ForceAddPlayer(force, P(1));
        _natives.ForceAddPlayer(force, P(2));
        var order = new List<int>();

        _natives.ForForce(force, () =>
        {
            order.Add(_natives.GetPlayerId(_natives.GetEnumPlayer()));
            _natives.ForceRemovePlayer(force, P(2));
            _natives.ForceAddPlayer(force, P(3));
        });

        Assert.Equal([1, 2], order);
        Assert.Equal([1, 3], Members(force));
    }

    [Fact]
    public void A_nested_ForForce_gives_the_outer_enum_player_back()
    {
        var outer = _natives.CreateForce();
        var inner = _natives.CreateForce();
        _natives.ForceAddPlayer(outer, P(1));
        _natives.ForceAddPlayer(outer, P(2));
        _natives.ForceAddPlayer(inner, P(5));
        var seen = new List<string>();

        _natives.ForForce(outer, () =>
        {
            _natives.ForForce(inner, () => seen.Add($"in {_natives.GetPlayerId(_natives.GetEnumPlayer())}"));
            seen.Add($"out {_natives.GetPlayerId(_natives.GetEnumPlayer())}");
        });

        Assert.Equal(["in 5", "out 1", "in 5", "out 2"], seen);
    }

    [Fact]
    public void ForForce_lets_go_of_its_callback()
    {
        var callback = new Callback();
        var force = _natives.CreateForce();
        _natives.ForceAddPlayer(force, P(0));

        _natives.ForForce(force, callback.Run);
        _natives.ForForce(null, new Callback().Run);

        Assert.Equal(1, callback.Runs);
        Assert.True(callback.Disposed);
    }

    sealed class Callback : IDisposable
    {
        public int Runs;
        public bool Disposed;
        public void Run() => Runs++;
        public void Dispose() => Disposed = true;
    }

    // the probe: the enumerations clear the force first and take the playing players of slots 0 to 23, not the
    // neutral ones, though they play; a player is its own ally
    [Fact]
    public void Enumerating_clears_the_force_and_takes_the_playing_players()
    {
        _natives.Seat([0, 2], [5]);
        _natives.SetPlayerAlliance(P(0), P(5), _natives.ConvertAllianceType(0), true);
        var force = _natives.CreateForce();
        _natives.ForceAddPlayer(force, P(7));

        _natives.ForceEnumPlayers(force, null);
        Assert.Equal([0, 2, 5], Members(force));
        _natives.ForceEnumAllies(force, P(0), null);
        Assert.Equal([0, 5], Members(force));
        _natives.ForceEnumEnemies(force, P(0), null);
        Assert.Equal([2], Members(force));
        _natives.ForceEnumPlayersCounted(force, null, 2);
        Assert.Equal([0, 2], Members(force));
    }

    // the events probe: a filter is called for each playing player in slot order with GetFilterPlayer set, nil after
    [Fact]
    public void A_filter_sees_each_playing_player_as_the_filter_player()
    {
        _natives.Seat([0, 2], [5]);
        var seen = new List<int>();
        var filter = _natives.Filter(() =>
        {
            seen.Add(_natives.GetPlayerId(_natives.GetFilterPlayer()));
            return _natives.GetFilterPlayer() != P(2);
        });
        var force = _natives.CreateForce();

        _natives.ForceEnumPlayers(force, filter);

        Assert.Equal([0, 2, 5], seen);
        Assert.Equal([0, 5], Members(force));
        Assert.Null(_natives.GetFilterPlayer());
    }

    [Fact]
    public void A_destroyed_force_is_gone()
    {
        var force = _natives.CreateForce()!;

        _natives.DestroyForce(force);

        Assert.Null(_game.Handle(force.Key));
    }
}
