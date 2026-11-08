using MirrorImaged.Lua;

namespace MirrorImaged.Natives;

public partial class location
{
    internal float X, Y;
}

public partial class rect
{
    internal float MinX, MinY, MaxX, MaxY;
}

// Locations, rects, the map's bounds and where a unit is put, as the probes units-bounds and terrain-edge measure them
// (docs/facts.md, "Rects and locations"). A rect's corners are kept in order and held in the world as the game holds
// them; a location is a place, anywhere.
public sealed partial class GameNatives
{
    const string BoundsProbe = "facts.md, Rects and locations: probe units-bounds (11-08)";
    const string EdgeProbe = "facts.md, Rects and locations: probe terrain-edge (11-08)";

    [Native(Level.Exact, Fact = BoundsProbe)]
    public location? Location(float x, float y) => Make(new location { X = x, Y = y });

    /// <summary>A removed location reads (0, 0).</summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public void RemoveLocation(location? whichLocation)
    {
        if (whichLocation is not null) _game.Remove(whichLocation.Key);
    }

    [Native(Level.Exact, Fact = BoundsProbe)]
    public void MoveLocation(location? whichLocation, float newX, float newY)
    {
        if (whichLocation is not null) (whichLocation.X, whichLocation.Y) = (newX, newY);
    }

    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetLocationX(location? whichLocation) => whichLocation?.X ?? 0;

    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetLocationY(location? whichLocation) => whichLocation?.Y ?? 0;

    /// <summary>
    /// The terrain's height (<c>Terrain</c>; off the map as the probe terrain-edge measures), or a standing bridge's
    /// deck where it is higher. Close for the decks: a deck is measured in whole units, 0.5 off the game's at most
    /// points; a dying deck is gone from the next step where the game's falls over 2 s and leaves its ends, and a
    /// restored one is back at the next step where the game's takes 0.5 s, or 7 s with birth; the walkable types
    /// without a measured deck (the ice floes ITf1 to ITf4, whose decks moved, DTep and RW00) have none here.
    /// </summary>
    [Native(Level.Close)]
    public float GetLocationZ(location? whichLocation) => whichLocation is null ? 0 : Height(whichLocation.X, whichLocation.Y);

    /// <summary>
    /// A rect with its corners in order, whichever way they are given, each held in the world: from its west (south)
    /// edge to 32 short of its east (north) one (probe units-bounds: -3e38 to 3e38 is -2048 to 2016 on a map of
    /// -2048 to 2048).
    /// </summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public rect? Rect(float minx, float miny, float maxx, float maxy) => Set(Make(new rect()), minx, miny, maxx, maxy);

    [Native(Level.Exact, Fact = BoundsProbe)]
    public rect? RectFromLoc(location? min, location? max) =>
        min is null || max is null ? null : Rect(min.X, min.Y, max.X, max.Y);

    [Native(Level.Exact, Fact = BoundsProbe)]
    public void SetRect(rect? whichRect, float minx, float miny, float maxx, float maxy)
    {
        if (whichRect is not null) Set(whichRect, minx, miny, maxx, maxy);
    }

    [Native(Level.Exact, Fact = BoundsProbe)]
    public void SetRectFromLoc(rect? whichRect, location? min, location? max)
    {
        if (min is not null && max is not null) SetRect(whichRect, min.X, min.Y, max.X, max.Y);
    }

    /// <summary>Keeps the rect's size about the new centre, which isn't held in the world (probe units-bounds).</summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public void MoveRectTo(rect? whichRect, float newCenterX, float newCenterY)
    {
        if (whichRect is null) return;
        var (halfX, halfY) = (Half(Native.mi_sub(whichRect.MaxX, whichRect.MinX)), Half(Native.mi_sub(whichRect.MaxY, whichRect.MinY)));
        (whichRect.MinX, whichRect.MinY) = (Native.mi_sub(newCenterX, halfX), Native.mi_sub(newCenterY, halfY));
        (whichRect.MaxX, whichRect.MaxY) = (Native.mi_add(newCenterX, halfX), Native.mi_add(newCenterY, halfY));
    }

    [Native(Level.Exact, Fact = BoundsProbe)]
    public void MoveRectToLoc(rect? whichRect, location? newCenterLoc)
    {
        if (newCenterLoc is not null) MoveRectTo(whichRect, newCenterLoc.X, newCenterLoc.Y);
    }

    /// <summary>A removed rect, and nil, read 0.</summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetRectMinX(rect? whichRect) => whichRect?.MinX ?? 0;

    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetRectMinY(rect? whichRect) => whichRect?.MinY ?? 0;

    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetRectMaxX(rect? whichRect) => whichRect?.MaxX ?? 0;

    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetRectMaxY(rect? whichRect) => whichRect?.MaxY ?? 0;

    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetRectCenterX(rect? whichRect) => whichRect is null ? 0 : Half(Native.mi_add(whichRect.MinX, whichRect.MaxX));

    [Native(Level.Exact, Fact = BoundsProbe)]
    public float GetRectCenterY(rect? whichRect) => whichRect is null ? 0 : Half(Native.mi_add(whichRect.MinY, whichRect.MaxY));

    static float Half(float f) => Native.mi_mul(f, 0.5f);

    [Native(Level.Exact, Fact = BoundsProbe)]
    public void RemoveRect(rect? whichRect)
    {
        if (whichRect is not null) _game.Remove(whichRect.Key);
    }

    /// <summary>A new rect each call, the whole of the map's terrain, held in nothing.</summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public rect? GetWorldBounds() => Make(World is (var minX, var minY, var maxX, var maxY) ? new rect { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY } : new rect());

