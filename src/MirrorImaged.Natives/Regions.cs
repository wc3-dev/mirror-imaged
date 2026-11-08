namespace MirrorImaged.Natives;

public partial class region
{
    // RegionAddRect's, RegionClearRect's and the cell natives' blocks of whole 32 cells, in order, each added or cleared:
    // a cell is the region's when the last block holding it added it
    internal readonly List<(int West, int South, int East, int North, bool Added)> Blocks = [];

    internal bool Covers(float x, float y)
    {
        var (cx, cy) = (Cell(x), Cell(y));
        for (var i = Blocks.Count - 1; i >= 0; i--)
            if (Blocks[i] is var b && cx >= b.West && cx <= b.East && cy >= b.South && cy <= b.North) return b.Added;
        return false;
    }

    internal static int Cell(float at) => (int)MathF.Floor(at / 32);
}

// An enter or leave registration and the units it has to fire for, in the order they came
sealed record RegionEvent(region Region, bool Enter, boolexpr? Filter, @event Event)
{
    internal readonly List<unit> Queued = [];
}

// Regions, and their enter and leave events, as the probe units-regions and the facts' "Units" measure them
// (docs/facts.md, "Regions"): a region is whole 32 cells on multiples of 32 from 0, and a rect covers the cells its
// corners are in, a max edge on the grid taking the cell past it.
public sealed partial class GameNatives
{
    const string RegionsProbe = "facts.md, Regions: probe units-regions (11-08)";
    const int EnterRegion = 5, LeaveRegion = 6; // EVENT_GAME_ENTER_REGION, EVENT_GAME_LEAVE_REGION

    // shortcut: every move reads every registration's region, cell by cell over its blocks; index the regions by place
    // once movement (wave 3) moves units every step, if the benchmark (A10) needs it.
    readonly List<RegionEvent> _regionEvents = []; // in the order registered

    [Native(Level.Exact, Fact = RegionsProbe)]
    public region? CreateRegion() => Make(new region());

    /// <summary>Its events stop: a unit moving into its cells fires nothing.</summary>
    [Native(Level.Exact, Fact = RegionsProbe)]
    public void RemoveRegion(region? whichRegion)
    {
        if (whichRegion is null) return;
        _regionEvents.RemoveAll(e => e.Region == whichRegion);
        _game.Remove(whichRegion.Key);
    }

    /// <summary>The cells the rect's corners are in and those between: (100.5, 100.5)-(300.5, 300.5) covers 96 to 320.</summary>
    [Native(Level.Exact, Fact = RegionsProbe + "; game-facts.md, Units: regions are whole 32 cells (10-25, 10-26)")]
    public void RegionAddRect(region? whichRegion, rect? r) => Block(whichRegion, r, true);

    [Native(Level.Exact, Fact = RegionsProbe)]
    public void RegionClearRect(region? whichRegion, rect? r) => Block(whichRegion, r, false);

    [Native(Level.Exact, Fact = RegionsProbe + "; game-facts.md, Units: RegionAddCell(10, 10) covers 0 to 32 (10-25)")]
    public void RegionAddCell(region? whichRegion, float x, float y) => Block(whichRegion, x, y, x, y, true);

    [Native(Level.Exact, Fact = RegionsProbe)]
    public void RegionClearCell(region? whichRegion, float x, float y) => Block(whichRegion, x, y, x, y, false);

    [Native(Level.Exact, Fact = RegionsProbe)]
    public void RegionAddCellAtLoc(region? whichRegion, location? whichLocation)
    {
        if (whichLocation is not null) RegionAddCell(whichRegion, whichLocation.X, whichLocation.Y);
    }

    [Native(Level.Exact, Fact = RegionsProbe)]
    public void RegionClearCellAtLoc(region? whichRegion, location? whichLocation)
    {
        if (whichLocation is not null) RegionClearCell(whichRegion, whichLocation.X, whichLocation.Y);
    }

    void Block(region? whichRegion, rect? r, bool added)
    {
        if (r is not null) Block(whichRegion, r.MinX, r.MinY, r.MaxX, r.MaxY, added);
    }

    static void Block(region? whichRegion, float minX, float minY, float maxX, float maxY, bool added) =>
        whichRegion?.Blocks.Add((region.Cell(minX), region.Cell(minY), region.Cell(maxX), region.Cell(maxY), added));

    [Native(Level.Exact, Fact = RegionsProbe)]
    public bool IsPointInRegion(region? whichRegion, float x, float y) => whichRegion?.Covers(x, y) ?? false;

