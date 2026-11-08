using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>
/// Player and game events, filters, the lobby and the game's end, against the probes lobby, events and forforce-wait
/// (docs/facts.md, "Players and the game" and "Player and game events") and the facts' "Version and launching" and
/// "Orders and events". Each map is laid out as the probe map with a computer: a user in slot 0, a computer in 1 and a
/// user slot 2 no one plays, all named in war3map.w3i, in custom forces with fixed player settings; no blizzard.j.
/// </summary>
public sealed class PlayerEventTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string main, string config = "", SimulationOptions? options = null) => Simulation.Load(_maps.Build($$"""
        local log = {}
        local function flush() local s = table.concat(log, ",") log = {} return s end
        local names = {"LT", "LE", "EQ", "GE", "GT", "NE"}
        local ops = {LT = LESS_THAN, LE = LESS_THAN_OR_EQUAL, EQ = EQUAL, GE = GREATER_THAN_OR_EQUAL, GT = GREATER_THAN, NE = NOT_EQUAL}
        local slots = {[PLAYER_SLOT_STATE_EMPTY] = "empty", [PLAYER_SLOT_STATE_PLAYING] = "playing", [PLAYER_SLOT_STATE_LEFT] = "left"}
        local function slot(p) return slots[GetPlayerSlotState(p)] end
        local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
        function config()
        {{config}}
          for i = 0, 2 do
            SetPlayerRacePreference(Player(i), RACE_PREF_HUMAN)
            SetPlayerRaceSelectable(Player(i), true)
            SetPlayerController(Player(i), i == 1 and MAP_CONTROL_COMPUTER or MAP_CONTROL_USER)
            SetPlayerTeam(Player(i), i == 1 and 1 or 0)
          end
        end
        function main()
        {{main}}
        end
        """, change: info =>
    {
        info.MapFlags |= MapFlags.UseCustomForces | MapFlags.FixedPlayerSettingsForCustomForces;
        info.Players.Clear();
        info.Players.AddRange([Slot(0, PlayerController.User, "Probe User"), Slot(1, PlayerController.Computer, "Probe Computer"), Slot(2, PlayerController.User, "Probe Empty")]);
    }), "", options);

    static PlayerData Slot(int id, PlayerController controller, string name) =>
        new() { Id = id, Controller = controller, Race = PlayerRace.Human, Flags = PlayerFlags.RaceSelectable, Name = name };

    static IEnumerable<string> Printed(Simulation simulation) => simulation.Log.Select(line => line.Replace(" print: ", " "));

    // the lobby probe: config runs before anything is seated; then the computer has its war3map.w3i name, the slot no
    // one plays keeps its controller and fixed race but not its team, fixed player settings keep every race preference
    // though selectable, the game type is melee and the lobby sets MAP_LOCK_ALLIANCE_CHANGES and 2^20
    [Fact]
    public void The_lobby_seats_after_config_as_the_lobby_probe_saw()
    {
        using var simulation = Load(config: """
              local names = {}
              for i = 0, 2 do names[#names + 1] = "'" .. GetPlayerName(Player(i)) .. "'" end
              print(table.concat(names, " "), slot(Player(1)), GetPlayerController(Player(1)) == MAP_CONTROL_NONE, GetPlayerRace(Player(1)) == ConvertRace(0),
                IsPlayerRacePrefSet(Player(1), RACE_PREF_RANDOM), GetGameTypeSelected() == GAME_TYPE_MELEE, IsMapFlagSet(MAP_LOCK_ALLIANCE_CHANGES),
                pid(GetLocalPlayer()), GetPlayers(), GetTeams())
            """, main: """
              local controls = {[MAP_CONTROL_USER] = "user", [MAP_CONTROL_COMPUTER] = "computer", [MAP_CONTROL_NONE] = "none"}
              for i = 0, 3 do
                local p = Player(i)
                print(i, GetPlayerName(p), slot(p), controls[GetPlayerController(p)], i == 3 or GetPlayerRace(p) == RACE_HUMAN, GetPlayerTeam(p),
                  IsPlayerRacePrefSet(p, RACE_PREF_RANDOM), IsPlayerRacePrefSet(p, RACE_PREF_HUMAN), GetPlayerState(p, PLAYER_STATE_PLACED))
              end
              print(GetGameTypeSelected() == GAME_TYPE_MELEE, IsMapFlagSet(MAP_LOCK_ALLIANCE_CHANGES), IsMapFlagSet(ConvertMapFlag(1048576)), IsMapFlagSet(ConvertMapFlag(2048)))
            """);

        Assert.Equal([
            "0.000 '' '' ''\tempty\ttrue\ttrue\ttrue\ttrue\tfalse\t0\t0\t24",
            "0.000 0\tLocal Player\tplaying\tuser\ttrue\t0\tfalse\ttrue\t1",
            "0.000 1\tProbe Computer\tplaying\tcomputer\ttrue\t1\tfalse\ttrue\t1",
            "0.000 2\tPlayer 3\tempty\tuser\ttrue\t-1\tfalse\ttrue\t0",
            "0.000 3\tPlayer 4\tempty\tnone\ttrue\t-1\ttrue\tfalse\t0",
            "0.000 true\ttrue\ttrue\tfalse",
        ], Printed(simulation));
    }

    // decisions D141, not measured: a name config set is kept, and a computer seated in a user slot isn't given that
    // slot's war3map.w3i name
    [Fact]
    public void A_name_config_set_is_kept_and_a_computer_in_a_user_slot_is_Player_N()
    {
        using var simulation = Load(config: "SetPlayerName(Player(2), \"Arthas\")", main: "print(GetPlayerName(Player(0)), GetPlayerName(Player(1)), GetPlayerName(Player(2)))",
            options: new SimulationOptions { Users = [2], Computers = [0, 1] });

        Assert.Equal(["0.000 Player 1\tProbe Computer\tArthas"], Printed(simulation));
    }

    const string RemoveEvents = """
          for _, p in ipairs({0, 1}) do
            for _, e in ipairs({"VICTORY", "DEFEAT", "LEAVE", "END_CINEMATIC", "STATE_LIMIT"}) do
              local t = CreateTrigger()
              TriggerRegisterPlayerEvent(t, Player(p), _G["EVENT_PLAYER_" .. e])
              TriggerAddAction(t, function()
                log[#log + 1] = p .. e:sub(1, 3) .. ":" .. tostring(GetTriggerEventId() == _G["EVENT_PLAYER_" .. e]) .. ":" .. pid(GetTriggerPlayer()) .. ":"
                  .. slot(Player(p)) .. ":" .. GetPlayerState(Player(p), PLAYER_STATE_GAME_RESULT)
              end)
            end
          end
          local function remove(p, result, label)
            RemovePlayer(Player(p), result)
            return label .. " " .. flush() .. " then " .. slot(Player(p)) .. " " .. GetPlayerState(Player(p), PLAYER_STATE_GAME_RESULT)
          end

        """;

    // the events probe: a computer's victory fires its event inside the call, after the change, and it stays playing;
    // after that RemovePlayer does nothing to it
    [Fact]
    public void RemovePlayer_fires_a_computers_victory_once_and_then_does_nothing()
    {
        using var simulation = Load(RemoveEvents + """
              print(remove(1, PLAYER_GAME_RESULT_VICTORY, "victory") .. "; " .. remove(1, PLAYER_GAME_RESULT_VICTORY, "again"))
              print(remove(1, PLAYER_GAME_RESULT_TIE, "1 tie") .. "; " .. remove(1, PLAYER_GAME_RESULT_DEFEAT, "defeat") .. "; " .. remove(1, PLAYER_GAME_RESULT_NEUTRAL, "neutral"))
            """);

        Assert.Equal([
            "0.000 victory 1VIC:true:1:playing:0 then playing 0; again  then playing 0",
            "0.000 1 tie  then playing 0; defeat  then playing 0; neutral  then playing 0",
        ], Printed(simulation));
    }

    // the events probe's first run: a neutral result leaves a computer's slot Left and fires nothing; facts, "Version and
    // launching": defeat fires its event inside the call and leaves the slot Left, no leave event
    [Fact]
    public void RemovePlayer_leaves_a_neutral_or_defeated_computer_and_fires_its_defeat()
    {
        using var neutral = Load(RemoveEvents + """
              print(remove(1, PLAYER_GAME_RESULT_NEUTRAL, "neutral") .. "; " .. remove(1, PLAYER_GAME_RESULT_VICTORY, "victory"))
            """);
        using var defeat = Load(RemoveEvents + """print(remove(1, PLAYER_GAME_RESULT_DEFEAT, "defeat"))""");

        Assert.Equal(["0.000 neutral  then left 3; victory  then left 3"], Printed(neutral));
        Assert.Equal(["0.000 defeat 1DEF:true:1:left:1 then left 1"], Printed(defeat));
    }

    // the events probe: the user's victory fires nothing, inside the call or a second later; facts, CustomDefeatBJ: the
    // user's defeat leaves its slot Left with no event inside the call
    [Fact]
    public void RemovePlayer_fires_nothing_for_a_user()
    {
        using var simulation = Load(RemoveEvents + """
              print(remove(0, PLAYER_GAME_RESULT_VICTORY, "victory"))
              TimerStart(CreateTimer(), 1, false, function() print("later " .. flush()) end)
            """);
        using var defeat = Load(RemoveEvents + """print(remove(0, PLAYER_GAME_RESULT_DEFEAT, "defeat"))""");

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(["0.000 victory  then playing 0", "1.000 later "], Printed(simulation));
        Assert.Equal(["0.000 defeat  then left 1"], Printed(defeat));
    }

    // the events probe and the facts: an alliance change fires inside SetPlayerAlliance on the giving player's
    // registrations, EVENT_PLAYER_ALLIANCE_CHANGED first and for ALLIANCE_PASSIVE only, with no trigger player; setting
    // the same value fires nothing, and a player's alliance with itself doesn't change
    [Fact]
    public void An_alliance_change_fires_the_giving_players_alliance_events()
    {
        using var simulation = Load("""
              local function on(name, register)
                local t = CreateTrigger()
                register(t)
                TriggerAddAction(t, function()
                  log[#log + 1] = name .. ":" .. tostring(GetTriggerEventId() == EVENT_PLAYER_ALLIANCE_CHANGED) .. ":" .. pid(GetTriggerPlayer()) .. ":"
                    .. tostring(GetPlayerAlliance(Player(0), Player(1), ALLIANCE_PASSIVE))
                end)
              end
              on("p0", function(t) TriggerRegisterPlayerAllianceChange(t, Player(0), ALLIANCE_PASSIVE) end)
              on("p1", function(t) TriggerRegisterPlayerAllianceChange(t, Player(1), ALLIANCE_PASSIVE) end)
              on("v0", function(t) TriggerRegisterPlayerAllianceChange(t, Player(0), ALLIANCE_SHARED_VISION) end)
              on("c0", function(t) TriggerRegisterPlayerEvent(t, Player(0), EVENT_PLAYER_ALLIANCE_CHANGED) end)
              on("c1", function(t) TriggerRegisterPlayerEvent(t, Player(1), EVENT_PLAYER_ALLIANCE_CHANGED) end)
              local function set(label, a, b, type, value)
                SetPlayerAlliance(Player(a), Player(b), type, value)
                return label .. " " .. flush()
              end
              print(set("0>1 passive on", 0, 1, ALLIANCE_PASSIVE, true) .. "; " .. set("again", 0, 1, ALLIANCE_PASSIVE, true))
              print(set("0>1 passive off", 0, 1, ALLIANCE_PASSIVE, false) .. "; " .. set("again", 0, 1, ALLIANCE_PASSIVE, false))
              print(set("1>0 passive on", 1, 0, ALLIANCE_PASSIVE, true) .. "; " .. set("off", 1, 0, ALLIANCE_PASSIVE, false))
              print(set("0>1 vision on", 0, 1, ALLIANCE_SHARED_VISION, true) .. "; " .. set("off", 0, 1, ALLIANCE_SHARED_VISION, false))
              print(set("0>0 passive off", 0, 0, ALLIANCE_PASSIVE, false) .. " reads " .. tostring(GetPlayerAlliance(Player(0), Player(0), ALLIANCE_PASSIVE)))
              local types = {}
              for n = 1, 9 do
                SetPlayerAlliance(Player(0), Player(1), ConvertAllianceType(n), true)
                SetPlayerAlliance(Player(0), Player(1), ConvertAllianceType(n), false)
                types[#types + 1] = flush():gsub("v0:[^,]*,?", "")
              end
              print("changed by types 1 to 9: " .. table.concat(types))
            """);

        Assert.Equal([
            "0.000 0>1 passive on c0:true:nil:true,p0:true:nil:true; again ",
            "0.000 0>1 passive off c0:true:nil:false,p0:true:nil:false; again ",
            "0.000 1>0 passive on c1:true:nil:false,p1:true:nil:false; off c1:true:nil:false,p1:true:nil:false",
            "0.000 0>1 vision on v0:true:nil:false; off v0:true:nil:false",
            "0.000 0>0 passive off  reads true",
            "0.000 changed by types 1 to 9: ",
        ], Printed(simulation));
    }

    // the events probe: a player state event fires on a change to a value that meets its comparison, NOT_EQUAL too,
    // with EVENT_PLAYER_STATE_LIMIT, the player and the state; gold set below 0 is 0; TriggerRegisterPlayerEvent's
    // EVENT_PLAYER_STATE_LIMIT and another player's registration don't fire; outside an event the state is the one of id 0
    [Fact]
    public void A_player_state_event_fires_on_a_change_that_meets_its_comparison()
    {
        using var simulation = Load("""
              local limit, limitFired = CreateTrigger(), 0
              TriggerRegisterPlayerEvent(limit, Player(0), EVENT_PLAYER_STATE_LIMIT)
              TriggerAddAction(limit, function() limitFired = limitFired + 1 end)
              local other = CreateTrigger()
              TriggerRegisterPlayerStateEvent(other, Player(1), PLAYER_STATE_RESOURCE_GOLD, GREATER_THAN_OR_EQUAL, 0)
              TriggerAddAction(other, function() log[#log + 1] = "other" end)
              local gold = PLAYER_STATE_RESOURCE_GOLD
              for _, name in ipairs(names) do
                local t = CreateTrigger()
                TriggerRegisterPlayerStateEvent(t, Player(0), gold, ops[name], 2)
                TriggerAddAction(t, function()
                  log[#log + 1] = GetPlayerState(Player(0), gold) .. ":" .. (GetTriggerEventId() == EVENT_PLAYER_STATE_LIMIT and "11" or "?")
                end)
                for _, value in ipairs({5, 0, 1, 2, 2, 3, 2, 1, 0, 4, -1, 0}) do SetPlayerState(Player(0), gold, value) end
                DisableTrigger(t)
                print("gold " .. name .. " " .. flush())
              end
              local inside = CreateTrigger()
              TriggerRegisterPlayerStateEvent(inside, Player(0), PLAYER_STATE_RESOURCE_LUMBER, GREATER_THAN, 10.5)
              TriggerAddAction(inside, function()
                local s = GetEventPlayerState()
                log[#log + 1] = table.concat({GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER), pid(GetTriggerPlayer()),
                  tostring(s == PLAYER_STATE_RESOURCE_LUMBER), (tostring(s):match("^%a+")), tostring(GetTriggerEventId() == EVENT_PLAYER_STATE_LIMIT)}, " ")
              end)
              SetPlayerState(Player(0), gold, 50)
              for _, value in ipairs({10, 11, 12, 10}) do SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER, value) end
              print("lumber over 10.5 " .. flush() .. "; state limit event fired " .. limitFired .. "; outside " .. tostring(GetEventPlayerState() == PLAYER_STATE_GAME_RESULT))
            """);

        Assert.Equal([
            "0.000 gold LT 0:11,1:11,1:11,0:11,0:11",
            "0.000 gold LE 0:11,1:11,2:11,2:11,1:11,0:11,0:11",
            "0.000 gold EQ 2:11,2:11",
            "0.000 gold GE 5:11,2:11,3:11,2:11,4:11",
            "0.000 gold GT 5:11,3:11,4:11",
            "0.000 gold NE 5:11,0:11,1:11,3:11,1:11,0:11,4:11,0:11",
            "0.000 lumber over 10.5 11 0 true playerstate true,12 0 true playerstate true; state limit event fired 0; outside true",
        ], Printed(simulation));
    }

    // the events probe (and triggers-3's reads): with the day stopped, which sets fire each comparison with 12 inside
    // SetFloatGameState, read as set; the event is EVENT_GAME_STATE_LIMIT with the state and no trigger player
    [Fact]
    public void A_time_of_day_set_fires_the_comparisons_it_crosses()
    {
        using var simulation = Load("""
              local tod, at = GAME_STATE_TIME_OF_DAY, 0
              SuspendTimeOfDay(true)
              for _, name in ipairs(names) do
                SetFloatGameState(tod, 0)
                local t = CreateTrigger()
                TriggerRegisterGameStateEvent(t, tod, ops[name], 12)
                TriggerAddAction(t, function() log[#log + 1] = at .. ":" .. string.format("%.9g", GetFloatGameState(tod)) end)
                for i, value in ipairs({20, 10, 11, 12, 12, 13, 12, 11, 10, 14, 11.5, 12.5, 11.5}) do
                  at = i
                  SetFloatGameState(tod, value)
                end
                DisableTrigger(t)
                print("time " .. name .. " " .. flush())
              end
              local t = CreateTrigger()
              TriggerRegisterGameStateEvent(t, tod, EQUAL, 6)
              TriggerAddAction(t, function()
                log[#log + 1] = table.concat({tostring(GetTriggerEventId() == EVENT_GAME_STATE_LIMIT), tostring(GetEventGameState() == tod), pid(GetTriggerPlayer())}, " ")
              end)
              SetFloatGameState(tod, 6)
              print("time 6 inside: " .. flush())
            """);

        Assert.Equal([
            "0.000 time LT 2:10,7:12,11:11.5,13:11.5",
            "0.000 time LE 2:10,7:12,11:11.5,13:11.5",
            "0.000 time EQ 4:12,7:12",
            "0.000 time GE 1:20,4:12,6:13,10:14,12:12.5",
            "0.000 time GT 1:20,4:12,6:13,10:14,12:12.5",
            "0.000 time NE ",
            "0.000 time 6 inside: true true nil",
        ], Printed(simulation));
    }

    // the events probe: running forward the day fires the limit's EQUAL, GREATER_THAN_OR_EQUAL and GREATER_THAN events
    // as it reaches it (2 s from 11.9 at scale 1), backward its EQUAL and LESS_THAN ones, reading the limit; set to the
    // limit while running, the set fires LESS_THAN and LESS_THAN_OR_EQUAL from above, and the day the rest at the next
    // step. A limit passes again a day later. The game's order among the events of one moment isn't followed.
    [Fact]
    public void The_running_day_fires_the_events_of_a_limit_as_it_passes_it()
    {
        using var simulation = Load("""
              local tod = GAME_STATE_TIME_OF_DAY
              for _, name in ipairs(names) do
                local t = CreateTrigger()
                TriggerRegisterGameStateEvent(t, tod, ops[name], 12)
                TriggerAddAction(t, function() print(name, string.format("%.9g", GetFloatGameState(tod))) end)
              end
              SuspendTimeOfDay(true)
              SetFloatGameState(tod, 11.9)
              SuspendTimeOfDay(false)
              TimerStart(CreateTimer(), 2.5, false, function()
                print("backward from 12.1")
                SetTimeOfDayScale(-1)
                SetFloatGameState(tod, 12.1)
              end)
              TimerStart(CreateTimer(), 5, false, function()
                print("stopped, set 13")
                SetTimeOfDayScale(1)
                SuspendTimeOfDay(true)
                SetFloatGameState(tod, 13)
              end)
              TimerStart(CreateTimer(), 5.5, false, function()
                print("running, set 12")
                SuspendTimeOfDay(false)
                SetFloatGameState(tod, 12)
              end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(6));
        Assert.Equal([
            "2.001 EQ\t12", "2.001 GE\t12", "2.001 GT\t12",
            "2.500 backward from 12.1",
            "4.501 LT\t12", "4.501 LE\t12", "4.501 EQ\t12",
            "5.000 stopped, set 13",
            "5.000 GE\t13", "5.000 GT\t13",
            "5.500 running, set 12",
            "5.500 LT\t12", "5.500 LE\t12",
            "5.501 EQ\t12", "5.501 GE\t12", "5.501 GT\t12",
        ], Printed(simulation));

        simulation.Advance(TimeSpan.FromSeconds(479));
        Assert.Equal(16, simulation.Log.Count);
        simulation.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(["485.501 EQ\t12", "485.501 GE\t12", "485.501 GT\t12"], Printed(simulation).Skip(16));
    }

    // decisions D144: a set onto a limit while the day runs passes it at the next step, whatever else reschedules the
    // day in the set's step
    [Fact]
    public void A_set_onto_a_limit_passes_it_at_the_next_step_after_another_registration()
    {
        using var simulation = Load("""
              local tod = GAME_STATE_TIME_OF_DAY
              for _, name in ipairs(names) do
                local t = CreateTrigger()
                TriggerRegisterGameStateEvent(t, tod, ops[name], 12)
                TriggerAddAction(t, function() print(name, string.format("%.9g", GetFloatGameState(tod))) end)
              end
              TimerStart(CreateTimer(), 1, false, function()
                SetFloatGameState(tod, 12)
                TriggerRegisterGameStateEvent(CreateTrigger(), GAME_STATE_DIVINE_INTERVENTION, EQUAL, 1)
                SetTimeOfDayScale(1)
              end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(["1.000 GE\t12", "1.000 GT\t12", "1.001 EQ\t12", "1.001 GE\t12", "1.001 GT\t12"], Printed(simulation));
    }

    // the events probe: none of TriggerRegisterGameEvent's events fire for timers, game state sets, a victory or EndGame
    [Fact]
    public void No_game_event_fires_in_a_run()
    {
        using var simulation = Load("""
              local fired = 0
              for _, n in ipairs({0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 256, 257, 258, 259, 310}) do
                local t = CreateTrigger()
                TriggerRegisterGameEvent(t, ConvertGameEvent(n))
                TriggerAddAction(t, function() fired = fired + 1 end)
              end
              TimerStart(CreateTimer(), 0.5, false, function()
                SetFloatGameState(GAME_STATE_TIME_OF_DAY, 12)
                SetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION, 1)
                RemovePlayer(Player(1), PLAYER_GAME_RESULT_VICTORY)
                EndGame(false)
                print("fired", fired)
              end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.500 fired\t0"], Printed(simulation));
    }

    // the events probe: a filter is called for each playing player of slots 0 to 23 in order, with GetFilterPlayer set
    // and no enum or trigger player, and passes as a condition does; the count stops the enumeration, filter and all;
    // ForceEnumAllies and ForceEnumEnemies call it for every playing player
    [Fact]
    public void A_filter_is_called_for_each_playing_player_with_GetFilterPlayer_set()
    {
        using var simulation = Load("""
              local function members(f)
                local list = {}
                for i = 0, 27 do if IsPlayerInForce(Player(i), f) then list[#list + 1] = i end end
                return table.concat(list, " ")
              end
              local f = CreateForce()
              local function filter(result)
                return Filter(function()
                  log[#log + 1] = pid(GetFilterPlayer()) .. (GetEnumPlayer() and "e" or "") .. (GetTriggerPlayer() and "t" or "")
                  if type(result) == "function" then return result() end
                  return result
                end)
              end
              local function enum(label, call)
                ForceClear(f)
                ForceAddPlayer(f, Player(5))
                call()
                return label .. " " .. flush() .. ">" .. members(f)
              end
              print("filter " .. enum("true", function() ForceEnumPlayers(f, filter(true)) end)
                .. "; " .. enum("false", function() ForceEnumPlayers(f, filter(false)) end)
                .. "; " .. enum("is 1", function() ForceEnumPlayers(f, filter(function() return GetFilterPlayer() == Player(1) end)) end)
                .. "; after " .. pid(GetFilterPlayer()))
              print("filter " .. enum("counted 1", function() ForceEnumPlayersCounted(f, filter(true), 1) end)
                .. "; " .. enum("counted 1 not 0", function() ForceEnumPlayersCounted(f, filter(function() return GetFilterPlayer() ~= Player(0) end), 1) end)
                .. "; " .. enum("allies of 0", function() ForceEnumAllies(f, Player(0), filter(true)) end)
                .. "; " .. enum("enemies of 0", function() ForceEnumEnemies(f, Player(0), filter(true)) end))
              local results = {}
              for _, r in ipairs({{"1", 1}, {"0", 0}, {"x", "x"}, {"nil", nil}, {"table", {}}}) do
                results[#results + 1] = enum(r[1], function() ForceEnumPlayers(f, filter(r[2])) end)
              end
              print("filter results " .. table.concat(results, "; "))
              print("filter " .. enum("condition", function() ForceEnumPlayers(f, Condition(function() log[#log + 1] = "c" return true end)) end)
                .. "; " .. enum("and", function() ForceEnumPlayers(f, And(filter(true), filter(false))) end)
                .. "; " .. enum("Filter(nil)", function() ForceEnumPlayers(f, Filter(nil)) end)
                .. "; " .. enum("raises", function() ForceEnumPlayers(f, Filter(function() log[#log + 1] = "r" error("raised") end)) end))
            """);

        Assert.Equal([
            "0.000 filter true 0,1>0 1; false 0,1>; is 1 0,1>1; after nil",
            "0.000 filter counted 1 0>0; counted 1 not 0 0,1>1; allies of 0 0,1>0; enemies of 0 0,1>1",
            "0.000 filter results 1 0,1>0 1; 0 0,1>; x 0,1>; nil 0,1>; table 0,1>",
            "0.000 filter condition c,c>0 1; and 0,0,1,1>; Filter(nil) >0 1; raises r,r>",
        ], Printed(simulation).Where(line => !line.Contains(" error: ")));
    }

    // the events probe: after EndGame the code after the call and a 0 s timer started before it run, then nothing
    [Fact]
    public void EndGame_lets_its_step_finish_and_then_the_clock_stops()
    {
        using var simulation = Load("""
              TimerStart(CreateTimer(), 1, false, function()
                TimerStart(CreateTimer(), 0, false, function() print("0 s timer") end)
                EndGame(false)
                print("after EndGame")
              end)
              TimerStart(CreateTimer(), 2, false, function() print("never") end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));
        Assert.True(simulation.Ended);
        simulation.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(["1.000 after EndGame", "1.000 0 s timer"], Printed(simulation));
    }

    // the forforce-wait probe: a wait started right after a force ForForce ran over was destroyed never ends, whether or
    // not the callback ran; ForForce or destroying alone don't, and the next wait after it ends; nor does a wait started
    // 1 s after the destroy, or one running across it while a new one beside it is lost
    [Fact]
    public void The_first_wait_after_destroying_a_force_ForForce_ran_over_never_ends()
    {
        using var simulation = Load("""
              local results = {}
              local steps = {
                {"base", function() end},
                {"create, destroy", function() DestroyForce(CreateForce()) end},
                {"ForForce empty", function() ForForce(CreateForce(), function() end) end},
                {"ForForce one", function() local f = CreateForce() ForceAddPlayer(f, Player(0)) ForForce(f, function() end) end},
                {"ForForce empty, destroy", function() local f = CreateForce() ForForce(f, function() end) DestroyForce(f) end},
                {"ForForce one, destroy", function() local f = CreateForce() ForceAddPlayer(f, Player(0)) ForForce(f, function() end) DestroyForce(f) end},
                {"next", function() end},
              }
              local function step(n)
                if n > #steps then
                  for i, s in ipairs(steps) do print(i .. " " .. s[1] .. ": " .. tostring(results[i])) end
                  return
                end
                if n == 1 then
                  local function waiter(label, timeout)
                    local t = CreateTrigger()
                    results[label] = "waiting"
                    TriggerAddAction(t, function() TriggerSleepAction(timeout) results[label] = "ended" end)
                    TriggerExecute(t)
                  end
                  local function lose() local f = CreateForce() ForForce(f, function() end) DestroyForce(f) end
                  local function after(s, f) TimerStart(CreateTimer(), s, false, f) end
                  after(12, function() lose() after(1, function() waiter("later", 0.5) end) end)
                  after(15, function() waiter("running", 1.5) end)
                  after(15.5, function() lose() waiter("beside", 0.5) end)
                  after(18, function() print(results.later, results.running, results.beside) end)
                end
                steps[n][2]()
                local t = CreateTrigger()
                results[n] = "waiting"
                TriggerAddAction(t, function() TriggerSleepAction(0.5) results[n] = "ended" end)
                TriggerExecute(t)
                TimerStart(CreateTimer(), 1.5, false, function() step(n + 1) end)
              end
              step(1)
            """);

        simulation.Advance(TimeSpan.FromSeconds(19));

        Assert.Equal([
            "10.500 1 base: ended",
            "10.500 2 create, destroy: ended",
            "10.500 3 ForForce empty: ended",
            "10.500 4 ForForce one: ended",
            "10.500 5 ForForce empty, destroy: waiting",
            "10.500 6 ForForce one, destroy: waiting",
            "10.500 7 next: ended",
            "18.000 ended\tended\twaiting",
        ], Printed(simulation));
    }
}
