using System.Text;
using MirrorImaged.Data;
using War3Net.Build;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>
/// The standard data read from small game files written here, as the install's are laid out: the metadata, the tables
/// merged with the text files, the balance sets, the graphics modes, the levels, the constants, strings and hotkeys.
/// </summary>
public sealed class StandardDataTests
{
    // An SLK table of rows of cells, the first row the header; a null cell is left out.
    static string Slk(params string?[][] rows) =>
        "ID;PWXL;N;E\n" + string.Concat(rows.SelectMany((row, y) => row.Select((cell, x) => cell is null ? "" : $"C;X{x + 1};Y{y + 1};K\"{cell}\"\n"))) + "E\n";

    static readonly string[] MetaHeader = ["ID", "field", "slk", "index", "repeat", "data", "appendIndex"];

    static readonly Dictionary<string, string> Files = new()
    {
        ["units/unitmetadata.slk"] = Slk(MetaHeader,
            ["uhpm", "HP", "UnitBalance", "-1", "0", null, null],
            ["unam", "Name", "Profile", "0", "0", null, null],
            ["usca", "modelScale", "UnitUI", "-1", "0", null, null],
            ["ubpx", "Buttonpos", "Profile", "0", "0", null, null],
            ["ubpy", "Buttonpos", "Profile", "1", "0", null, null]),
        ["units/unitdata.slk"] = Slk(["unitID", "race"], ["hfoo", "human"], ["hkni", "human"], ["Hpal", "human"]),
        ["units/unitbalance.slk"] = Slk(["unitBalanceID", "HP"], ["hfoo", "420"], ["hkni", "800"], ["Hpal", "100"], ["zzzz", "5"]),
        ["units/unitui.slk"] = Slk(["unitUIID", "modelScale"], ["hfoo", "1"], ["hkni", "1.1"], ["Hpal", "-"]),
        ["_balance/custom_v1.w3mod/units/unitbalance.slk"] = Slk(["unitBalanceID", "HP"], ["hfoo", "450"], ["Hpal", "100"]),
        ["units/humanunitfunc.txt"] = "[hfoo]\nButtonpos=1,2\n[HPAL]\nButtonpos=0,0\n",
        ["units/unitskin.txt"] = "[hfoo]\nmodelScale:hd=0.95\nmodelScale:de=0.8\n[hkni]\nmodelScale:sd=2\nmodelScale:hd=1.2\n",
        ["_locales/enus.w3mod/units/humanunitstrings.txt"] = "[hfoo]\nName=Footman\n[Hpal]\nName=\"Paladin, the Pure\",Paladin\n",
        ["units/abilitymetadata.slk"] = Slk(MetaHeader,
            ["alev", "levels", "AbilityData", "-1", "0", "0", null],
            ["Hhb1", "Data", "AbilityData", "-1", "4", "1", null],
            ["acdn", "Cool", "AbilityData", "-1", "4", "0", null],
            ["atp1", "Tip", "Profile", "0", "3", "0", null]),
        ["units/abilitydata.slk"] = Slk(["alias", "levels", "DataA1", "DataA2", "DataA3", "DataA4", "Cool1", "Cool2"],
            ["AHhb", "3", "200", "400", "600", "800", "5", "-"],
            ["AXsx", "6", "1", "2", null, "4", null, null]),
        ["_locales/enus.w3mod/units/humanabilitystrings.txt"] = "[AHhb]\nTip=One,Two,\"Three, with a comma\"\n",
        ["units/upgrademetadata.slk"] = Slk(MetaHeader,
            ["glvl", "maxlevel", "UpgradeData", "-1", "0", null, "0"],
            ["gnam", "Name", "Profile", "0", "1", null, "0"],
            ["greq", "Requires", "Profile", "-1", "1", null, "1"]),
        ["units/upgradedata.slk"] = Slk(["upgradeid", "maxlevel"], ["Rhme", "3"]),
        ["units/humanupgradefunc.txt"] = "[Rhme]\nRequires=\nRequires1=hkee\nRequires2=hcas\n",
        ["_locales/enus.w3mod/units/humanupgradestrings.txt"] = "[Rhme]\nName=Iron,Steel,Mithril\n",
        ["units/miscdata.txt"] = "[Misc]\nDayLength=480\nDayHours=24\n",
        ["units/miscgame.txt"] = "[Misc]\nStrHitPointBonus=25\n[Other]\nDayHours=99\n",
        ["_balance/custom_v1.w3mod/units/miscgame.txt"] = "[Misc]\nStrHitPointBonus=30\n",
        ["_locales/enus.w3mod/ui/framedef/globalstrings.fdf"] =
            "// strings\nStringList {\n    PLAYER          \"Player\",\n    TWO_LINES       \"a\nb\",\n    LAST \"x\"\n}\n",
        ["ui/miscui.txt"] = "[Hotkeys]\nGAMEOVER_CONTINUE='C'\nGAMEOVER_CONTINUE:deDE='W'\nGERMAN_ONLY:deDE='X'\nLISTED:esES,enUS='L'\n",
    };

