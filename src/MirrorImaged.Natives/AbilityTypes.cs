using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// An ability type's tooltips, icons, button places, cooldown, mana cost and sounds, as the probe abilities-types reads and
// sets them (docs/facts.md, "Abilities"; game-facts, "Spells and abilities" and "Files and Preload"). They are the game's
// for every player here, a run being one machine; the game keeps them per player. A text is kept for each entry the
// data has for it, a tooltip a level: a level outside them reads the last, and a set there is lost (game-facts: 100
// pieces read back 3 on a 100-level copy without tooltips). The getters read an ability's, a unit's, an item's or an
// upgrade's (probe abilities-types: hfoo's "Train Footman").
public sealed partial class GameNatives
{
    const string TypesProbe = "facts.md, Abilities: probe abilities-types (11-08)";
    const string Temp = @"ReplaceableTextures\CommandButtons\BTNTemp.blp";

    readonly Dictionary<(ObjectKind Kind, int Id, int Field, int At), string> _texts = []; // the type setters'
    readonly Dictionary<(int Id, string Column, int Element), int> _buttons = []; // BlzSetAbilityPosX's and the rest

    GameObject? Described(int id) =>
        Data.Object(ObjectKind.Ability, id) ?? Data.Object(ObjectKind.Unit, id) ?? Data.Object(ObjectKind.Item, id) ?? Data.Object(ObjectKind.Upgrade, id);

    // What a text the data lacks reads (probe abilities-types)
    static string Missing(string column) => column.ToLowerInvariant() switch
    {
        "tip" or "untip" or "researchtip" => "Tool tip missing!",
        "art" or "unart" or "researchart" => Temp,
        _ => "",
    };

    static bool IsIcon(string column) => column.ToLowerInvariant() is "art" or "unart" or "researchart";

    static bool IsPlace(string column) => column.ToLowerInvariant() is "buttonpos" or "unbuttonpos" or "researchbuttonpos";

    // A text's entries: an icon's one; a field without levels one if the data has it; a tooltip a level up to the last
    // the data has one for (a map's "-" counts: game-facts, 100 levels held)
    static int Entries(GameObject type, int field, Field where)
    {
        if (IsIcon(where.Column)) return 1;
        if (where.PerLevel == PerLevel.None) return type.Text(field) is null ? 0 : 1;
        var count = 0;
        for (var level = 1; level <= type.Levels; level++)
            if (type.Text(field, level) is not null) count = level;
        return count;
    }

    string TypeText(GameObject type, int field, Field where, int at) =>
        _texts.TryGetValue((type.Kind, type.Id, field, at), out var set) ? set
        : type.Text(field, where.PerLevel == PerLevel.None ? 0 : at + 1) ?? Missing(where.Column);

    string TypeText(int id, string column, int level)
    {
        if (Described(id) is not { } type || Data.FieldAt(type.Kind, column) is not { } field || Data.Field(type.Kind, field) is not { } where) return Missing(column);
        var count = Entries(type, field, where);
        return count == 0 ? Missing(column) : TypeText(type, field, where, level >= 0 && level < count ? level : count - 1);
    }

    // An empty text, an id of nothing and a level outside the entries change nothing (probe abilities-types)
    void SetTypeText(int id, string column, string? text, int level)
    {
        if (text is { Length: > 0 } && Described(id) is { } type && Data.FieldAt(type.Kind, column) is { } field && Data.Field(type.Kind, field) is { } where
            && level >= 0 && level < Entries(type, field, where))
            _texts[(type.Kind, type.Id, field, level)] = text;
    }

    int TypePlace(int id, string column, int element) =>
        _buttons.TryGetValue((id, column.ToLowerInvariant(), element), out var set) ? set
        : Described(id) is { } type && Data.FieldAt(type.Kind, column, element) is { } field ? type.Integer(field) : 0;

    // A column from 0 to 3, a row from 0 to 2, for any id (probe abilities-types: 7 and -1 not kept, an unknown id's kept)
    void SetTypePlace(int id, string column, int element, int value)
    {
        if (value >= 0 && value <= (element == 0 ? 3 : 2)) _buttons[(id, column.ToLowerInvariant(), element)] = value;
    }

