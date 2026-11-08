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
        Assert.Null(Data.String("player"));
        Assert.Equal("|CFFFFFFFFC|Rontinue", Data.String("GAMEOVER_CONTINUE"));
        Assert.Equal("|CffffffffH|Rotkeys", Data.String("KEY_HOTKEYS"));
        Assert.Null(Data.String("WESTRING_APPNAME"));
        Assert.Equal("C", Data.Hotkey("GAMEOVER_CONTINUE"));
        Assert.Equal("C", Data.Hotkey("gameover_continue"));
        Assert.Equal("Q", Data.Hotkey("GAMEOVER_QUIT_GAME"));
        Assert.Equal("O", Data.Hotkey("GAMEOVER_OK"));
        Assert.Equal("D", Data.Hotkey("KEY_DELETE"));
        Assert.Null(Data.Hotkey("MinimapTerrain"));
        Assert.Null(Data.Hotkey("ACCEPT"));
        Assert.Null(Data.Hotkey("KEY_HOTKEYS"));
        Assert.Null(Data.Hotkey("PLAYER"));
        Assert.Equal("480", Data.Constant("DayLength"));
        Assert.Equal("24", Data.Constant("DayHours"));
    }

    // probe destructables: the names GetDestructableName read, and the pathing textures whose sizes put each type on
    // its grid
    [InstallFact]
    public void Destructables_read_their_names_and_pathing_textures()
    {
        string Name(string id) => Data.EditorString(Data.Object(ObjectKind.Destructable, Id(id))!.Text(Id("bnam"))!)!;

        Assert.Equal(["Summer Tree Wall", "Barrel", "Long Stone Bridge", "Shimmering Portal"], new[] { "LTlt", "LTbr", "YT16", "OTsp" }.Select(Name));
        Assert.Equal((4, 4), Data.PathingSize(Data.Object(ObjectKind.Destructable, Id("LTlt"))!.Text(Id("bptx"))!));
        Assert.Equal((32, 18), Data.PathingSize(@"PathTextures\CityBridgeLarge0.tga"));
        Assert.Equal((8, 2), Data.PathingSize(@"PathTextures\8x2Default.tga"));
        Assert.Null(Data.Object(ObjectKind.Destructable, Id("OTsp"))!.Text(Id("bptx")));
    }

    // docs/upstream.md: War3Net refuses (1)SilverpineSprint's war3map.doo for a doodad state 14, which is read here
    // (decisions D258)
    [InstallFact]
    public void SilverpineSprints_doodads_are_read() =>
        Installed("War3.w3mod:Maps/ForsakenKingdom/Scenario/(1)SilverpineSprint.w3x", map =>
        {
            Assert.Empty(map.Unread);
            Assert.Contains(map.Files.Doodads!.Doodads, doodad => (int)doodad.State == 14);
        });

    // docs/upstream.md: War3Net refuses (10)DustwallowKeys' war3map.w3e for a cliff type 0, which is read here (decisions
    // D270), so the melee map has its ground
    [InstallFact]
    public void DustwallowKeys_terrain_is_read() =>
        Installed("War3.w3mod:Maps/(10)DustwallowKeys.w3m", map =>
        {
            Assert.Empty(map.Unread);
            Assert.Contains(map.Files.Environment!.CliffTypes, type => (int)type == 0);
            Assert.Equal((map.Files.Environment.Width + 1) * (map.Files.Environment.Height + 1), (uint)map.Files.Environment.TerrainTiles.Count);
        });

    static void Installed(string name, Action<MapArchive> check)
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".w3x");
        File.WriteAllBytes(path, storage.Read(name)!);
        try { check(MapArchive.Read(path)); }
        finally { File.Delete(path); }
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

    // probes data-sets-melee and units: GetObjectName names units, items, abilities and upgrades, also one the set leaves
    // out (fapo, ckdk and AA12 under Melee with The Frozen Throne), and nothing else (D229)
    [InstallFact]
    public void A_name_is_read_also_for_an_object_the_set_leaves_out()
    {
        var info = MapFactory.Info(32, 32);
        info.MapFlags = MapFlags.MeleeMap;
        var data = new GameData(StandardData.Installed(GameInstall.Require(), info));

        Assert.Null(data.Object(ObjectKind.Unit, Id("fapo")));
        Assert.Equal(["Footman", "Apothecary", "Potion of Healing", "|cffffff00Dungeon Key|r", "Holy Light", "Item Hero Stat Bonus", "Iron Forged Swords", null, null],
            new[] { "hfoo", "fapo", "phea", "ckdk", "AHhb", "AA12", "Rhme", "Bdef", "LTlt" }.Select(id => data.Name(Id(id))));
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
