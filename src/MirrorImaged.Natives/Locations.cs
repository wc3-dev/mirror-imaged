namespace MirrorImaged.Natives;

public partial class location
{
    internal float X, Y;
}

public partial class rect
{
    internal float MinX, MinY, MaxX, MaxY;
}

// The smallest locations and rects GetLocationZ and EnumDestructablesInRect need (decisions D259); W2-6 brings the
// rest of them, with regions and groups, and measures them.
public sealed partial class GameNatives
{
    [Native(Level.Close)]
    public location? Location(float x, float y) => Make(new location { X = x, Y = y });

    [Native(Level.Close)]
    public void RemoveLocation(location? whichLocation)
    {
        if (whichLocation is not null) _game.Remove(whichLocation.Key);
    }

    [Native(Level.Close)]
    public void MoveLocation(location? whichLocation, float newX, float newY)
    {
        if (whichLocation is not null) (whichLocation.X, whichLocation.Y) = (newX, newY);
    }

    [Native(Level.Close)]
    public float GetLocationX(location? whichLocation) => whichLocation?.X ?? 0;

    [Native(Level.Close)]
    public float GetLocationY(location? whichLocation) => whichLocation?.Y ?? 0;

    /// <summary>
    /// The terrain's height (<c>Terrain</c>), or a standing bridge's deck where it is higher. Close: a deck is measured in
    /// whole units, 0.5 off the game's at most points; a dying deck is gone from the next step where the game's falls
    /// over 2 s and leaves its ends, and a restored one is back at the next step where the game's takes 0.5 s, or 7 s
    /// with birth; the walkable types without a measured deck (the ice floes ITf1 to ITf4, whose decks moved, DTep and
    /// RW00) have none here; the terrain off the map isn't measured (W2-6).
    /// </summary>
    [Native(Level.Close)]
    public float GetLocationZ(location? whichLocation) => whichLocation is null ? 0 : Height(whichLocation.X, whichLocation.Y);

    [Native(Level.Close)]
    public rect? Rect(float minx, float miny, float maxx, float maxy) => Make(new rect { MinX = minx, MinY = miny, MaxX = maxx, MaxY = maxy });

    [Native(Level.Close)]
    public void RemoveRect(rect? whichRect)
    {
        if (whichRect is not null) _game.Remove(whichRect.Key);
    }
}
