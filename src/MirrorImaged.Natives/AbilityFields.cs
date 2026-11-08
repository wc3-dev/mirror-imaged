using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// The ability field natives on a unit's ability, and its cooldown and mana cost, as the probes abilities-fields,
// abilities-columns and abilities-sets read and set them (docs/facts.md, "Abilities"). A native takes only the fields
// common.j names for its kind. A level counts from 0. A field kept in a table column (DataA, Cool) reads the column the
// ability keeps under its own field there (GameData.Own), so a field of another ability on that column reads it too, when
// it is of the same type; a set goes to that column, with its type. A field of the text files (tooltips, icons) reads the
// ability type's (AbilityTypes.cs) unless the ability's own was set.
public sealed partial class GameNatives
{
    const string FieldsProbe = "facts.md, Abilities: probes abilities-fields, abilities-columns and abilities-sets (11-08)";
    static readonly int LevelCount = Code("alev"), LevelSkip = Code("alsk"), TargetAttachments = Code("atac"), CasterAttachments = Code("acac"),
        HeroAbility = Code("aher"), ItemAbility = Code("aite"), AbilityName = Code("anam"), Cooldown = Code("acdn"), ManaCost = Code("amcs");

    Dictionary<int, Type>? _abilityFields;

    // The kind of field native common.j gives an ability field: ABILITY_RLF_COOLDOWN's is abilityreallevelfield
    Type? Declared(int field) => (_abilityFields ??= Globals()
        .Where(global => global.Name.StartsWith("ABILITY_", StringComparison.Ordinal) && global.Value is handle)
        .ToDictionary(global => ((handle)global.Value!).Id, global => global.Value!.GetType())).GetValueOrDefault(field);

    bool Declares(handle field) => Declared(field.Id) == field.GetType();

    static Type? ValueOf(Type? field) =>
        field == typeof(abilityintegerlevelfield) || field == typeof(abilityintegerfield) ? typeof(int)
        : field == typeof(abilityreallevelfield) || field == typeof(abilityrealfield) ? typeof(float)
        : field == typeof(abilitybooleanlevelfield) || field == typeof(abilitybooleanfield) ? typeof(bool)
        : field == typeof(abilitystringlevelfield) || field == typeof(abilitystringfield) ? typeof(string)
        : null;

    // What a column holds for an ability: the type of its own field there
    Type CellOf(int own) => ValueOf(Declared(own)) ?? Data.Field(ObjectKind.Ability, own)?.Type switch
    {
        "bool" => typeof(bool),
        "int" => typeof(int),
        "real" or "unreal" => typeof(float),
        _ => typeof(string),
    };

    object Cell(GameObject type, int own, int level, Type kind) =>
        kind == typeof(int) ? (Data.Field(ObjectKind.Ability, own)?.Type.EndsWith("Code", StringComparison.Ordinal) == true && type.Text(own, level) is { Length: 4 } code ? Code(code) : type.Integer(own, level))
        : kind == typeof(float) ? type.Real(own, level)
        : kind == typeof(bool) ? type.Integer(own, level) != 0
        : type.Text(own, level) ?? "";

    T Read<T>(ability? a, handle? field, int level, T none)
    {
        if (a is null || field is null || !Declares(field)) return none;
        if (Data.Field(ObjectKind.Ability, field.Id) is { Profile: true } where)
            return typeof(T) == typeof(string) ? (T)(object)Text(a, field.Id, where, level) : none;
        if (level < 0 || level >= a.Type.Levels) return none;
        var own = Data.Own(a.Type, field.Id);
        var value = a.Sets.TryGetValue((own, level), out var set) ? set : CellOf(own) == typeof(T) ? Cell(a.Type, own, level + 1, typeof(T)) : null;
        if (value is bool && set is not null)
            throw new GameClosedException($"The game closes: a boolean set on {Name(a.Type.Id)}'s {Name(own)} read back");
        return value is T read ? read : none;
    }