    // The map's terrain from its west and south edges to its east and north ones; null without terrain
    (float MinX, float MinY, float MaxX, float MaxY)? World => Data.Terrain?.Bounds;

    rect Set(rect r, float minx, float miny, float maxx, float maxy)
    {
        (r.MinX, r.MaxX) = (Held(Math.Min(minx, maxx), true), Held(Math.Max(minx, maxx), true));
        (r.MinY, r.MaxY) = (Held(Math.Min(miny, maxy), false), Held(Math.Max(miny, maxy), false));
        return r;
    }

    // A coordinate held in the world as rects and units are: from its west (south) edge to 32 short of its east (north)
    // one (probe units-bounds); anything without terrain
    float Held(float at, bool x) => World is (var minX, var minY, var maxX, var maxY)
        ? x ? Math.Clamp(at, minX, maxX - 32) : Math.Clamp(at, minY, maxY - 32)
        : at;

    /// <summary>A new location each call at the start location's place: (0, 0) for one not defined.</summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public location? GetStartLocationLoc(int whichStartLocation) => Location(GetStartLocationX(whichStartLocation), GetStartLocationY(whichStartLocation));

    /// <summary>As DefineStartLocation; nil defines nothing.</summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public void DefineStartLocationLoc(int whichStartLoc, location? whichLocation)
    {
        if (whichLocation is not null) DefineStartLocation(whichStartLoc, whichLocation.X, whichLocation.Y);
    }

    /// <summary>A new location each call at the unit's place; nil for nil.</summary>
    [Native(Level.Exact, Fact = BoundsProbe)]
    public location? GetUnitLoc(unit? whichUnit) => whichUnit is null ? null : Location(whichUnit.X, whichUnit.Y);

    /// <summary>As CreateUnit at the location; nil for no location.</summary>
    [Native(Level.Close)]
    public unit? CreateUnitAtLoc(player? id, int unitid, location? whichLocation, float face) =>
        whichLocation is null ? null : CreateUnit(id, unitid, whichLocation.X, whichLocation.Y, face);

    /// <summary>
    /// Moves the unit, held in the world as CreateUnit holds a unit made off it, firing the regions' events. Close: the
    /// game also moves a ground unit off the map's edge and off other units and buildings, by its pathing (wave 3), and
    /// stops its order.
    /// </summary>
    [Native(Level.Close)]
    public void SetUnitPosition(unit? whichUnit, float newX, float newY)
    {
        if (whichUnit is not null) Move(whichUnit, Held(newX, true), Held(newY, false));
    }

    /// <summary>As SetUnitPosition; nil moves nothing.</summary>
    [Native(Level.Close)]
    public void SetUnitPositionLoc(unit? whichUnit, location? whichLocation)
    {
        if (whichLocation is not null) SetUnitPosition(whichUnit, whichLocation.X, whichLocation.Y);
    }

    // Puts the unit there and tells the grid and the regions
    void Move(unit u, float x, float y)
    {
        var (fromX, fromY) = (u.X, u.Y);
        (u.X, u.Y) = (x, y);
        Moved(u, fromX, fromY, made: false);
    }

    // A unit made: held in the world, kept in the game's list, and entering the regions it stands in
    void Arrived(unit u)
    {
        (u.X, u.Y) = (Held(u.X, true), Held(u.Y, false));
        _units.Add(u.Id, u);
        Moved(u, u.X, u.Y, made: true);
    }

    void Moved(unit u, float fromX, float fromY, bool made)
    {
        if (SquareOf(u) is var square && (made || square != u.Square)) (u.Square, u.Entered) = (square, ++_entered);
        Crossed(u, fromX, fromY, made);
    }

    // The square of 256 holding the unit in the game's grid, by the corner of its collision (probe units-order)
    (int X, int Y) SquareOf(unit u)
    {
        var size = BlzGetUnitCollisionSize(u);
        return ((int)MathF.Floor((u.X - size) / 256), (int)MathF.Floor((u.Y - size) / 256));
    }

    /// <summary>The unit type's collision size (probe units-groups: a footman 31, a peasant 16, a gryphon rider 8).</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public float BlzGetUnitCollisionSize(unit? whichUnit) => whichUnit?.Type.Real(Collision) ?? 0;

    static readonly int Collision = Code("ucol");

    /// <summary>
    /// Whether the unit's place is within the distance of (x, y) and its collision size: compared squared, so a
    /// distance below minus the size counts from the size's far side.
    /// </summary>
    [Native(Level.Exact, Fact = GroupsProbe + "; game-facts.md, Groups and performance: IsUnitInRangeXY is true up to range plus collision size")]
    public bool IsUnitInRangeXY(unit? whichUnit, float x, float y, float distance) =>
        whichUnit is not null && Within(whichUnit.X - x, whichUnit.Y - y, distance + BlzGetUnitCollisionSize(whichUnit));

    [Native(Level.Exact, Fact = GroupsProbe)]
    public bool IsUnitInRangeLoc(unit? whichUnit, location? whichLocation, float distance) =>
        whichLocation is not null && IsUnitInRangeXY(whichUnit, whichLocation.X, whichLocation.Y, distance);

    /// <summary>As IsUnitInRangeXY, with both units' collision sizes.</summary>
    [Native(Level.Exact, Fact = GroupsProbe)]
    public bool IsUnitInRange(unit? whichUnit, unit? otherUnit, float distance) =>
        whichUnit is not null && otherUnit is not null &&
        Within(whichUnit.X - otherUnit.X, whichUnit.Y - otherUnit.Y, distance + BlzGetUnitCollisionSize(whichUnit) + BlzGetUnitCollisionSize(otherUnit));

    static bool Within(float dx, float dy, float reach) => Distance2(dx, dy) <= reach * reach;
}
