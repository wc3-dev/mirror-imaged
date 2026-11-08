using System.Buffers.Binary;
using System.Globalization;
using War3Net.Build.Object;

namespace MirrorImaged.Data;

/// <summary>
/// The game's data as a run reads it: the standard objects and gameplay constants with the map's changes (its object
/// data, <c>war3mapMisc.txt</c>, strings resolved from <c>war3map.wts</c>), and the map's own files. An object is found
/// by its kind and id, and each of its fields by its id and level (<see cref="GameObject.Text"/>).
/// </summary>
public sealed class GameData
{
    readonly Dictionary<(ObjectKind, int), GameObject> _objects = [];
    readonly Dictionary<string, string> _constants;

    /// <summary>
    /// The standard data with the map's changes: a change to a standard object changes it; a new object starts from
    /// the standard object it is based on, not from the map's change to it (facts, "Object data"). The skin files'
    /// changes come after the others'. A change to an object the balance set doesn't have is dropped.
    /// </summary>
    public GameData(StandardData standard, MapArchive? map = null)
    {
        Standard = standard;
        Map = map;
        Terrain = map?.Files.Environment is { } environment ? new Terrain(environment) : null;
        _constants = new(standard.Constants, StringComparer.OrdinalIgnoreCase);
        foreach (var ((kind, id), entry) in standard.Objects) _objects[(kind, id)] = new GameObject(this, kind, id, id, entry);
        if (map is null) return;

        foreach (var (name, keys) in map.Misc is { } misc ? Tables.Profile(misc) : [])
            if (name.Equals("Misc", StringComparison.OrdinalIgnoreCase))
                foreach (var (key, value) in keys) _constants[key] = value;

        var files = map.Files;
        foreach (var (kind, old, id, changes) in Changes(ObjectKind.Unit, files.UnitObjectData?.BaseUnits, files.UnitObjectData?.NewUnits, files.UnitSkinObjectData?.BaseUnits, files.UnitSkinObjectData?.NewUnits)
            .Concat(Changes(ObjectKind.Item, files.ItemObjectData?.BaseItems, files.ItemObjectData?.NewItems, files.ItemSkinObjectData?.BaseItems, files.ItemSkinObjectData?.NewItems))
            .Concat(Changes(ObjectKind.Destructable, files.DestructableObjectData?.BaseDestructables, files.DestructableObjectData?.NewDestructables, files.DestructableSkinObjectData?.BaseDestructables, files.DestructableSkinObjectData?.NewDestructables))
            .Concat(Changes(ObjectKind.Doodad, files.DoodadObjectData?.BaseDoodads, files.DoodadObjectData?.NewDoodads, files.DoodadSkinObjectData?.BaseDoodads, files.DoodadSkinObjectData?.NewDoodads))
            .Concat(Changes(ObjectKind.Ability, files.AbilityObjectData?.BaseAbilities, files.AbilityObjectData?.NewAbilities, files.AbilitySkinObjectData?.BaseAbilities, files.AbilitySkinObjectData?.NewAbilities))
            .Concat(Changes(ObjectKind.Buff, files.BuffObjectData?.BaseBuffs, files.BuffObjectData?.NewBuffs, files.BuffSkinObjectData?.BaseBuffs, files.BuffSkinObjectData?.NewBuffs))
            .Concat(Changes(ObjectKind.Upgrade, files.UpgradeObjectData?.BaseUpgrades, files.UpgradeObjectData?.NewUpgrades, files.UpgradeSkinObjectData?.BaseUpgrades, files.UpgradeSkinObjectData?.NewUpgrades)))
        {
            GameObject? changed;
            if (id == 0) changed = _objects.GetValueOrDefault((kind, old));
            else if (!_objects.TryGetValue((kind, id), out changed) || changed.BaseId == id)
                _objects[(kind, id)] = changed = new GameObject(this, kind, id, old, standard.Objects.GetValueOrDefault((kind, old)));
            foreach (var (field, level, value) in changes)
                changed?.Changes[(field, level)] = value is string text ? map.Resolve(text) : Written(value);
        }
    }

    internal StandardData Standard { get; }

    /// <summary>The map's files; null when the run has no map's data.</summary>
    public MapArchive? Map { get; }

    /// <summary>The ground of the map's <c>war3map.w3e</c>; null when it has none.</summary>
    public Terrain? Terrain { get; }

    /// <summary>The object of <paramref name="kind"/> with <paramref name="id"/>; null when the game has none.</summary>
    public GameObject? Object(ObjectKind kind, int id) => _objects.GetValueOrDefault((kind, id));

    /// <summary>A gameplay constant (<c>DayLength</c>) as written, the map's own over the game's; null when neither has it.</summary>
    public string? Constant(string key) => _constants.GetValueOrDefault(key);

    /// <summary>A gameplay constant as a number, or <paramref name="otherwise"/> when there is none.</summary>
    public float Constant(string key, float otherwise) => Constant(key) is { } text ? Numbers.Real(text) : otherwise;

    /// <summary>The game's string for <paramref name="key"/> (in its case) from its string lists; null when it has none.</summary>
    public string? String(string key) => Standard.Strings.GetValueOrDefault(key);

    /// <summary>The hotkey of <c>ui/miscui.txt</c> for <paramref name="key"/> (any case), as written (<c>C</c>); null when it has none.</summary>
    public string? Hotkey(string key) => Standard.Hotkeys.GetValueOrDefault(key);

