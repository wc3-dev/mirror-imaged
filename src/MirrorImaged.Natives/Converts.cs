namespace MirrorImaged.Natives;

// A Convert native gives the handle standing for a value of its type: made once per type and value, so the same value
// gives the same handle and Lua's == holds, as the facts measure for player colours, races and the damage natives'
// types. Its id is the value, and it takes none of the game's ids (decisions D51). Not measured: the others, and a
// value common.j doesn't name.
public sealed partial class GameNatives
{
    const string ColorAndRace = "game-facts.md, Version and launching: GetPlayerColor compares equal to ConvertPlayerColor(n), and GetPlayerRace to RACE_*, with Lua's == (10-20)";
    const string DamageTypes = "game-facts.md, C# through CSharp.lua: every attack, damage and weapon type dealt from code reads back the same through Convert*Type handles made once and compared with == (Moonwell.Damage gate)";

    readonly Dictionary<(Type, int), handle> _converted = [];

    /// <summary>The handle of <paramref name="type"/> with <paramref name="id"/>: a converted one, else the game's; null once it is removed.</summary>
    public handle? Handle(Type type, int id) => _converted.GetValueOrDefault((type, id)) ?? _game.Handle(id) as handle;

    T Convert<T>(int value) where T : handle, new()
    {
        if (!_converted.TryGetValue((typeof(T), value), out var made))
            _converted.Add((typeof(T), value), made = new T { Id = value });
        return (T)made;
    }

