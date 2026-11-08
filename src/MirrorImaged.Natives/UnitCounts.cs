using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class player
{
    internal readonly Dictionary<int, int> UnitCounts = []; // its living units by type, a removed one until it is gone
}

// Units by name, the player's unit counts and food, as the probe units-more measures them (docs/facts.md, "Units").
public sealed partial class GameNatives
{
    const int FoodCap = 4, FoodUsedState = 5; // PLAYER_STATE_RESOURCE_FOOD_*
    static readonly int OwnName = Code("name"), UpgradesTo = Code("uupt"); // name: unitui.slk's, which the data keeps as a field of its own
    Dictionary<string, int>? _unitIds;

    // A living unit counts, and its food, from when it is made until it dies or is gone, for its owner: +1 as it starts,
    // -1 as it stops. Each food change sets the player's state, firing its events inside the call (probe units-more).
    void Counting(unit u, int sign)
    {
        var (p, type) = (u.Owner, u.Type.Id);
        p.UnitCounts[type] = p.UnitCounts.GetValueOrDefault(type) + sign;
        if (u.UsesFood) Food(p, FoodUsedState, sign * u.Type.Integer(FoodUsed));
        Food(p, FoodCap, sign * u.Type.Integer(FoodMade));
    }

    void Food(player p, int state, int change)
    {
        if (change != 0) SetPlayerState(p, Convert<playerstate>(state), p.States[state] + change);
    }

    /// <summary>
    /// The unit type with the name, in any case (footman, Footman), as unitui.slk names it; two types of one name give
    /// the first in the tables (paladin is Hpal, not Hssa); 0 for an id or anything else (probe units-more).
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public int UnitId(string? unitIdString)
    {
        if (_unitIds is null)
        {
            _unitIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (var type in Data.Objects(ObjectKind.Unit))
                if (type.Text(OwnName) is { } name) _unitIds.TryAdd(name, type.Id);
        }
        return unitIdString is null ? 0 : _unitIds.GetValueOrDefault(unitIdString);
    }

    /// <summary>
    /// The unit type's name (footman); "" for an id that is no unit's, an item's or an ability's (probe units-more). Not
    /// measured: a map's own unit type reads its base type's name, and a JASS map gets null.
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public string? UnitId2String(int unitId) => Data.Object(ObjectKind.Unit, unitId)?.Text(OwnName) ?? (Jass ? null : "");

    /// <summary>CreateUnit of the type UnitId names; nil for a name no type has, an id too (probe units-more).</summary>
    [Native(Level.Close)]
    public unit? CreateUnitByName(player? whichPlayer, string? unitname, float x, float y, float face) => CreateUnit(whichPlayer, UnitId(unitname), x, y, face);

    /// <summary>
    /// The player's living units but structures, hidden and paused ones too, and one removed until it is gone (probe
    /// units-more). There is no construction yet (wave 3), so <paramref name="includeIncomplete"/> changes nothing.
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public int GetPlayerUnitCount(player? whichPlayer, bool includeIncomplete) => Count(whichPlayer, structures: false);

    /// <summary>The player's living structures, as GetPlayerUnitCount counts units (probe units-more).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public int GetPlayerStructureCount(player? whichPlayer, bool includeIncomplete) => Count(whichPlayer, structures: true);

    int Count(player? p, bool structures) =>
        p?.UnitCounts.Where(c => (Data.Object(ObjectKind.Unit, c.Key)?.Integer(IsBuilding) != 0) == structures).Sum(c => c.Value) ?? 0;

    /// <summary>
    /// The player's living units of the type UnitId names, with the types it upgrades to when asked (townhall counts a
    /// keep and a castle), as GetPlayerUnitCount counts them (probe units-more).
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public int GetPlayerTypedUnitCount(player? whichPlayer, string? unitName, bool includeIncomplete, bool includeUpgrades) =>
        Typed(whichPlayer, UnitId(unitName), includeUpgrades);

    // The units of a type, and with its upgrades the types it upgrades to, theirs too, each once
    int Typed(player? p, int type, bool upgrades)
    {
        if (p is null || type == 0) return 0;
        var types = new List<int> { type };
        for (var i = 0; upgrades && i < types.Count; i++)
            foreach (var next in (Data.Object(ObjectKind.Unit, types[i])?.Text(UpgradesTo) ?? "").Split(',', StringSplitOptions.TrimEntries))
                if (next.Length == 4 && !types.Contains(Code(next))) types.Add(Code(next));
        return types.Sum(t => p.UnitCounts.GetValueOrDefault(t));
    }

    /// <summary>
    /// Whether the unit uses food: off, its food goes back to its owner, and on again it uses it; the food it makes
    /// stays (probe units-more).
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public void SetUnitUseFood(unit? whichUnit, bool useFood)
    {
        if (whichUnit is null || whichUnit.UsesFood == useFood) return;
        whichUnit.UsesFood = useFood;
        if (!whichUnit.Dead) Food(whichUnit.Owner, FoodUsedState, (useFood ? 1 : -1) * whichUnit.Type.Integer(FoodUsed));
    }

    [Native(Level.Exact, Fact = MoreProbe)]
    public int GetFoodUsed(int unitId) => Data.Object(ObjectKind.Unit, unitId)?.Integer(FoodUsed) ?? 0;

    [Native(Level.Exact, Fact = MoreProbe)]
    public int GetFoodMade(int unitId) => Data.Object(ObjectKind.Unit, unitId)?.Integer(FoodMade) ?? 0;
}
