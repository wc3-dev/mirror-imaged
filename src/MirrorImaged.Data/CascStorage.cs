using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace MirrorImaged.Data;

/// <summary>
/// The game's files in a Warcraft III install's CASC storage (<c>Data\data</c>), read in place: the active build config
/// named in <c>.build.info</c>, the newest <c>.idx</c> file of each bucket, the encoding table and the text root, whose
/// lines are <c>War3.w3mod:path|content key|...</c>. Only files stored locally can be read. Not for use from several
/// threads at once.
/// </summary>
public sealed class CascStorage : IDisposable
{
    readonly string _data;
    readonly Dictionary<(ulong, byte), (int Archive, long Offset, int Size)> _index = [];
    readonly Dictionary<UInt128, byte[]> _encodingKeys = [];
    readonly Dictionary<string, UInt128> _root = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<int, FileStream> _archives = [];

    CascStorage(string folder, GameBuild build)
    {
        _data = Path.Combine(folder, "Data");
        Build = build;
    }

    /// <summary>The install's active build.</summary>
    public GameBuild Build { get; }

    /// <summary>Opens the storage of the install in <paramref name="gameFolder"/> (the folder with <c>.build.info</c>).</summary>
    /// <exception cref="IOException">The folder holds no install, or its storage cannot be read.</exception>
    public static CascStorage Open(string gameFolder)
    {
        CascStorage? storage = null;
        try
        {
            storage = new CascStorage(gameFolder, GameInstall.Build(gameFolder));
            var config = BuildConfig(storage._data, storage.Build.Key);
            storage.ReadIndexes();
            storage.ReadEncoding(Convert.FromHexString(config["encoding"][1]));
            storage.ReadRoot(Convert.FromHexString(config["root"][0]));
            return storage;
        }
        catch (Exception exception)
        {
            storage?.Dispose();
            if (exception is IOException)
                throw;
            throw new IOException($"The game's storage in {gameFolder} cannot be read: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// The file's bytes by its path in the root (<c>War3.w3mod:scripts/common.j</c>, any case); null when the root
    /// does not name it or it is not stored locally.
    /// </summary>
    public byte[]? Read(string path) =>
        _root.TryGetValue(path, out var contentKey) && _encodingKeys.TryGetValue(contentKey, out var encodingKey)
            ? ReadEncoded(encodingKey)
            : null;

    /// <summary>The paths the root names, as <see cref="Read"/> takes them, stored locally or not.</summary>
    internal IEnumerable<string> Names => _root.Keys;

    public void Dispose()
    {
        foreach (var archive in _archives.Values)
            archive.Dispose();
        _archives.Clear();
    }

    // The active build's config: key = values, from Data\config\ab\cd\abcd....
    static Dictionary<string, string[]> BuildConfig(string data, string key)
    {
        var config = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var line in File.ReadLines(Path.Combine(data, "config", key[..2], key[2..4], key)))
        {
            var equals = line.IndexOf('=');
            if (equals > 0 && !line.StartsWith('#'))
                config[line[..equals].Trim()] = line[(equals + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        return config;
    }

    // The newest .idx of each bucket (the file name is the bucket, then the version, in hex). An entry is 9 bytes of
    // encoding key, 5 bytes of archive and offset (big-endian: the top 10 bits the archive), then a 4-byte size.
    void ReadIndexes()
    {
        var newest = new Dictionary<int, (long Version, string Path)>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(_data, "data"), "*.idx"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var bucket = Convert.ToInt32(name[..2], 16);
            var version = Convert.ToInt64(name[2..], 16);
            if (!newest.TryGetValue(bucket, out var known) || known.Version < version)
                newest[bucket] = (version, path);
        }

        foreach (var (_, path) in newest.Values)
        {
            var bytes = ReadShared(path);
            var headerSize = BinaryPrimitives.ReadInt32LittleEndian(bytes);
            var at = (8 + headerSize + 15) & ~15;
            var entriesSize = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(at));
            at += 8;
            for (var entry = at; entry + 18 <= at + entriesSize; entry += 18)
            {
                var key = (BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(entry)), bytes[entry + 8]);
                var place = ((long)bytes[entry + 9] << 32) | BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(entry + 10));
                var size = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(entry + 14));
                _index.TryAdd(key, ((int)(place >> 30), place & 0x3FFFFFFF, size));
            }
        }
    }