    [Native(Level.Close)] public aidifficulty? ConvertAIDifficulty(int i) => Convert<aidifficulty>(i);
    [Native(Level.Close)] public abilitybooleanfield? ConvertAbilityBooleanField(int i) => Convert<abilitybooleanfield>(i);
    [Native(Level.Close)] public abilitybooleanlevelarrayfield? ConvertAbilityBooleanLevelArrayField(int i) => Convert<abilitybooleanlevelarrayfield>(i);
    [Native(Level.Close)] public abilitybooleanlevelfield? ConvertAbilityBooleanLevelField(int i) => Convert<abilitybooleanlevelfield>(i);
    [Native(Level.Close)] public abilityintegerfield? ConvertAbilityIntegerField(int i) => Convert<abilityintegerfield>(i);
    [Native(Level.Close)] public abilityintegerlevelarrayfield? ConvertAbilityIntegerLevelArrayField(int i) => Convert<abilityintegerlevelarrayfield>(i);
    [Native(Level.Close)] public abilityintegerlevelfield? ConvertAbilityIntegerLevelField(int i) => Convert<abilityintegerlevelfield>(i);
    [Native(Level.Close)] public abilityrealfield? ConvertAbilityRealField(int i) => Convert<abilityrealfield>(i);
    [Native(Level.Close)] public abilityreallevelarrayfield? ConvertAbilityRealLevelArrayField(int i) => Convert<abilityreallevelarrayfield>(i);
    [Native(Level.Close)] public abilityreallevelfield? ConvertAbilityRealLevelField(int i) => Convert<abilityreallevelfield>(i);
    [Native(Level.Close)] public abilitystringfield? ConvertAbilityStringField(int i) => Convert<abilitystringfield>(i);
    [Native(Level.Close)] public abilitystringlevelarrayfield? ConvertAbilityStringLevelArrayField(int i) => Convert<abilitystringlevelarrayfield>(i);
    [Native(Level.Close)] public abilitystringlevelfield? ConvertAbilityStringLevelField(int i) => Convert<abilitystringlevelfield>(i);
    [Native(Level.Close)] public alliancetype? ConvertAllianceType(int i) => Convert<alliancetype>(i);
    [Native(Level.Close)] public animtype? ConvertAnimType(int i) => Convert<animtype>(i);
    [Native(Level.Close)] public armortype? ConvertArmorType(int i) => Convert<armortype>(i);
    [Native(Level.Exact, Fact = DamageTypes)] public attacktype? ConvertAttackType(int i) => Convert<attacktype>(i);
    [Native(Level.Close)] public blendmode? ConvertBlendMode(int i) => Convert<blendmode>(i);
    [Native(Level.Close)] public camerafield? ConvertCameraField(int i) => Convert<camerafield>(i);
    [Native(Level.Exact, Fact = DamageTypes)] public damagetype? ConvertDamageType(int i) => Convert<damagetype>(i);
    [Native(Level.Close)] public defensetype? ConvertDefenseType(int i) => Convert<defensetype>(i);
    [Native(Level.Close)] public dialogevent? ConvertDialogEvent(int i) => Convert<dialogevent>(i);
    [Native(Level.Close)] public effecttype? ConvertEffectType(int i) => Convert<effecttype>(i);
    [Native(Level.Close)] public equipmentType? ConvertEquipmentType(int i) => Convert<equipmentType>(i);
    [Native(Level.Close)] public fgamestate? ConvertFGameState(int i) => Convert<fgamestate>(i);
    [Native(Level.Close)] public fogstate? ConvertFogState(int i) => Convert<fogstate>(i);
    [Native(Level.Close)] public fogstyle? ConvertFogStyle(int i) => Convert<fogstyle>(i);
    [Native(Level.Close)] public frameeventtype? ConvertFrameEventType(int i) => Convert<frameeventtype>(i);
    [Native(Level.Close)] public framepointtype? ConvertFramePointType(int i) => Convert<framepointtype>(i);
    [Native(Level.Close)] public gamedifficulty? ConvertGameDifficulty(int i) => Convert<gamedifficulty>(i);
    [Native(Level.Close)] public gameevent? ConvertGameEvent(int i) => Convert<gameevent>(i);
    [Native(Level.Close)] public gamespeed? ConvertGameSpeed(int i) => Convert<gamespeed>(i);
    [Native(Level.Close)] public gametype? ConvertGameType(int i) => Convert<gametype>(i);
    [Native(Level.Close)] public heroattribute? ConvertHeroAttribute(int i) => Convert<heroattribute>(i);
    [Native(Level.Close)] public igamestate? ConvertIGameState(int i) => Convert<igamestate>(i);
    [Native(Level.Close)] public itembooleanfield? ConvertItemBooleanField(int i) => Convert<itembooleanfield>(i);
    [Native(Level.Close)] public itemintegerfield? ConvertItemIntegerField(int i) => Convert<itemintegerfield>(i);
    [Native(Level.Close)] public itemrealfield? ConvertItemRealField(int i) => Convert<itemrealfield>(i);
    [Native(Level.Close)] public itemstringfield? ConvertItemStringField(int i) => Convert<itemstringfield>(i);
    [Native(Level.Close)] public itemTag? ConvertItemTag(int i) => Convert<itemTag>(i);
    [Native(Level.Close)] public itemtype? ConvertItemType(int i) => Convert<itemtype>(i);
    [Native(Level.Close)] public limitop? ConvertLimitOp(int i) => Convert<limitop>(i);
    [Native(Level.Close)] public loadoutslot? ConvertLoadoutSlot(int i) => Convert<loadoutslot>(i);
    [Native(Level.Close)] public mapcontrol? ConvertMapControl(int i) => Convert<mapcontrol>(i);
    [Native(Level.Close)] public mapdensity? ConvertMapDensity(int i) => Convert<mapdensity>(i);
    [Native(Level.Close)] public mapflag? ConvertMapFlag(int i) => Convert<mapflag>(i);
    [Native(Level.Close)] public mapsetting? ConvertMapSetting(int i) => Convert<mapsetting>(i);
    [Native(Level.Close)] public mapvisibility? ConvertMapVisibility(int i) => Convert<mapvisibility>(i);
    [Native(Level.Close)] public mousebuttontype? ConvertMouseButtonType(int i) => Convert<mousebuttontype>(i);
    [Native(Level.Close)] public movetype? ConvertMoveType(int i) => Convert<movetype>(i);
    [Native(Level.Close)] public originframetype? ConvertOriginFrameType(int i) => Convert<originframetype>(i);
    [Native(Level.Close)] public oskeytype? ConvertOsKeyType(int i) => Convert<oskeytype>(i);
    [Native(Level.Close)] public pathingflag? ConvertPathingFlag(int i) => Convert<pathingflag>(i);
    [Native(Level.Close)] public pathingtype? ConvertPathingType(int i) => Convert<pathingtype>(i);
    [Native(Level.Close)] public placement? ConvertPlacement(int i) => Convert<placement>(i);
    [Native(Level.Exact, Fact = ColorAndRace)] public playercolor? ConvertPlayerColor(int i) => Convert<playercolor>(i);
    [Native(Level.Close)] public playerevent? ConvertPlayerEvent(int i) => Convert<playerevent>(i);
    [Native(Level.Close)] public playergameresult? ConvertPlayerGameResult(int i) => Convert<playergameresult>(i);
    [Native(Level.Close)] public playerscore? ConvertPlayerScore(int i) => Convert<playerscore>(i);
    [Native(Level.Close)] public playerslotstate? ConvertPlayerSlotState(int i) => Convert<playerslotstate>(i);
    [Native(Level.Close)] public playerstate? ConvertPlayerState(int i) => Convert<playerstate>(i);
    [Native(Level.Close)] public playerunitevent? ConvertPlayerUnitEvent(int i) => Convert<playerunitevent>(i);
    [Native(Level.Exact, Fact = ColorAndRace)] public race? ConvertRace(int i) => Convert<race>(i);
    [Native(Level.Close)] public racepreference? ConvertRacePref(int i) => Convert<racepreference>(i);
    [Native(Level.Close)] public raritycontrol? ConvertRarityControl(int i) => Convert<raritycontrol>(i);
    [Native(Level.Close)] public regentype? ConvertRegenType(int i) => Convert<regentype>(i);
    [Native(Level.Close)] public soundtype? ConvertSoundType(int i) => Convert<soundtype>(i);
    [Native(Level.Close)] public startlocprio? ConvertStartLocPrio(int i) => Convert<startlocprio>(i);
    [Native(Level.Close)] public subanimtype? ConvertSubAnimType(int i) => Convert<subanimtype>(i);
    [Native(Level.Close)] public targetflag? ConvertTargetFlag(int i) => Convert<targetflag>(i);
    [Native(Level.Close)] public texmapflags? ConvertTexMapFlags(int i) => Convert<texmapflags>(i);
    [Native(Level.Close)] public textaligntype? ConvertTextAlignType(int i) => Convert<textaligntype>(i);
    [Native(Level.Close)] public unitbooleanfield? ConvertUnitBooleanField(int i) => Convert<unitbooleanfield>(i);
    [Native(Level.Close)] public unitcategory? ConvertUnitCategory(int i) => Convert<unitcategory>(i);
    [Native(Level.Close)] public unitevent? ConvertUnitEvent(int i) => Convert<unitevent>(i);
    [Native(Level.Close)] public unitintegerfield? ConvertUnitIntegerField(int i) => Convert<unitintegerfield>(i);
    [Native(Level.Close)] public unitrealfield? ConvertUnitRealField(int i) => Convert<unitrealfield>(i);
    [Native(Level.Close)] public unitstate? ConvertUnitState(int i) => Convert<unitstate>(i);
    [Native(Level.Close)] public unitstringfield? ConvertUnitStringField(int i) => Convert<unitstringfield>(i);
    [Native(Level.Close)] public unittype? ConvertUnitType(int i) => Convert<unittype>(i);
    [Native(Level.Close)] public unitweaponbooleanfield? ConvertUnitWeaponBooleanField(int i) => Convert<unitweaponbooleanfield>(i);
    [Native(Level.Close)] public unitweaponintegerfield? ConvertUnitWeaponIntegerField(int i) => Convert<unitweaponintegerfield>(i);
    [Native(Level.Close)] public unitweaponrealfield? ConvertUnitWeaponRealField(int i) => Convert<unitweaponrealfield>(i);
    [Native(Level.Close)] public unitweaponstringfield? ConvertUnitWeaponStringField(int i) => Convert<unitweaponstringfield>(i);
    [Native(Level.Close)] public version? ConvertVersion(int i) => Convert<version>(i);
    [Native(Level.Close)] public volumegroup? ConvertVolumeGroup(int i) => Convert<volumegroup>(i);
    [Native(Level.Exact, Fact = DamageTypes)] public weapontype? ConvertWeaponType(int i) => Convert<weapontype>(i);
    [Native(Level.Close)] public widgetevent? ConvertWidgetEvent(int i) => Convert<widgetevent>(i);
}
