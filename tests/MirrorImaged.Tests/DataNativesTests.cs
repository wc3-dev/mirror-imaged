using MirrorImaged.Data;
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
            map.TriggerStrings = new TriggerStrings { Strings = { new TriggerString { Key = 1, Value = "Probe Alpha" }, new TriggerString { Key = 2, Value = "The Horde" } } };
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

    // The data probe: SetPlayerName(p, "TRIGSTR_002") reads "The Horde" and StringCase("TRIGSTR_001", false) "probe alpha"
    [Fact]
    public void A_trigstr_given_to_a_native_is_the_maps_string()
    {
        using var simulation = Run("""
            SetPlayerName(Player(5), "TRIGSTR_002")
            print(GetPlayerName(Player(5)), StringCase("TRIGSTR_001", false), StringLength("TRIGSTR_1"))
            """, new StandardData());

        Assert.Equal(["0.000 print: The Horde\tprobe alpha\t11"], simulation.Log);
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
}
