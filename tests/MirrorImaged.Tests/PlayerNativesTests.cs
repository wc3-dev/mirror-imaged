using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>
/// The player natives, against the facts' "Version and launching" and the players probe (docs/facts.md, "Players and
/// the game"): a one-player <c>-loadfile</c> game, seated as <see cref="GameNatives.Seat"/> seats it.
/// </summary>
public sealed class PlayerNativesTests
{
    readonly Game _game = new(0);
    readonly GameNatives _natives;

    public PlayerNativesTests() => _natives = new GameNatives(_game);

    player P(int number) => _natives.Player(number)!;

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

    // facts: player 1 is "Local Player", Playing, a user, no observer; the probe: an unused slot is "Player N", Empty,
    // MAP_CONTROL_NONE, team -1, start -1, its own colour, its race selectable
    [Fact]
    public void Seated_alone_the_local_user_is_Local_Player_playing_and_unused_slots_are_empty()
    {
        _natives.Seat([0], []);

        var local = _natives.GetLocalPlayer();
        Assert.Equal("Local Player", _natives.GetPlayerName(local));
        Assert.Same(_natives.ConvertPlayerSlotState(1), _natives.GetPlayerSlotState(local));
        Assert.Same(_natives.ConvertMapControl(0), _natives.GetPlayerController(local));
        Assert.False(_natives.IsPlayerObserver(local));
        Assert.Equal(1, _natives.GetPlayerState(local, _natives.ConvertPlayerState(9))); // PLAYER_STATE_PLACED
        for (int slot = 1; slot < 24; slot++)
        {
            var unused = P(slot);
            Assert.Equal($"Player {slot + 1}", _natives.GetPlayerName(unused));
            Assert.Same(_natives.ConvertPlayerSlotState(0), _natives.GetPlayerSlotState(unused));
            Assert.Same(_natives.ConvertMapControl(5), _natives.GetPlayerController(unused));
            Assert.Equal(-1, _natives.GetPlayerTeam(unused));
            Assert.Equal(-1, _natives.GetPlayerStartLocation(unused));
            Assert.Same(_natives.ConvertPlayerColor(slot), _natives.GetPlayerColor(unused));
            Assert.True(_natives.GetPlayerSelectable(unused));
            Assert.Equal(0, _natives.GetPlayerState(unused, _natives.ConvertPlayerState(9)));
        }
    }

    // the probe: the four neutral slots are "Player 25" to "Player 28", Playing, MAP_CONTROL_CREEP, team 0, race 0
    [Fact]
    public void The_neutral_slots_play_as_creeps_on_team_0_with_race_0()
    {
        _natives.Seat([0], []);

        for (int slot = 24; slot < 28; slot++)
        {
            Assert.Equal($"Player {slot + 1}", _natives.GetPlayerName(P(slot)));
            Assert.Same(_natives.ConvertPlayerSlotState(1), _natives.GetPlayerSlotState(P(slot)));
            Assert.Same(_natives.ConvertMapControl(4), _natives.GetPlayerController(P(slot)));
            Assert.Equal(0, _natives.GetPlayerTeam(P(slot)));
            Assert.Same(_natives.ConvertRace(0), _natives.GetPlayerRace(P(slot)));
            Assert.Same(_natives.ConvertPlayerColor(slot), _natives.GetPlayerColor(P(slot)));
        }
    }

    [Fact]
    public void Users_and_computers_take_their_slots_the_first_user_is_local_and_the_rest_are_empty()
    {
        _natives.SetPlayerController(P(1), _natives.ConvertMapControl(0));
        _natives.Seat([3, 1], [5]);

        Assert.Same(P(3), _natives.GetLocalPlayer());
        foreach (var (slot, controller) in new[] { (3, 0), (1, 0), (5, 1) })
        {
            Assert.Same(_natives.ConvertPlayerSlotState(1), _natives.GetPlayerSlotState(P(slot)));
            Assert.Same(_natives.ConvertMapControl(controller), _natives.GetPlayerController(P(slot)));
        }
        Assert.Same(_natives.ConvertPlayerSlotState(0), _natives.GetPlayerSlotState(P(0)));
        Assert.Equal("Player 4", _natives.GetPlayerName(P(3)));
    }

    [Theory]
    [InlineData(new int[0], new int[0])]
    [InlineData(new[] { 24 }, new int[0])]
    [InlineData(new[] { -1 }, new int[0])]
    [InlineData(new[] { 0 }, new[] { 0 })]
    [InlineData(new[] { 1, 1 }, new int[0])]
    public void Seating_needs_a_user_and_slots_0_to_23_each_once(int[] users, int[] computers)
    {
        Assert.Throws<ArgumentException>(() => _natives.Seat(users, computers));
    }

