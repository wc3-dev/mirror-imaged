using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal bool IgnoresAlarm;
    internal int? Gold; // what SetResourceAmount and AddResourceAmount left
    internal HashSet<player>? Rescuers;
    internal float RescueRange;
}

// The rest of the unit natives, as the probes units-more, units-more-2, units-types and units-gold measure them
// (docs/facts.md, "Units"): kinds of a type and added kinds, sameness, illusions, selection, the alarm, rescue, a gold
// mine's gold, and what only shows.
public sealed partial class GameNatives
{
    const string TypesProbe = "facts.md, Units: probe units-types (11-08)";
    const string GoldProbe = "facts.md, Units: probe units-gold (11-08)";
    static readonly int[] Mines = [Code("Agld"), Code("Abgm"), Code("Aegm")]; // a gold mine's, a haunted one's, an entangled one's
    static readonly int MaxGold = Code("Gld1");

    /// <summary>Whether the two are one unit; nil and nil are (probe units-more).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool IsUnit(unit? whichUnit, unit? whichSpecifiedUnit) => whichUnit == whichSpecifiedUnit;

    /// <summary>
    /// The kinds of a unit type, as IsUnitType reads a unit of it, but never dead or ground (a footman isn't, a gryphon
    /// rider flies); false for an id that is no unit's (probe units-types).
    /// </summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public bool IsUnitIdType(int unitId, unittype? whichUnitType) =>
        whichUnitType is not null && Data.Object(ObjectKind.Unit, unitId) is { } type && Is(type, null, whichUnitType.Id);

    /// <summary>
    /// Adds a kind of the classification (giant, summoned, undead, mechanical, peon, sapper, townhall, ancient, tauren)
    /// to the one unit, and its classification field, true; any other kind changes nothing, false (probe units-types).
    /// </summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public bool UnitAddType(unit? whichUnit, unittype? whichUnitType) => Classify(whichUnit, whichUnitType, true);

    /// <summary>Takes a kind of the classification off the unit, its type's too (a barracks' mechanical), as UnitAddType adds one (probe units-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public bool UnitRemoveType(unit? whichUnit, unittype? whichUnitType) => Classify(whichUnit, whichUnitType, false);

    bool Classify(unit? u, unittype? kind, bool add)
    {
        if (u is null || kind is null || !Classes.TryGetValue(kind.Id, out var name)) return false;
        var classes = BlzGetUnitIntegerField(u, Convert<unitintegerfield>(Classification));
        u.Fields[Classification] = add ? classes | Categories[name] : classes & ~Categories[name];
        return true;
    }

    /// <summary>
    /// False, as for every unit the probe made (probe units-more). Close: the abilities and items that make illusions,
    /// which read true (game-facts, Units), come with wave 4.
    /// </summary>
    [Native(Level.Close)]
    public bool IsUnitIllusion(unit? whichUnit) => false;

    /// <summary>
    /// Whether it can be selected: alive, shown and not removed, a paused unit, a hero and a building too (probe
    /// units-more). Not measured: a unit with Locust, which W2-7 is to probe.
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool BlzIsUnitSelectable(unit? whichUnit) => whichUnit is { Dead: false, Hidden: false, Removed: false };

    /// <summary>A structure keeps the flag and answers true; any other unit false, keeping nothing (probe units-more-2).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool UnitIgnoreAlarm(unit? whichUnit, bool flag)
    {
        if (whichUnit is null || !IsBuildingNow(whichUnit)) return false;
        whichUnit.IgnoresAlarm = flag;
        return true;
    }

    /// <summary>What UnitIgnoreAlarm kept; false at first (probes units-more and units-more-2).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool UnitIgnoreAlarmToggled(unit? whichUnit) => whichUnit?.IgnoresAlarm ?? false;

    /// <summary>Does nothing yet: creep camps guarding and going back are the AI's (wave 7), which will keep and read it.</summary>
    [Native(Level.Close)]
    public void SetUnitCreepGuard(unit? whichUnit, bool creepGuard)
    {
    }

    [Native(Level.Recorded, Reason = "Shows the unit's other icon; no native reads it back")]
    public void UnitSetUsesAltIcon(unit? whichUnit, bool flag)
    {
    }

    [Native(Level.Recorded, Reason = "Shows or hides the unit's team glow; no native reads it back")]
    public void BlzShowUnitTeamGlow(unit? whichUnit, bool show)
    {
    }

    [Native(Level.Recorded, Reason = "Gives no unit away, fires nothing and changes nothing a native reads (probe units-more); what the game keeps it for isn't known")]
    public void SetPlayerUnitsOwner(player? whichPlayer, int newOwner)
    {
    }

    /// <summary>
    /// Kept on the unit. Close: the game rescues it once a unit of a player it is rescuable by stands within its range,
    /// firing the rescue events on its owner's and its own registrations without changing its owner (probe units-more: a
    /// footman made 100 away, 0.5 s later), which needs the units in range (W2-6); here no unit is rescued.
    /// </summary>
    [Native(Level.Close)]
    public void SetUnitRescuable(unit? whichUnit, player? byWhichPlayer, bool flag)
    {
        if (whichUnit is null || byWhichPlayer is null) return;
        whichUnit.Rescuers ??= [];
        if (flag) whichUnit.Rescuers.Add(byWhichPlayer);
        else whichUnit.Rescuers.Remove(byWhichPlayer);
    }

    /// <summary>Kept on the unit; Close as SetUnitRescuable (probe units-more: 50 kept a rescuer 100 away out).</summary>
    [Native(Level.Close)]
    public void SetUnitRescueRange(unit? whichUnit, float range)
    {
        if (whichUnit is not null) whichUnit.RescueRange = range;
    }

    /// <summary>Nil outside a rescue (probe units-more). Close: no unit is rescued here yet (SetUnitRescuable).</summary>
    [Native(Level.Close)]
    public unit? GetRescuer() => null;

    /// <summary>
    /// A gold mine's gold: its Max Gold (its gold mine ability's Gld1, 12500) until set; 0 for any other unit, a
    /// haunted or entangled mine too (probe units-gold). A dead mine keeps it.
    /// </summary>
    [Native(Level.Exact, Fact = GoldProbe)]
    public int GetResourceAmount(unit? whichUnit) =>
        whichUnit is null ? 0 : whichUnit.Gold ?? Abilities(whichUnit).FirstOrDefault(a => a?.BaseId == Mines[0])?.Integer(MaxGold, 1) ?? 0;

    /// <summary>
    /// Sets a mine's gold, a haunted or entangled one's too, as it is, 0 and below too, with no death; any other unit
    /// keeps none (probe units-gold).
    /// </summary>
    [Native(Level.Exact, Fact = GoldProbe)]
    public void SetResourceAmount(unit? whichUnit, int amount)
    {
        if (whichUnit is not null && Abilities(whichUnit).Any(a => a is not null && Mines.Contains(a.BaseId))) whichUnit.Gold = amount;
    }

    /// <summary>Adds to a mine's gold as SetResourceAmount sets it, in 32 bits that wrap (probe units-gold).</summary>
    [Native(Level.Exact, Fact = GoldProbe)]
    public void AddResourceAmount(unit? whichUnit, int amount) => SetResourceAmount(whichUnit, unchecked(GetResourceAmount(whichUnit) + amount));
}