    /// <summary>
    /// The tooltip of the level (from 0), the last level's past them and at -1; "Tool tip missing!" for an id of nothing
    /// and a buff (probe abilities-types; game-facts: "Holy Light - [|cffffcc00Level 1|r]").
    /// </summary>
    [Native(Level.Exact, Fact = TypesProbe + "; game-facts.md, Files and Preload: tooltips (11-03)")]
    public string? BlzGetAbilityTooltip(int abilCode, int level) => TypeText(abilCode, "Tip", level);

    /// <summary>Sets the level's tooltip, kept only within the levels the data has tooltips for; "" isn't kept (probe abilities-types; game-facts, Files and Preload).</summary>
    [Native(Level.Exact, Fact = TypesProbe + "; game-facts.md, Files and Preload: tooltips (11-03)")]
    public void BlzSetAbilityTooltip(int abilCode, string? tooltip, int level) => SetTypeText(abilCode, "Tip", tooltip, level);

    /// <summary>
    /// As the tooltip, "" where there is none. Close: its <c>&lt;id,column&gt;</c> tags stay as written, which the game
    /// fills (game-facts: <c>&lt;AHhb,DataA1&gt;</c> reads 200), W2-8's.
    /// </summary>
    [Native(Level.Close)]
    public string? BlzGetAbilityExtendedTooltip(int abilCode, int level) => TypeText(abilCode, "Ubertip", level);

    [Native(Level.Exact, Fact = TypesProbe + "; game-facts.md, Files and Preload: tooltips (11-03)")]
    public void BlzSetAbilityExtendedTooltip(int abilCode, string? extendedTooltip, int level) => SetTypeText(abilCode, "Ubertip", extendedTooltip, level);

    /// <summary>The learn tooltip, one for every level with its <c>%d</c> (game-facts; probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe + "; game-facts.md, Spells and abilities: BlzGetAbilityResearchTooltip keeps its %d (10-14)")]
    public string? BlzGetAbilityResearchTooltip(int abilCode, int level) => TypeText(abilCode, "Researchtip", level);

    /// <summary>Sets the learn tooltip at level 0, the one it has; another level isn't kept (probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityResearchTooltip(int abilCode, string? researchTooltip, int level) => SetTypeText(abilCode, "Researchtip", researchTooltip, level);

    /// <summary>The learn extended tooltip, one for every level. Close: its tags stay as written (W2-8).</summary>
    [Native(Level.Close)]
    public string? BlzGetAbilityResearchExtendedTooltip(int abilCode, int level) => TypeText(abilCode, "Researchubertip", level);

    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityResearchExtendedTooltip(int abilCode, string? researchExtendedTooltip, int level) => SetTypeText(abilCode, "Researchubertip", researchExtendedTooltip, level);

    /// <summary>The turn-off tooltip ("Deactivate Divine Shield", one for every level); "Tool tip missing!" without (probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public string? BlzGetAbilityActivatedTooltip(int abilCode, int level) => TypeText(abilCode, "Untip", level);

    /// <summary>Kept only within the entries the data has: Holy Light, without one, keeps none (probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityActivatedTooltip(int abilCode, string? tooltip, int level) => SetTypeText(abilCode, "Untip", tooltip, level);

    /// <summary>"" without one. Close: its tags stay as written (W2-8).</summary>
    [Native(Level.Close)]
    public string? BlzGetAbilityActivatedExtendedTooltip(int abilCode, int level) => TypeText(abilCode, "Unubertip", level);

    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityActivatedExtendedTooltip(int abilCode, string? extendedTooltip, int level) => SetTypeText(abilCode, "Unubertip", extendedTooltip, level);

    /// <summary>The button's icon; BTNTemp for an id of nothing (probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public string? BlzGetAbilityIcon(int abilCode) => TypeText(abilCode, "Art", 0);

    /// <summary>Sets the icon, which the type's abilities on units read unless their own was set; an id of nothing keeps none (probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityIcon(int abilCode, string? iconPath) => SetTypeText(abilCode, "Art", iconPath, 0);