    // A set past the type's levels is refused, though ABILITY_IF_LEVELS gives more (probe abilities-sets); a text field's
    // answers true, kept only within its entries
    bool Write(ability? a, handle? field, int level, object value)
    {
        if (a is null || field is null || !Declares(field)) return false;
        if (Data.Field(ObjectKind.Ability, field.Id) is { Profile: true } where)
        {
            if (value is string { Length: > 0 } text && level >= 0 && level < Entries(a.Type, field.Id, where)) a.Sets[(field.Id, level)] = text;
            return true;
        }
        if (level < 0 || level >= a.Type.Levels) return false;
        var own = Data.Own(a.Type, field.Id);
        if (value is bool && CellOf(own) == typeof(bool))
            throw new GameClosedException($"The game closes: a boolean level field ({Name(field.Id)}) set on {Name(a.Type.Id)}");
        a.Sets[(own, level)] = value;
        return true;
    }

    // A text field of the ability: its own set, else its type's; a level outside its entries reads the last
    string Text(ability a, int field, Field where, int level)
    {
        var count = Entries(a.Type, field, where);
        if (count == 0) return Missing(where.Column);
        var at = level >= 0 && level < count ? level : count - 1;
        return a.Sets.TryGetValue((field, at), out var own) ? (string)own : TypeText(a.Type, field, where, at);
    }

    /// <summary>
    /// An integer field without levels: the level count (its own, which SetUnitAbilityLevel keeps to), the level needed
    /// and skipped, priority, the button places (the type's, BlzSetAbilityPosX's too), missile speed and the attachment
    /// counts (1 where the data has none); 0 for a field of another kind (probes abilities-fields and abilities-columns).
    /// </summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public int BlzGetAbilityIntegerField(ability? whichAbility, abilityintegerfield? whichField)
    {
        if (whichAbility is not { } a || whichField is not { } f || !Declares(f)) return 0;
        if (f.Id == LevelCount) return a.Levels;
        if (a.Sets.TryGetValue((f.Id, -1), out var set)) return (int)set;
        if (Data.Field(ObjectKind.Ability, f.Id) is { } where && IsPlace(where.Column)) return TypePlace(a.Type.Id, where.Column, where.Element);
        return a.Type.Text(f.Id) is null && (f.Id == TargetAttachments || f.Id == CasterAttachments) ? 1 : a.Type.Integer(f.Id);
    }

    /// <summary>
    /// Sets it for this ability and answers true; the level count lets SetUnitAbilityLevel go to it (probe
    /// abilities-fields). The level skip and attachment counts refuse a set (false; probe abilities-sets).
    /// </summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public bool BlzSetAbilityIntegerField(ability? whichAbility, abilityintegerfield? whichField, int value)
    {
        if (whichAbility is not { } a || whichField is not { } f || !Declares(f) || f.Id == LevelSkip || f.Id == TargetAttachments || f.Id == CasterAttachments) return false;
        if (f.Id == LevelCount) a.Levels = value;
        else a.Sets[(f.Id, -1)] = value;
        return true;
    }

    [Native(Level.Exact, Fact = FieldsProbe)]
    public float BlzGetAbilityRealField(ability? whichAbility, abilityrealfield? whichField) =>
        whichAbility is not { } a || whichField is not { } f || !Declares(f) ? 0 : a.Sets.TryGetValue((f.Id, -1), out var set) ? (float)set : a.Type.Real(f.Id);

    [Native(Level.Exact, Fact = FieldsProbe)]
    public bool BlzSetAbilityRealField(ability? whichAbility, abilityrealfield? whichField, float value)
    {
        if (whichAbility is not { } a || whichField is not { } f || !Declares(f)) return false;
        a.Sets[(f.Id, -1)] = value;
        return true;
    }

    /// <summary>
    /// Whether it is a hero skill and whether it checks its requirements, from the data, and whether an item carries it:
    /// false for one added to a unit (probe abilities-fields; game-facts: true on Claws of Attack's carrier, W2-3's).
    /// </summary>
    [Native(Level.Exact, Fact = FieldsProbe + "; game-facts.md, Spells and abilities: ABILITY_BF_HERO_ABILITY (11-02)")]
    public bool BlzGetAbilityBooleanField(ability? whichAbility, abilitybooleanfield? whichField) =>
        whichAbility is { } a && whichField is { } f && Declares(f)
        && (a.Sets.TryGetValue((f.Id, -1), out var set) ? (bool)set : f.Id != ItemAbility && a.Type.Integer(f.Id) != 0);

    /// <summary>Sets it for this ability and answers true; being a hero skill refuses a set (probes abilities-fields and abilities-sets).</summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public bool BlzSetAbilityBooleanField(ability? whichAbility, abilitybooleanfield? whichField, bool value)
    {
        if (whichAbility is not { } a || whichField is not { } f || !Declares(f) || f.Id == HeroAbility) return false;
        a.Sets[(f.Id, -1)] = value;
        return true;
    }

