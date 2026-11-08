using War3Net.IO.Mpq;

namespace MirrorImaged.Data;

/// <summary>Reads a map's files from its archive, a <c>.w3x</c> or <c>.w3m</c>.</summary>
public static class MapArchive
{
    /// <summary>The map's <c>war3map.lua</c>, as it is stored.</summary>
    /// <exception cref="FileNotFoundException">The map has no <c>war3map.lua</c>, as a JASS map has none.</exception>
    public static byte[] ReadLua(string path)
    {
        using var archive = MpqArchive.Open(path, false);
        if (!archive.TryOpenFile("war3map.lua", out var stream))
            throw new FileNotFoundException($"{path} has no war3map.lua; only Lua maps run so far", path);
        using (stream)
        {
            var script = new MemoryStream();
            stream.CopyTo(script);
            return script.ToArray();
        }
    }
}
