using System.Text;
using System.Text.RegularExpressions;
using War3Net.Build.Info;

namespace MirrorImaged.Data;

/// <summary>The seven kinds of game object, each with its tables and its object data file.</summary>
public enum ObjectKind { Unit, Item, Destructable, Doodad, Ability, Buff, Upgrade }

/// <summary>How a field keeps a value for each level.</summary>
enum PerLevel
{
    /// <summary>One value.</summary>
    None,

    /// <summary>A table column a level: <c>DataA1</c>, <c>DataA2</c>.</summary>
    Column,

    /// <summary>A comma list in the text files, level 1 first: <c>Tip=a,b,c</c>.</summary>
    List,

    /// <summary>A key a level, the first bare and the rest numbered from 1: <c>Requires</c>, <c>Requires1</c>.</summary>
    Appended,
}

/// <summary>Where a field's value is in an object's rows, from its metadata row; <paramref name="Element"/> is its entry in a comma list, or -1 for the whole value.</summary>
sealed record Field(string Column, PerLevel PerLevel, int Element);

/// <summary>
/// A standard object: its tables' and text files' values merged, by column (any case), and the values a test gives by
/// field and level.
/// </summary>
sealed class StandardObject(Dictionary<string, string> rows)
{
    public Dictionary<(int Field, int Level), string> Values { get; } = [];

    /// <summary>
    /// The field's text at a level in the rows; null for none (blank, or the tables' <c>-</c> and <c>_</c>). A key with
    /// a graphics mode is read as the game reads it in Definitive Edition graphics: <c>:de</c>, else <c>:hd</c>, else the
    /// plain key (decisions D203).
    /// </summary>
    public string? Cell(Field field, int level)
    {
        switch (field.PerLevel)
        {
            case PerLevel.Column:
                return Clean(Get(field.Column + level));
            case PerLevel.Appended:
                return Clean(Get(level == 1 ? field.Column : field.Column + (level - 1)));
            case PerLevel.List:
                var entries = Get(field.Column) is { } list ? Tables.Entries(list) : [];
                return level >= 1 && level <= entries.Count ? Clean(entries[level - 1]) : null;
            default:
                if (Get(field.Column) is not { } value) return null;
                if (field.Element < 0) return Clean(Tables.Unquoted(value));
                var all = Tables.Entries(value);
                return field.Element < all.Count ? Clean(all[field.Element]) : null;
        }
    }

    string? Get(string key) => rows.GetValueOrDefault(key + ":de") ?? rows.GetValueOrDefault(key + ":hd") ?? rows.GetValueOrDefault(key);

    static string? Clean(string? value) => value?.Trim() is { Length: > 0 } text && text is not "-" and not "_" ? text : null;
}

/// <summary>
/// The game's standard objects and gameplay constants: from an installed game's tables
/// (<see cref="Read(Func{string, byte[]}, string?, string?, string)"/>, <see cref="Installed"/>), or, for the tests of
/// the runtime, empty and filled with what a test needs.
/// </summary>
public sealed partial class StandardData
{
    static readonly string[] Races = ["campaign", "human", "neutral", "nightelf", "orc", "undead"];
    static readonly string[] AbilityFiles = ["campaign", "common", "human", "item", "neutral", "nightelf", "orc", "undead"];