    /// <summary>
    /// Its name, sounds ("" without) and the activated and research icons (the type's, BTNTemp without); "" for nil
    /// (probes abilities-fields and abilities-types).
    /// </summary>
    [Native(Level.Exact, Fact = FieldsProbe + "; facts.md, Abilities: probe abilities-types")]
    public string? BlzGetAbilityStringField(ability? whichAbility, abilitystringfield? whichField)
    {
        if (whichAbility is not { } a || whichField is not { } f || !Declares(f)) return "";
        if (a.Sets.TryGetValue((f.Id, -1), out var set)) return (string)set;
        return Data.Field(ObjectKind.Ability, f.Id) is { } where ? Text(a, f.Id, where, 0) : a.Type.Text(f.Id) ?? "";
    }

    /// <summary>Sets it for this ability and answers true, the type's staying; the name refuses a set (probe abilities-sets).</summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public bool BlzSetAbilityStringField(ability? whichAbility, abilitystringfield? whichField, string? value)
    {
        if (whichAbility is not { } a || whichField is not { } f || value is null || !Declares(f) || f.Id == AbilityName) return false;
        a.Sets[(f.Id, -1)] = value;
        return true;
    }

    /// <summary>
    /// The level's value (from 0); 0 outside the ability's levels, for a field of another kind, and where the column holds
    /// another type for this ability (Blizzard's waves read as a real) (probes abilities-fields and abilities-columns).
    /// </summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public int BlzGetAbilityIntegerLevelField(ability? whichAbility, abilityintegerlevelfield? whichField, int level) => Read(whichAbility, whichField, level, 0);

    /// <summary>Sets the level's value for this ability, the column then holding an integer; false outside its levels (probes abilities-fields and abilities-columns).</summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public bool BlzSetAbilityIntegerLevelField(ability? whichAbility, abilityintegerlevelfield? whichField, int level, int value) => Write(whichAbility, whichField, level, value);

    [Native(Level.Exact, Fact = FieldsProbe)]
    public float BlzGetAbilityRealLevelField(ability? whichAbility, abilityreallevelfield? whichField, int level) => Read(whichAbility, whichField, level, 0f);

    [Native(Level.Exact, Fact = FieldsProbe)]
    public bool BlzSetAbilityRealLevelField(ability? whichAbility, abilityreallevelfield? whichField, int level, float value) => Write(whichAbility, whichField, level, value);

    /// <summary>
    /// As the other kinds; a boolean a set put in a column closes the game when read back (probe abilities-boolean:
    /// inferred, as the game closed after the set answered true and before the read's line came).
    /// </summary>
    [Native(Level.Exact, Fact = FieldsProbe + "; facts.md, Abilities: probe abilities-boolean")]
    public bool BlzGetAbilityBooleanLevelField(ability? whichAbility, abilitybooleanlevelfield? whichField, int level) => Read(whichAbility, whichField, level, false);

    /// <summary>
    /// False past the ability's levels and for a field common.j doesn't name; true on a column holding another type, as
    /// the other kinds (probe abilities-boolean); on one holding a boolean the game closes: Divine Shield's
    /// can-deactivate (probe abilities-columns), a Paladin's inventory's drop-items-on-death (game-facts).
    /// </summary>
    [Native(Level.Exact, Fact = "facts.md, Abilities: probes abilities-columns and abilities-boolean; game-facts.md, Orders and events: ABILITY_BLF_DROP_ITEMS_ON_DEATH (11-02)")]
    public bool BlzSetAbilityBooleanLevelField(ability? whichAbility, abilitybooleanlevelfield? whichField, int level, bool value) => Write(whichAbility, whichField, level, value);

    /// <summary>
    /// A text field (tooltips, icons, arts) reads the level's entry, the last outside them, the ability type's unless this
    /// one's was set; a table column as the other kinds do (probes abilities-fields, abilities-columns and abilities-types).
    /// Close: an extended tooltip's <c>&lt;id,column&gt;</c> tags stay as written, which the game fills (W2-8).
    /// </summary>
    [Native(Level.Close)]
    public string? BlzGetAbilityStringLevelField(ability? whichAbility, abilitystringlevelfield? whichField, int level) => Read(whichAbility, whichField, level, "");