    // facts: GetPlayerRace compares equal to RACE_*; IsPlayerRacePrefSet(p, RACE_PREF_RANDOM) is true for a selectable
    // race. The probe: a selectable user and the unused slots read random races; a fixed race is the preference's.
    [Fact]
    public void A_fixed_race_is_the_preferences_and_a_selectable_one_turns_random()
    {
        _natives.SetPlayerRacePreference(P(0), _natives.ConvertRacePref(8)); // undead
        _natives.SetPlayerRaceSelectable(P(0), false);
        _natives.SetPlayerRacePreference(P(1), _natives.ConvertRacePref(1)); // human, selectable
        _natives.SetPlayerRacePreference(P(2), _natives.ConvertRacePref(32)); // random
        _natives.SetPlayerRaceSelectable(P(2), false);
        _natives.Seat([0, 1], [2]);

        Assert.Same(_natives.ConvertRace(3), _natives.GetPlayerRace(P(0)));
        Assert.True(_natives.IsPlayerRacePrefSet(P(0), _natives.ConvertRacePref(8)));
        Assert.False(_natives.GetPlayerSelectable(P(0)));
        Assert.True(_natives.IsPlayerRacePrefSet(P(1), _natives.ConvertRacePref(32)));
        Assert.False(_natives.IsPlayerRacePrefSet(P(1), _natives.ConvertRacePref(1)));
        var four = new[] { 1, 2, 3, 4 }.Select(_natives.ConvertRace).ToList();
        Assert.Contains(_natives.GetPlayerRace(P(1)), four);
        Assert.Contains(_natives.GetPlayerRace(P(2)), four);
        Assert.All(Enumerable.Range(3, 21), slot => Assert.Contains(_natives.GetPlayerRace(P(slot)), four));
    }

    [Fact]
    public void Random_races_come_from_the_runs_seed()
    {
        string[] Races(int seed)
        {
            var natives = new GameNatives(new Game(seed));
            natives.Seat([0], []);
            return [.. Enumerable.Range(0, 24).Select(slot => natives.GetPlayerRace(natives.Player(slot))!.Id.ToString())];
        }

        Assert.Equal(Races(5), Races(5));
        Assert.NotEqual(Races(5), Races(6));
        Assert.Equal(4, Races(5).Distinct().Count());
    }

    // facts: GetPlayerColor compares equal to ConvertPlayerColor(n)
    [Fact]
    public void A_players_colour_is_its_slots_until_set()
    {
        Assert.Same(_natives.ConvertPlayerColor(4), _natives.GetPlayerColor(P(4)));

        _natives.SetPlayerColor(P(4), _natives.ConvertPlayerColor(12));

        Assert.Same(_natives.ConvertPlayerColor(12), _natives.GetPlayerColor(P(4)));
    }

    // facts: an unused slot's team is -1; SetPlayerTeam changes the team number only, no alliance
    [Fact]
    public void Setting_a_team_changes_the_team_only()
    {
        _natives.SetPlayerTeam(P(1), 2);
        _natives.SetPlayerTeam(P(2), 2);

        Assert.Equal(2, _natives.GetPlayerTeam(P(1)));
        Assert.Equal(-1, _natives.GetPlayerTeam(P(3)));
        Assert.False(_natives.IsPlayerAlly(P(1), P(2)));
        Assert.Equal(0, Alliances(1, 2));
    }

    int Alliances(int source, int other) =>
        Enumerable.Range(0, 10).Sum(n => _natives.GetPlayerAlliance(P(source), P(other), _natives.ConvertAllianceType(n)) ? 1 << n : 0);

    // facts: alliances go one way; IsPlayerAlly is true with ALLIANCE_PASSIVE alone and false with ALLIANCE_SHARED_VISION
    // alone, IsPlayerEnemy its opposite
    [Fact]
    public void Alliances_go_one_way_and_passive_alone_makes_an_ally()
    {
        _natives.SetPlayerAlliance(P(1), P(2), _natives.ConvertAllianceType(0), true);
        _natives.SetPlayerAlliance(P(3), P(4), _natives.ConvertAllianceType(5), true);

        Assert.True(_natives.GetPlayerAlliance(P(1), P(2), _natives.ConvertAllianceType(0)));
        Assert.False(_natives.GetPlayerAlliance(P(2), P(1), _natives.ConvertAllianceType(0)));
        Assert.True(_natives.IsPlayerAlly(P(1), P(2)));
        Assert.False(_natives.IsPlayerEnemy(P(1), P(2)));
        Assert.True(_natives.IsPlayerEnemy(P(2), P(1)));
        Assert.False(_natives.IsPlayerAlly(P(3), P(4)));
        Assert.True(_natives.IsPlayerEnemy(P(3), P(4)));

        _natives.SetPlayerAlliance(P(1), P(2), _natives.ConvertAllianceType(0), false);
        Assert.False(_natives.IsPlayerAlly(P(1), P(2)));
    }