    // Each kind's metadata, tables (the first one names the objects), func and skin files (a balance set replaces them)
    // and locale strings (never replaced), each merged over the one before, as Moonwell.GameData reads them.
    static readonly (ObjectKind Kind, string Meta, string[] Tables, string[] Text, string[] Strings)[] Sources =
    [
        (ObjectKind.Unit, "units/unitmetadata.slk",
            ["units/unitdata.slk", "units/unitbalance.slk", "units/unitui.slk", "units/unitweapons.slk", "units/unitabilities.slk"],
            [.. Races.Select(race => $"units/{race}unitfunc.txt"), "units/unitweaponsfunc.txt", "units/unitskin.txt", "units/unitweaponsskin.txt"],
            [.. Races.Select(race => $"units/{race}unitstrings.txt"), "units/unitskinstrings.txt"]),
        (ObjectKind.Item, "units/unitmetadata.slk", ["units/itemdata.slk"],
            ["units/itemfunc.txt", "units/itemskin.txt"], ["units/itemstrings.txt", "units/itemskinstrings.txt"]),
        (ObjectKind.Destructable, "units/destructablemetadata.slk", ["units/destructabledata.slk"],
            ["units/destructableskin.txt"], ["units/destructableskinstrings.txt"]),
        (ObjectKind.Doodad, "doodads/doodadmetadata.slk", ["doodads/doodads.slk"],
            ["doodads/doodadskins.txt"], ["doodads/doodadskinstrings.txt"]),
        (ObjectKind.Ability, "units/abilitymetadata.slk", ["units/abilitydata.slk"],
            [.. AbilityFiles.Select(race => $"units/{race}abilityfunc.txt"), "units/commandfunc.txt", "units/abilityskin.txt"],
            [.. AbilityFiles.Select(race => $"units/{race}abilitystrings.txt"), "units/abilityskinstrings.txt", "units/commandstrings.txt", "units/commandskinstrings.txt"]),
        (ObjectKind.Buff, "units/abilitybuffmetadata.slk", ["units/abilitybuffdata.slk"],
            [.. AbilityFiles.Select(race => $"units/{race}abilityfunc.txt"), "units/commandfunc.txt", "units/abilityskin.txt"],
            [.. AbilityFiles.Select(race => $"units/{race}abilitystrings.txt"), "units/abilityskinstrings.txt", "units/commandstrings.txt", "units/commandskinstrings.txt"]),
        (ObjectKind.Upgrade, "units/upgrademetadata.slk", ["units/upgradedata.slk"],
            [.. Races.Select(race => $"units/{race}upgradefunc.txt"), "units/upgradeskin.txt"],
            [.. Races.Select(race => $"units/{race}upgradestrings.txt"), "units/upgradeskinstrings.txt"]),
    ];

    // The string lists GetLocalizedString reads (probe data): FrameDef.toc's first four.
    static readonly string[] StringFiles =
        ["ui/framedef/globalstrings.fdf", "ui/framedef/infopanelstrings.fdf", "ui/framedef/networkstrings.fdf", "ui/framedef/datestrings.fdf"];

    static readonly Dictionary<(string Build, string? Values, string? Objects), StandardData> Cache = [];

    internal Dictionary<(ObjectKind Kind, int Id), StandardObject> Objects { get; } = [];

    internal Dictionary<ObjectKind, Dictionary<int, Field>> Fields { get; } = [];

