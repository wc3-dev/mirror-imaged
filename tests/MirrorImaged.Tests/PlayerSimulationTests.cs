using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>Who plays a run, from the map's <c>war3map.w3i</c> or the test's options, and the player natives from Lua.</summary>
public sealed class PlayerSimulationTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // Players 1 and 3 are people, 2 a computer and 4 rescuable, as World Editor writes them in war3map.w3i and config.
    string Map(string main, MapFlags flags = 0, string top = "") => _maps.Build($$"""
        {{top}}
        function config()
          SetPlayers(4)
          SetPlayerController(Player(0), MAP_CONTROL_USER)
          SetPlayerController(Player(1), MAP_CONTROL_COMPUTER)
          SetPlayerController(Player(2), MAP_CONTROL_USER)
          SetPlayerController(Player(3), MAP_CONTROL_RESCUABLE)
          SetPlayerRacePreference(Player(1), RACE_PREF_ORC)
          SetPlayerRaceSelectable(Player(1), false)
          SetPlayerTeam(Player(0), 0)
          SetPlayerTeam(Player(1), 1)
          SetPlayerTeam(Player(2), 0)
          SetPlayerTeam(Player(3), 2)
        end
        function main()
        {{main}}
        end
        """, change: info =>
    {
        info.MapFlags |= flags;
        info.Players.Clear();
        info.Players.AddRange([Slot(0, PlayerController.User), Slot(1, PlayerController.Computer), Slot(2, PlayerController.User), Slot(3, PlayerController.Rescuable)]);
    });

    static PlayerData Slot(int id, PlayerController controller) => new() { Id = id, Controller = controller, Race = PlayerRace.Human, Name = $"Player {id + 1}" };

    const string Lineup = """
        local states = {[PLAYER_SLOT_STATE_EMPTY] = "empty", [PLAYER_SLOT_STATE_PLAYING] = "playing", [PLAYER_SLOT_STATE_LEFT] = "left"}
        local controls = {[MAP_CONTROL_USER] = "user", [MAP_CONTROL_COMPUTER] = "computer", [MAP_CONTROL_RESCUABLE] = "rescuable", [MAP_CONTROL_NONE] = "none"}
        for i = 0, 4 do
          print(i, GetPlayerName(Player(i)), states[GetPlayerSlotState(Player(i))], controls[GetPlayerController(Player(i))], GetPlayerTeam(Player(i)))
        end
        print("local", GetPlayerId(GetLocalPlayer()))
        """;

    // A rescuable slot plays as the game likely seats it (decisions D91, inferred); a user slot no one plays keeps its
    // controller and is on team -1 (the lobby probe)
    [Fact]
    public void By_default_the_maps_first_user_plays_with_its_computers_and_rescuable_slots_and_the_other_users_slot_is_empty()
    {
        using var simulation = Simulation.Load(Map(Lineup), "");

        Assert.Equal([
            "0.000 print: 0\tLocal Player\tplaying\tuser\t0",
            "0.000 print: 1\tComputer (Normal)\tplaying\tcomputer\t1",
            "0.000 print: 2\tPlayer 3\tempty\tuser\t-1",
            "0.000 print: 3\tPlayer 4\tplaying\trescuable\t2",
            "0.000 print: 4\tPlayer 5\tempty\tnone\t-1",
            "0.000 print: local\t0",
        ], simulation.Log);
    }

    [Fact]
    public void The_options_seat_the_users_the_first_local_and_leave_a_computer_not_chosen_empty()
    {
        using var simulation = Simulation.Load(Map(Lineup), "", new SimulationOptions { Users = [2, 0], Computers = [] });

        Assert.Equal([
            "0.000 print: 0\tPlayer 1\tplaying\tuser\t0",
            "0.000 print: 1\tPlayer 2\tempty\tcomputer\t-1",
            "0.000 print: 2\tPlayer 3\tplaying\tuser\t0",
            "0.000 print: 3\tPlayer 4\tplaying\trescuable\t2",
            "0.000 print: 4\tPlayer 5\tempty\tnone\t-1",
            "0.000 print: local\t2",
        ], simulation.Log);
    }

    [Fact]
    public void A_computer_can_take_a_users_slot()
    {
        using var simulation = Simulation.Load(Map(Lineup), "", new SimulationOptions { Users = [2], Computers = [0, 1] });

        Assert.Equal("0.000 print: 0\tComputer (Normal)\tplaying\tcomputer\t0", simulation.Log[0]);
        Assert.Equal("0.000 print: 2\tLocal Player\tplaying\tuser\t0", simulation.Log[2]);
    }

    [Fact]
    public void Users_chosen_without_computers_play_with_the_maps_computers()
    {
        using var simulation = Simulation.Load(Map("print(GetPlayerSlotState(Player(1)) == PLAYER_SLOT_STATE_PLAYING, GetPlayerId(GetLocalPlayer()))"), "",
            new SimulationOptions { Users = [2] });

        Assert.Equal(["0.000 print: true\t2"], simulation.Log);
    }

    [Theory]
    [InlineData(new int[0], null, "A run needs a person playing, and no user slot is given")]
    [InlineData(new[] { 0 }, new[] { 0 }, "Slot 0 is given twice")]
    [InlineData(new[] { 24 }, null, "Slot 24 can't be played: people and computers play slots 0 to 23")]
    [InlineData(new[] { 4 }, null, "Slot 4 isn't one of the map's players in war3map.w3i (0, 1, 2, 3)")]
    [InlineData(new[] { 0 }, new[] { 5 }, "Slot 5 isn't one of the map's players in war3map.w3i (0, 1, 2, 3)")]
    [InlineData(new[] { 1 }, null, "A person can't play slot 1: the map gives it to a computer player")]
    [InlineData(new[] { 0 }, new[] { 3 }, "A computer can't play slot 3: the map gives it to a rescuable player")]
    public void Options_are_checked_against_the_maps_slots_before_its_lua_runs(int[] users, int[]? computers, string message)
    {
        // The map's top level would stop the run, so a run that got that far would come back stopped instead.
        var map = Map("", top: "StartMeleeAI(Player(0), 'human.ai')");

        var error = Assert.Throws<ArgumentException>(() => Simulation.Load(map, "", new SimulationOptions { Users = users, Computers = computers }));

        Assert.Equal(message, error.Message);
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

    // facts: a map with custom forces and fixed player settings starts with MAP_LOCK_ALLIANCE_CHANGES set; the lobby
    // probe: its game type is melee, as without custom forces
    [Fact]
    public void Custom_forces_with_fixed_settings_lock_alliance_changes()
    {
        const string read = "print(IsMapFlagSet(MAP_LOCK_ALLIANCE_CHANGES), GetGameTypeSelected() == GAME_TYPE_MELEE)";
        using var locked = Simulation.Load(Map(read, MapFlags.UseCustomForces | MapFlags.FixedPlayerSettingsForCustomForces), "");
        using var custom = Simulation.Load(Map(read, MapFlags.UseCustomForces), "");
        using var melee = Simulation.Load(Map(read), "");

        Assert.Equal(["0.000 print: true\ttrue"], locked.Log);
        Assert.Equal(["0.000 print: false\ttrue"], custom.Log);
        Assert.Equal(["0.000 print: false\ttrue"], melee.Log);
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
    public void A_pause_lets_its_step_finish_and_the_paused_game_moves_no_time_until_the_map_goes_on()
    {
        using var simulation = Simulation.Load(Map("""
            TimerStart(CreateTimer(), 1, false, function() print("pause") PauseGame(true) end)
            TimerStart(CreateTimer(), 1, false, function() print("same step") end)
            TimerStart(CreateTimer(), 2, false, function() print("two") end)
            """), "");

        simulation.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(["1.000 print: pause", "1.000 print: same step"], simulation.Log);
        Assert.True(simulation.Paused);

        simulation.Natives.PauseGame(false); // as the map would, from a dialog's button
        Assert.False(simulation.Paused);
        simulation.Advance(TimeSpan.FromSeconds(0.5));
        Assert.Equal(2, simulation.Log.Count);
        simulation.Advance(TimeSpan.FromSeconds(0.5));

        Assert.Equal("2.000 print: two", simulation.Log[^1]);
    }

    [Fact]
    public void A_pause_ended_in_the_same_step_moves_on()
    {
        using var simulation = Simulation.Load(Map("""
            TimerStart(CreateTimer(), 1, false, function() PauseGame(true) end)
            TimerStart(CreateTimer(), 1, false, function() PauseGame(false) end)
            TimerStart(CreateTimer(), 2, false, function() print("two") end)
            """), "");

        simulation.Advance(TimeSpan.FromSeconds(3));

        Assert.False(simulation.Paused);
        Assert.Equal(["2.000 print: two"], simulation.Log);
    }

    // decisions D92: random races come from a generator of their own, so the map's numbers don't depend on the seating
    [Fact]
    public void The_maps_random_numbers_dont_depend_on_who_plays()
    {
        string Draws(SimulationOptions options)
        {
            using var simulation = Simulation.Load(Map("print(GetRandomInt(1, 1000000), math.random(1, 1000000))"), "", options);
            return simulation.Log.Single();
        }

        Assert.Equal(Draws(new SimulationOptions { Seed = 9 }), Draws(new SimulationOptions { Seed = 9, Users = [0, 2], Computers = [] }));
    }
}
