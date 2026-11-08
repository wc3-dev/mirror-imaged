using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// A unit's speeds, facing, height, owner and kind, as the units probe measures them (docs/facts.md, "Units").
public sealed partial class GameNatives
{
    static readonly int MoveSpeed = Code("umvs"), PropWindow = Code("uprw"), Race = Code("urac"), FoodUsed = Code("ufoo"),
        FoodMade = Code("ufma"), Points = Code("upoi"), WeaponsOn = Code("uaen");

    static readonly Dictionary<string, int> Races = Words("human 1 orc 2 undead 3 nightelf 4 demon 5 other 7 creeps 8 commoner 9 critters 10 naga 11");

    /// <summary>The unit's move speed: its type's, or what a set left (probe units).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitMoveSpeed(unit? whichUnit) => whichUnit is null ? 0 : Own(whichUnit, SpeedNow, whichUnit.Type.Real(MoveSpeed));

    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitDefaultMoveSpeed(unit? whichUnit) => whichUnit?.Type.Real(MoveSpeed) ?? 0;

    /// <summary>
    /// Kept from the constants' MinUnitSpeed to MaxUnitSpeed (150 and 400: 1000 read 400, 5 and -10 150); a unit that
    /// can't move, as a building, keeps 0 (probe units).
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public void SetUnitMoveSpeed(unit? whichUnit, float newSpeed)
    {
        if (whichUnit is null || GetUnitDefaultMoveSpeed(whichUnit) <= 0) return;
        // the most wins where a map gives a least above it (not measured)
        whichUnit.Fields[SpeedNow] = Math.Min(Math.Max(newSpeed, Data.Constant("MinUnitSpeed", 0)), Data.Constant("MaxUnitSpeed", float.MaxValue));
    }

    const float MinimumTurn = 0.001f;

    /// <summary>The unit's turn rate, an angle in radians from 0.001 below 2π: 0.001 for a type with none (probe units).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitTurnSpeed(unit? whichUnit) =>
        whichUnit is null ? 0 : Own(whichUnit, TurnRate, Math.Max(GetUnitDefaultTurnSpeed(whichUnit), MinimumTurn));

    /// <summary>The type's turn rate; 0 for a type with none (probe units).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitDefaultTurnSpeed(unit? whichUnit) => whichUnit?.Type.Real(TurnRate) ?? 0;

    /// <summary>
    /// Kept as an angle from 0 below 2π, at least 0.001: -1 reads 5.283185, 7 0.7168145, 7.5 1.216815 and 0 0.001 (probe
    /// units). Close: past a turn either way only those are measured; here any angle comes round by a float modulo.
    /// </summary>
    [Native(Level.Close)]
    public void SetUnitTurnSpeed(unit? whichUnit, float newTurnSpeed)
    {
        if (whichUnit is null) return;
        const float Turn = 2 * MathF.PI;
        var rate = newTurnSpeed % Turn;
        whichUnit.Fields[TurnRate] = Math.Max(rate < 0 ? rate + Turn : rate, MinimumTurn);
    }

    /// <summary>The type's acquisition range, or what a set left; 0 for a unit without an attack (probe units).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitAcquireRange(unit? whichUnit) =>
        whichUnit is null || !Armed(whichUnit) ? 0 : Own(whichUnit, Acquire, whichUnit.Type.Real(Acquire));

    /// <summary>
    /// The type's acquisition range, also for a unit without an attack; its day sight where the data has none (probe
    /// units: a barracks 900, its sight).
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitDefaultAcquireRange(unit? whichUnit) =>
        whichUnit is null ? 0 : whichUnit.Type.Text(Acquire) is null ? whichUnit.Type.Real(Code("usid")) : whichUnit.Type.Real(Acquire);

    static bool Armed(unit u) => u.Type.Integer(WeaponsOn) != 0;

