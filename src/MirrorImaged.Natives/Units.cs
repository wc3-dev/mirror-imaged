using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal GameObject Type = null!;
    internal player Owner = null!;
    internal float X, Y, Life, MaxLife, Mana, MaxMana;
    internal bool Dead, Removed, Hidden, Invulnerable, Exploded, Paused, GivenWhilePaused, UsesFood = true;
    internal Action? Unpausing; // the order a frame after PauseUnit(false) gives back, until it comes
    internal int UserData, Skin, HeroLevel, Color, Pauses;
    internal float Scale = 1, FlyHeight;
    internal int[] Tint = [255, 255, 255, 255];
    internal string? Name; // BlzSetUnitName's
    internal readonly Dictionary<int, object> Fields = []; // what BlzSetUnit*Field and the setters sharing a field set
    internal Change Facing;
}

/// <summary>
/// A value moving at a rate from where it was to where it is going, as a unit turns: read at a time, it is where it
/// has got to.
/// </summary>
struct Change(float to)
{
    double _from = to, _start, _rate;

    public float To { get; private set; } = to;

    public readonly float At(double seconds) =>
        _rate <= 0 || double.IsInfinity(_rate) ? To
        : (float)(_from + Math.Clamp((seconds - _start) * _rate, 0, Math.Abs(To - _from)) * Math.Sign(To - _from));

    /// <summary>Goes on to <paramref name="to"/> from where it is at <paramref name="seconds"/>, at once when the rate is 0 or less.</summary>
    public void Go(float to, double seconds, double rate)
    {
        _from = At(seconds);
        _start = seconds;
        _rate = rate;
        To = to;
    }
}

// Units without movement, as the units and units-decay probes measure them (docs/facts.md, "Units") with the facts'
// "Units" and "Orders and events". A unit's data is its type's (GameData) with its own changes on top.
public sealed partial class GameNatives
{
    const string DecayProbe = "facts.md, Units: probe units-decay (11-08)";
    const string BonesProbe = "facts.md, Units: probe units-bones (11-08)";
    const float Dying = 0.405f; // a unit whose life is set below it dies (facts, Units)

    static int Code(string id) => Ids.Of(id);

    static readonly int HitPoints = Code("uhpm"), ManaMaximum = Code("umpm"), ManaInitial = Code("umpi"), Strength = Code("ustr"),
        Intelligence = Code("uint"), UnitName = Code("unam"), DeathTime = Code("udtm"), IsBuilding = Code("ubdg"), ModelScale = Code("usca");

    /// <summary>A hero's type id starts with a capital letter (the units probe).</summary>
    static bool IsHero(int id) => (id >> 24) is >= 'A' and <= 'Z';

    unit? Create(player? owner, int id, float x, float y, float face, int skin, bool corpse = false)
    {
        if (owner is null || Data.Object(ObjectKind.Unit, id) is not { } type) return null;
        var hero = IsHero(id);
        var u = Make(new unit
        {
            Type = type, Owner = owner, X = x, Y = y, Skin = skin, HeroLevel = hero ? 1 : 0, Color = owner.Color, Scale = type.Real(ModelScale),
            Facing = new(Angle(face)), FlyHeight = type.Real(Code("umvh")),
        });
        // A hero's most life and mana count its strength and intelligence (facts, Units: StrHitPointBonus, IntManaBonus)
        u.MaxLife = type.Integer(HitPoints) + (hero ? Data.Constant("StrHitPointBonus", 25) * type.Integer(Strength) : 0);
        u.MaxMana = type.Integer(ManaMaximum) + (hero ? Data.Constant("IntManaBonus", 15) * type.Integer(Intelligence) : 0);
        u.Life = corpse ? 0 : u.MaxLife;
        u.Mana = Math.Min(type.Integer(ManaInitial) + (hero ? Data.Constant("IntManaBonus", 15) * type.Integer(Intelligence) : 0), u.MaxMana);
        u.Dead = corpse;
        FallAsleep(u);
        // A corpse counts its food and gives it back (two food events), and gets its undefend, with no death event
        // (probes units-more and units-bones)
        Counting(u, 1);
        if (!corpse) return u;
        Counting(u, -1);
        Undefend(u, Defends(u));
        if (!hero) Rot(u, Seconds + Bones);
        return u;
    }

