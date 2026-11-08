using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.IO.Mpq;

namespace MirrorImaged.Data;

/// <summary>Reads a map's files from its archive, a <c>.w3x</c> or <c>.w3m</c>.</summary>
public static class MapArchive
{
    /// <summary>
    /// The map's script as it is stored, and whether it is JASS: its <c>war3map.lua</c>, else its <c>war3map.j</c>
    /// (also as <c>scripts\war3map.j</c>, where older maps keep it).
    /// </summary>
    /// <exception cref="FileNotFoundException">The map has neither.</exception>
    public static (byte[] Script, bool Jass) ReadScript(string path)
    {
        using var archive = MpqArchive.Open(path, false);
        foreach (var (name, jass) in new[] { ("war3map.lua", false), ("war3map.j", true), (@"scripts\war3map.j", true) })
        {
            if (!archive.TryOpenFile(name, out var stream)) continue;
            using (stream)
            {
                var script = new MemoryStream();
                stream.CopyTo(script);
                return (script.ToArray(), jass);
            }
        }
        throw new FileNotFoundException($"{path} has no war3map.lua or war3map.j", path);
    }

    /// <summary>The map's <c>war3map.w3i</c>, with its players, forces and settings; null when it has none.</summary>
    public static MapInfo? ReadInfo(string path)
    {
        using var archive = MpqArchive.Open(path, false);
        if (!archive.TryOpenFile("war3map.w3i", out var stream)) return null;
        using var reader = new BinaryReader(stream);
        return reader.ReadMapInfo();
    }
}
