using System.Buffers.Binary;
using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class destructable
{
    internal GameObject Type = null!;
    internal float X, Y, Face, Scale, Life, MaxLife, Occluder, Ground;
    internal bool Invulnerable, Listed = true;
    internal Deck? Deck;
    internal bool WasDead; // dead or not before the step its life last changed in, which its deck shows until the next
    internal long Turned = -1;

    internal bool Dead => Life <= 0;
}

// Destructables, trees and gates, as the destructables probe measures them (docs/facts.md, "Destructables") with the
// facts' "Units". A destructable's data is its type's; widgets of other kinds wait for their tasks (units W2-2, items
// W2-3) and stop the run as Missing until then.
public sealed partial class GameNatives
{
    const string Destructables = "facts.md, Destructables: probe destructables (11-08)";
    const int WidgetDied = 89; // EVENT_WIDGET_DEATH

    static readonly int DestructableLife = Rawcode("bhps"), Occlusion = Rawcode("boch"), FixedRotation = Rawcode("bfxr"),
        PathTexture = Rawcode("bptx"), DestructableName = Rawcode("bnam");

    readonly List<destructable> _destructables = []; // in the order made
    readonly List<destructable> _decks = []; // those with a deck, which GetLocationZ reads
    readonly List<(destructable Destructable, @event Event)> _deaths = [];
    destructable? _enumDestructable, _filterDestructable;

    /// <summary>
    /// Makes the map's placed destructables, as the game makes those of <c>war3map.doo</c> before the script runs: one
    /// whose state has bit 0 or 1 set, unlisted by the enumerations when bit 0 is (game-facts, "World Editor and map
    /// files"), at its life share (not measured).
    /// </summary>
    internal void Place()
    {
        foreach (var placed in Data.Map?.Files.Doodads?.Doodads ?? [])
        {
            var state = (int)placed.State;
            if ((state & 3) == 0
                || Create(BinaryPrimitives.ReverseEndianness(placed.TypeId), placed.Position.X, placed.Position.Y, placed.Rotation * 180 / MathF.PI, placed.Scale.X, false) is not { } d)
                continue;
            d.Listed = (state & 1) == 0;
            d.Life = d.MaxLife * Math.Min((int)placed.Life, 100) / 100;
        }
    }

    // A place is cut down to a quarter; then, for a type with a pathing texture, onto the 64 grid toward 0, or 32 off it
    // away from 0 when half the texture's cells across that way is odd (probe destructables). A type turned exactly 0 or
    // 180 degrees, by its fixed rotation or else its facing, has the texture's sides swapped.
    destructable? Create(int objectid, float x, float y, float face, float scale, bool dead)
    {
        if (Data.Object(ObjectKind.Destructable, objectid) is not { } type) return null;
        var rotation = type.Real(FixedRotation) is >= 0 and var fixedRotation ? fixedRotation : face;
        var (across, up) = type.Text(PathTexture) is { } texture ? Data.PathingSize(texture) ?? (0, 0) : (0, 0);
        if (rotation is 0 or 180) (across, up) = (up, across);
        var d = Make(new destructable
        {
            Type = type, X = Snap(x, across), Y = Snap(y, up), Face = face, Scale = scale, MaxLife = type.Real(DestructableLife), Occluder = type.Real(Occlusion),
            Deck = Deck.Of(type.Id) ?? Deck.Of(type.BaseId),
        });
        d.Life = dead ? 0 : d.MaxLife;
        d.Ground = Data.Terrain?.Ground(d.X, d.Y) ?? 0;
        _destructables.Add(d);
        if (d.Deck is not null) _decks.Add(d);
        return d;
    }

    static int Rawcode(string id) => id.Aggregate(0, (value, letter) => value << 8 | letter);

    static float Snap(float at, int cells)
    {
        at = MathF.Floor(at * 4) / 4;
        var grid = MathF.Truncate(at / 64) * 64 + 0f; // + 0 makes -0 0, as the game's place reads (probe destructables)
        return cells == 0 ? at : cells / 2 % 2 == 0 ? grid : grid + (at < 0 ? -32 : 32);
    }