    // the probe: a player gives itself alliance types 0 to 7 (255); neutral passive and every player are passive to
    // each other; other players give each other nothing
    [Fact]
    public void A_player_is_its_own_ally_and_neutral_passive_is_passive_both_ways()
    {
        Assert.Equal(255, Alliances(0, 0));
        Assert.Equal(255, Alliances(24, 24));
        Assert.True(_natives.IsPlayerAlly(P(0), P(0)));
        Assert.Equal(1, Alliances(0, 27));
        Assert.Equal(1, Alliances(27, 0));
        Assert.Equal(1, Alliances(24, 27));
        Assert.Equal(1, Alliances(27, 24));
        Assert.Equal(0, Alliances(0, 1));
        Assert.Equal(0, Alliances(0, 24));
        Assert.Equal(0, Alliances(24, 0));
    }

    // the probe: game result 3, food cap ceiling 100, PLACED 1 for the seated player, bounty from neutral hostile and
    // passive; gold below 0 reads 0, 2,000,000 and a food cap past the ceiling read as set, gathered gold doesn't move
    [Fact]
    public void Player_states_start_as_the_game_starts_them_and_read_back()
    {
        _natives.Seat([0], []);
        int State(int slot, int state) => _natives.GetPlayerState(P(slot), _natives.ConvertPlayerState(state));
        int[] states = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 25];

        Assert.Equal([3, 0, 0, 0, 0, 0, 100, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0], states.Select(s => State(0, s)));
        Assert.Equal([3, 0, 0, 0, 0, 0, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], states.Select(s => State(1, s)));
        Assert.Equal([3, 0, 0, 0, 0, 0, 100, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], states.Select(s => State(24, s)));
        Assert.Equal([3, 0, 0, 0, 0, 0, 100, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], states.Select(s => State(27, s)));

