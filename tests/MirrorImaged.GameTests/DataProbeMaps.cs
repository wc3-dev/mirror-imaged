using War3Net.Build;
using War3Net.Build.Common;
using War3Net.Build.Info;
using War3Net.Build.Object;
using War3Net.Build.Script;
using War3Net.Common.Extensions;

namespace MirrorImaged.GameTests;

// The maps of W2-1's probes (docs/facts.md, "The game's data"), each the probe map changed (ProbeComparison.Change).
public static class DataProbeMaps
{
    /// <summary>The probe map (info format 28, which has no data version, and the Default data set) with the melee flag.</summary>
    public static void Melee(Map map) => map.Info!.MapFlags |= MapFlags.MeleeMap;

    /// <summary>The probe map with the melee flag in info format 31, as World Editor 2.0 writes it, The Frozen Throne's data.</summary>
    public static void FrozenThroneMelee(Map map)
    {
        Melee(map);
        var info = map.Info!;
        info.FormatVersion = MapInfoFormatVersion.v31;
        info.GameDataVersion = GameDataVersion.TFT;
        info.Players[0].EnemyLowPriorityFlags = new Bitmask32(0);
        info.Players[0].EnemyHighPriorityFlags = new Bitmask32(0);
    }

    /// <summary>
    /// The data and tags probes' map: slot 0 named <c>TRIGSTR_001</c> and a computer slot 2 named <c>TRIGSTR_002</c> in
    /// <c>war3map.w3i</c>, the computer seated in <c>config</c>, four strings in <c>war3map.wts</c>, hfoo renamed
    /// <c>TRIGSTR_003</c>, hkni renamed in the object data and again in the skin file, h000 made from hfoo, Holy Light's
    /// level 1 healing 250, Rhme given 5 levels and R000 made from Rhme. Each object has one set flag, 0, as World
    /// Editor writes it (the facts: a run without, whose ids were also reversed, closed the game).
    /// </summary>
    public static void Data(Map map)
    {
        var info = map.Info!;
        info.Players[0].Name = "TRIGSTR_001";
        info.Players.Add(new PlayerData
        {
            Id = 2, Controller = PlayerController.Computer, Race = PlayerRace.Orc, Name = "TRIGSTR_002",
            AllyLowPriorityFlags = new Bitmask32(0), AllyHighPriorityFlags = new Bitmask32(0),
            EnemyLowPriorityFlags = new Bitmask32(0), EnemyHighPriorityFlags = new Bitmask32(0),
        });
        info.Forces[0].Players = new Bitmask32(0b101);
        // config seats the computer as World Editor's InitCustomPlayerSlots does: the lobby takes config's slots (facts).
        map.Script = map.Script!.Replace("SetPlayers(1)", "SetPlayers(2)").Replace("    InitGenericPlayerSlots()", """
                DefineStartLocation(1, 0.0, 0.0)
                SetPlayerStartLocation(Player(2), 1)
                SetPlayerColor(Player(2), ConvertPlayerColor(2))
                SetPlayerRacePreference(Player(2), RACE_PREF_ORC)
                SetPlayerRaceSelectable(Player(2), false)
                SetPlayerController(Player(2), MAP_CONTROL_COMPUTER)
                InitGenericPlayerSlots()
            """);
        map.TriggerStrings = new TriggerStrings
        {
            Strings = { String(1, "Probe Alpha"), String(2, "The Horde"), String(3, "Probe Footman"), String(4, "Fourth string") },
        };
        map.UnitObjectData = new UnitObjectData(ObjectDataFormatVersion.v3)
        {
            BaseUnits = { Named("hfoo", "TRIGSTR_003"), Named("hkni", "Main Knight") },
            NewUnits = { new SimpleObjectModification { OldId = Id("hfoo"), NewId = Id("h000"), Unk = { 0 } } },
        };
        map.UnitSkinObjectData = new UnitObjectData(ObjectDataFormatVersion.v3) { BaseUnits = { Named("hkni", "Skin Knight") } };
        map.AbilityObjectData = new AbilityObjectData(ObjectDataFormatVersion.v3)
        {
            BaseAbilities =
            {
                new LevelObjectModification
                {
                    OldId = Id("AHhb"),
                    Unk = { 0 },
                    Modifications = { new LevelObjectDataModification { Id = Id("Hhb1"), Type = ObjectDataType.Unreal, Value = 250f, Level = 1, Pointer = 1 } },
                },
            },
        };
        map.UpgradeObjectData = new UpgradeObjectData(ObjectDataFormatVersion.v3)
        {
            BaseUpgrades =
            {
                new LevelObjectModification
                {
                    OldId = Id("Rhme"),
                    Unk = { 0 },
                    Modifications = { new LevelObjectDataModification { Id = Id("glvl"), Type = ObjectDataType.Int, Value = 5 } },
                },
            },
            NewUpgrades = { new LevelObjectModification { OldId = Id("Rhme"), NewId = Id("R000"), Unk = { 0 } } },
        };
    }

    static TriggerString String(uint key, string value) => new() { Key = key, Value = value };

    static SimpleObjectModification Named(string id, string name) => new()
    {
        OldId = Id(id),
        Unk = { 0 },
        Modifications = { new SimpleObjectDataModification { Id = Id("unam"), Type = ObjectDataType.String, Value = name } },
    };

    static int Id(string id) => id.FromRawcode();
}
