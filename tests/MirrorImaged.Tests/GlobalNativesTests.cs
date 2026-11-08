using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>common.j's globals and the natives they and blizzard.j's globals are made with; no game needed.</summary>
public sealed class GlobalNativesTests
{
    readonly Game _game = new(0);
    readonly GameNatives _natives;

    public GlobalNativesTests() => _natives = new GameNatives(_game);

    // common.j, 3.0.1.24371
    [Fact]
    public void The_globals_have_common_j_s_values()
    {
        var globals = _natives.Globals().ToDictionary(global => global.Name, global => global.Value);

        Assert.Equal(32768, globals["JASS_MAX_ARRAY_SIZE"]);
        Assert.Equal(true, globals["TRUE"]);
        Assert.Equal(false, globals["FALSE"]);
        Assert.Equal(27, globals["PLAYER_NEUTRAL_PASSIVE"]);
        Assert.Equal(24, globals["PLAYER_NEUTRAL_AGGRESSIVE"]);
        Assert.Same(_natives.ConvertPlayerColor(0), globals["PLAYER_COLOR_RED"]);
        Assert.Same(_natives.ConvertPlayerColor(1), globals["PLAYER_COLOR_BLUE"]);
        Assert.Same(_natives.ConvertRace(1), globals["RACE_HUMAN"]);
        Assert.Same(_natives.ConvertPlayerUnitEvent(20), globals["EVENT_PLAYER_UNIT_DEATH"]);
        Assert.Same(_natives.ConvertMapFlag(8192 * 2), globals["MAP_LOCK_SPEED"]);
        Assert.Same(_natives.ConvertOsKeyType(0x08), globals["OSKEY_BACKSPACE"]);
        Assert.Same(_natives.ConvertAbilityIntegerField(0x61627078), globals["ABILITY_IF_BUTTON_POSITION_NORMAL_X"]); // 'abpx'
    }

    [Fact]
    public void The_globals_are_sorted_by_name_each_once()
    {
        var names = _natives.Globals().Select(global => global.Name).ToList();

        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void Every_convert_native_gives_a_handle_of_its_type_whose_id_is_the_value_the_same_every_call()
    {
        var converts = typeof(GameNatives).GetMethods()
            .Where(method => method.Name.StartsWith("Convert") && method.ReturnType.IsSubclassOf(typeof(handle)))
            .ToList();

        Assert.Equal(88, converts.Count);
        foreach (var convert in converts)
        {
            var made = Assert.IsAssignableFrom<handle>(convert.Invoke(_natives, [-7]));
            Assert.Equal(convert.ReturnType, made.GetType());
            Assert.Equal(-7, made.Id);
            Assert.Same(made, convert.Invoke(_natives, [-7]));
            Assert.NotSame(made, convert.Invoke(_natives, [7]));
        }
    }

    [Fact]
    public void A_converted_handle_is_found_by_its_type_and_value_and_takes_no_id_from_the_game()
    {
        var color = _natives.ConvertPlayerColor(0x100000)!;
        var timer = _natives.CreateTimer()!;

        Assert.Same(color, _natives.Handle(typeof(playercolor), 0x100000));
        Assert.Same(_natives.Player(0), _natives.Handle(typeof(player), 0x100000));
        Assert.Same(_natives.ConvertRace(0x100000), _natives.Handle(typeof(race), 0x100000));
        Assert.Equal(0x10001C, timer.Id);
        Assert.Same(timer, _natives.Handle(typeof(timer), timer.Id));
    }

    // facts: neutral passive is slot 27; the other slots as common.j and blizzard.j name them, not measured
    [Fact]
    public void The_slots_are_24_players_then_four_neutral_ones()
    {
        Assert.Equal(24, _natives.GetBJMaxPlayers());
        Assert.Equal(24, _natives.GetPlayerNeutralAggressive());
        Assert.Equal(25, _natives.GetBJPlayerNeutralVictim());
        Assert.Equal(26, _natives.GetBJPlayerNeutralExtra());
        Assert.Equal(27, _natives.GetPlayerNeutralPassive());
        Assert.Equal(28, _natives.GetBJMaxPlayerSlots());
    }

    [Fact]
    public void CreateGroup_makes_a_new_group_each_call()
    {
        var first = _natives.CreateGroup()!;
        var second = _natives.CreateGroup()!;

        Assert.Equal(first.Id + 1, second.Id);
        Assert.Same(first, _game.Handle(first.Id));
    }
}