    // The encoding table's pages map each content key to its encoding keys (the first one is used).
    void ReadEncoding(byte[] encodingKey)
    {
        var bytes = ReadEncoded(encodingKey) ?? throw new InvalidDataException("the encoding table is not stored locally");
        if (bytes[0] != 'E' || bytes[1] != 'N')
            throw new InvalidDataException("the encoding table has no EN header");

        int contentKeySize = bytes[3], encodingKeySize = bytes[4];
        var pageSize = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(5)) * 1024;
        var pageCount = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(9));
        var specSize = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(18));
        var pages = 22 + specSize + pageCount * 32;
        for (var page = 0; page < pageCount; page++)
        {
            var start = pages + page * pageSize;
            for (var at = start; at < start + pageSize;)
            {
                var count = bytes[at];
                if (count == 0)
                    break;

                var contentKey = BinaryPrimitives.ReadUInt128BigEndian(bytes.AsSpan(at + 6, contentKeySize));
                _encodingKeys.TryAdd(contentKey, bytes.AsSpan(at + 6 + contentKeySize, encodingKeySize).ToArray());
                at += 6 + contentKeySize + count * encodingKeySize;
            }
        }
    }

    void ReadRoot(byte[] contentKey)
    {
        var encodingKey = _encodingKeys[BinaryPrimitives.ReadUInt128BigEndian(contentKey)];
        var bytes = ReadEncoded(encodingKey) ?? throw new InvalidDataException("the root is not stored locally");
        foreach (var line in Encoding.UTF8.GetString(bytes).Split('\n'))
        {
            var parts = line.TrimEnd('\r').Split('|');
            if (parts.Length >= 2 && parts[1].Length == 32)
                _root.TryAdd(parts[0], BinaryPrimitives.ReadUInt128BigEndian(Convert.FromHexString(parts[1])));
        }
    }

    // A file by its encoding key: its place in a data.NNN archive, after a 30-byte header, BLTE-encoded.
    byte[]? ReadEncoded(byte[] encodingKey)
    {
        var key = (BinaryPrimitives.ReadUInt64BigEndian(encodingKey), encodingKey[8]);
        if (!_index.TryGetValue(key, out var place))
            return null;

        if (!_archives.TryGetValue(place.Archive, out var archive))
        {
            var path = Path.Combine(_data, "data", $"data.{place.Archive:D3}");
            _archives[place.Archive] = archive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }

        var raw = new byte[place.Size];
        archive.Position = place.Offset;
        archive.ReadExactly(raw);

        return Blte(raw.AsSpan(30));
    }

    // The launcher and the game keep the storage's files open for writing while they run.
    static byte[] ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    // BLTE: a header of chunk sizes (or none, for one chunk), then chunks that are plain (N) or zlib (Z).
    internal static byte[] Blte(ReadOnlySpan<byte> bytes)
    {
        if (!bytes[..4].SequenceEqual("BLTE"u8))
            throw new InvalidDataException("a file is not BLTE-encoded");

        var headerSize = BinaryPrimitives.ReadInt32BigEndian(bytes[4..]);
        var chunks = new List<Range>();
        if (headerSize == 0)
        {
            chunks.Add(8..bytes.Length);
        }
        else
        {
            var count = (bytes[9] << 16) | (bytes[10] << 8) | bytes[11];
            var at = headerSize;
            for (var chunk = 0; chunk < count; chunk++)
            {
                var size = BinaryPrimitives.ReadInt32BigEndian(bytes[(12 + chunk * 24)..]);
                chunks.Add(at..(at + size));
                at += size;
            }
        }

        using var output = new MemoryStream();
        foreach (var range in chunks)
        {
            var chunk = bytes[range];
            switch ((char)chunk[0])
            {
                case 'N':
                    output.Write(chunk[1..]);
                    break;
                case 'Z':
                    using (var zlib = new ZLibStream(new MemoryStream(chunk[1..].ToArray()), CompressionMode.Decompress))
                        zlib.CopyTo(output);
                    break;
                default:
                    throw new InvalidDataException($"a BLTE chunk has the unknown mode '{(char)chunk[0]}'");
            }
        }

        return output.ToArray();
    }
}