    static GameData Read(string? values = null, string? objects = null, string locale = "enUS") =>
        new(StandardData.Read(path => Files.TryGetValue(path, out var text) ? Encoding.UTF8.GetBytes(text) : null, values, objects, locale));

    static int Id(string id) => Ids.Of(id);

    [Fact]
    public void An_objects_fields_come_from_its_tables_and_text_by_the_metadata()
    {
        var footman = Read().Object(ObjectKind.Unit, Id("hfoo"))!;

        Assert.Equal(420, footman.Integer(Id("uhpm")));
        Assert.Equal("Footman", footman.Text(Id("unam")));
        Assert.Equal(1, footman.Integer(Id("ubpx")));
        Assert.Equal(2, footman.Integer(Id("ubpy")));
        Assert.Null(footman.Text(Id("zzzz")));
        Assert.Equal(ObjectKind.Unit, footman.Kind);
        Assert.Equal(Id("hfoo"), footman.BaseId);
    }

    // The first table names the objects; a text section is the object's whatever its case; a list field's first entry
    // keeps a quoted entry's comma
    [Fact]
    public void The_first_table_names_the_objects_and_text_sections_match_in_any_case()
    {
        var data = Read();
        var paladin = data.Object(ObjectKind.Unit, Id("Hpal"))!;

        Assert.Null(data.Object(ObjectKind.Unit, Id("zzzz")));
        Assert.Null(data.Object(ObjectKind.Item, Id("hfoo")));
        Assert.Equal("Paladin, the Pure", paladin.Text(Id("unam")));
        Assert.Equal(0, paladin.Integer(Id("ubpx")));
        Assert.Null(paladin.Text(Id("usca"))); // "-" is no value
    }

    // game-facts, "Object data": in graphics mode 2 the game reads :de, else :hd, never :sd (hfoo 0.8)
    [Fact]
    public void A_value_with_graphics_modes_is_read_as_definitive_edition_graphics_read_it()
    {
        var data = Read();

        Assert.Equal(0.8f, data.Object(ObjectKind.Unit, Id("hfoo"))!.Real(Id("usca")));
        Assert.Equal(1.2f, data.Object(ObjectKind.Unit, Id("hkni"))!.Real(Id("usca")));
    }

    // game-facts, "Object data": a set's tables replace the base's, and an object one of them leaves out doesn't exist
    [Fact]
    public void A_balance_sets_files_replace_the_bases_and_say_which_objects_exist()
    {
        var custom = Read("custom_v1", "custom_v1");
        var melee = Read(null, "custom_v1");

        Assert.Equal(450, custom.Object(ObjectKind.Unit, Id("hfoo"))!.Integer(Id("uhpm")));
        Assert.Null(custom.Object(ObjectKind.Unit, Id("hkni")));
        Assert.Equal(420, melee.Object(ObjectKind.Unit, Id("hfoo"))!.Integer(Id("uhpm")));
        Assert.Null(melee.Object(ObjectKind.Unit, Id("hkni")));
        Assert.Equal("Footman", custom.Object(ObjectKind.Unit, Id("hfoo"))!.Text(Id("unam")));
        Assert.NotNull(Read().Object(ObjectKind.Unit, Id("hkni")));
    }

    [Fact]
    public void A_field_of_each_level_reads_its_column_its_list_entry_or_its_numbered_key()
    {
        var data = Read();
        var holyLight = data.Object(ObjectKind.Ability, Id("AHhb"))!;
        var swords = data.Object(ObjectKind.Upgrade, Id("Rhme"))!;

        Assert.Equal(3, holyLight.Levels);
        Assert.Equal([200, 400, 600], new[] { 1, 2, 3 }.Select(level => holyLight.Integer(Id("Hhb1"), level)));
        Assert.Equal(5, holyLight.Integer(Id("acdn"), 1));
        Assert.Null(holyLight.Text(Id("acdn"), 2));
        Assert.Equal(["One", "Two", "Three, with a comma"], new[] { 1, 2, 3 }.Select(level => holyLight.Text(Id("atp1"), level)));
        Assert.Equal(3, swords.Levels);
        Assert.Equal(["Iron", "Steel", "Mithril"], new[] { 1, 2, 3 }.Select(level => swords.Text(Id("gnam"), level)));
        Assert.Equal([null, "hkee", "hcas"], new[] { 1, 2, 3 }.Select(level => swords.Text(Id("greq"), level)));
    }

