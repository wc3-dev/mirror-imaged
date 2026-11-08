using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// The unit field natives, as the units probe reads and sets every one of them on a footman, a Paladin, a barracks and a
// gryphon rider (docs/facts.md, "Units"). A field reads the unit's type's data (as a number, a word read as the
// constant common.j gives it, or a list of words read as flags), or the unit's own state where the field is one; a set
// changes only that unit. The data's useUnit, useHero and useBuilding aren't read: a field applies to any unit, but for
// what only a hero has: its attributes and their gains, its proper names and its primary attribute (decisions D220).
public sealed partial class GameNatives
{
    static readonly int Agility = Code("uagc"), AgilityPermanent = Code("uagm"), AgilityWithBonus = Code("uagb"),
        IntelligenceNow = Code("uinc"), IntelligencePermanent = Code("uinm"), IntelligenceWithBonus = Code("uinb"),
        StrengthNow = Code("ustc"), StrengthPermanent = Code("ustm"), StrengthWithBonus = Code("ustb"),
        UnitLevel = Code("ulev"), ArmorType = Code("uarm"), DefenseType = Code("udty"), RegenType = Code("uhrt"), MoveType = Code("umvt"),
        PreventedBy = Code("upar"), Primary = Code("upra"), TargetedAs = Code("utar"), Classification = Code("utyp"),
        TintAlpha = Code("ucal"), TintRed = Code("uclr"), TintGreen = Code("uclg"), TintBlue = Code("uclb"),
        Acquire = Code("uacq"), AgilityPerLevel = Code("uagp"), IntelligencePerLevel = Code("uinp"), StrengthPerLevel = Code("ustp"),
        Defense = Code("udfc"), FlyHeightNow = Code("ufyh"), FlyMaxHeight = Code("ufmh"), HitPointsNow = Code("uhpc"), ManaNow = Code("umpc"),
        MaxPitch = Code("umxp"), MaxRoll = Code("umxr"), MinimumRange = Code("uamn"), ShadowHeight = Code("ushh"), ShadowWidth = Code("ushw"),
        Sight = Code("usir"), SpeedNow = Code("umvc"), TurnRate = Code("umvr"), Decayable = Code("udec"), Raisable = Code("urai"),
        ProperNames = Code("upro");

    // Words the data writes and the numbers the natives give for them: common.j's constants (probe units)
    static readonly Dictionary<string, int> ArmorTypes = Words("flesh 1 metal 2 wood 3 ethereal 4 stone 5");
    static readonly Dictionary<string, int> DefenseTypes = Words("small 0 medium 1 large 2 fort 3 normal 4 hero 5 divine 6 none 7");
    static readonly Dictionary<string, int> RegenTypes = Words("none 0 always 1 blight 2 day 3 night 4");
    static readonly Dictionary<string, int> MoveTypes = Words("foot 1 fly 2 horse 4 hover 8 float 16 amph 32");
    static readonly Dictionary<string, int> Attributes = Words("str 1 int 2 agi 3");
    static readonly Dictionary<string, int> PathingFlags = Words("unwalkable 2 unflyable 4 unbuildable 8 unpeonharvest 16 blighted 32 unfloat 64 unamph 128");
    static readonly Dictionary<string, int> TargetFlags = Words("none 1 ground 2 air 4 structure 8 ward 16 item 32 tree 64 wall 128 debris 256 decoration 512 bridge 1024");
    static readonly Dictionary<string, int> Categories = Words("giant 1 undead 2 summoned 4 mechanical 8 peon 16 sapper 32 townhall 64 ancient 128 neutral 256 ward 512 standon 1024 tauren 2048");

    static Dictionary<string, int> Words(string pairs)
    {
        var parts = pairs.Split(' ');
        return Enumerable.Range(0, parts.Length / 2).ToDictionary(i => parts[2 * i], i => int.Parse(parts[2 * i + 1]), StringComparer.OrdinalIgnoreCase);
    }

