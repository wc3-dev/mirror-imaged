using System.Text;
using MirrorImaged.Data;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

public sealed class MapArchiveTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    [Fact]
    public void Reads_a_maps_war3map_lua_as_it_is_stored()
    {
        const string script = "-- é\nfunction main() end\n";

        Assert.Equal(script, Encoding.UTF8.GetString(MapArchive.ReadLua(_maps.Build(script))));
    }

    [Fact]
    public void A_jass_map_has_no_war3map_lua()
    {
        var map = _maps.Build("function main takes nothing returns nothing\nendfunction\n", ScriptLanguage.Jass);

        var error = Assert.Throws<FileNotFoundException>(() => MapArchive.ReadLua(map));
        Assert.Contains("war3map.lua", error.Message);
    }
}
