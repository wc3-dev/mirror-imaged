namespace MirrorImaged.Natives;

public partial class group
{
    internal readonly List<unit> Units = []; // in the order made (decisions D271); a unit gone stays until cleared
}

public partial class unit
{
    internal (int X, int Y) Square; // its square of the game's grid, for the order of the enumerations by place
    internal long Entered; // when it entered that square
}

// Groups and the unit enumerations, as the probes units-groups and units-order measure them (docs/facts.md, "Groups").
// A group holds its units in an order of their own whatever order they were added in: the game's is the order made
// while its memory is fresh, and otherwise its memory's (decisions D271); here always the order made.
public sealed partial class GameNatives
{
    const string GroupsProbe = "facts.md, Groups: probes units-groups and units-order (11-08)";

    // The game's units by handle id, the order made; one leaves once gone
    readonly SortedDictionary<int, unit> _units = [];
    unit? _enumUnit, _filterUnit;
    long _entered;

    /// <summary>A new, empty group.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public group? CreateGroup() => Make(new group());

    [Native(Level.Exact, Fact = GroupsProbe)]
    public void DestroyGroup(group? whichGroup)
    {
        if (whichGroup is not null) _game.Remove(whichGroup.Key);
    }

    /// <summary>True when the unit wasn't in the group; false for one already in it, nil and no group.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public bool GroupAddUnit(group? whichGroup, unit? whichUnit)
    {
        if (whichGroup is null || whichUnit is null) return false;
        var at = Place(whichGroup, whichUnit);
        if (at >= 0) return false;
        whichGroup.Units.Insert(~at, whichUnit);
        return true;
    }

    /// <summary>True when the unit was in the group.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public bool GroupRemoveUnit(group? whichGroup, unit? whichUnit)
    {
        if (whichGroup is null || whichUnit is null) return false;
        var at = Place(whichGroup, whichUnit);
        if (at < 0) return false;
        whichGroup.Units.RemoveAt(at);
        return true;
    }

    // Where the unit is in the group by its handle id, or the complement of where it would go
    static int Place(group g, unit u) => g.Units.BinarySearch(u, ById);

    // Ids are given in the order made and never again (decisions D111)

    static readonly Comparer<unit> ById = Comparer<unit>.Create((a, b) => a.Id.CompareTo(b.Id));

    /// <summary>Empties the group, of the units gone too.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupClear(group? whichGroup) => whichGroup?.Units.Clear();

    [Native(Level.Exact, Fact = GroupsProbe)]
    public bool IsUnitInGroup(unit? whichUnit, group? whichGroup) => whichGroup is not null && whichUnit is not null && Place(whichGroup, whichUnit) >= 0;

    /// <summary>How many units the group holds, a unit gone among them: removed units stay until the group is cleared.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public int BlzGroupGetSize(group? whichGroup) => whichGroup?.Units.Count ?? 0;

    /// <summary>
    /// The unit at the index, the group's units in the order made; nil past the ends, and for a unit gone. Close: the
    /// game's order is its memory's once units have been removed and others made (decisions D271).
    /// </summary>
    [Native(Level.Close)]
    public unit? BlzGroupUnitAt(group? whichGroup, int index) =>
        whichGroup is not null && index >= 0 && index < whichGroup.Units.Count ? Live(whichGroup.Units[index]) : null;

    /// <summary>The unit at index 0: nil while the group holds a unit gone there, though it holds others. Close as BlzGroupUnitAt.</summary>
    [Native(Level.Close)]
    public unit? FirstOfGroup(group? whichGroup) => BlzGroupUnitAt(whichGroup, 0);

    unit? Live(unit u) => _game.Handle(u.Key) is null ? null : u;

    /// <summary>
    /// Runs the callback for each unit the group holds as it starts, in its order, passing over units gone, with
    /// GetEnumUnit the unit; then lets go of it. A unit the callback takes out of the group is still visited, one it adds
    /// isn't. Close as BlzGroupUnitAt, for the order.
    /// </summary>
    [Native(Level.Close)]
    public void ForGroup(group? whichGroup, Action? callback)
    {
        var outer = _enumUnit;
        try
        {
            foreach (var u in whichGroup?.Units.ToArray() ?? [])
            {
                if (Live(u) is null) continue;
                _enumUnit = u;
                callback?.Invoke();
            }
        }
        finally
        {
            _enumUnit = outer;
            Release(callback);
        }
    }

    /// <summary>The unit ForGroup is running for; nil outside it, and in an enumeration's filter.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public unit? GetEnumUnit() => _enumUnit;

    /// <summary>The unit an enumeration's filter, or a region event's, is called for; nil outside one.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public unit? GetFilterUnit() => _filterUnit;

    /// <summary>Adds the first group's units to the second; answers how many weren't there.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public int BlzGroupAddGroupFast(group? whichGroup, group? addGroup) =>
        whichGroup is null || addGroup is null ? 0 : whichGroup.Units.ToArray().Count(u => GroupAddUnit(addGroup, u));