    // The flags of a comma list's words; a word it doesn't know adds none
    static int Flags(string? list, Dictionary<string, int> words) =>
        (list ?? "").Split(',').Aggregate(0, (flags, word) => flags | words.GetValueOrDefault(word.Trim()));

    static bool IsAttribute(int field) =>
        field == Agility || field == AgilityPermanent || field == AgilityWithBonus || field == IntelligenceNow || field == IntelligencePermanent
        || field == IntelligenceWithBonus || field == StrengthNow || field == StrengthPermanent || field == StrengthWithBonus;

    // A hero's attribute read on a unit that isn't one closes the game (probe units-attribute)
    void Attribute(unit u, int field)
    {
        if (!IsHero(u.Type.Id) && IsAttribute(field))
            throw new GameClosedException($"The game closes: a hero attribute field ({Name(field)}) read or set on {Name(u.Type.Id)}, which is no hero");
    }

    static string Name(int id) => new([(char)(id >> 24 & 255), (char)(id >> 16 & 255), (char)(id >> 8 & 255), (char)(id & 255)]);

    // What a set left in the field, if a set of this field's type did: a field id given as another type (a Convert of
    // one's own) reads the type's value
    static T Own<T>(unit u, int field, T otherwise) => u.Fields.TryGetValue(field, out var value) && value is T own ? own : otherwise;

    /// <summary>
    /// The unit's integer field: its type's number, or the constant for its word (armour, defence, regeneration and
    /// move types, primary attribute), or the flags of its words (targeted as, classification, placement prevented
    /// by), unless a set changed it; a hero's level for a hero. A hero attribute field on a unit that isn't a hero
    /// closes the game (probe units-attribute). Close: with no bonuses and no permanent ones yet (W2-3), an attribute's
    /// permanent and with-bonus fields read the attribute itself; the game read the permanent one 1 after its own set.
    /// </summary>
    [Native(Level.Close)]
    public int BlzGetUnitIntegerField(unit? whichUnit, unitintegerfield? whichField)
    {
        if (whichUnit is null || whichField is null) return 0;
        var (u, field, type) = (whichUnit, whichField.Id, whichUnit.Type);
        Attribute(u, field);
        if (field == Agility || field == AgilityPermanent || field == AgilityWithBonus) return Own(u, Agility, type.Integer(Code("uagi")));
        if (field == IntelligenceNow || field == IntelligencePermanent || field == IntelligenceWithBonus) return Own(u, IntelligenceNow, type.Integer(Intelligence));
        if (field == StrengthNow || field == StrengthPermanent || field == StrengthWithBonus) return Own(u, StrengthNow, type.Integer(Strength));
        if (field == UnitLevel) return IsHero(type.Id) ? u.HeroLevel : Own(u, field, type.Integer(UnitLevel));
        if (field == TintAlpha || field == TintRed || field == TintGreen || field == TintBlue) return 255;
        return Own(u, field, field == ArmorType ? ArmorTypes.GetValueOrDefault(type.Text(field) ?? "")
            : field == DefenseType ? DefenseTypes.GetValueOrDefault(type.Text(field) ?? "")
            : field == RegenType ? RegenTypes.GetValueOrDefault(type.Text(field) ?? "")
            : field == MoveType ? MoveTypes.GetValueOrDefault(type.Text(field) ?? "")
            : field == Primary ? Attributes.GetValueOrDefault(type.Text(field) ?? "", 1)
            : field == PreventedBy ? Flags(type.Text(Code("upap")), PathingFlags)
            : field == TargetedAs ? Flags(type.Text(field), TargetFlags)
            : field == Classification ? Flags(type.Text(field), Categories)
            : type.Integer(field));
    }