    /// <summary>The gameplay constants, the <c>[Misc]</c> keys of <c>units/miscdata.txt</c> and <c>units/miscgame.txt</c>.</summary>
    internal Dictionary<string, string> Constants { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The game's strings by key, in the key's case, as <c>GetLocalizedString</c> reads them: the locale's string lists.</summary>
    internal Dictionary<string, string> Strings { get; } = new(StringComparer.Ordinal);

    /// <summary>The hotkeys of <c>ui/miscui.txt</c>'s <c>[Hotkeys]</c> by key (any case), in the installed language.</summary>
    internal Dictionary<string, string> Hotkeys { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Reads the standard data from the game's files: <paramref name="file"/> gives a file's bytes by its path in the
    /// game's <c>War3.w3mod</c> (<c>units/unitdata.slk</c>, <c>_balance/custom_v1.w3mod/units/unitdata.slk</c>), or null
    /// when there is none, which is passed over. <paramref name="values"/> is the balance set the values come from and
    /// <paramref name="objects"/> the one that says which objects exist (null for the base tables; see
    /// <see cref="Sets"/>): a set's file replaces the base's of the same name, and an object one of the set's tables
    /// leaves out doesn't exist. Text comes from <paramref name="locale"/>'s strings.
    /// </summary>
    public static StandardData Read(Func<string, byte[]?> file, string? values, string? objects, string locale)
    {
        var data = new StandardData();
        // Abilities and buffs share their text files: each file is read and parsed once.
        var read = new Dictionary<string, byte[]?>();
        var parsed = new Dictionary<string, List<(string Name, Dictionary<string, string> Keys)>>();
        byte[]? File(string path) => read.TryGetValue(path, out var bytes) ? bytes : read[path] = file(path);
        List<(string Name, Dictionary<string, string> Keys)> Profile(string path) =>
            parsed.TryGetValue(path, out var sections) ? sections : parsed[path] = File(path) is { } bytes ? Tables.Profile(bytes) : [];
        string InSet(string path, string? set) => set is not null && File($"_balance/{set}.w3mod/{path}") is not null ? $"_balance/{set}.w3mod/{path}" : path;
        var localized = $"_locales/{locale.ToLowerInvariant()}.w3mod/";

        var metadata = new Dictionary<string, Dictionary<int, Field>>();
        foreach (var (kind, meta, tables, text, strings) in Sources)
        {
            if (!metadata.TryGetValue(meta, out var fields))
                metadata[meta] = fields = File(meta) is { } bytes ? ReadFields(bytes) : [];
            data.Fields[kind] = fields;

            var rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            for (var table = 0; table < tables.Length; table++)
                foreach (var (id, cells) in File(InSet(tables[table], values)) is { } bytes ? Tables.Slk(bytes) : [])
                {
                    // The first table names the objects; another's row of any other id is dropped.
                    if (!rows.TryGetValue(id, out var merged))
                        if (table == 0) rows[id] = merged = new(StringComparer.OrdinalIgnoreCase);
                        else continue;
                    foreach (var (column, value) in cells) merged[column] = value;
                }

            if (objects is not null)
                foreach (var table in tables)
                    if (File($"_balance/{objects}.w3mod/{table}") is { } bytes)
                    {
                        var listed = Tables.Slk(bytes).Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
                        foreach (var id in rows.Keys.Where(id => !listed.Contains(id)).ToList()) rows.Remove(id);
                    }

            // A text section is the object's whatever its case (the Command Aura buff BOac's is [Boac]), its exact id first.
            var folded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in rows.Keys) folded.TryAdd(id, id);
            foreach (var path in text.Select(path => InSet(path, values)).Concat(strings.Select(path => localized + path)))
                foreach (var (name, keys) in Profile(path))
                    if (rows.TryGetValue(name, out var merged) || folded.TryGetValue(name, out var id) && rows.TryGetValue(id, out merged))
                        foreach (var (key, value) in keys) merged[key] = value;

            foreach (var (id, merged) in rows)
                if (id.Length == 4) data.Objects[(kind, Ids.Of(id))] = new StandardObject(merged);
        }

        foreach (var path in new[] { "units/miscdata.txt", "units/miscgame.txt" })
            foreach (var (name, keys) in Profile(InSet(path, values)))
                if (name.Equals("Misc", StringComparison.OrdinalIgnoreCase))
                    foreach (var (key, value) in keys) data.Constants[key] = value;

        foreach (var path in StringFiles)
            if (File(localized + path) is { } bytes)
                foreach (Match entry in StringEntry().Matches(Encoding.UTF8.GetString(bytes)))
                    data.Strings.TryAdd(entry.Groups[1].Value, entry.Groups[2].Value);

        // A hotkey is key='C', and in another language key:deDE='W' (or key:esES,itIT=...); the installed language's first.
        foreach (var (name, keys) in Profile("ui/miscui.txt"))
            if (name.Equals("Hotkeys", StringComparison.OrdinalIgnoreCase))
                foreach (var (key, value) in keys.OrderBy(pair => pair.Key.Contains(':') ? 0 : 1))
                {
                    var parts = key.Split(':');
                    if (parts.Length == 1 || parts[1].Split(',').Contains(locale, StringComparer.OrdinalIgnoreCase))
                        data.Hotkeys.TryAdd(parts[0], value.Trim('\''));
                }
        return data;
    }

    /// <summary>
    /// The standard data of the install in <paramref name="folder"/> for a map with <paramref name="info"/>, read from its
    /// storage once a process for each build and pair of balance sets (<see cref="Sets"/>).
    /// </summary>
    /// <exception cref="IOException">There is no install there, or its storage can't be read.</exception>
    public static StandardData Installed(string folder, MapInfo? info)
    {
        var build = GameInstall.Build(folder);
        var (values, objects) = Sets(info);
        lock (Cache)
        {
            if (!Cache.TryGetValue((build.Key, values, objects), out var data))
            {
                using var storage = CascStorage.Open(folder);
                Cache[(build.Key, values, objects)] = data = Read(path => storage.Read("War3.w3mod:" + path.Replace(".w3mod/", ".w3mod:")), values, objects, build.Locale);
            }
            return data;
        }
    }

    /// <summary>
    /// The balance sets the game reads for the map's data set and data version (facts, "Object data"): the one the
    /// values come from and the one that says which objects exist, null for the base tables. Forsaken Kingdom reads the
    /// base tables; Melee with Reign of Chaos <c>melee_v0</c>; Melee with The Frozen Throne the base values with
    /// <c>custom_v1</c>'s objects; Custom <c>custom_v0</c> or <c>custom_v1</c>; Default as Melee on a melee map and as
    /// Custom on another (probes <c>data-sets-melee</c> and <c>data-sets-tft</c>).
    /// </summary>
    public static (string? Values, string? Objects) Sets(MapInfo? info)
    {
        // An info file older than format 31 has no data version: 28 reads The Frozen Throne's (probe), and so is taken
        // 25, World Editor's first for it; older ones Reign of Chaos's.
        var version = info is null || info.FormatVersion >= MapInfoFormatVersion.v31 ? info?.GameDataVersion ?? GameDataVersion.TFT
            : info.FormatVersion >= MapInfoFormatVersion.v25 ? GameDataVersion.TFT
            : GameDataVersion.RoC;
        if (version == GameDataVersion.Unset) return (null, null); // Forsaken Kingdom
        var melee = info?.GameDataSet == GameDataSet.Melee || info?.GameDataSet == GameDataSet.Default && info.MapFlags.HasFlag(MapFlags.MeleeMap);
        if (melee) return version == GameDataVersion.RoC ? ("melee_v0", "melee_v0") : (null, "custom_v1");
        var set = version == GameDataVersion.RoC ? "custom_v0" : "custom_v1";
        return (set, set);
    }

    /// <summary>
    /// Adds a standard object with the values a test gives, by field and level: <c>uhpm</c> for a field without levels,
    /// <c>Hhb1:2</c> for level 2 of a field with them.
    /// </summary>
    internal StandardData Add(ObjectKind kind, string id, IReadOnlyDictionary<string, string>? values = null)
    {
        var standard = new StandardObject([]);
        foreach (var (key, value) in values ?? new Dictionary<string, string>())
        {
            var parts = key.Split(':');
            standard.Values[(Ids.Of(parts[0]), parts.Length > 1 ? int.Parse(parts[1]) : 0)] = value;
        }
        Objects[(kind, Ids.Of(id))] = standard;
        return this;
    }

    // A metadata table: each field's id to where its value is. A field shares its column with others of the kind when
    // it is one entry of a list (Buttonpos[0] and [1]); one of the text files with an index is that entry of its list
    // too (a unit's Name is the first entry of Name=). Its data pointer is a letter after the column (DataA for 1).
    static Dictionary<int, Field> ReadFields(byte[] meta)
    {
        var rows = Tables.Slk(meta).ToList();
        var shared = rows.GroupBy(row => row.Cells.GetValueOrDefault("field", "")).ToDictionary(group => group.Key, group => group.Count());
        var fields = new Dictionary<int, Field>();
        foreach (var (id, cells) in rows)
        {
            string Cell(string name) => cells.GetValueOrDefault(name, "").Trim();
            int Number(string name) => int.TryParse(Cell(name), out var number) ? number : 0;
            var name = Cell("field");
            var column = Number("data") > 0 ? name + (char)('A' + Number("data") - 1) : name;
            var profile = Cell("slk") == "Profile";
            var perLevel = Number("repeat") <= 0 ? PerLevel.None : !profile ? PerLevel.Column : Cell("appendIndex") == "1" ? PerLevel.Appended : PerLevel.List;
            var element = perLevel == PerLevel.None && (shared[cells.GetValueOrDefault("field", "")] > 1 || profile) && Cell("index").Length > 0 ? Number("index") : -1;
            if (id.Length == 4) fields[Ids.Of(id)] = new Field(column, perLevel, element);
        }
        return fields;
    }
}

public sealed partial class StandardData
{
    // An entry of a StringList: a key and its quoted text, which may run over lines.
    [GeneratedRegex("""^\s*([A-Za-z0-9_]+)\s+"([^"]*)"\s*,?""", RegexOptions.Multiline)]
    private static partial Regex StringEntry();
}

/// <summary>Object and field ids: four characters, read as the game reads them, big-endian.</summary>
static class Ids
{
    public static int Of(string id) => id.Aggregate(0, (value, letter) => (value << 8) | (byte)letter);
}
