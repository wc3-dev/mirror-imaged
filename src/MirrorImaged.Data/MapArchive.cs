using War3Net.Build;
using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.IO.Mpq;

namespace MirrorImaged.Data;

/// <summary>
/// A map's files, read from its archive (a <c>.w3x</c> or <c>.w3m</c>) with War3Net into <see cref="Files"/>: the info
/// (<c>war3map.w3i</c>), strings (<c>war3map.wts</c>), object data and its skin files, terrain (<c>w3e</c>), pathing
/// (<c>wpm</c>), doodads and destructables (<c>war3map.doo</c>), placed units (<c>war3mapUnits.doo</c>), regions
/// (<c>w3r</c>), cameras (<c>w3c</c>), sounds (<c>w3s</c>) and the import list (<c>imp</c>); a file the map lacks is null.
/// The terrain, doodad and placed-unit files, which no wave reads yet, are null too when War3Net can't read them, and
/// <see cref="Unread"/> names them (decisions D209). <c>war3mapMisc.txt</c> is kept as it is, and any other file, such
/// as an FDF or TOC file, is read by name.
/// </summary>
public sealed class MapArchive
{
    readonly string _path;
    readonly Dictionary<uint, string> _strings = [];

    MapArchive(string path, Map map, (byte[]? Script, bool Jass) script, byte[]? misc, IReadOnlyList<string> unread)
    {
        _path = path;
        Files = map;
        (Script, Jass) = script;
        Misc = misc;
        Unread = unread;
        foreach (var text in map.TriggerStrings?.Strings ?? []) _strings.TryAdd(text.Key, text.Value ?? "");
    }

    /// <summary>The files War3Net reads, in its <c>Map</c>.</summary>
    public Map Files { get; }

    /// <summary>
    /// The map's script as it is stored: its <c>war3map.lua</c> or its <c>war3map.j</c> (also as
    /// <c>scripts\war3map.j</c>, where older maps keep it), the one <c>war3map.w3i</c>'s script language names when it
    /// has both; null for a map with neither.
    /// </summary>
    public byte[]? Script { get; }

    /// <summary>Whether <see cref="Script"/> is JASS, a <c>war3map.j</c>.</summary>
    public bool Jass { get; }

    /// <summary>The map's <c>war3mapMisc.txt</c>, its changes to the gameplay constants.</summary>
    public byte[]? Misc { get; }

    /// <summary>The files left out as War3Net can't read them, each with why: <c>war3map.w3e: Value '0' is not ...</c>.</summary>
    public IReadOnlyList<string> Unread { get; }

    // The install's three maps War3Net can't read whole have one of these (D209), which no wave reads yet.
    static readonly string[] LeftOut = ["war3map.w3e", "war3map.doo", "war3mapUnits.doo"];

    /// <summary>Reads the map at <paramref name="path"/>.</summary>
    /// <exception cref="InvalidDataException">A file other than those <see cref="Unread"/> may name can't be read; the message names it.</exception>
    public static MapArchive Read(string path)
    {
        using var archive = MpqArchive.Open(path, false);
        var unread = new List<string>();
        T? Read<T>(string name, Func<BinaryReader, T> read) where T : class
        {
            if (Bytes(archive, name) is not { } bytes) return null;
            try { return read(new BinaryReader(new MemoryStream(bytes))); }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                if (!LeftOut.Contains(name)) throw new InvalidDataException($"{name} in {path} can't be read: {e.Message}", e);
                unread.Add($"{name}: {e.Message}");
                return null;
            }
        }