        var gold = _natives.ConvertPlayerState(1);
        _natives.SetPlayerState(P(0), gold, -5);
        Assert.Equal(0, _natives.GetPlayerState(P(0), gold));
        _natives.SetPlayerState(P(0), gold, 2000000);
        Assert.Equal(2000000, _natives.GetPlayerState(P(0), gold));
        _natives.SetPlayerState(P(0), _natives.ConvertPlayerState(4), 150);
        Assert.Equal(150, State(0, 4));
        Assert.Equal(0, State(0, 15));
    }

    [Fact]
    public void Lumber_below_0_reads_0_and_an_unknown_state_reads_0()
    {
        _natives.SetPlayerState(P(0), _natives.ConvertPlayerState(2), -1);
        _natives.SetPlayerState(P(0), _natives.ConvertPlayerState(99), 7);

        Assert.Equal(0, _natives.GetPlayerState(P(0), _natives.ConvertPlayerState(2)));
        Assert.Equal(0, _natives.GetPlayerState(P(0), _natives.ConvertPlayerState(99)));
    }

    // the probe: every handicap is 1
    [Fact]
    public void Handicaps_start_at_1_and_read_back()
    {
        Assert.Equal([1f, 1f, 1f, 1f], [_natives.GetPlayerHandicap(P(0)), _natives.GetPlayerHandicapXP(P(0)), _natives.GetPlayerHandicapDamage(P(0)), _natives.GetPlayerHandicapReviveTime(P(0))]);

        _natives.SetPlayerHandicap(P(0), 0.5f);
        _natives.SetPlayerHandicapXP(P(0), 2);
        _natives.SetPlayerHandicapDamage(P(0), 0.75f);
        _natives.SetPlayerHandicapReviveTime(P(0), 3);

        Assert.Equal([0.5f, 2f, 0.75f, 3f], [_natives.GetPlayerHandicap(P(0)), _natives.GetPlayerHandicapXP(P(0)), _natives.GetPlayerHandicapDamage(P(0)), _natives.GetPlayerHandicapReviveTime(P(0))]);
    }

    const int Footman = 0x68666F6F, IronSwords = 0x52686D65; // hfoo, Rhme

    // facts: GetPlayerTechMaxAllowed is 2147483647 for a unit type without a limit, 0 and 2147483647 set read back;
    // SetPlayerTechResearched sets the level GetPlayerTechCount reads. The probe: setting lowers it too, Add adds, Dec
    // takes away, and a negative maximum reads 2147483647.
    [Fact]
    public void Tech_limits_and_levels_are_kept_per_player()
    {
        Assert.Equal(int.MaxValue, _natives.GetPlayerTechMaxAllowed(P(0), Footman));
        _natives.SetPlayerTechMaxAllowed(P(0), Footman, 0);
        Assert.Equal(0, _natives.GetPlayerTechMaxAllowed(P(0), Footman));
        Assert.Equal(int.MaxValue, _natives.GetPlayerTechMaxAllowed(P(1), Footman));
        _natives.SetPlayerTechMaxAllowed(P(0), Footman, -5);
        Assert.Equal(int.MaxValue, _natives.GetPlayerTechMaxAllowed(P(0), Footman));

        Assert.Equal(0, _natives.GetPlayerTechCount(P(0), IronSwords, true));
        Assert.False(_natives.GetPlayerTechResearched(P(0), IronSwords, true));
        var levels = new List<int>();
        _natives.SetPlayerTechResearched(P(0), IronSwords, 2);
        levels.Add(_natives.GetPlayerTechCount(P(0), IronSwords, true));
        _natives.SetPlayerTechResearched(P(0), IronSwords, 1);
        levels.Add(_natives.GetPlayerTechCount(P(0), IronSwords, true));
        _natives.AddPlayerTechResearched(P(0), IronSwords, 2);
        levels.Add(_natives.GetPlayerTechCount(P(0), IronSwords, false));
        _natives.BlzDecPlayerTechResearched(P(0), IronSwords, 1);
        levels.Add(_natives.GetPlayerTechCount(P(0), IronSwords, true));

        Assert.Equal([2, 1, 3, 2], levels);
        Assert.True(_natives.GetPlayerTechResearched(P(0), IronSwords, true));
        Assert.Equal(0, _natives.GetPlayerTechCount(P(1), IronSwords, true));
    }

    // facts: RemovePlayer with defeat leaves the slot Left, its alliances as they were; with victory it stays Playing
    [Fact]
    public void A_defeated_player_leaves_and_a_victorious_one_stays()
    {
        _natives.Seat([0], [1, 2]);
        _natives.SetPlayerAlliance(P(1), P(0), _natives.ConvertAllianceType(0), true);

        _natives.RemovePlayer(P(1), _natives.ConvertPlayerGameResult(1));
        _natives.RemovePlayer(P(2), _natives.ConvertPlayerGameResult(0));

        Assert.Same(_natives.ConvertPlayerSlotState(2), _natives.GetPlayerSlotState(P(1)));
        Assert.True(_natives.IsPlayerAlly(P(1), P(0)));
        Assert.Same(_natives.ConvertPlayerSlotState(1), _natives.GetPlayerSlotState(P(2)));
    }

    [Fact]
    public void Names_controllers_and_start_locations_read_back()
    {
        _natives.SetPlayerName(P(2), "Arthas");
        _natives.SetPlayerController(P(2), _natives.ConvertMapControl(1));
        _natives.SetPlayerStartLocation(P(2), 4);
        _natives.ForcePlayerStartLocation(P(3), 5);

        Assert.Equal("Arthas", _natives.GetPlayerName(P(2)));
        Assert.Same(_natives.ConvertMapControl(1), _natives.GetPlayerController(P(2)));
        Assert.Equal(4, _natives.GetPlayerStartLocation(P(2)));
        Assert.Equal(5, _natives.GetPlayerStartLocation(P(3)));
    }

    [Fact]
    public void A_null_player_reads_as_nothing_and_takes_nothing()
    {
        _natives.SetPlayerName(null, "x");
        _natives.SetPlayerTeam(null, 3);
        _natives.SetPlayerAlliance(null, P(0), _natives.ConvertAllianceType(0), true);
        _natives.SetPlayerState(null, _natives.ConvertPlayerState(1), 5);

        Assert.Null(_natives.GetPlayerName(null));
        Assert.Null(_natives.GetPlayerColor(null));
        Assert.Equal(0, _natives.GetPlayerTeam(null));
        Assert.False(_natives.GetPlayerAlliance(null, P(0), _natives.ConvertAllianceType(0)));
        Assert.False(_natives.IsPlayerAlly(P(0), null));
        Assert.Equal(0, _natives.GetPlayerState(null, _natives.ConvertPlayerState(1)));
    }
}