    // game-facts, "Spells and abilities": the standard Storm Bolt reads 0 past its third level; a copy given 6 levels
    // reads the 4th column at levels 4 to 6
    [Fact]
    public void A_level_past_the_count_has_no_value_and_one_past_the_4th_without_its_own_takes_the_one_before()
    {
        var data = Read();
        var holyLight = data.Object(ObjectKind.Ability, Id("AHhb"))!;
        var six = data.Object(ObjectKind.Ability, Id("AXsx"))!;

        Assert.Null(holyLight.Text(Id("Hhb1"), 4));
        Assert.Null(holyLight.Text(Id("Hhb1"), 0));
        Assert.Null(holyLight.Text(Id("atp1"), 4));
        Assert.Equal(["1", "2", null, "4", "4", "4", null], new[] { 1, 2, 3, 4, 5, 6, 7 }.Select(level => six.Text(Id("Hhb1"), level)));
    }

    [Fact]
    public void A_field_without_levels_reads_the_same_at_any_level()
    {
        var holyLight = Read().Object(ObjectKind.Ability, Id("AHhb"))!;

        Assert.Equal(3, holyLight.Integer(Id("alev"), 2));
    }

    [Fact]
    public void The_constants_are_misc_datas_and_misc_games_the_sets_file_replacing_the_bases()
    {
        var data = Read();

        Assert.Equal("480", data.Constant("DayLength"));
        Assert.Equal(24, data.Constant("dayhours", 0));
        Assert.Equal("25", data.Constant("StrHitPointBonus"));
        Assert.Equal("30", Read("custom_v1", "custom_v1").Constant("StrHitPointBonus"));
        Assert.Null(data.Constant("Nothing"));
        Assert.Equal(7, data.Constant("Nothing", 7));
    }

    // docs/facts.md, "The game's data": the string lists' keys in their case; the hotkeys in any case and the installed language
    [Fact]
    public void Strings_and_hotkeys_are_the_locales()
    {
        var data = Read();
        var german = Read(locale: "deDE");

        Assert.Equal("Player", data.String("PLAYER"));
        Assert.Null(data.String("player"));
        Assert.Equal("a\nb", data.String("TWO_LINES"));
        Assert.Equal("x", data.String("LAST"));
        Assert.Equal("C", data.Hotkey("gameover_continue"));
        Assert.Equal("W", german.Hotkey("GAMEOVER_CONTINUE"));
        Assert.Null(data.Hotkey("GERMAN_ONLY"));
        Assert.Equal("X", german.Hotkey("GERMAN_ONLY"));
        Assert.Equal("L", data.Hotkey("LISTED"));
    }

    // game-facts, "Object data", and the probes data, data-sets-melee and data-sets-tft
    [Theory]
    [InlineData(MapInfoFormatVersion.v31, GameDataSet.Custom, GameDataVersion.TFT, false, "custom_v1", "custom_v1")]
    [InlineData(MapInfoFormatVersion.v31, GameDataSet.Custom, GameDataVersion.RoC, false, "custom_v0", "custom_v0")]
    [InlineData(MapInfoFormatVersion.v31, GameDataSet.Melee, GameDataVersion.RoC, false, "melee_v0", "melee_v0")]
    [InlineData(MapInfoFormatVersion.v31, GameDataSet.Melee, GameDataVersion.TFT, false, null, "custom_v1")]
    [InlineData(MapInfoFormatVersion.v31, GameDataSet.Custom, GameDataVersion.Unset, false, null, null)]
    [InlineData(MapInfoFormatVersion.v31, GameDataSet.Default, GameDataVersion.RoC, false, "custom_v0", "custom_v0")]
    [InlineData(MapInfoFormatVersion.v31, GameDataSet.Default, GameDataVersion.TFT, true, null, "custom_v1")]
    [InlineData(MapInfoFormatVersion.v28, GameDataSet.Default, GameDataVersion.RoC, false, "custom_v1", "custom_v1")]
    [InlineData(MapInfoFormatVersion.v28, GameDataSet.Default, GameDataVersion.RoC, true, null, "custom_v1")]
    [InlineData(MapInfoFormatVersion.v18, GameDataSet.Unset, GameDataVersion.TFT, false, "custom_v0", "custom_v0")]
    public void The_balance_sets_follow_the_maps_data_set_version_and_melee_flag(
        MapInfoFormatVersion format, GameDataSet set, GameDataVersion version, bool melee, string? values, string? objects)
    {
        var info = MapFactory.Info(32, 32);
        (info.FormatVersion, info.GameDataSet, info.GameDataVersion) = (format, set, version);
        info.MapFlags = melee ? MapFlags.MeleeMap : 0;

        Assert.Equal((values, objects), StandardData.Sets(info));
        Assert.Equal(("custom_v1", "custom_v1"), StandardData.Sets(null));
    }
}