        var map = new Map
        {
            Info = Read("war3map.w3i", reader => reader.ReadMapInfo()),
            TriggerStrings = Read("war3map.wts", reader => new StreamReader(reader.BaseStream).ReadTriggerStrings()),
            Environment = Read("war3map.w3e", reader => reader.ReadMapEnvironment()),
            PathingMap = Read("war3map.wpm", reader => reader.ReadMapPathingMap()),
            Doodads = Read("war3map.doo", reader => reader.ReadMapDoodads()),
            Units = Read("war3mapUnits.doo", reader => reader.ReadMapUnits()),
            Regions = Read("war3map.w3r", reader => reader.ReadMapRegions()),
            Cameras = Read("war3map.w3c", reader => reader.ReadMapCameras()),
            Sounds = Read("war3map.w3s", reader => reader.ReadMapSounds()),
            ImportedFiles = Read("war3map.imp", reader => reader.ReadImportedFiles()),
            UnitObjectData = Read("war3map.w3u", reader => reader.ReadUnitObjectData()),
            ItemObjectData = Read("war3map.w3t", reader => reader.ReadItemObjectData()),
            DestructableObjectData = Read("war3map.w3b", reader => reader.ReadDestructableObjectData()),
            DoodadObjectData = Read("war3map.w3d", reader => reader.ReadDoodadObjectData()),
            AbilityObjectData = Read("war3map.w3a", reader => reader.ReadAbilityObjectData()),
            BuffObjectData = Read("war3map.w3h", reader => reader.ReadBuffObjectData()),
            UpgradeObjectData = Read("war3map.w3q", reader => reader.ReadUpgradeObjectData()),
            UnitSkinObjectData = Read("war3mapSkin.w3u", reader => reader.ReadUnitObjectData()),
            ItemSkinObjectData = Read("war3mapSkin.w3t", reader => reader.ReadItemObjectData()),
            DestructableSkinObjectData = Read("war3mapSkin.w3b", reader => reader.ReadDestructableObjectData()),
            DoodadSkinObjectData = Read("war3mapSkin.w3d", reader => reader.ReadDoodadObjectData()),
            AbilitySkinObjectData = Read("war3mapSkin.w3a", reader => reader.ReadAbilityObjectData()),
            BuffSkinObjectData = Read("war3mapSkin.w3h", reader => reader.ReadBuffObjectData()),
            UpgradeSkinObjectData = Read("war3mapSkin.w3q", reader => reader.ReadUpgradeObjectData()),
        };
        // With both, war3map.w3i's script language chooses (decisions D130; not measured)
        var (lua, jass) = (Bytes(archive, "war3map.lua"), Bytes(archive, "war3map.j") ?? Bytes(archive, @"scripts\war3map.j"));
        (byte[]? Script, bool Jass) script = lua is not null && (jass is null || map.Info?.ScriptLanguage != ScriptLanguage.Jass) ? (lua, false)
            : jass is not null ? (jass, true) : (null, false);
        return new MapArchive(path, map, script, Bytes(archive, "war3mapMisc.txt"), unread);
    }

    /// <summary>A file of the map by its name in the archive (<c>war3mapImported\ui.toc</c>); null when it has none.</summary>
    public byte[]? File(string name)
    {
        using var archive = MpqArchive.Open(_path, false);
        return Bytes(archive, name);
    }

    /// <summary>
    /// <paramref name="text"/> with a <c>TRIGSTR_n</c> replaced by string n of <c>war3map.wts</c>, n read as C's
    /// <c>atoi</c> reads it (<c>TRIGSTR_1</c>, <c>TRIGSTR_0001</c> and <c>TRIGSTR_001 </c> are string 1; the prefix in
    /// capitals, probe <c>data</c>); any other text, and a string the file doesn't have, as it is.
    /// </summary>
    public string Resolve(string text) =>
        text.StartsWith("TRIGSTR_", StringComparison.Ordinal) && _strings.TryGetValue((uint)Numbers.Integer(text[8..]), out var value) ? value : text;

    static byte[]? Bytes(MpqArchive archive, string name)
    {
        if (!archive.TryOpenFile(name, out var stream)) return null;
        using (stream)
        {
            var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            return bytes.ToArray();
        }
    }
}
