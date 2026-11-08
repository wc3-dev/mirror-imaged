using MirrorImaged.Data;
using War3Net.Build.Info;
using War3Net.Build.Object;
using War3Net.Build.Script;
using War3Net.Common.Extensions;

namespace MirrorImaged.Tests;

/// <summary>
/// The natives that read the game's data, run on maps with data the test gives (decisions D207) against the probes of
/// docs/facts.md, "The game's data", and the facts' "Time of day".
/// </summary>
public sealed class DataNativesTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static readonly StandardData Standard = new StandardData()
        .Add(ObjectKind.Upgrade, "Rhme", new Dictionary<string, string> { ["glvl"] = "3" })
        .Add(ObjectKind.Upgrade, "Rhar", new Dictionary<string, string> { ["glvl"] = "3" })
        .Add(ObjectKind.Unit, "hfoo");

    static StandardData WithStrings()
    {
        var data = new StandardData();
        data.Strings["PLAYER"] = "Player";
        data.Strings["GAMEOVER_CONTINUE"] = "|CFFFFFFFFC|Rontinue";
        data.Hotkeys["GAMEOVER_CONTINUE"] = "C";
        return data;
    }

    Simulation Run(string main, StandardData data, IReadOnlyDictionary<string, string>? files = null) =>
        Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n", map: map =>
        {
            map.TriggerStrings = new TriggerStrings
            {
                Strings =
                {
                    new TriggerString { Key = 0, Value = "Zero String" }, new TriggerString { Key = 1, Value = "Probe Alpha" },
                    new TriggerString { Key = 2, Value = "The Horde" }, new TriggerString { Key = 5, Value = "42" },
                },
            };
            map.UpgradeObjectData = new UpgradeObjectData(ObjectDataFormatVersion.v3)
            {
                BaseUpgrades =
                {
                    new LevelObjectModification
                    {
                        OldId = "Rhme".FromRawcode(),
                        Unk = { 0 },
                        Modifications = { new LevelObjectDataModification { Id = "glvl".FromRawcode(), Type = ObjectDataType.Int, Value = 5 } },
                    },
                },
                NewUpgrades = { new LevelObjectModification { OldId = "Rhme".FromRawcode(), NewId = "R000".FromRawcode(), Unk = { 0 } } },
            };
        }, files: files), "", data: data);

    // The data probe: max Rhme 5 R000 3 Rhar 3, and adding 9 stops each there; the players probe: hfoo and zzzz 2147483647
    [Fact]
    public void An_upgrades_level_count_is_its_limit_and_where_adding_stops()
    {
        using var simulation = Run("""
            local p = Player(0)
            print(GetPlayerTechMaxAllowed(p, FourCC("Rhme")), GetPlayerTechMaxAllowed(p, FourCC("R000")), GetPlayerTechMaxAllowed(p, FourCC("Rhar")),
              GetPlayerTechMaxAllowed(p, FourCC("hfoo")), GetPlayerTechMaxAllowed(p, FourCC("zzzz")))
            for _, id in ipairs({"Rhme", "R000", "Rhar", "zzzz"}) do AddPlayerTechResearched(p, FourCC(id), 9) end
            print(GetPlayerTechCount(p, FourCC("Rhme"), true), GetPlayerTechCount(p, FourCC("R000"), true), GetPlayerTechCount(p, FourCC("Rhar"), true), GetPlayerTechCount(p, FourCC("zzzz"), true))
            SetPlayerTechMaxAllowed(p, FourCC("Rhme"), 2)
            print(GetPlayerTechMaxAllowed(p, FourCC("Rhme")))
            """, Standard);

        Assert.Equal(["0.000 print: 5\t3\t3\t2147483647\t2147483647", "0.000 print: 5\t3\t3\t9", "0.000 print: 2"], simulation.Log);
    }

    // The data probe: the string lists in their keys' case, a TRIGSTR the map's string, anything else as it is
    [Fact]
    public void A_localized_string_is_the_string_lists_else_the_text_given()
    {
        using var simulation = Run("""
            for _, key in ipairs({"PLAYER", "player", "GAMEOVER_CONTINUE", "TRIGSTR_001", "TRIGSTR_1", "TRIGSTR_999", "trigstr_001", "NO_SUCH_KEY", ""}) do
              print(GetLocalizedString(key))
            end
            """, WithStrings());

        Assert.Equal(["Player", "player", "|CFFFFFFFFC|Rontinue", "Probe Alpha", "Probe Alpha", "TRIGSTR_999", "trigstr_001", "NO_SUCH_KEY", ""],
            simulation.Log.Select(line => line["0.000 print: ".Length..]));
    }

    // The data probe: GAMEOVER_CONTINUE 67 in any case, a string without a hotkey 0
    [Fact]
    public void A_localized_hotkey_is_its_letter_and_0_without_one()
    {
        using var simulation = Run("""print(GetLocalizedHotkey("GAMEOVER_CONTINUE"), GetLocalizedHotkey("gameover_continue"), GetLocalizedHotkey("PLAYER"), GetLocalizedHotkey(""))""", WithStrings());

        Assert.Equal(["0.000 print: 67\t67\t0\t0"], simulation.Log);
    }

    // The data probe: SetPlayerName, StringCase and StringLength read a TRIGSTR_n as the map's string n (TRIGSTR_ and
    // TRIGSTR_abc as string 0, as atoi reads them; TRIGSTR_-1 isn't one); StringHash, S2I, SubString and SaveStr don't
    [Fact]
    public void A_trigstr_is_the_maps_string_for_the_natives_that_read_it_so()
    {
        using var simulation = Run("""
            SetPlayerName(Player(5), "TRIGSTR_002")
            print(GetPlayerName(Player(5)), StringCase("TRIGSTR_001", false), StringLength("TRIGSTR_"))
            print(StringCase("TRIGSTR_abc", false), StringCase("TRIGSTR_-1", false))
            print(StringHash("TRIGSTR_001") == StringHash("Probe Alpha"), S2I("TRIGSTR_005"), SubString("TRIGSTR_001", 0, 5))
            local h = InitHashtable()
            SaveStr(h, 0, 0, "TRIGSTR_001")
            print(LoadStr(h, 0, 0))
            """, new StandardData());

        Assert.Equal([
            "0.000 print: The Horde\tprobe alpha\t11",
            "0.000 print: zero string\ttrigstr_-1",
            "0.000 print: false\t0\tTRIGS",
            "0.000 print: TRIGSTR_001",
        ], simulation.Log);
    }

    // Inferred (decisions D206): World Editor writes the text a map shows as TRIGSTR_n, and the game shows the string
    [Fact]
    public void Text_shown_to_a_player_reads_a_trigstr_as_the_maps_string()
    {
        using var simulation = Run("""
            DisplayTextToPlayer(Player(0), 0, 0, "TRIGSTR_001")
            DisplayTimedTextToPlayer(Player(1), 0, 0, 5, "TRIGSTR_002")
            DisplayTextToPlayer(Player(0), 0, 0, "TRIGSTR_999")
            """, new StandardData());

        Assert.Equal(["0.000 player 0: Probe Alpha", "0.000 player 1: The Horde", "0.000 player 0: TRIGSTR_999"], simulation.Log);
    }

    // The lobby probe: with fixed player settings a computer reads its war3map.w3i name; the data probe: without, it is
    // "Computer (Normal)", and the person "Local Player" either way (decisions D210)
    [Theory]
    [InlineData(true, "Local Player\tThe Horde")]
    [InlineData(false, "Local Player\tComputer (Normal)")]
    public void A_computer_reads_its_maps_name_only_with_fixed_player_settings(bool fixedSettings, string names)
    {
        var path = _maps.Build("""
            function config() SetPlayerController(Player(1), MAP_CONTROL_COMPUTER) end
            function main() print(GetPlayerName(Player(0)), GetPlayerName(Player(1))) end
            """, change: info =>
        {
            info.MapFlags |= fixedSettings ? MapFlags.UseCustomForces | MapFlags.FixedPlayerSettingsForCustomForces : 0;
            info.Players[0].Name = "TRIGSTR_001";
            info.Players.Add(new PlayerData { Id = 1, Controller = PlayerController.Computer, Race = PlayerRace.Orc, Name = "TRIGSTR_002" });
        }, map: map => map.TriggerStrings = new TriggerStrings { Strings = { new TriggerString { Key = 1, Value = "Probe Alpha" }, new TriggerString { Key = 2, Value = "The Horde" } } });

        using var simulation = Simulation.Load(path, "");

        Assert.Equal([$"0.000 print: {names}"], simulation.Log);
    }

    // game-facts, "Time of day": DayHours=30 moves an hour every 16 s (480 / 30) and reads no more than 24
    [Fact]
    public void A_maps_day_has_its_own_hours_and_length()
    {
        using var simulation = Run("""
            SetFloatGameState(GAME_STATE_TIME_OF_DAY, 1)
            TimerStart(CreateTimer(), 16, false, function()
              print(GetFloatGameState(GAME_STATE_TIME_OF_DAY))
              SetFloatGameState(GAME_STATE_TIME_OF_DAY, 26)
              print(GetFloatGameState(GAME_STATE_TIME_OF_DAY))
            end)
            """, Standard, new Dictionary<string, string> { ["war3mapMisc.txt"] = "[Misc]\nDayHours=30\n" });
        simulation.Advance(TimeSpan.FromSeconds(16));

        Assert.Equal(["16.000 print: 2.0", "16.000 print: 24.0"], simulation.Log);
    }

    [Fact]
    public void A_maps_day_length_sets_how_fast_its_hours_pass()
    {
        using var simulation = Run("""
            TimerStart(CreateTimer(), 10, false, function() print(GetFloatGameState(GAME_STATE_TIME_OF_DAY)) end)
            """, Standard, new Dictionary<string, string> { ["war3mapMisc.txt"] = "[Misc]\nDayLength=240\n" });
        simulation.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(["10.000 print: 1.0"], simulation.Log);
    }

    // decisions D215, not measured: a day of no length or no hours is the standard one
    [Theory]
    [InlineData("DayLength=0")]
    [InlineData("DayLength=-5")]
    [InlineData("DayHours=0")]
    public void A_day_of_no_length_or_hours_is_the_standard_one(string misc)
    {
        using var simulation = Run("""
            TimerStart(CreateTimer(), 20, false, function() print(GetFloatGameState(GAME_STATE_TIME_OF_DAY)) end)
            """, Standard, new Dictionary<string, string> { ["war3mapMisc.txt"] = "[Misc]\n" + misc + "\n" });
        simulation.Advance(TimeSpan.FromSeconds(20));

        Assert.Equal(["20.000 print: 1.0"], simulation.Log);
    }
}