    /// <summary>
    /// A range of 50 or less isn't taken, nor any by a unit without an attack (probe units: 0, 7.5 and 50 not, 55 and
    /// on taken). Close: 51 to 54 aren't measured.
    /// </summary>
    [Native(Level.Close)]
    public void SetUnitAcquireRange(unit? whichUnit, float newAcquireRange)
    {
        if (whichUnit is not null && Armed(whichUnit) && newAcquireRange > 50) whichUnit.Fields[Acquire] = newAcquireRange;
    }

    /// <summary>The unit's propulsion window in radians: its type's degrees made radians as Deg2Rad makes them (probe units: 60 reads 1.047197).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitPropWindow(unit? whichUnit) => whichUnit is null ? 0 : Own(whichUnit, PropWindow, Deg2Rad(whichUnit.Type.Real(PropWindow)));

    /// <summary>The type's propulsion window in degrees.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitDefaultPropWindow(unit? whichUnit) => whichUnit?.Type.Real(PropWindow) ?? 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public void SetUnitPropWindow(unit? whichUnit, float newPropWindowAngle)
    {
        if (whichUnit is not null) whichUnit.Fields[PropWindow] = newPropWindowAngle;
    }

    /// <summary>
    /// The height set, at once, whatever the rate (the game draws the unit getting there, but reads the new height):
    /// on a ground unit too (probe units; facts, Units).
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: SetUnitFlyHeight on a ground unit reads the new height (10-21); " + UnitsProbe)]
    public float GetUnitFlyHeight(unit? whichUnit) => whichUnit?.FlyHeight ?? 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitDefaultFlyHeight(unit? whichUnit) => whichUnit is null ? 0 : whichUnit.Type.Real(Code("umvh"));

    /// <summary>The rate isn't kept: the height reads the new one at once.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public void SetUnitFlyHeight(unit? whichUnit, float newHeight, float rate)
    {
        if (whichUnit is not null) whichUnit.FlyHeight = newHeight;
    }

    /// <summary>
    /// The facing at this moment, in degrees from 0 below 360. Close: a unit turning by SetUnitFacing is modelled as
    /// starting 1/32 s after the call and turning at 16 x its turn rate less 3.2 radians a second, the footman's,
    /// knight's and gryphon rider's steady pace (6.4, 4.8 and 3.2 for 0.6, 0.5 and 0.4), at least 0.5, the short way
    /// round, straight to the end; the game eases into the end and its frames don't fall on the clock's steps.
    /// </summary>
    [Native(Level.Close)]
    public float GetUnitFacing(unit? whichUnit) => whichUnit is null ? 0 : Angle(whichUnit.Facing.At(Seconds));

    /// <summary>Turns the unit at its turn rate from the next frame (GetUnitFacing).</summary>
    [Native(Level.Close)]
    public void SetUnitFacing(unit? whichUnit, float facingAngle)
    {
        if (whichUnit is null) return;
        var from = GetUnitFacing(whichUnit);
        var rate = Math.Max(16 * whichUnit.Type.Real(TurnRate) - 3.2, 0.5) * 180 / Math.PI;
        whichUnit.Facing = new(from);
        whichUnit.Facing.Go(from + Turn(from, facingAngle), Seconds + 1.0 / 32, rate);
    }

    /// <summary>As SetUnitFacing over the duration given; Close, not measured.</summary>
    [Native(Level.Close)]
    public void SetUnitFacingTimed(unit? whichUnit, float facingAngle, float duration)
    {
        if (whichUnit is null) return;
        var from = GetUnitFacing(whichUnit);
        var turn = Turn(from, facingAngle);
        whichUnit.Facing = new(from);
        whichUnit.Facing.Go(from + turn, Seconds, duration > 0 ? Math.Abs(turn) / duration : 0);
    }

    // The short way round, from -180 below 180 degrees; half a turn goes up (probe units: 0 to 180 turned through 12)
    static float Turn(float from, float to) => Angle(to - from) is var turn && turn > 180 ? turn - 360 : turn;