    /// <summary>
    /// Sets the unit's integer field and answers true (probe units), but: the attributes with their bonus can't be set
    /// (false); a hero's level becomes one more than the value (3 reads 4, 7 reads 8, both from level 1, so adding the
    /// value fits as well: inferred, W2-3 to probe from level 5); the primary attribute and the tint stay as they were,
    /// and the primary attribute of a unit that isn't a hero, set to an attribute (1 to 3), closes the game (probe
    /// units-primary; 1 and 2 inferred), and so does setting a hero attribute there (inferred: only the read is
    /// measured). Close: a hero's attribute set changes only the field here, not its life, mana or armour, and its
    /// permanent attribute (read 1 after a set of 7 and of 0) isn't modelled; heroes are W2-3's.
    /// </summary>
    [Native(Level.Close)]
    public bool BlzSetUnitIntegerField(unit? whichUnit, unitintegerfield? whichField, int value)
    {
        if (whichUnit is null || whichField is null) return false;
        var (u, field) = (whichUnit, whichField.Id);
        Attribute(u, field);
        if (field == AgilityWithBonus || field == IntelligenceWithBonus || field == StrengthWithBonus) return false;
        if (field == Primary && value is >= 1 and <= 3 && !IsHero(u.Type.Id))
            throw new GameClosedException($"The game closes: the primary attribute of {Name(u.Type.Id)}, which is no hero, set to {value}");
        if (field == UnitLevel && IsHero(u.Type.Id)) u.HeroLevel = value + 1;
        else if (field != Primary && field != TintAlpha && field != TintRed && field != TintGreen && field != TintBlue
            && field != AgilityPermanent && field != IntelligencePermanent && field != StrengthPermanent) u.Fields[field] = value;
        return true;
    }

    /// <summary>
    /// The unit's real field: its type's number, or its state where the field is one (life, mana, move speed, turn rate,
    /// acquisition range, fly height, armour, sight at this time of day), unless a set changed it. The hero gains per
    /// level read 0 on a unit that isn't a hero; the most pitch and roll always 0 (probe units).
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float BlzGetUnitRealField(unit? whichUnit, unitrealfield? whichField)
    {
        if (whichUnit is null || whichField is null) return 0;
        var (u, field, type) = (whichUnit, whichField.Id, whichUnit.Type);
        if (field == HitPointsNow) return u.Life;
        if (field == ManaNow) return u.Mana;
        if (field == SpeedNow) return GetUnitMoveSpeed(u);
        if (field == TurnRate) return GetUnitTurnSpeed(u);
        if (field == Acquire) return GetUnitAcquireRange(u);
        if (field == FlyHeightNow) return GetUnitFlyHeight(u);
        if (field == MaxPitch || field == MaxRoll) return 0;
        if ((field == AgilityPerLevel || field == IntelligencePerLevel || field == StrengthPerLevel) && !IsHero(type.Id)) return 0;
        return Own(u, field, field == Defense ? type.Real(Code("udef")) + HeroArmor(u)
            : field == Sight ? type.Real(Code(IsDay ? "usid" : "usin"))
            : field == FlyMaxHeight ? type.Real(Code("umvh"))
            : type.Real(field));
    }

    // A hero's agility adds to its armour (the constants' AgiDefenseBase and AgiDefenseBonus; probe units: 2 + -2 + 13 x 0.3)
    float HeroArmor(unit u) => IsHero(u.Type.Id)
        ? Data.Constant("AgiDefenseBase", 0) + u.Type.Integer(Code("uagi")) * Data.Constant("AgiDefenseBonus", 0)
        : 0;

    bool IsDay => GetFloatGameState(Convert<fgamestate>(2)) is var hour && hour >= Data.Constant("Dawn", 6) && hour < Data.Constant("Dusk", 18);