    // An angle in degrees from 0 below 360 (facts: 400 reads 40, -90 reads 270, 720 reads 0)
    static float Angle(float degrees) => degrees % 360 is var a && a < 0 ? a + 360 : a;

    /// <summary>
    /// A unit of the type for the player, taking one id, alive, at full life, at the place and facing given; nil for an
    /// id that is no unit's (an item's, a destructable's, an ability's, 0) and for no player (probe units). Close: the
    /// game moves a unit made where others stand off them, and one made off the map onto it (wave 3's pathing, W2-6's
    /// bounds), and keeps a facing outside 0 to 360 a little otherwise (decisions D223, D224).
    /// </summary>
    [Native(Level.Close)]
    public unit? CreateUnit(player? id, int unitid, float x, float y, float face) => Create(id, unitid, x, y, face, unitid);

    /// <summary>As CreateUnit, with the skin's id kept for BlzGetUnitSkin; its data stay the type's (probe units).</summary>
    [Native(Level.Close)]
    public unit? BlzCreateUnitWithSkin(player? id, int unitid, float x, float y, float face, int skinId) => Create(id, unitid, x, y, face, skinId);

    /// <summary>
    /// A dead unit of the type, life 0, with no death or decay event: its food counts and goes back, and a footman's
    /// Defend gives it undefend (852056), inside the call; it is gone DecayTime plus BoneDecayTime later, as a body at
    /// its decay is, with undefend again (probes units-decay, units-more, units-bones: 90 s). A hero's stays (inferred).
    /// </summary>
    [Native(Level.Exact, Fact = BonesProbe + "; " + MoreProbe + "; " + DecayProbe)]
    public unit? CreateCorpse(player? whichPlayer, int unitid, float x, float y, float face) =>
        Create(whichPlayer, unitid, x, y, face, unitid, corpse: true);

