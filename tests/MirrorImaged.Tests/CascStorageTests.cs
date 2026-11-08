using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

public sealed class CascStorageTests
{
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

    static byte[] Zlib(string text)
    {
        using var bytes = new MemoryStream();
        using (var zlib = new ZLibStream(bytes, CompressionLevel.Optimal))
            zlib.Write(Encoding.ASCII.GetBytes(text));
        return bytes.ToArray();
    }
}
