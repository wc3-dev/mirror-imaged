using System.Buffers.Binary;
using System.Numerics;
using War3Net.Build;
using War3Net.Build.Common;
using War3Net.Build.Environment;
using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.Build.Widget;
using War3Net.IO.Mpq;

namespace MirrorImaged.Data;

/// <summary>
/// A map's files, read from its archive (a <c>.w3x</c> or <c>.w3m</c>) with War3Net into <see cref="Files"/>: the info
/// (<c>war3map.w3i</c>), strings (<c>war3map.wts</c>), object data and its skin files, terrain (<c>w3e</c>), pathing
/// (<c>wpm</c>), doodads and destructables (<c>war3map.doo</c>), placed units (<c>war3mapUnits.doo</c>), regions
/// (<c>w3r</c>), cameras (<c>w3c</c>), sounds (<c>w3s</c>) and the import list (<c>imp</c>); a file the map lacks is null.
/// The placed-unit file, which no wave reads yet, is null too when War3Net can't read it, and <see cref="Unread"/> names
/// it (decisions D209); a terrain file whose tile or cliff types War3Net refuses, and a doodad file whose doodad states
/// it refuses, are read here (decisions D270, D258), and are left out only when they can't be read either. Two kinds of info and sound file War3Net
/// refuses are mended before it reads them (D300, D301). <c>war3mapMisc.txt</c> is kept as it is, and any other file,
/// such as an FDF or TOC file, is read by name.
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

    // One of the install's maps has a placed-unit file War3Net can't read (D209); a terrain or doodad file neither it nor
    // Ground or Doodads reads is left out too (D258, D270).
    static readonly string[] LeftOut = ["war3map.w3e", "war3map.doo", "war3mapUnits.doo"];

    /// <summary>Reads the map at <paramref name="path"/>.</summary>
    /// <exception cref="InvalidDataException">A file other than those <see cref="Unread"/> may name can't be read; the message names it.</exception>
    public static MapArchive Read(string path)
    {
        using var archive = MpqArchive.Open(path, false);
        var unread = new List<string>();
        T? Read<T>(string name, Func<BinaryReader, T> read, Func<byte[], byte[]>? mend = null) where T : class
        {
            if (Bytes(archive, name) is not { } bytes) return null;
            // A file is mended only when War3Net can't read it whole as it is (D300)
            try { return mend is not null && Whole(read, bytes) is { } plain ? plain : read(new BinaryReader(new MemoryStream(mend?.Invoke(bytes) ?? bytes))); }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                if (!LeftOut.Contains(name)) throw new InvalidDataException($"{name} in {path} can't be read: {e.Message}", e);
                unread.Add($"{name}: {e.Message}");
                return null;
            }
        }

        var map = new Map
        {
            Info = Read("war3map.w3i", reader => reader.ReadMapInfo(), PlayerRaceHuds),
            TriggerStrings = Read("war3map.wts", reader => new StreamReader(reader.BaseStream).ReadTriggerStrings()),
            Environment = Read("war3map.w3e", reader =>
            {
                try { return reader.ReadMapEnvironment(); }
                catch (ArgumentException)
                {
                    reader.BaseStream.Position = 0;
                    return Ground(reader);
                }
            }),
            PathingMap = Read("war3map.wpm", reader => reader.ReadMapPathingMap()),
            Doodads = Read("war3map.doo", reader =>
            {
                try { return reader.ReadMapDoodads(); }
                catch (ArgumentException)
                {
                    reader.BaseStream.Position = 0;
                    return Doodads(reader);
                }
            }),
            Units = Read("war3mapUnits.doo", reader => reader.ReadMapUnits()),
            Regions = Read("war3map.w3r", reader => reader.ReadMapRegions()),
            Cameras = Read("war3map.w3c", reader => reader.ReadMapCameras()),
            Sounds = Read("war3map.w3s", reader => reader.ReadMapSounds(), SoundNamesRepeated),
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

    // war3map.doo's doodads read as War3Net reads them, but each one's state kept as written: War3Net refuses a state it
    // doesn't define, as (1)SilverpineSprint's 14 (format 13), which the game loads. What comes after the doodads, the
    // terrain's own doodads, which nothing reads, is left out.
    static MapDoodads Doodads(BinaryReader reader)
    {
        if (reader.ReadInt32() != MapDoodads.FileFormatSignature) throw new InvalidDataException("war3map.doo doesn't start with W3do");
        var version = (MapWidgetsFormatVersion)reader.ReadInt32();
        var subVersion = (MapWidgetsSubVersion)reader.ReadInt32();
        var doodads = new MapDoodads(version, subVersion, useNewFormat: false);
        for (var count = reader.ReadInt32(); count > 0; count--)
        {
            var doodad = new DoodadData
            {
                TypeId = reader.ReadInt32(), Variation = reader.ReadInt32(), Position = Vector(reader), Rotation = reader.ReadSingle(), Scale = Vector(reader),
            };
            doodads.UseNewFormat = reader.PeekChar() >= 0x20; // a skin id, as War3Net tells it
            doodad.SkinId = doodads.UseNewFormat ? reader.ReadInt32() : doodad.TypeId;
            if (version >= MapWidgetsFormatVersion.v12) doodad.GroupId = reader.ReadUInt32();
            if (version > MapWidgetsFormatVersion.v6) doodad.State = (DoodadState)reader.ReadByte();
            doodad.Life = reader.ReadByte();
            if (version >= MapWidgetsFormatVersion.v8)
            {
                doodad.MapItemTableId = reader.ReadInt32();
                for (var sets = reader.ReadInt32(); sets > 0; sets--)
                    doodad.ItemTableSets.Add(reader.ReadRandomItemSet(version, subVersion, doodads.UseNewFormat));
            }
            if (version >= MapWidgetsFormatVersion.v13) doodad.Unk1 = reader.ReadInt32();
            doodad.CreationNumber = reader.ReadInt32();
            if (version >= MapWidgetsFormatVersion.v12)
            {
                (doodad.Roll, doodad.Pitch) = (reader.ReadSingle(), reader.ReadSingle());
                for (var lights = reader.ReadUInt32(); lights > 0; lights--) doodad.DoodadLights.Add(reader.ReadMapDoodadLightData());
            }
            doodads.Doodads.Add(doodad);
        }
        return doodads;
    }

    // war3map.w3e read as War3Net reads it, but its tile and cliff types kept as written: War3Net refuses a type it
    // doesn't define, as (10)DustwallowKeys' cliff type 0, which the game loads. Nothing reads the types.
    static MapEnvironment Ground(BinaryReader reader)
    {
        if (reader.ReadInt32() != MapEnvironment.FileFormatSignature) throw new InvalidDataException("war3map.w3e doesn't start with W3E!");
        var ground = new MapEnvironment((MapEnvironmentFormatVersion)reader.ReadInt32()) { Tileset = (Tileset)reader.ReadByte(), IsCustomTileset = reader.ReadInt32() != 0 };
        for (var n = reader.ReadInt32(); n > 0; n--) ground.TerrainTypes.Add((TerrainType)reader.ReadInt32());
        for (var n = reader.ReadInt32(); n > 0; n--) ground.CliffTypes.Add((CliffType)reader.ReadInt32());
        (ground.Width, ground.Height, ground.Left, ground.Bottom) = (reader.ReadUInt32() - 1, reader.ReadUInt32() - 1, reader.ReadSingle(), reader.ReadSingle());
        for (var n = (ground.Width + 1) * (ground.Height + 1); n > 0; n--) ground.TerrainTiles.Add(reader.ReadTerrainTile(ground.FormatVersion));
        return ground;
    }

    static Vector3 Vector(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

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

    // World Editor 6117 and 6127 (2.0.4's campaign maps) wrote format 39 without the race HUD each player has from
    // 6140, which War3Net 6.1.2 reads in every format-39 file, so it took a player's flags from its name: such a file
    // is given a race HUD of none in each player (D300).
    static byte[] PlayerRaceHuds(byte[] bytes)
    {
        if (Int(bytes, 0) != 39 || Int(bytes, 8) >= 6140) return bytes;
        var at = Strings(bytes, 28, 4);       // after the versions: the name, author, description and players' text
        at = Strings(bytes, at + 69, 4);      // after the camera to the race HUD: the loading screen's path and texts
        at = Strings(bytes, at + 4, 4);       // after the data set: the prologue's path and texts
        at = Strings(bytes, at + 48, 1) + 69; // after the fog and weather: the sound environment; lighting to minimap
        var count = Int(bytes, at);
        var output = new MemoryStream();
        output.Write(bytes, 0, at += 4);
        for (; count > 0; count--)
        {
            // The id, controller and race, the race HUD, then the flags, name, start and priorities
            var end = Strings(bytes, at + 16, 1) + 24;
            output.Write(bytes, at, 12);
            output.Write(new byte[4]);
            output.Write(bytes, at + 12, end - at - 12);
            at = end;
        }

        output.Write(bytes, at, bytes.Length - at);
        return output.ToArray();
    }

    // war3map.w3s repeats each sound's name and file after its settings, and War3Net 6.1.2 refuses a sound whose repeat
    // differs (15 in four of Reforged's maps: g_snd_X, then gg_snd_X, the scripts' name): the name is taken as repeated
    // (D301).
    static byte[] SoundNamesRepeated(byte[] bytes)
    {
        var format = Int(bytes, 0);
        if (format < 2) return bytes;
        var output = new MemoryStream();
        output.Write(bytes, 0, 8);
        var at = 8;
        for (var count = Int(bytes, 4); count > 0; count--)
        {
            var file = Strings(bytes, at, 1);
            var repeat = Strings(bytes, file, 2) + 68;  // after the file, EAX setting and settings
            var label = Strings(bytes, repeat, 1);
            var keys = Strings(bytes, label, 2);        // after the label and the file repeated
            var unk4 = Strings(bytes, keys + 4, 1) + 4; // after the text key, a text and the speaker key
            var end = Strings(bytes, unk4 + 4, Int(bytes, unk4) != 0 ? 6 : 5) + (format >= 3 ? 4 : 0);
            output.Write(bytes, repeat, label - repeat); // the name as repeated, in the first name's place
            output.Write(bytes, file, end - file);
            at = end;
        }

        output.Write(bytes, at, bytes.Length - at);
        return output.ToArray();
    }

    // What War3Net reads from bytes when it reads every one of them; null when it throws or stops short
    static T? Whole<T>(Func<BinaryReader, T> read, byte[] bytes) where T : class
    {
        var reader = new BinaryReader(new MemoryStream(bytes));
        try { return read(reader) is var value && reader.BaseStream.Position == bytes.Length ? value : null; }
        catch (Exception e) when (e is not OutOfMemoryException) { return null; }
    }

    static int Int(byte[] bytes, int at) => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(at));

    // The place after the count null-terminated strings at at
    static int Strings(byte[] bytes, int at, int count)
    {
        for (; count > 0; count--)
            at = Array.IndexOf(bytes, (byte)0, at) is >= 0 and var end ? end + 1 : throw new EndOfStreamException();
        return at;
    }
}