    /// <summary>Turns the unit at once (probe units: 123.5 and -30 read 123.5 and 330).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public void BlzSetUnitFacingEx(unit? whichUnit, float facingAngle)
    {
        if (whichUnit is not null) whichUnit.Facing = new(Angle(facingAngle));
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public player? GetOwningPlayer(unit? whichUnit) => whichUnit?.Owner;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool IsUnitOwnedByPlayer(unit? whichUnit, player? whichPlayer) => whichUnit is not null && whichUnit.Owner == whichPlayer;

    /// <summary>Whether the unit's owner is the player's ally (IsPlayerAlly of the owner and the player).</summary>
    [Native(Level.Close)]
    public bool IsUnitAlly(unit? whichUnit, player? whichPlayer) => whichUnit is not null && IsPlayerAlly(whichUnit.Owner, whichPlayer);

    [Native(Level.Close)]
    public bool IsUnitEnemy(unit? whichUnit, player? whichPlayer) => whichUnit is not null && IsPlayerEnemy(whichUnit.Owner, whichPlayer);

    /// <summary>
    /// Gives the unit to the player (the units probe; the facts' "Orders and events"): a living unit first gets a stop
    /// (order 851972), whose order events read the old owner, then changes hands, then its owner-change events fire on
    /// the new owner's registrations, reading the old one from GetChangingUnitPrevOwner; given to its owner, nothing
    /// happens. A dead unit changes hands with no stop. The colour is the new owner's when asked. Close: the undefend
    /// order (852056) a unit with Defend gets after the change comes with abilities (W2-7, W2-10); and when a stop's
    /// event gives the unit away first, this call changes nothing more (not measured).
    /// </summary>
    [Native(Level.Close)]
    public void SetUnitOwner(unit? whichUnit, player? whichPlayer, bool changeColor)
    {
        if (whichUnit is null || whichPlayer is null || whichUnit.Owner == whichPlayer) return;
        var previous = whichUnit.Owner;
        if (!whichUnit.Dead) FireUnit(new(whichUnit, Order: 851972), UnitIssuedOrder, PlayerUnitIssuedOrder);
        if (whichUnit.Owner != previous) return;
        whichUnit.Owner = whichPlayer;
        if (changeColor) whichUnit.Color = whichPlayer.Color;
        FireUnit(new(whichUnit, Previous: previous), UnitChangeOwner, PlayerUnitChangeOwner);
    }

    /// <summary>The level of the unit's type, a hero's its hero level (facts, Units: a footman's is 2).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: GetUnitLevel (10-21); " + UnitsProbe)]
    public int GetUnitLevel(unit? whichUnit) => whichUnit is null ? 0 : BlzGetUnitIntegerField(whichUnit, Convert<unitintegerfield>(UnitLevel));

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitFoodUsed(unit? whichUnit) => whichUnit?.Type.Integer(FoodUsed) ?? 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitFoodMade(unit? whichUnit) => whichUnit?.Type.Integer(FoodMade) ?? 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitPointValue(unit? whichUnit) => whichUnit?.Type.Integer(Points) ?? 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitPointValueByType(int unitType) => Data.Object(ObjectKind.Unit, unitType)?.Integer(Points) ?? 0;

    /// <summary>The race of the unit's type, as common.j's RACE_* and the game's own for the rest (probe units: critters 10).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public race? GetUnitRace(unit? whichUnit) => whichUnit is null ? null : Convert<race>(Races.GetValueOrDefault(whichUnit.Type.Text(Race) ?? ""));

    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool IsUnitRace(unit? whichUnit, race? whichRace) => whichUnit is not null && whichRace?.Id == GetUnitRace(whichUnit)!.Id;

    /// <summary>The skin given at creation; the type's own otherwise.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public int BlzGetUnitSkin(unit? whichUnit) => whichUnit?.Skin ?? 0;

    [Native(Level.Recorded, Reason = "Changes the unit's model; the skin is kept for BlzGetUnitSkin")]
    public void BlzSetUnitSkin(unit? whichUnit, int skinId)
    {
        if (whichUnit is not null) whichUnit.Skin = skinId;
    }

    [Native(Level.Recorded, Reason = "Paints the unit in a player's colour; kept on the unit, which no native reads back")]
    public void SetUnitColor(unit? whichUnit, playercolor? whichColor)
    {
        if (whichUnit is not null && whichColor is not null) whichUnit.Color = whichColor.Id;
    }

    /// <summary>The game draws it by the x scale alone (facts, Units); UNIT_RF_SCALING_VALUE keeps the type's.</summary>
    [Native(Level.Recorded, Reason = "Sizes the unit's model; kept on the unit, which no native reads back (UNIT_RF_SCALING_VALUE keeps the type's)")]
    public void SetUnitScale(unit? whichUnit, float scaleX, float scaleY, float scaleZ)
    {
        if (whichUnit is not null) whichUnit.Scale = scaleX;
    }

    /// <summary>Each part kept from 0 to 255 (facts, Units: 300 reads as 255, -50 as 0); the tint fields stay 255.</summary>
    [Native(Level.Recorded, Reason = "Tints the unit's model; kept on the unit, which no native reads back (the tint fields stay 255)")]
    public void SetUnitVertexColor(unit? whichUnit, int red, int green, int blue, int alpha)
    {
        if (whichUnit is not null) whichUnit.Tint = [Math.Clamp(red, 0, 255), Math.Clamp(green, 0, 255), Math.Clamp(blue, 0, 255), Math.Clamp(alpha, 0, 255)];
    }

    /// <summary>
    /// The unit's kinds (probe units): hero, dead, structure, flying and ground (neither flying nor a structure), from
    /// its enabled weapons attacks ground, attacks flying, melee (a weapon of type normal) and ranged, and its type's
    /// classification. Close: stunned, plagued, snared, poisoned, polymorphed, sleeping, resistant, ethereal and magic
    /// immune need buffs and abilities (wave 4) and read false.
    /// </summary>
    [Native(Level.Close)]
    public bool IsUnitType(unit? whichUnit, unittype? whichUnitType)
    {
        if (whichUnit is null || whichUnitType is null) return false;
        var type = whichUnit.Type;
        bool Is(string category) => (BlzGetUnitIntegerField(whichUnit, Convert<unitintegerfield>(Classification)) & Categories[category]) != 0;
        // Its enabled weapons, of 1 and 2: their type and the flags of their targets
        IEnumerable<(string? Kind, int Targets)> Weapons() => Enumerable.Range(1, 2).Where(n => (type.Integer(WeaponsOn) & n) != 0)
            .Select(n => (type.Text(Code($"ua{n}w")), Flags(type.Text(Code($"ua{n}g")), TargetFlags)));
        return whichUnitType.Id switch
        {
            0 => IsHero(type.Id),
            1 => whichUnit.Dead,
            2 => IsBuildingNow(whichUnit),
            3 => Flies(whichUnit),
            4 => !Flies(whichUnit) && !IsBuildingNow(whichUnit),
            5 => Weapons().Any(w => (w.Targets & TargetFlags["air"]) != 0),
            6 => Weapons().Any(w => (w.Targets & TargetFlags["ground"]) != 0),
            7 => Weapons().FirstOrDefault().Kind is "normal",
            8 => Weapons().FirstOrDefault().Kind is { } kind && kind != "normal",
            9 => Is("giant"),
            10 => Is("summoned"),
            14 => Is("undead"),
            15 => Is("mechanical"),
            16 => Is("peon"),
            17 => Is("sapper"),
            18 => Is("townhall"),
            19 => Is("ancient"),
            20 => Is("tauren"),
            _ => false,
        };
    }
}