    /// <summary>Takes the first group's units out of the second; answers how many were there.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public int BlzGroupRemoveGroupFast(group? whichGroup, group? removeGroup) =>
        whichGroup is null || removeGroup is null ? 0 : whichGroup.Units.ToArray().Count(u => GroupRemoveUnit(removeGroup, u));

    /// <summary>
    /// The units whose place is within the radius of (x, y), the edge in, their size left out; a hidden unit and one
    /// removed in this step aren't found, a dead one is. Each is offered to the filter in the order of the game's grid
    /// (squares of 256, rows from the south, west to east, the last to enter a square first). The group is emptied
    /// first.
    /// </summary>
    [Native(Level.Exact, Fact = GroupsProbe + "; game-facts.md, Groups and performance: tests unit origins and clears its group")]
    public void GroupEnumUnitsInRange(group? whichGroup, float x, float y, float radius, boolexpr? filter) =>
        Enumerate(whichGroup, ByPlace(u => radius >= 0 && Distance2(u.X - x, u.Y - y) <= radius * radius), filter);

    /// <summary>As GroupEnumUnitsInRange: the count is passed over, as in the game.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupEnumUnitsInRangeCounted(group? whichGroup, float x, float y, float radius, boolexpr? filter, int countLimit) =>
        GroupEnumUnitsInRange(whichGroup, x, y, radius, filter);

    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupEnumUnitsInRangeOfLoc(group? whichGroup, location? whichLocation, float radius, boolexpr? filter)
    {
        if (whichLocation is not null) GroupEnumUnitsInRange(whichGroup, whichLocation.X, whichLocation.Y, radius, filter);
    }

    /// <summary>As GroupEnumUnitsInRangeOfLoc: the count is passed over, as in the game.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupEnumUnitsInRangeOfLocCounted(group? whichGroup, location? whichLocation, float radius, boolexpr? filter, int countLimit) =>
        GroupEnumUnitsInRangeOfLoc(whichGroup, whichLocation, radius, filter);

    /// <summary>
    /// The units whose 32 cell lies in the rect, its west and south edges past the rect's (as EnumDestructablesInRect
    /// finds destructables), as GroupEnumUnitsInRange takes and orders them. A nil rect leaves the group as it is.
    /// </summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupEnumUnitsInRect(group? whichGroup, rect? r, boolexpr? filter)
    {
        if (r is not null) Enumerate(whichGroup, ByPlace(u => Inside(r, u.X, u.Y)), filter);
    }

    /// <summary>As GroupEnumUnitsInRect: the count is passed over, as in the game.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupEnumUnitsInRectCounted(group? whichGroup, rect? r, boolexpr? filter, int countLimit) => GroupEnumUnitsInRect(whichGroup, r, filter);

    /// <summary>
    /// The player's units, hidden, dead and removed in this step too, offered to the filter newest first.
    /// </summary>
    [Native(Level.Exact, Fact = GroupsProbe + "; game-facts.md, Units: finds a unit hidden in a rect (10-26)")]
    public void GroupEnumUnitsOfPlayer(group? whichGroup, player? whichPlayer, boolexpr? filter)
    {
        if (whichPlayer is not null) Enumerate(whichGroup, Newest(u => u.Owner == whichPlayer), filter);
    }

    /// <summary>
    /// Every player's units of the type named as UnitId names it, in any case ("footman", not "hfoo"), taken and
    /// ordered as GroupEnumUnitsOfPlayer takes them.
    /// </summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupEnumUnitsOfType(group? whichGroup, string? unitname, boolexpr? filter)
    {
        var type = UnitId(unitname);
        Enumerate(whichGroup, Newest(u => u.Type.Id == type), filter);
    }

    /// <summary>As GroupEnumUnitsOfType: the count is passed over, as in the game.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public void GroupEnumUnitsOfTypeCounted(group? whichGroup, string? unitname, boolexpr? filter, int countLimit) =>
        GroupEnumUnitsOfType(whichGroup, unitname, filter);

    static float Distance2(float dx, float dy) => dx * dx + dy * dy;

    // By place: neither hidden nor removed, in the grid's order
    IEnumerable<unit> ByPlace(Func<unit, bool> inside) =>
        _units.Values.Where(u => !u.Hidden && !u.Removed && inside(u)).OrderBy(u => u.Square.Y).ThenBy(u => u.Square.X).ThenByDescending(u => u.Entered);

    IEnumerable<unit> Newest(Func<unit, bool> take) => _units.Values.Reverse().Where(take);

    // Empties the group and adds the units the filter passes, each offered with GetFilterUnit set (probe units-groups)
    void Enumerate(group? g, IEnumerable<unit> units, boolexpr? filter)
    {
        if (g is null) return;
        g.Units.Clear();
        var outer = _filterUnit;
        try
        {
            foreach (var u in units.ToArray())
            {
                _filterUnit = u;
                if (filter is null || Holds(filter, alone: true)) GroupAddUnit(g, u);
            }
        }
        finally { _filterUnit = outer; }
    }
}