    // A number as its invariant text, a real as the shortest that reads back the same float.
    static string Written(object? value) => value switch
    {
        float real => real.ToString(CultureInfo.InvariantCulture),
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value?.ToString() ?? "",
    };

    // Every change of the main file, then of the skin file: the kind, the object it is based on, its own id (0 for a
    // change to a standard object), and each field's id, level (0 for a kind without levels) and value. War3Net keeps an
    // id's first letter in its lowest byte, the game in its highest.
    static IEnumerable<(ObjectKind, int, int, IEnumerable<(int, int, object)>)> Changes<T>(ObjectKind kind, params IEnumerable<T>?[] lists) =>
        lists.SelectMany(list => list ?? []).Select(entry => entry switch
        {
            SimpleObjectModification simple => (kind, Game(simple.OldId), Game(simple.NewId), simple.Modifications.Select(change => (Game(change.Id), 0, change.Value))),
            LevelObjectModification levels => (kind, Game(levels.OldId), Game(levels.NewId), levels.Modifications.Select(change => (Game(change.Id), change.Level, change.Value))),
            VariationObjectModification variations => (kind, Game(variations.OldId), Game(variations.NewId), variations.Modifications.Select(change => (Game(change.Id), change.Variation, change.Value))),
            _ => throw new InvalidOperationException($"{typeof(T)} isn't an object modification"),
        });

    static int Game(int war3NetId) => BinaryPrimitives.ReverseEndianness(war3NetId);
}

/// <summary>
/// One object of the game's data: a standard one, with the map's changes, or one the map made from a standard one.
/// </summary>
public sealed class GameObject
{
    readonly GameData _data;
    readonly StandardObject? _standard;

    internal GameObject(GameData data, ObjectKind kind, int id, int baseId, StandardObject? standard)
    {
        _data = data;
        Kind = kind;
        Id = id;
        BaseId = baseId;
        _standard = standard;
    }

    internal Dictionary<(int Field, int Level), string> Changes { get; } = [];

    public ObjectKind Kind { get; }

    public int Id { get; }

    /// <summary>The standard object it is made from: its own id for a standard object.</summary>
    public int BaseId { get; }

    /// <summary>
    /// Its level count: an ability's levels (<c>alev</c>), an upgrade's (<c>glvl</c>), a destructable's or doodad's
    /// variations (<c>bvar</c>, <c>dvar</c>); 0 for the other kinds.
    /// </summary>
    public int Levels => LevelField(Kind) is { } levels ? Integer(levels) : 0;

    /// <summary>
    /// The value of the field <paramref name="id"/> (<c>uhpm</c>) as text: the map's change, else the standard value; null when it has none. A field without
    /// levels is read at level 0, whatever level is given; a field with levels from 1 to <see cref="Levels"/>, and
    /// past that it has none. An ability's level past the 4th that has no value of its own, in the map or the tables,
    /// takes the level before's (facts, "Spells and abilities": 3.0.1 keeps data past level 4).
    /// </summary>
    public string? Text(int id, int level = 0)
    {
        var where = _data.Standard.Fields.GetValueOrDefault(Kind)?.GetValueOrDefault(id);
        if (where?.PerLevel == PerLevel.None) level = 0;
        else if (level > 0 && LevelField(Kind) is not null && level > Levels) return null;
        return Read(id, level, where);
    }

    /// <summary>The field's value as an integer, read as C's <c>atoi</c> reads it: 0 when it has none.</summary>
    public int Integer(int id, int level = 0) => Text(id, level) is { } text ? Numbers.Integer(text) : 0;

    /// <summary>The field's value as a real, read as C's <c>atof</c> reads it: 0 when it has none.</summary>
    public float Real(int id, int level = 0) => Text(id, level) is { } text ? Numbers.Real(text) : 0;

    string? Read(int id, int level, Field? where) =>
        Changes.GetValueOrDefault((id, level))
        ?? _standard?.Values.GetValueOrDefault((id, level))
        ?? (where is null ? null : _standard?.Cell(where, level))
        ?? (where?.PerLevel == PerLevel.Column && level > 4 ? Read(id, level - 1, where) : null);

    static int? LevelField(ObjectKind kind) => kind switch
    {
        ObjectKind.Ability => Ids.Of("alev"),
        ObjectKind.Upgrade => Ids.Of("glvl"),
        ObjectKind.Destructable => Ids.Of("bvar"),
        ObjectKind.Doodad => Ids.Of("dvar"),
        _ => null,
    };
}

/// <summary>Numbers in the tables' text, read as C's <c>atoi</c> and <c>atof</c> read them: the longest number at the start, else 0.</summary>
static class Numbers
{
    public static int Integer(string text)
    {
        var start = text.Length - text.TrimStart().Length;
        var end = start < text.Length && text[start] is '-' or '+' ? start + 1 : start;
        while (end < text.Length && char.IsAsciiDigit(text[end])) end++;
        return long.TryParse(text.AsSpan(start, end - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
            ? (int)Math.Clamp(number, int.MinValue, int.MaxValue)
            : 0;
    }

    public static float Real(string text)
    {
        for (var end = text.Length; end > 0; end--)
            if (float.TryParse(text.AsSpan(0, end), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return number;
        return 0;
    }
}