    /// <summary>
    /// Nil for an id that isn't a destructable's (a unit's, 0); a variation past the type's count or below 0 makes one.
    /// It is put on the grid of its pathing texture, with its type's life, occluder height and name (decisions D251).
    /// </summary>
    [Native(Level.Exact, Fact = Destructables)]
    public destructable? CreateDestructable(int objectid, float x, float y, float face, float scale, int variation) =>
        Create(objectid, x, y, face, scale, false);

    /// <summary>As CreateDestructable; the height isn't read back.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public destructable? CreateDestructableZ(int objectid, float x, float y, float z, float face, float scale, int variation) =>
        Create(objectid, x, y, face, scale, false);

    /// <summary>Made dead, its life 0 of its most: KillDestructable fires nothing, and restoring brings it back.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public destructable? CreateDeadDestructable(int objectid, float x, float y, float face, float scale, int variation) =>
        Create(objectid, x, y, face, scale, true);

    /// <summary>As CreateDeadDestructable (not measured).</summary>
    [Native(Level.Close)]
    public destructable? CreateDeadDestructableZ(int objectid, float x, float y, float z, float face, float scale, int variation) =>
        Create(objectid, x, y, face, scale, true);

    // As CreateDestructable and CreateDeadDestructable: the height, skin, colour, pitch and roll aren't read back (not measured).

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructablePitchRoll(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructablePitchRollWithColor(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableWithColor(int objectid, float x, float y, float face, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableWithSkin(int objectid, float x, float y, float face, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableWithSkinColor(int objectid, float x, float y, float face, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableWithSkinPitchRoll(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableWithSkinPitchRollColor(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableZPitchRoll(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableZPitchRollWithColor(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableZWithColor(int objectid, float x, float y, float z, float face, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableZWithSkin(int objectid, float x, float y, float z, float face, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableZWithSkinColor(int objectid, float x, float y, float z, float face, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableZWithSkinPitchRoll(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDeadDestructableZWithSkinPitchRollColor(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, true);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructablePitchRoll(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructablePitchRollWithColor(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableWithColor(int objectid, float x, float y, float face, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableWithSkin(int objectid, float x, float y, float face, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableWithSkinColor(int objectid, float x, float y, float face, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableWithSkinPitchRoll(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableWithSkinPitchRollColor(int objectid, float x, float y, float face, float roll, float pitch, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableZPitchRoll(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableZPitchRollWithColor(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableZWithColor(int objectid, float x, float y, float z, float face, float scale, int variation, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableZWithSkin(int objectid, float x, float y, float z, float face, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableZWithSkinColor(int objectid, float x, float y, float z, float face, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableZWithSkinPitchRoll(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation, int skinId) =>
        Create(objectid, x, y, face, scale, false);

    [Native(Level.Close)]
    public destructable? BlzCreateDestructableZWithSkinPitchRollColor(int objectid, float x, float y, float z, float face, float roll, float pitch, float scale, int variation, int skinId, playercolor? color) =>
        Create(objectid, x, y, face, scale, false);

    /// <summary>
    /// Fires its death event inside the call, dead or not, reading the life it had; then it is gone, and every native
    /// reads it as nil (its type id, life and place 0).
    /// </summary>
    [Native(Level.Exact, Fact = Destructables + "; game-facts.md, Units: RemoveDestructable fires the death event (10-23)")]
    public void RemoveDestructable(destructable? d)
    {
        if (d is null || !_destructables.Remove(d)) return;
        FireDeath(d);
        _decks.Remove(d);
        _deaths.RemoveAll(registered => registered.Destructable == d);
        _game.Remove(d.Key);
    }

    /// <summary>
    /// Its life goes to 0 and its death event fires inside the call; a destructable whose life is 0 already, dead or
    /// with its most life set to 0, fires nothing.
    /// </summary>
    [Native(Level.Exact, Fact = Destructables)]
    public void KillDestructable(destructable? d)
    {
        if (d is { Dead: false }) Die(d);
    }

    void Die(destructable d)
    {
        Live(d, 0);
        FireDeath(d);
    }

    // A destructable is dead while its life is 0. Its deck shows the life it had at the start of the step its life
    // changed in until the next step (probe destructables: a killed bridge's deck still reads in that instant, a restored
    // one's not yet).
    void Live(destructable d, float life)
    {
        if (d.Turned != _game.Step) (d.WasDead, d.Turned) = (d.Dead, _game.Step);
        d.Life = life;
    }

    void FireDeath(destructable d)
    {
        foreach (var (registered, @event) in _deaths.ToArray())
            if (registered == d) Fire(@event, Convert<widgetevent>(WidgetDied), widget: d);
    }

    [Native(Level.Exact, Fact = Destructables)]
    public float GetDestructableLife(destructable? d) => d?.Life ?? 0;

    /// <summary>
    /// Kept with its fraction, up to the most life. Below 0.405 a living one dies, its death event inside the call reading
    /// 0; a dead one stays at 0. At 0.405 or more a dead one reads that life and lives again, so KillDestructable fires
    /// again, though the game still shows it dead.
    /// </summary>
    [Native(Level.Exact, Fact = Destructables + "; game-facts.md, Units: SetDestructableLife (11-02)")]
    public void SetDestructableLife(destructable? d, float life)
    {
        if (d is null) return;
        if (life >= 0.405f) Live(d, Math.Min(life, d.MaxLife));
        else if (!d.Dead) Die(d);
    }

    [Native(Level.Exact, Fact = Destructables)]
    public float GetDestructableMaxLife(destructable? d) => d?.MaxLife ?? 0;

    /// <summary>
    /// The most life is rounded to a whole number, halves away from 0 (80.5 is 81), and may be 0 or below; the life
    /// keeps its share of it, at least 0, and nothing dies.
    /// </summary>
    [Native(Level.Exact, Fact = Destructables + "; game-facts.md, Units: SetDestructableMaxLife (11-02)")]
    public void SetDestructableMaxLife(destructable? d, float max)
    {
        if (d is null) return;
        var most = MathF.Round(max, MidpointRounding.AwayFromZero);
        Live(d, d.MaxLife == 0 ? 0 : Math.Max(0, d.Life * most / d.MaxLife));
        d.MaxLife = most;
    }

    /// <summary>
    /// Brings a dead one back with <paramref name="life"/> from 0.5 up to its most, 0 giving the most; a living one is
    /// left as it is. Its death registrations still hold. Birth changes how it is drawn, which isn't simulated.
    /// </summary>
    [Native(Level.Exact, Fact = Destructables + "; game-facts.md, Units: DestructableRestoreLife (11-02)")]
    public void DestructableRestoreLife(destructable? d, float life, bool birth)
    {
        if (d is { Dead: true }) Live(d, life == 0 ? d.MaxLife : Math.Min(Math.Max(life, 0.5f), d.MaxLife));
    }

    /// <summary>A flag: an invulnerable destructable still dies to KillDestructable and to its life set below 0.405.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public void SetDestructableInvulnerable(destructable? d, bool flag)
    {
        if (d is not null) d.Invulnerable = flag;
    }

    [Native(Level.Exact, Fact = Destructables)]
    public bool IsDestructableInvulnerable(destructable? d) => d?.Invulnerable ?? false;

    /// <summary>Its type's occluder height (<c>boch</c>) until set; a set is kept as given, below 0 too.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public float GetDestructableOccluderHeight(destructable? d) => d?.Occluder ?? 0;

    [Native(Level.Exact, Fact = Destructables)]
    public void SetDestructableOccluderHeight(destructable? d, float height)
    {
        if (d is not null) d.Occluder = height;
    }

    /// <summary>
    /// Its type's name (<c>bnam</c>), a World Editor string key read from the strings the game keeps
    /// (<c>WESTRING_DEST_BARREL</c> is "Barrel"), without the editor suffix; any other name as it is; "" for nil.
    /// </summary>
    [Native(Level.Exact, Fact = Destructables)]
    public string? GetDestructableName(destructable? d) =>
        d?.Type.Text(DestructableName) is { } name ? Data.EditorString(name) ?? name : "";

    [Native(Level.Exact, Fact = Destructables)]
    public int GetDestructableTypeId(destructable? d) => d?.Type.Id ?? 0;

    [Native(Level.Exact, Fact = Destructables)]
    public float GetDestructableX(destructable? d) => d?.X ?? 0;

    [Native(Level.Exact, Fact = Destructables)]
    public float GetDestructableY(destructable? d) => d?.Y ?? 0;

    /// <summary>
    /// The listed destructables whose 32 cell, the one holding its place, lies in the rect with its west and south edges
    /// past the rect's (a rect of 32 around one never finds it), each passed to the filter, then the callback, with
    /// GetFilterDestructable and GetEnumDestructable it; in rows of 256 cells from the south, west to east, the last made
    /// first in a cell. Hidden destructables are listed.
    /// </summary>
    [Native(Level.Exact, Fact = Destructables + "; game-facts.md, Units: a rect of 32 finds none (10-26)")]
    public void EnumDestructablesInRect(rect? r, boolexpr? filter, Action? actionFunc)
    {
        var (outerEnum, outerFilter) = (_enumDestructable, _filterDestructable);
        try
        {
            if (r is null) return;
            var found = _destructables.Where(d => d.Listed && Inside(r, d))
                .OrderBy(d => MathF.Floor(d.Y / 256)).ThenBy(d => MathF.Floor(d.X / 256)).ThenByDescending(d => d.Key).ToList();
            foreach (var d in found)
            {
                if (_game.Handle(d.Key) is null) continue; // removed by a callback before it
                _enumDestructable = _filterDestructable = d;
                if (filter is null || Holds(filter, alone: true)) actionFunc?.Invoke();
            }
        }
        finally
        {
            (_enumDestructable, _filterDestructable) = (outerEnum, outerFilter);
            Release(actionFunc);
        }
    }

    static bool Inside(rect r, destructable d)
    {
        float west = MathF.Floor(d.X / 32) * 32, south = MathF.Floor(d.Y / 32) * 32;
        return west > r.MinX && west + 32 <= r.MaxX && south > r.MinY && south + 32 <= r.MaxY;
    }

    /// <summary>The destructable EnumDestructablesInRect is calling back for; nil outside it.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public destructable? GetEnumDestructable() => _enumDestructable;

    /// <summary>The destructable EnumDestructablesInRect's filter, and its callback, is running for; nil outside it.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public destructable? GetFilterDestructable() => _filterDestructable;

    /// <summary>
    /// Fires inside the destructable's death: KillDestructable, its life set below 0.405 and RemoveDestructable. A unit's
    /// or an item's waits for its task (units W2-2, items W2-3) and stops the run as Missing.
    /// </summary>
    [Native(Level.Close)]
    public @event? TriggerRegisterDeathEvent(trigger? whichTrigger, widget? whichWidget)
    {
        if (whichWidget is not (null or destructable)) throw new MissingNativeException(nameof(TriggerRegisterDeathEvent));
        if (whichTrigger is null || whichWidget is not destructable d) return null;
        var registered = Register(whichTrigger);
        _deaths.Add((d, registered));
        return registered;
    }

    /// <summary>The destructable of the death event that fired the trigger; nil outside one.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public destructable? GetTriggerDestructable() => Current.Widget as destructable;

    /// <summary>The widget of the event that fired the trigger, == the destructable; nil outside one.</summary>
    [Native(Level.Exact, Fact = Destructables)]
    public widget? GetTriggerWidget() => Current.Widget;

    /// <summary>A destructable's life; a unit's or item's waits for its task and stops the run as Missing.</summary>
    [Native(Level.Close)]
    public float GetWidgetLife(widget? whichWidget) => Widget(whichWidget, nameof(GetWidgetLife))?.Life ?? 0;

    /// <summary>A destructable's as SetDestructableLife; a unit's or item's waits for its task and stops the run as Missing.</summary>
    [Native(Level.Close)]
    public void SetWidgetLife(widget? whichWidget, float newLife) => SetDestructableLife(Widget(whichWidget, nameof(SetWidgetLife)), newLife);

    /// <summary>A destructable's place; a unit's or item's waits for its task and stops the run as Missing.</summary>
    [Native(Level.Close)]
    public float GetWidgetX(widget? whichWidget) => Widget(whichWidget, nameof(GetWidgetX))?.X ?? 0;

    /// <summary>As GetWidgetX.</summary>
    [Native(Level.Close)]
    public float GetWidgetY(widget? whichWidget) => Widget(whichWidget, nameof(GetWidgetY))?.Y ?? 0;

    static destructable? Widget(widget? w, string native) =>
        w is null or destructable ? w as destructable : throw new MissingNativeException(native);

    // GetLocationZ: the ground, or a standing walkable destructable's deck where it is higher; the deck stands on the ground
    // beneath any water at its place, turned with its facing unless its type's rotation is fixed, and scaled about its
    // place (game-facts, "Units"; probe destructables).
    float Height(float x, float y)
    {
        var height = Data.Terrain?.Height(x, y) ?? 0;
        foreach (var d in _decks)
        {
            if (_game.Step > d.Turned ? d.Dead : d.WasDead) continue;
            var turn = (d.Type.Real(FixedRotation) < 0 ? d.Face - 270 : 0) * MathF.PI / 180;
            var (cos, sin, dx, dy) = (MathF.Cos(turn), MathF.Sin(turn), (x - d.X) / d.Scale, (y - d.Y) / d.Scale);
            if (d.Deck!.Height(dx * cos + dy * sin, dy * cos - dx * sin) is { } deck && d.Ground + d.Scale * deck > height)
                height = d.Ground + d.Scale * deck;
        }
        return height;
    }

    /// <summary>Not simulated: only how it is drawn changes. It is still listed by the enumerations.</summary>
    [Native(Level.Recorded, Reason = "Shows or hides how the destructable is drawn; it is still listed (probe destructables)")]
    public void ShowDestructable(destructable? d, bool flag) { }

    [Native(Level.Recorded, Reason = "Plays an animation, which nothing reads back")]
    public void SetDestructableAnimation(destructable? d, string? whichAnimation) { }

    [Native(Level.Recorded, Reason = "Queues an animation, which nothing reads back")]
    public void QueueDestructableAnimation(destructable? d, string? whichAnimation) { }

    [Native(Level.Recorded, Reason = "Changes how fast its animations play, which nothing reads back")]
    public void SetDestructableAnimationSpeed(destructable? d, float speedFactor) { }

    [Native(Level.Recorded, Reason = "Changes its team colour, which nothing reads back")]
    public void SetDestructableColor(destructable? d, playercolor? color) { }

    [Native(Level.Recorded, Reason = "Changes its tint, which nothing reads back")]
    public void SetDestructableVertexColor(destructable? whichDestructable, int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "Plays an animation of doodads near a point, which nothing reads back")]
    public void SetDoodadAnimation(float x, float y, float radius, int doodadID, bool nearestOnly, string? animName, bool animRandom) { }

    [Native(Level.Recorded, Reason = "Plays an animation of doodads in a rect, which nothing reads back")]
    public void SetDoodadAnimationRect(rect? r, int doodadID, string? animName, bool animRandom) { }
}
