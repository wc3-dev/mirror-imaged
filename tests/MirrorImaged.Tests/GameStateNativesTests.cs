using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>
/// The game's settings, pause and the time of day, against the facts' "Version and launching" and "Time of day" and
/// the players probe (docs/facts.md, "Players and the game").
/// </summary>
public sealed class GameStateNativesTests
{
    readonly Game _game = new(0);
    readonly GameNatives _natives;

    public GameStateNativesTests() => _natives = new GameNatives(_game);

    fgamestate TimeOfDay => _natives.ConvertFGameState(2)!;

    float Time => _natives.GetFloatGameState(TimeOfDay);

    void Advance(double seconds) => _game.Advance(Game.Steps(seconds));

    // facts: the time starts at 0 and moves an hour per 20 s at scale 1, which reads 1 (the game's own steps lose up
    // to 2.5 millionths in 2 s, which isn't modelled)
    [Fact]
    public void The_time_of_day_starts_at_0_and_moves_an_hour_every_20_seconds()
    {
        Assert.Equal(0f, Time);
        Assert.Equal(1f, _natives.GetTimeOfDayScale());

        Advance(2);
        Assert.Equal(0.1, Time, 1e-6);
        Advance(18);
        Assert.Equal(1f, Time);
    }

    // facts and the probe: a time set reads back at once as set; 24 and more, below 0, and from 23.995 read 0
    [Theory]
    [InlineData(12f, 12f)]
    [InlineData(23.9f, 23.9f)]
    [InlineData(23.99f, 23.99f)]
    [InlineData(7f, 7f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(23.995f, 0f)]
    [InlineData(23.999f, 0f)]
    [InlineData(24f, 0f)]
    [InlineData(25f, 0f)]
    [InlineData(30.5f, 0f)]
    [InlineData(48f, 0f)]
    [InlineData(-1f, 0f)]
    public void A_time_set_reads_back_at_once(float set, float read)
    {
        Advance(3);
        _natives.SetFloatGameState(TimeOfDay, set);

        Assert.Equal(read, Time);
    }

    // facts: from 23.9, 2 s later it reads just under 24 (23.99999), not 0
    [Fact]
    public void The_time_runs_up_to_24_and_round_to_0()
    {
        _natives.SetFloatGameState(TimeOfDay, 23.9f);
        Advance(2);
        Assert.InRange(Time, 23.99999f, 23.999999f);

        Advance(20);
        Assert.Equal(1, Time, 1e-5);
    }

    // facts: scale 2 runs twice as fast, 0 stops, -1 runs backward; the scale reads what was set
    [Fact]
    public void The_scale_sets_the_times_speed_and_direction()
    {
        _natives.SetFloatGameState(TimeOfDay, 12);
        _natives.SetTimeOfDayScale(2);
        Advance(10);
        Assert.Equal(13f, Time);
        Assert.Equal(2f, _natives.GetTimeOfDayScale());

        _natives.SetTimeOfDayScale(0);
        Advance(10);
        Assert.Equal(13f, Time);

        _natives.SetTimeOfDayScale(-1);
        Advance(20);
        Assert.Equal(12f, Time);
        Assert.Equal(-1f, _natives.GetTimeOfDayScale());
    }

    [Fact]
    public void Running_backward_past_0_comes_round_to_24()
    {
        _natives.SetFloatGameState(TimeOfDay, 0.5f);
        _natives.SetTimeOfDayScale(-1);

        Advance(20);

        Assert.Equal(23.5f, Time);
    }

    // facts: suspending stops the time and keeps the scale; a time or a scale set while suspended holds; one resume
    // after two suspends runs it again
    [Fact]
    public void Suspending_stops_the_time_and_one_resume_runs_it_at_the_scale()
    {
        _natives.SetFloatGameState(TimeOfDay, 6);
        _natives.SuspendTimeOfDay(true);
        _natives.SuspendTimeOfDay(true);
        Advance(20);
        Assert.Equal(6f, Time);

        _natives.SetFloatGameState(TimeOfDay, 8);
        _natives.SetTimeOfDayScale(2);
        Advance(20);
        Assert.Equal(8f, Time);
        Assert.Equal(2f, _natives.GetTimeOfDayScale());

        _natives.SuspendTimeOfDay(false);
        Advance(10);
        Assert.Equal(9f, Time);
    }

    [Fact]
    public void Another_float_game_state_reads_0()
    {
        _natives.SetFloatGameState(_natives.ConvertFGameState(7), 3);

        Assert.Equal(0f, _natives.GetFloatGameState(_natives.ConvertFGameState(7)));
        Assert.Equal(0f, _natives.GetFloatGameState(null));
    }

    [Fact]
    public void Integer_game_states_start_at_0_and_read_back()
    {
        var disconnected = _natives.ConvertIGameState(1);
        Assert.Equal(0, _natives.GetIntegerGameState(_natives.ConvertIGameState(0)));
        Assert.Equal(0, _natives.GetIntegerGameState(disconnected));

        _natives.SetIntegerGameState(disconnected, 2);

        Assert.Equal(2, _natives.GetIntegerGameState(disconnected));
    }

    // the probe: the speed starts at normal; slowest is taken, fastest isn't
    [Fact]
    public void The_speed_starts_normal_and_fastest_is_refused()
    {
        Assert.Same(_natives.ConvertGameSpeed(2), _natives.GetGameSpeed());

        _natives.SetGameSpeed(_natives.ConvertGameSpeed(4));
        Assert.Same(_natives.ConvertGameSpeed(2), _natives.GetGameSpeed());
        _natives.SetGameSpeed(_natives.ConvertGameSpeed(0));
        Assert.Same(_natives.ConvertGameSpeed(0), _natives.GetGameSpeed());
    }

    [Fact]
    public void A_paused_game_runs_no_time_until_it_goes_on()
    {
        var ran = new List<long>();
        _natives.TimerStart(_natives.CreateTimer(), 1, false, () => _natives.PauseGame(true));
        _natives.TimerStart(_natives.CreateTimer(), 2, false, () => ran.Add(_game.Step));

        Advance(10);
        Assert.Empty(ran);
        Assert.Equal(Game.Steps(1), _game.Step);
        Assert.Equal(0.05, Time, 1e-6);

        _natives.PauseGame(false);
        _game.Advance(Game.Steps(1));
        Assert.Equal([Game.Steps(2)], ran);
    }

    // the probe: easy, version Frozen Throne, no cheats, melee, use map settings placement as config set it
    [Fact]
    public void The_games_settings_are_a_single_player_games()
    {
        Assert.Same(_natives.ConvertGameDifficulty(0), _natives.GetGameDifficulty());
        Assert.Same(_natives.ConvertVersion(1), _natives.VersionGet());
        Assert.False(_natives.IsNoVictoryCheat());
        Assert.False(_natives.IsNoDefeatCheat());
        Assert.Same(_natives.ConvertGameType(1), _natives.GetGameTypeSelected());

        _natives.SetGameDifficulty(_natives.ConvertGameDifficulty(2));
        _natives.SetGamePlacement(_natives.ConvertPlacement(2));

        Assert.Same(_natives.ConvertGameDifficulty(2), _natives.GetGameDifficulty());
        Assert.Same(_natives.ConvertPlacement(2), _natives.GetGamePlacement());
    }

    // facts: a map with custom forces and fixed player settings starts with MAP_LOCK_ALLIANCE_CHANGES set; set false, it
    // reads false. The probe: a map without custom forces has no flag set and plays melee.
    [Fact]
    public void Custom_forces_with_fixed_settings_lock_alliances_and_use_the_maps_settings()
    {
        var locked = _natives.ConvertMapFlag(1024);
        _natives.Seat([0], [], customForces: true, fixedSettings: true);

        Assert.True(_natives.IsMapFlagSet(locked));
        Assert.Same(_natives.ConvertGameType(4), _natives.GetGameTypeSelected());
        _natives.SetMapFlag(locked, false);
        Assert.False(_natives.IsMapFlagSet(locked));
    }

    [Fact]
    public void Without_custom_forces_no_map_flag_is_set()
    {
        _natives.Seat([0], []);

        Assert.All(Enumerable.Range(0, 21), n => Assert.False(_natives.IsMapFlagSet(_natives.ConvertMapFlag(1 << n))));
    }

    // the probe: players and teams as config set them, an undefined start location at 0, 0
    [Fact]
    public void The_maps_setup_reads_back()
    {
        _natives.SetPlayers(3);
        _natives.SetTeams(2);
        _natives.DefineStartLocation(1, -7744, 960.5f);
        _natives.SetStartLocPrioCount(1, 2);
        _natives.SetStartLocPrio(1, 1, 3, _natives.ConvertStartLocPrio(1));

        Assert.Equal(3, _natives.GetPlayers());
        Assert.Equal(2, _natives.GetTeams());
        Assert.Equal(-7744f, _natives.GetStartLocationX(1));
        Assert.Equal(960.5f, _natives.GetStartLocationY(1));
        Assert.Equal(0f, _natives.GetStartLocationX(5));
        Assert.Equal(3, _natives.GetStartLocPrioSlot(1, 1));
        Assert.Same(_natives.ConvertStartLocPrio(1), _natives.GetStartLocPrio(1, 1));
    }
}