    /// <summary>
    /// Sets this ability's own; a text field answers true but keeps only a level within its entries, none where the data
    /// has none (a turn-off tooltip Blizzard lacks) (probes abilities-fields and abilities-columns).
    /// </summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public bool BlzSetAbilityStringLevelField(ability? whichAbility, abilitystringlevelfield? whichField, int level, string? value) =>
        value is not null && Write(whichAbility, whichField, level, value);

    // No ability field read as a level array held anything: buffs, effects, targets, arts, requirements and data, read as
    // each of the four kinds at indexes -1 to 2, and every set, add and remove answered false (probes abilities-fields
    // and abilities-columns). Close: not every field was tried.
    [Native(Level.Close)] public int BlzGetAbilityIntegerLevelArrayField(ability? whichAbility, abilityintegerlevelarrayfield? whichField, int level, int index) => 0;
    [Native(Level.Close)] public float BlzGetAbilityRealLevelArrayField(ability? whichAbility, abilityreallevelarrayfield? whichField, int level, int index) => 0;
    [Native(Level.Close)] public bool BlzGetAbilityBooleanLevelArrayField(ability? whichAbility, abilitybooleanlevelarrayfield? whichField, int level, int index) => false;
    [Native(Level.Close)] public string? BlzGetAbilityStringLevelArrayField(ability? whichAbility, abilitystringlevelarrayfield? whichField, int level, int index) => "";
    [Native(Level.Close)] public bool BlzSetAbilityIntegerLevelArrayField(ability? whichAbility, abilityintegerlevelarrayfield? whichField, int level, int index, int value) => false;
    [Native(Level.Close)] public bool BlzSetAbilityRealLevelArrayField(ability? whichAbility, abilityreallevelarrayfield? whichField, int level, int index, float value) => false;
    [Native(Level.Close)] public bool BlzSetAbilityBooleanLevelArrayField(ability? whichAbility, abilitybooleanlevelarrayfield? whichField, int level, int index, bool value) => false;
    [Native(Level.Close)] public bool BlzSetAbilityStringLevelArrayField(ability? whichAbility, abilitystringlevelarrayfield? whichField, int level, int index, string? value) => false;
    [Native(Level.Close)] public bool BlzAddAbilityIntegerLevelArrayField(ability? whichAbility, abilityintegerlevelarrayfield? whichField, int level, int value) => false;
    [Native(Level.Close)] public bool BlzAddAbilityRealLevelArrayField(ability? whichAbility, abilityreallevelarrayfield? whichField, int level, float value) => false;
    [Native(Level.Close)] public bool BlzAddAbilityBooleanLevelArrayField(ability? whichAbility, abilitybooleanlevelarrayfield? whichField, int level, bool value) => false;
    [Native(Level.Close)] public bool BlzAddAbilityStringLevelArrayField(ability? whichAbility, abilitystringlevelarrayfield? whichField, int level, string? value) => false;
    [Native(Level.Close)] public bool BlzRemoveAbilityIntegerLevelArrayField(ability? whichAbility, abilityintegerlevelarrayfield? whichField, int level, int value) => false;
    [Native(Level.Close)] public bool BlzRemoveAbilityRealLevelArrayField(ability? whichAbility, abilityreallevelarrayfield? whichField, int level, float value) => false;
    [Native(Level.Close)] public bool BlzRemoveAbilityBooleanLevelArrayField(ability? whichAbility, abilitybooleanlevelarrayfield? whichField, int level, bool value) => false;
    [Native(Level.Close)] public bool BlzRemoveAbilityStringLevelArrayField(ability? whichAbility, abilitystringlevelarrayfield? whichField, int level, string? value) => false;

    /// <summary>The unit's ability's cooldown at the level (from 0), as ABILITY_RLF_COOLDOWN reads it; 0 past its levels and for one the unit lacks.</summary>
    [Native(Level.Exact, Fact = FieldsProbe + "; game-facts.md, Spells and abilities: count levels from 0 (10-14)")]
    public float BlzGetUnitAbilityCooldown(unit? whichUnit, int abilId, int level) => Read(Find(whichUnit, abilId), Convert<abilityreallevelfield>(Cooldown), level, 0f);

    /// <summary>Sets the unit's ability's cooldown at the level, as ABILITY_RLF_COOLDOWN does; past its levels, nothing.</summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public void BlzSetUnitAbilityCooldown(unit? whichUnit, int abilId, int level, float cooldown) => Write(Find(whichUnit, abilId), Convert<abilityreallevelfield>(Cooldown), level, cooldown);

