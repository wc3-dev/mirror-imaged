using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>Who plays a run, from the map's <c>war3map.w3i</c> or the test's options, and the player natives from Lua.</summary>
public sealed class PlayerSimulationTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // Players 1 and 3 are people and 2 a computer, as World Editor writes them in war3map.w3i and config.
    string Map(string main, MapFlags flags = 0) => _maps.Build($$"""
        function config()
          SetPlayers(3)
          SetPlayerController(Player(0), MAP_CONTROL_USER)
          SetPlayerController(Player(1), MAP_CONTROL_COMPUTER)
          SetPlayerController(Player(2), MAP_CONTROL_USER)
          SetPlayerRacePreference(Player(1), RACE_PREF_ORC)
          SetPlayerRaceSelectable(Player(1), false)
          SetPlayerTeam(Player(0), 0)
          SetPlayerTeam(Player(1), 1)
          SetPlayerTeam(Player(2), 0)
        end
        function main()
        {{main}}
        end
        """, change: info =>
    {
        info.MapFlags |= flags;
        info.Players.Clear();
        info.Players.AddRange([Slot(0, PlayerController.User), Slot(1, PlayerController.Computer), Slot(2, PlayerController.User)]);
    });

    static PlayerData Slot(int id, PlayerController controller) => new() { Id = id, Controller = controller, Race = PlayerRace.Human, Name = $"Player {id + 1}" };

    const string Lineup = """
        local states = {[PLAYER_SLOT_STATE_EMPTY] = "empty", [PLAYER_SLOT_STATE_PLAYING] = "playing", [PLAYER_SLOT_STATE_LEFT] = "left"}
        local controls = {[MAP_CONTROL_USER] = "user", [MAP_CONTROL_COMPUTER] = "computer", [MAP_CONTROL_NONE] = "none"}
        for i = 0, 3 do
          print(i, GetPlayerName(Player(i)), states[GetPlayerSlotState(Player(i))], controls[GetPlayerController(Player(i))], GetPlayerTeam(Player(i)))
        end
        print("local", GetPlayerId(GetLocalPlayer()))
        """;

    [Fact]
    public void By_default_the_maps_first_user_plays_with_its_computers_and_the_other_users_slot_is_empty()
    {
        using var simulation = Simulation.Load(Map(Lineup), "");

        Assert.Equal([
            "0.000 print: 0\tLocal Player\tplaying\tuser\t0",
            "0.000 print: 1\tPlayer 2\tplaying\tcomputer\t1",
            "0.000 print: 2\tPlayer 3\tempty\tuser\t0",
            "0.000 print: 3\tPlayer 4\tempty\tnone\t-1",
            "0.000 print: local\t0",
        ], simulation.Log);
    }

    [Fact]
    public void The_options_seat_the_users_and_computers_the_first_user_local()
    {
        using var simulation = Simulation.Load(Map(Lineup), "", new SimulationOptions { Users = [2, 0], Computers = [3] });

        Assert.Equal([
            "0.000 print: 0\tPlayer 1\tplaying\tuser\t0",
            "0.000 print: 1\tPlayer 2\tempty\tcomputer\t1",
            "0.000 print: 2\tPlayer 3\tplaying\tuser\t0",
            "0.000 print: 3\tPlayer 4\tplaying\tcomputer\t-1",
            "0.000 print: local\t2",
        ], simulation.Log);
    }

    [Fact]
    public void Users_chosen_without_computers_play_with_the_maps_computers_in_other_slots()
    {
        using var simulation = Simulation.Load(Map("print(GetPlayerSlotState(Player(1)) == PLAYER_SLOT_STATE_PLAYING, GetPlayerId(GetLocalPlayer()))"), "",
            new SimulationOptions { Users = [2] });

        Assert.Equal(["0.000 print: true\t2"], simulation.Log);
    }

    [Fact]
    public void Options_that_seat_no_user_or_a_slot_twice_are_refused()
    {
        var map = Map("");

        Assert.Throws<ArgumentException>(() => Simulation.Load(map, "", new SimulationOptions { Users = [] }));
        Assert.Throws<ArgumentException>(() => Simulation.Load(map, "", new SimulationOptions { Users = [0], Computers = [0] }));
        Assert.Throws<ArgumentException>(() => Simulation.Load(map, "", new SimulationOptions { Users = [24] }));
    }

    // facts: GetPlayerColor compares equal to ConvertPlayerColor(n), and GetPlayerRace to RACE_*, with Lua's ==
    [Fact]
    public void A_players_colour_and_race_compare_equal_to_the_constants_with_lua_s_equals()
    {
        using var simulation = Simulation.Load(Map("""
            print(GetPlayerColor(Player(1)) == PLAYER_COLOR_BLUE, GetPlayerColor(Player(1)) == ConvertPlayerColor(1), GetPlayerColor(Player(1)) == PLAYER_COLOR_RED)
            SetPlayerColor(Player(1), PLAYER_COLOR_MAROON)
            print(GetPlayerColor(Player(1)) == ConvertPlayerColor(12), GetPlayerRace(Player(1)) == RACE_ORC, GetPlayerRace(Player(1)) == ConvertRace(2))
            """), "");

        Assert.Equal(["0.000 print: true\ttrue\tfalse", "0.000 print: true\ttrue\ttrue"], simulation.Log);
    }

    // facts: a map with custom forces and fixed player settings starts with MAP_LOCK_ALLIANCE_CHANGES set
    [Fact]
    public void Custom_forces_with_fixed_settings_lock_alliance_changes()
    {
        const string read = "print(IsMapFlagSet(MAP_LOCK_ALLIANCE_CHANGES), GetGameTypeSelected() == GAME_TYPE_USE_MAP_SETTINGS)";
        using var locked = Simulation.Load(Map(read, MapFlags.UseCustomForces | MapFlags.FixedPlayerSettingsForCustomForces), "");
        using var custom = Simulation.Load(Map(read, MapFlags.UseCustomForces), "");
        using var melee = Simulation.Load(Map(read), "");

        Assert.Equal(["0.000 print: true\ttrue"], locked.Log);
        Assert.Equal(["0.000 print: false\ttrue"], custom.Log);
        Assert.Equal(["0.000 print: false\tfalse"], melee.Log);
    }

    [Fact]
    public void ForForce_runs_a_lua_function_for_each_player_and_an_error_in_it_is_logged()
    {
        using var simulation = Simulation.Load(Map("""
            local f = CreateForce()
            ForceAddPlayer(f, Player(2))
            ForceAddPlayer(f, Player(1))
            ForForce(f, function()
              print(GetPlayerId(GetEnumPlayer()))
              if GetEnumPlayer() == Player(1) then error("one") end
            end)
            print("after", GetEnumPlayer())
            """), "");

        Assert.Equal(4, simulation.Log.Count);
        Assert.Equal("0.000 print: 1", simulation.Log[0]);
        Assert.StartsWith("0.000 error: war3map.lua:", simulation.Log[1]);
        Assert.EndsWith("one", simulation.Log[1]);
        Assert.Equal(["0.000 print: 2", "0.000 print: after\tnil"], simulation.Log.Skip(2));
    }

    [Fact]
    public void A_paused_game_moves_no_time_and_goes_on_from_where_it_stopped()
    {
        using var simulation = Simulation.Load(Map("""
            TimerStart(CreateTimer(), 1, false, function() print("pause") PauseGame(true) end)
            TimerStart(CreateTimer(), 2, false, function() print("two") end)
            """), "");

        simulation.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(["1.000 print: pause"], simulation.Log);

        simulation.Natives.PauseGame(false);
        simulation.Advance(TimeSpan.FromSeconds(0.5));
        Assert.Equal(["1.000 print: pause"], simulation.Log);
        simulation.Advance(TimeSpan.FromSeconds(0.5));

        Assert.Equal(["1.000 print: pause", "2.000 print: two"], simulation.Log);
    }
}