    [Native(Level.Exact, Fact = RegionsProbe)]
    public bool IsLocationInRegion(region? whichRegion, location? whichLocation) =>
        whichLocation is not null && IsPointInRegion(whichRegion, whichLocation.X, whichLocation.Y);

    /// <summary>
    /// Whether the unit's place is in the region's cells as they are now, whatever events fired: also for a dead unit,
    /// a unit removed in this step, and one under a cell added while it stood there.
    /// </summary>
    [Native(Level.Exact, Fact = RegionsProbe + "; game-facts.md, Units: IsUnitInRegion keeps answering true for a unit that died inside (10-26)")]
    public bool IsUnitInRegion(region? whichRegion, unit? whichUnit) => whichUnit is not null && IsPointInRegion(whichRegion, whichUnit.X, whichUnit.Y);

    /// <summary>
    /// Fires for each unit made in the region or moved into it by SetUnitX, SetUnitY or SetUnitPosition, dead, hidden
    /// or removed in that step too. The filter runs inside that call, with GetFilterUnit the unit, and a unit it fails
    /// is passed over. The trigger fires after the code that moved the unit, in the same step: each registration
    /// keeps the units it has to fire for, and fires them all, in turn, at its place among what falls due in the
    /// step, the place it got when its first unit came; each read as it is then. Nothing fires for a unit already in
    /// the region when it was made or registered, a cell added or cleared under a unit standing still, a death, or a
    /// removal.
    /// </summary>
    [Native(Level.Exact, Fact = RegionsProbe + "; game-facts.md, Units: enter and leave events (10-17, 10-25, 10-26)")]
    public @event? TriggerRegisterEnterRegion(trigger? whichTrigger, region? whichRegion, boolexpr? filter) =>
        RegisterRegion(whichTrigger, whichRegion, filter, true);

    /// <summary>As TriggerRegisterEnterRegion, for a unit moved out of the region.</summary>
    [Native(Level.Exact, Fact = RegionsProbe + "; game-facts.md, Units: enter and leave events (10-25)")]
    public @event? TriggerRegisterLeaveRegion(trigger? whichTrigger, region? whichRegion, boolexpr? filter) =>
        RegisterRegion(whichTrigger, whichRegion, filter, false);

    @event? RegisterRegion(trigger? t, region? r, boolexpr? filter, bool enter)
    {
        if (t is null || r is null) return null;
        var registered = Register(t);
        _regionEvents.Add(new(r, enter, filter, registered));
        return registered;
    }

    /// <summary>The region of an enter or leave event; nil outside one.</summary>
    [Native(Level.Exact, Fact = RegionsProbe)]
    public region? GetTriggeringRegion() => Current.Region;

    /// <summary>The unit of an enter event; nil in a leave event and outside one.</summary>
    [Native(Level.Exact, Fact = RegionsProbe)]
    public unit? GetEnteringUnit() => Current.Event.Id == EnterRegion ? Current.Unit?.Unit : null;

    /// <summary>The unit of a leave event; nil in an enter event and outside one.</summary>
    [Native(Level.Exact, Fact = RegionsProbe)]
    public unit? GetLeavingUnit() => Current.Event.Id == LeaveRegion ? Current.Unit?.Unit : null;

    // The unit moved from (fromX, fromY), or was made (made): the registrations whose region it entered or left, in the
    // order registered, take it if their filter passes
    void Crossed(unit u, float fromX, float fromY, bool made)
    {
        foreach (var e in _regionEvents.ToArray())
        {
            var (was, now) = (!made && e.Region.Covers(fromX, fromY), e.Region.Covers(u.X, u.Y));
            if (was == now || now != e.Enter || !Admits(e.Filter, u)) continue;
            e.Queued.Add(u);
            if (e.Queued.Count == 1) _game.At(_game.Step, () => FireRegion(e));
        }
    }

    bool Admits(boolexpr? filter, unit u)
    {
        if (filter is null) return true;
        var outer = _filterUnit;
        _filterUnit = u;
        try { return Holds(filter, alone: true); }
        finally { _filterUnit = outer; }
    }

    void FireRegion(RegionEvent e)
    {
        var units = e.Queued.ToArray();
        e.Queued.Clear();
        foreach (var u in units) Fire(e.Event, Convert<gameevent>(e.Enter ? EnterRegion : LeaveRegion), unit: new(u), region: e.Region);
    }
}