    [Native(Level.Exact, Fact = FieldsProbe + "; game-facts.md, Spells and abilities: count levels from 0 (10-14)")]
    public int BlzGetUnitAbilityManaCost(unit? whichUnit, int abilId, int level) => Read(Find(whichUnit, abilId), Convert<abilityintegerlevelfield>(ManaCost), level, 0);

    [Native(Level.Exact, Fact = FieldsProbe)]
    public void BlzSetUnitAbilityManaCost(unit? whichUnit, int abilId, int level, int manaCost) => Write(Find(whichUnit, abilId), Convert<abilityintegerlevelfield>(ManaCost), level, manaCost);

    // A cooldown started without a cast (probes abilities-fields and abilities-sets): what is left, counting down with the
    // game's time, and the share of it gone. Close: the game's reads carry its frames' float error (4.999999 for 5, a
    // share of 1.192093e-07 at the start), and the cast that starts one is wave 4's.
    float CooldownLeft(ability a) => a.CooldownEnd > Seconds ? (float)(a.CooldownEnd - Seconds) : 0;

    void Running(unit? u, int id, Action<ability> change)
    {
        if (Find(u, id) is { } a && CooldownLeft(a) > 0) change(a);
    }

    /// <summary>What is left of the unit's ability's cooldown; 0 with none running and for one the unit lacks.</summary>
    [Native(Level.Close)]
    public float BlzGetUnitAbilityCooldownRemaining(unit? whichUnit, int abilId) => Find(whichUnit, abilId) is { } a ? CooldownLeft(a) : 0;

    /// <summary>The share of the cooldown gone, 1 less what is left over its length: below 0 when something added to it; 0 with none running.</summary>
    [Native(Level.Close)]
    public float BlzGetUnitAbilityCooldownPercent(unit? whichUnit, int abilId) =>
        Find(whichUnit, abilId) is { } a && CooldownLeft(a) is > 0 and var left ? 1 - left / (float)a.Cooldown : 0;

    /// <summary>Starts a cooldown of that length, also over one running; one of 0 or less, and on an ability the unit lacks, none.</summary>
    [Native(Level.Close)]
    public void BlzStartUnitAbilityCooldown(unit? whichUnit, int abilCode, float cooldown)
    {
        if (Find(whichUnit, abilCode) is not { } a) return;
        a.Cooldown = cooldown;
        a.CooldownEnd = Seconds + cooldown;
    }

    [Native(Level.Close)]
    public void BlzEndUnitAbilityCooldown(unit? whichUnit, int abilCode)
    {
        if (Find(whichUnit, abilCode) is { } a) a.CooldownEnd = 0;
    }

    /// <summary>Sets what is left of a cooldown running, its length kept; with none running, nothing.</summary>
    [Native(Level.Close)]
    public void BlzSetUnitAbilityCooldownRemaining(unit? whichUnit, int abilId, float duration) => Running(whichUnit, abilId, a => a.CooldownEnd = Seconds + duration);

    /// <summary>Adds to what is left of a cooldown running, which then has at least 0.01 s left; with none running, nothing.</summary>
    [Native(Level.Close)]
    public void BlzAdjustUnitAbilityCooldownRemaining(unit? whichUnit, int abilId, float duration) =>
        Running(whichUnit, abilId, a => a.CooldownEnd = Seconds + Math.Max(CooldownLeft(a) + duration, 0.01));

    /// <summary>Leaves that share of the cooldown's length to go (0.25 of 8 leaves 2); with none running, nothing.</summary>
    [Native(Level.Close)]
    public void BlzSetUnitAbilityCooldownPercent(unit? whichUnit, int abilId, float percent) => Running(whichUnit, abilId, a => a.CooldownEnd = Seconds + a.Cooldown * percent);

    /// <summary>Adds that share of the cooldown's length to what is left (0.5 of 4 left 2, then 0.25 left 3); with none running, nothing.</summary>
    [Native(Level.Close)]
    public void BlzAdjustUnitAbilityCooldownPercent(unit? whichUnit, int abilId, float percent) =>
        Running(whichUnit, abilId, a => a.CooldownEnd = Seconds + Math.Max(CooldownLeft(a) + a.Cooldown * percent, 0.01));
}