    /// <summary>
    /// Sets the unit's real field; life, mana, move speed, turn rate and acquisition range as their own natives set them.
    /// False for a hero gain per level on a unit that isn't a hero, for the most pitch and roll, and for the least attack
    /// range of a unit with no attack; true but not kept for the shadow's size, the sight and a ground unit's fly height
    /// (probe units).
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool BlzSetUnitRealField(unit? whichUnit, unitrealfield? whichField, float value)
    {
        if (whichUnit is null || whichField is null) return false;
        var (u, field, type) = (whichUnit, whichField.Id, whichUnit.Type);
        if (field == MaxPitch || field == MaxRoll) return false;
        if ((field == AgilityPerLevel || field == IntelligencePerLevel || field == StrengthPerLevel) && !IsHero(type.Id)) return false;
        if (field == MinimumRange && type.Integer(Code("uaen")) == 0) return false;
        if (field == HitPointsNow) SetLife(u, value);
        else if (field == ManaNow) SetUnitState(u, Convert<unitstate>(ManaState), value);
        else if (field == SpeedNow) SetUnitMoveSpeed(u, value);
        else if (field == TurnRate) SetUnitTurnSpeed(u, value);
        else if (field == Acquire) SetUnitAcquireRange(u, value);
        else if (field == FlyHeightNow) { if (Flies(u)) SetUnitFlyHeight(u, value, 0); }
        else if (field != ShadowHeight && field != ShadowWidth && field != Sight) u.Fields[field] = value;
        // A flier's height is kept from its least height (moveFloor) up to the most set (probe units: 100 to 90 under 50)
        if (field == FlyMaxHeight && Flies(u)) SetUnitFlyHeight(u, Math.Max(Math.Min(GetUnitFlyHeight(u), value), type.Real(Code("umvf"))), 0);
        return true;
    }

    bool Flies(unit u) => BlzGetUnitIntegerField(u, Convert<unitintegerfield>(MoveType)) == 2;

    static readonly int NeutralMinimapIcon = Code("unbm"), HideOnMinimap = Code("uhom");

    /// <summary>
    /// The unit's boolean field: its type's, decayable and raisable from its death type (probe units: the footman's 3
    /// both, the Paladin's and barracks' 2 decays, the gryphon rider's 0 neither), a neutral building's minimap icon
    /// true where the data has none, unless a set changed it.
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool BlzGetUnitBooleanField(unit? whichUnit, unitbooleanfield? whichField)
    {
        if (whichUnit is null || whichField is null) return false;
        var (field, type) = (whichField.Id, whichUnit.Type);
        return Own(whichUnit, field, field == Decayable ? (type.Integer(Code("udea")) & 2) != 0
            : field == Raisable ? (type.Integer(Code("udea")) & 1) != 0
            : field == NeutralMinimapIcon && type.Text(field) is null || type.Integer(field) != 0);
    }

    /// <summary>Sets the unit's boolean field and answers true; hiding it on the minimap isn't kept (probe units).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool BlzSetUnitBooleanField(unit? whichUnit, unitbooleanfield? whichField, bool value)
    {
        if (whichUnit is null || whichField is null) return false;
        if (whichField.Id != HideOnMinimap) whichUnit.Fields[whichField.Id] = value;
        return true;
    }

    /// <summary>
    /// The unit's string field: its type's, its name as GetUnitName reads it; a hero's proper name, which a unit that
    /// isn't a hero doesn't have or keep (probe units). Close: a hero's proper name is the first of its type's here; the
    /// game draws one at random and numbers it once taken ("Zann the Defender XII"), W2-3's.
    /// </summary>
    [Native(Level.Close)]
    public string? BlzGetUnitStringField(unit? whichUnit, unitstringfield? whichField)
    {
        if (whichUnit is null || whichField is null) return "";
        var (field, type) = (whichField.Id, whichUnit.Type);
        if (field == UnitName) return GetUnitName(whichUnit);
        if (field == ProperNames) return IsHero(type.Id) ? Own(whichUnit, field, (type.Text(field) ?? "").Split(',')[0]) : "";
        return Own(whichUnit, field, type.Text(field) ?? "");
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool BlzSetUnitStringField(unit? whichUnit, unitstringfield? whichField, string? value)
    {
        if (whichUnit is null || whichField is null || value is null) return false;
        if (whichField.Id == UnitName) BlzSetUnitName(whichUnit, value);
        else if (whichField.Id != ProperNames || IsHero(whichUnit.Type.Id)) whichUnit.Fields[whichField.Id] = value;
        return true;
    }
}