    /// <summary>The turned-on icon; BTNTemp without one (game-facts; probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe + "; game-facts.md, Spells and abilities: BTNTemp (10-14)")]
    public string? BlzGetAbilityActivatedIcon(int abilCode) => TypeText(abilCode, "Unart", 0);

    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityActivatedIcon(int abilCode, string? iconPath) => SetTypeText(abilCode, "Unart", iconPath, 0);

    /// <summary>The button's column from the left (game-facts; probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe + "; game-facts.md, Spells and abilities: BlzGetAbilityPosX/Y (10-14)")]
    public int BlzGetAbilityPosX(int abilCode) => TypePlace(abilCode, "Buttonpos", 0);

    /// <summary>The button's row from the top.</summary>
    [Native(Level.Exact, Fact = TypesProbe + "; game-facts.md, Spells and abilities: BlzGetAbilityPosX/Y (10-14)")]
    public int BlzGetAbilityPosY(int abilCode) => TypePlace(abilCode, "Buttonpos", 1);

    /// <summary>Kept from 0 to 3, for any id, an unknown one too (probe abilities-types).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityPosX(int abilCode, int x) => SetTypePlace(abilCode, "Buttonpos", 0, x);

    /// <summary>Kept from 0 to 2 (probe abilities-types: -1 isn't).</summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityPosY(int abilCode, int y) => SetTypePlace(abilCode, "Buttonpos", 1, y);

    [Native(Level.Exact, Fact = TypesProbe)]
    public int BlzGetAbilityActivatedPosX(int abilCode) => TypePlace(abilCode, "UnButtonpos", 0);

    [Native(Level.Exact, Fact = TypesProbe)]
    public int BlzGetAbilityActivatedPosY(int abilCode) => TypePlace(abilCode, "UnButtonpos", 1);

    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityActivatedPosX(int abilCode, int x) => SetTypePlace(abilCode, "UnButtonpos", 0, x);

    [Native(Level.Exact, Fact = TypesProbe)]
    public void BlzSetAbilityActivatedPosY(int abilCode, int y) => SetTypePlace(abilCode, "UnButtonpos", 1, y);

    /// <summary>The type's cooldown at the level (from 0) from its data, which a unit's sets don't change; 0 past its levels and for an id of nothing (probe abilities-fields).</summary>
    [Native(Level.Exact, Fact = FieldsProbe)]
    public float BlzGetAbilityCooldown(int abilId, int level) =>
        Data.Object(ObjectKind.Ability, abilId) is { } type && level >= 0 && level < type.Levels ? type.Real(Cooldown, level + 1) : 0;

    [Native(Level.Exact, Fact = FieldsProbe)]
    public int BlzGetAbilityManaCost(int abilId, int level) =>
        Data.Object(ObjectKind.Ability, abilId) is { } type && level >= 0 && level < type.Levels ? type.Integer(ManaCost, level + 1) : 0;

    /// <summary>
    /// An ability's or a buff's sound label, looped for <c>SOUND_TYPE_EFFECT_LOOPED</c> and the effect's for any other
    /// type, nil too; "" without (probe abilities-types: Stampede's, Aerial Shackles' buff's loop). A unit's ability's own
    /// set doesn't change it.
    /// </summary>
    [Native(Level.Exact, Fact = TypesProbe)]
    public string? GetAbilitySoundById(int abilityId, soundtype? t) =>
        SoundOf(ObjectKind.Ability, abilityId, t) ?? SoundOf(ObjectKind.Buff, abilityId, t) ?? "";

    /// <summary>
    /// "" for every string measured: an ability's id ("Arsp"), its name, a buff's id and "" (probe abilities-types).
    /// Close: what it reads wasn't found.
    /// </summary>
    [Native(Level.Close)]
    public string? GetAbilitySound(string? abilityString, soundtype? t) => "";

    string? SoundOf(ObjectKind kind, int id, soundtype? t) =>
        Data.Object(kind, id) is { } type && Data.FieldAt(kind, t?.Id == 1 ? "Effectsoundlooped" : "Effectsound") is { } field ? type.Text(field) ?? "" : null;
}
