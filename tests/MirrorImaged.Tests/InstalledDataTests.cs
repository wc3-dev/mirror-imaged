using MirrorImaged.Data;
using War3Net.Build;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>
/// The installed game's standard data, against what the game gives (game-facts, "Units", "Object data", "Spells and
/// abilities" and "Heroes"; docs/facts.md, "The game's data"), for a map of The Frozen Throne's custom data, as
/// World Editor makes one.
/// </summary>
[Trait("Needs", "Game")]
public sealed class InstalledDataTests
{
    static readonly GameData Data = new(StandardData.Installed(GameInstall.Require(), null));

    static int Id(string id) => Ids.Of(id);

    // facts: a footman made from hfoo has 420 life and scale 0.8, its level is 2, its name "Footman"; ParseTags <hfoo,HP> 420
    [InstallFact]
    public void The_footman_reads_the_values_the_game_gives()
    {
        var footman = Data.Object(ObjectKind.Unit, Id("hfoo"))!;

        Assert.Equal("Footman", footman.Text(Id("unam")));
        Assert.Equal(420, footman.Integer(Id("uhpm")));
        Assert.Equal(0.8f, footman.Real(Id("usca")));
        Assert.Equal(2, footman.Integer(Id("ulev")));
        Assert.Equal(270, footman.Integer(Id("umvs")));
        Assert.Equal("Adef,Aihn", footman.Text(Id("uabi")));
    }

    // facts: the Paladin's scale is 0.935; it starts with 100 + 255 mana, 17 intelligence at 15 each, and its life is
    // its base plus 25 a strength
    [InstallFact]
    public void The_paladin_reads_the_values_the_game_gives()
    {
        var paladin = Data.Object(ObjectKind.Unit, Id("Hpal"))!;

        Assert.Equal("Paladin", paladin.Text(Id("unam")));
        Assert.Equal(0.935f, paladin.Real(Id("usca")));
        Assert.Equal(100, paladin.Integer(Id("uhpm")));
        Assert.Equal(22, paladin.Integer(Id("ustr")));
        Assert.Equal(17, paladin.Integer(Id("uint")));
        Assert.Equal(100, paladin.Integer(Id("umpi")));
        Assert.Equal(0, paladin.Integer(Id("umpm")));
        Assert.Equal("AHhb,AHds,AHre,AHad", paladin.Text(Id("uhab")));
        Assert.Equal(25, Data.Constant("StrHitPointBonus", 0));
        Assert.Equal(15, Data.Constant("IntManaBonus", 0));
    }

    // facts: BlzGetAbilityTooltip("AHhb", 0) is "Holy Light - [|cffffcc00Level 1|r]"; ParseTags reads DataA1 200,
    // DataA2 400 and Cool1 5
    [InstallFact]
    public void Holy_light_reads_the_values_the_game_gives()
    {
        var holyLight = Data.Object(ObjectKind.Ability, Id("AHhb"))!;

        Assert.Equal("Holy Light", holyLight.Text(Id("anam")));
        Assert.Equal(3, holyLight.Levels);
        Assert.Equal([200, 400, 600], new[] { 1, 2, 3 }.Select(level => holyLight.Real(Id("Hhb1"), level)));
        Assert.Null(holyLight.Text(Id("Hhb1"), 4));
        Assert.Equal(5, holyLight.Real(Id("acdn"), 1));
        Assert.Equal(65, holyLight.Integer(Id("amcs"), 1));
        Assert.Equal("Holy Light - [|cffffcc00Level 1|r]", holyLight.Text(Id("atp1"), 1));
        Assert.Equal("T", holyLight.Text(Id("ahky")));
    }

    // facts: Iron Forged Swords has 3 levels; the strings and hotkeys the data probe read
    [InstallFact]
    public void Upgrades_strings_and_hotkeys_read_what_the_game_gives()
    {
        Assert.Equal(3, Data.Object(ObjectKind.Upgrade, Id("Rhme"))!.Levels);
        Assert.Equal("Iron Forged Swords", Data.Object(ObjectKind.Upgrade, Id("Rhme"))!.Text(Id("gnam"), 1));
        Assert.Equal("Player", Data.String("PLAYER"));
        Assert.Equal("Agility", Data.String("AGILITY"));
        Assert.Equal("January", Data.String("MONTH_01"));
        Assert.Equal("The parameters provided are invalid.", Data.String("ERROR_ID_INVALIDPARAMS"));
        Assert.Equal("C", Data.Hotkey("GAMEOVER_CONTINUE"));
        Assert.Equal("D", Data.Hotkey("KEY_DELETE"));
        Assert.Null(Data.Hotkey("MinimapTerrain"));
        Assert.Equal("480", Data.Constant("DayLength"));
        Assert.Equal("24", Data.Constant("DayHours"));
    }

    // game-facts, "Object data": oshm 350 in the base tables, 260 under custom_v0, 335 under custom_v1, 355 under
    // melee_v0; ebsh missing from custom_v0 and melee_v0, fapo from all three sets; and the probes data-sets-melee and
    // data-sets-tft
    [InstallTheory]
    [InlineData(GameDataSet.Custom, GameDataVersion.Unset, false, 350, 1000, 400)]
    [InlineData(GameDataSet.Custom, GameDataVersion.RoC, false, 260, 0, 0)]
    [InlineData(GameDataSet.Custom, GameDataVersion.TFT, false, 335, 1000, 0)]
    [InlineData(GameDataSet.Melee, GameDataVersion.RoC, false, 355, 0, 0)]
    [InlineData(GameDataSet.Default, GameDataVersion.TFT, true, 350, 1000, 0)]
    public void Each_balance_set_reads_the_life_the_game_gives(GameDataSet set, GameDataVersion version, bool melee, int shaman, int bush, int footman)
    {
        var info = MapFactory.Info(32, 32);
        (info.FormatVersion, info.GameDataSet, info.GameDataVersion) = (MapInfoFormatVersion.v31, set, version);
        info.MapFlags = melee ? MapFlags.MeleeMap : 0;
        var data = new GameData(StandardData.Installed(GameInstall.Require(), info));

        Assert.Equal([shaman, bush, footman], new[] { "oshm", "ebsh", "fapo" }.Select(id => data.Object(ObjectKind.Unit, Id(id))?.Integer(Id("uhpm")) ?? 0));
    }
}

/// <summary>A theory that reads the Warcraft III install on this computer; skipped when there is none.</summary>
public sealed class InstallTheoryAttribute : TheoryAttribute
{
    public InstallTheoryAttribute()
    {
        if (GameInstall.Find() is null)
            Skip = $"No Warcraft III install in {GameInstall.Place()}; set {GameInstall.Variable} to run it";
    }
}
