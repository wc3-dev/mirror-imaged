using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

public sealed class CascStorageTests : IDisposable
{
    const string BuildKey = "abcd0000000000000000000000000000";

    readonly string _game = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_game, true);

    [Fact]
    public void A_blte_file_with_no_chunk_table_is_one_chunk()
    {
        byte[] file = [.. "BLTE"u8, 0, 0, 0, 0, .. "Nplain"u8];

        Assert.Equal("plain", Encoding.ASCII.GetString(CascStorage.Blte(file)));
    }

    [Fact]
    public void A_blte_file_joins_its_plain_and_zlib_chunks()
    {
        byte[] first = [.. "N"u8, .. "plain, "u8];
        byte[] second = [.. "Z"u8, .. Zlib("then packed")];
        var headerSize = 12 + 2 * 24;
        var file = new byte[headerSize + first.Length + second.Length];
        "BLTE"u8.CopyTo(file);
        BinaryPrimitives.WriteInt32BigEndian(file.AsSpan(4), headerSize);
        file[11] = 2;
        BinaryPrimitives.WriteInt32BigEndian(file.AsSpan(12), first.Length);
        BinaryPrimitives.WriteInt32BigEndian(file.AsSpan(12 + 24), second.Length);
        first.CopyTo(file, headerSize);
        second.CopyTo(file, headerSize + first.Length);

        Assert.Equal("plain, then packed", Encoding.ASCII.GetString(CascStorage.Blte(file)));
    }

    [Fact]
    public void A_file_that_is_not_blte_is_an_error()
    {
        Assert.Throws<InvalidDataException>(() => CascStorage.Blte("NOPE\0\0\0\0"u8));
    }

    [Fact]
    public void A_blte_chunk_of_an_unknown_mode_is_an_error()
    {
        byte[] file = [.. "BLTE"u8, 0, 0, 0, 0, .. "Edata"u8];

        Assert.Throws<InvalidDataException>(() => CascStorage.Blte(file));
    }

    [Fact]
    public void Opening_a_folder_with_no_storage_is_an_io_error()
    {
        Assert.ThrowsAny<IOException>(() => CascStorage.Open(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())));
    }

    [Fact]
    public void Build_info_with_no_active_build_is_wrapped_in_an_io_error()
    {
        Write(".build.info", "Branch!STRING:0|Active!DEC:1|Build Key!HEX:16|Version!STRING:0\n");

        var error = Assert.Throws<IOException>(() => CascStorage.Open(_game));

        Assert.IsType<InvalidDataException>(error.InnerException);
        Assert.Contains(_game, error.Message);
    }

    [Fact]
    public void A_storage_that_cannot_be_decoded_is_wrapped_in_an_io_error_and_leaves_no_file_open()
    {
        WriteStorage([.. new byte[30], .. "NOPE"u8]);

        var error = Assert.Throws<IOException>(() => CascStorage.Open(_game));

        Assert.IsType<InvalidDataException>(error.InnerException);
        Assert.Contains(_game, error.Message);
        AssertClosed("Data/data/data.000");
    }

    [Fact]
    public void A_storage_cut_short_is_the_io_error_itself_and_leaves_no_file_open()
    {
        WriteStorage(new byte[10]);

        Assert.Throws<EndOfStreamException>(() => CascStorage.Open(_game));

        AssertClosed("Data/data/data.000");
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void The_install_s_scripts_are_read_from_its_storage()
    {
        using var storage = CascStorage.Open(GameInstall.Require());

        var commonJ = Encoding.UTF8.GetString(storage.Read("War3.w3mod:scripts/common.j")!);

        Assert.StartsWith("//", commonJ);
        Assert.Contains("native GetPlayerId", commonJ);
        Assert.NotNull(storage.Read("WAR3.W3MOD:Scripts/Common.ai"));
        Assert.Null(storage.Read("War3.w3mod:scripts/no-such-file.j"));
        Assert.Equal(GameInstall.Build(GameInstall.Require()), storage.Build);
    }

    // A storage whose index puts the encoding table in data.000 at 0, 34 bytes long, after a 30-byte header.
    void WriteStorage(byte[] archive)
    {
        Write(".build.info", $"Branch!STRING:0|Active!DEC:1|Build Key!HEX:16|Version!STRING:0\neu|1|{BuildKey}|1.0\n");
        Write($"Data/config/ab/cd/{BuildKey}", $"encoding = {new string('0', 32)} {new string('1', 32)}\nroot = {new string('2', 32)}\n");
        var index = new byte[42];
        BinaryPrimitives.WriteInt32LittleEndian(index.AsSpan(16), 18);
        index.AsSpan(24, 9).Fill(0x11);
        BinaryPrimitives.WriteInt32LittleEndian(index.AsSpan(38), 34);
        Write("Data/data/0000000001.idx", index);
        Write("Data/data/data.000", archive);
    }

    void Write(string path, string text) => Write(path, Encoding.ASCII.GetBytes(text));

    void Write(string path, byte[] bytes)
    {
        var full = Path.Combine(_game, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
    }

    // Opening a file no one else may share fails while the storage still holds it.
    void AssertClosed(string path)
    {
        using var _ = new FileStream(Path.Combine(_game, path), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    static byte[] Zlib(string text)
    {
        using var bytes = new MemoryStream();
        using (var zlib = new ZLibStream(bytes, CompressionLevel.Optimal))
            zlib.Write(Encoding.ASCII.GetBytes(text));
        return bytes.ToArray();
    }
}