    /// <summary>
    /// The unit reads as it was for the rest of the step, alive or dead, and is gone after it: every getter then reads
    /// 0 or nil (facts, Units). Killed in that step, it dies and stays as a body (probe units). As it goes it reads
    /// dead, life 0 and unpaused, gives its food back and gets undefend (852056) twice for each Defend if it lived, once
    /// if dead (probe units-more).
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe + "; " + UnitsProbe)]
    public void RemoveUnit(unit? whichUnit)
    {
        if (whichUnit is null || whichUnit.Removed) return;
        whichUnit.Removed = true;
        GoneAfterStep(whichUnit);
    }

    void GoneAfterStep(unit u) => _game.At(_game.Step, () => Gone(u));

    void Gone(unit u)
    {
        if (!u.Removed) return;
        var undefends = Defends(u) * (u.Dead ? 1 : 2);
        if (!u.Dead)
        {
            Die(u);
            Counting(u, -1);
        }
        Undefend(u, undefends);
        _game.Remove(u.Key);
        _widgetEvents.Remove(u);
        _sleepers.Remove(u);
    }

    // Dead: life 0, its pause and timed life over (probe units-more-2: a paused unit's death events read it unpaused),
    // its sleep settled first
    void Die(unit u)
    {
        Asleep(u);
        u.Dead = true;
        u.Life = 0;
        u.Paused = false;
        u.Unpausing = null;
        u.TimedEnd = u.TimedLeft = null;
    }

    /// <summary>
    /// The death fires inside the call, the killer nil; on a dead unit nothing happens (facts, Units). First the unit
    /// reads dead with life 0 and gives its food back, then a unit with Defend gets undefend (852056) on its owner's and
    /// its own order events, then the death events (probes units-more and units-more-2).
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe + "; " + UnitsProbe)]
    public void KillUnit(unit? whichUnit)
    {
        if (whichUnit is not null) Kill(whichUnit);
    }

    // Death, then the body (probes units-decay, units-bones), its death time after the death: an exploded unit is gone
    // after the step and a hero stays; a building is gone StructureDecayTime after its death time, a unit that doesn't
    // decay at its death time, neither with a decay event; any other unit decays then (the decay event) and is gone
    // DecayTime plus BoneDecayTime after that. A unit removed and then killed in one step stays (probe units). The killer
    // comes with damage (W2-5): no death this wave makes has one.
    void Kill(unit u)
    {
        if (u.Dead) return;
        var life = u.Life;
        Die(u);
        u.Removed = false;
        Counting(u, -1);
        StateChanged(u, LifeState, life, 0);
        Undefend(u, Defends(u));
        FireUnit(new(u), UnitDeath, PlayerUnitDeath);
        if (u.Exploded)
        {
            u.Removed = true;
            GoneAfterStep(u);
            return;
        }
        if (IsHero(u.Type.Id)) return;
        var decay = Seconds + Math.Max(0, BlzGetUnitRealField(u, Convert<unitrealfield>(DeathTime)));
        if (IsBuildingNow(u)) Rot(u, decay + Data.Constant("StructureDecayTime", 30));
        else if (!BlzGetUnitBooleanField(u, Convert<unitbooleanfield>(Decayable))) Rot(u, decay);
        else
            _game.At(Game.Steps(decay), () =>
            {
                if (!u.Dead || _game.Handle(u.Key) is null) return;
                FireUnit(new(u), UnitDecay, PlayerUnitDecay);
                Rot(u, decay + Bones);
            });
    }

    // How long a body's bones last after its decay: 90 s (probe units-bones, footman, rifleman and corpse)
    double Bones => Data.Constant("DecayTime", 2) + Data.Constant("BoneDecayTime", 88);

    // A step after its time, as the game's timers due then run first (probe units-bones: a corpse whose time was 90.00
    // read there in a 90.00 timer)
    void Rot(unit u, double at) => _game.At(Game.Steps(at) + 1, () =>
    {
        if (!u.Dead || _game.Handle(u.Key) is null) return;
        u.Removed = true;
        Gone(u);
    });

    bool IsBuildingNow(unit u) => BlzGetUnitBooleanField(u, Convert<unitbooleanfield>(IsBuilding));

    /// <summary>An exploded unit's death fires as any other, but it leaves no body and is gone after the step (probe units-decay).</summary>
    [Native(Level.Exact, Fact = DecayProbe)]
    public void SetUnitExploded(unit? whichUnit, bool exploded)
    {
        if (whichUnit is not null) whichUnit.Exploded = exploded;
    }

    /// <summary>
    /// Changes nothing a native reads: a body suspended right after its death still decays and goes when another does,
    /// also when it is let go later (probes units-decay and units-bones). Close: suspended later, in its decay or bones,
    /// isn't measured.
    /// </summary>
    [Native(Level.Close)]
    public void UnitSuspendDecay(unit? whichUnit, bool suspend)
    {
    }

    /// <summary>The unit's type id; 0 for nil and once the unit is gone.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitTypeId(unit? whichUnit) => whichUnit?.Type.Id ?? 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool IsHeroUnitId(int unitId) => IsHero(unitId);

    /// <summary>
    /// The name of the unit, item, ability or upgrade with the id, the map's changes and a <c>TRIGSTR_n</c> read, also of
    /// one the map's balance set leaves out; "Default string" for anything else, a buff's and a destructable's name too,
    /// as the units probe and the facts' "Object data" read them. Not the name BlzSetUnitName gives one unit.
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; facts.md, The game's data: probes data-objects and data-sets-melee; game-facts.md, Object data: GetObjectName (10-18)")]
    public string? GetObjectName(int objectId) => Data.Name(objectId) ?? "Default string";

    /// <summary>The name BlzSetUnitName gave the unit, else its type's: a hero's type name, not its proper name (facts, Units).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: GetUnitName and BlzSetUnitName (10-21, 10-28); " + UnitsProbe)]
    public string? GetUnitName(unit? whichUnit) => whichUnit is null ? null : whichUnit.Name ?? whichUnit.Type.Text(UnitName) ?? "";

    /// <summary>Renames the one unit: neither its type's GetObjectName nor the next unit of the type (facts, Units).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: BlzSetUnitName changes that one unit's name (10-21); " + UnitsProbe)]
    public void BlzSetUnitName(unit? whichUnit, string? name)
    {
        if (whichUnit is not null && name is not null) whichUnit.Name = name;
    }

    /// <summary>
    /// Alive, for a unit that hasn't died, also once removed in its step (facts, Units).
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: a removed living unit reads alive for the rest of the frame (11-06); " + UnitsProbe)]
    public bool UnitAlive(unit? id) => id is { Dead: false };

    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool BlzUnitIsAlive(unit? whichUnit) => UnitAlive(whichUnit);
}
