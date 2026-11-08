using System.Text;
using MirrorImaged.Data;
using War3Net.Build;
using War3Net.Build.Info;
using War3Net.IO.Mpq;

namespace MirrorImaged.Tests;

public sealed class MapArchiveTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    [Fact]
    public void Reads_a_maps_war3map_lua_as_it_is_stored()
    {
        const string script = "-- é\nfunction main() end\n";

        var (read, jass) = MapArchive.ReadScript(_maps.Build(script));

        Assert.Equal(script, Encoding.UTF8.GetString(read));
        Assert.False(jass);
    }

    [Fact]
    public void A_jass_map_s_script_is_its_war3map_j()
    {
        const string script = "function main takes nothing returns nothing\nendfunction\n";

        var (read, jass) = MapArchive.ReadScript(_maps.Build(script, ScriptLanguage.Jass));

        Assert.Equal(script, Encoding.UTF8.GetString(read));
        Assert.True(jass);
    }

    // where older maps keep it
    [Fact]
    public void A_war3map_j_in_the_scripts_folder_is_read_too()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".w3x");
        var info = MapFactory.Info(32, 32);
        var builder = new MapBuilder(new Map { Info = info, Environment = MapFactory.Environment(info) });
        builder.AddFiles(MpqFile.New(new MemoryStream("// old\n"u8.ToArray()), @"scripts\war3map.j"));
        builder.Build(path);
        try
        {
            var (read, jass) = MapArchive.ReadScript(path);

            Assert.Equal("// old\n", Encoding.UTF8.GetString(read));
            Assert.True(jass);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void A_map_without_a_script_says_so()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".w3x");
        var info = MapFactory.Info(32, 32);
        new MapBuilder(new Map { Info = info, Environment = MapFactory.Environment(info) }).Build(path);
        try
        {
            var error = Assert.Throws<FileNotFoundException>(() => MapArchive.ReadScript(path));
            Assert.Equal($"{path} has no war3map.lua or war3map.j", error.Message);
        }
        finally { File.Delete(path); }
    }
}
