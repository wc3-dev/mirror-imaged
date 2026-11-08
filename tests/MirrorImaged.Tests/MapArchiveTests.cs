using System.Text;
using MirrorImaged.Data;
using War3Net.Build;
using War3Net.Build.Audio;
using War3Net.Build.Environment;
using War3Net.Build.Import;
using War3Net.Build.Info;
using War3Net.Build.Object;
using War3Net.Build.Script;
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

        Assert.Equal(script, Encoding.UTF8.GetString(MapArchive.Read(_maps.Build(script)).Script!));
    }

    [Fact]
    public void A_jass_map_has_no_war3map_lua_and_doesnt_run()
    {
        var map = _maps.Build("function main takes nothing returns nothing\nendfunction\n", ScriptLanguage.Jass);

        Assert.Null(MapArchive.Read(map).Script);
        var error = Assert.Throws<FileNotFoundException>(() => Simulation.Load(map, ""));
        Assert.Contains("war3map.lua", error.Message);
    }

    [Fact]
    public void Every_map_file_is_read_and_one_the_map_lacks_is_null()
    {
        var path = _maps.Build("function main() end", map: map =>
        {
            map.TriggerStrings = new TriggerStrings { Strings = { new TriggerString { Key = 1, Value = "One" } } };
            map.UnitObjectData = new UnitObjectData(ObjectDataFormatVersion.v3) { NewUnits = { new SimpleObjectModification { OldId = 1, NewId = 2, Unk = { 0 } } } };
            map.UnitSkinObjectData = new UnitObjectData(ObjectDataFormatVersion.v3);
            map.AbilityObjectData = new AbilityObjectData(ObjectDataFormatVersion.v3);
            map.Regions = new MapRegions(MapRegionsFormatVersion.v5) { Regions = { new Region { Name = "Gate", Left = -64, Bottom = -64, Right = 64, Top = 64 } } };
            map.Sounds = new MapSounds(MapSoundsFormatVersion.v1) { Sounds = { new Sound { Name = "gg_snd_Horn", FilePath = "Sound\\Horn.flac", EaxSetting = "DefaultEAXON", SoundName = "", Unk2 = "", Unk5 = "", Unk6 = "", Unk7 = "" } } };
            map.ImportedFiles = new ImportedFiles(ImportedFilesFormatVersion.v1) { Files = { new ImportedFile { Flags = ImportedFileFlags.UNK1 | ImportedFileFlags.UNK4, FullPath = "war3mapImported\\ui.toc" } } };
        }, files: new Dictionary<string, string>
        {
            ["war3mapMisc.txt"] = "[Misc]\nDayHours=30\n",
            ["war3mapImported\\ui.toc"] = "war3mapImported\\ui.fdf\r\n",
            ["war3mapImported\\ui.fdf"] = "Frame \"TEXT\" \"Name\" {}\n",
        });

        var map = MapArchive.Read(path).Files;
        var files = MapArchive.Read(path);

        Assert.Equal("One", map.TriggerStrings!.Strings[0].Value);
        Assert.Equal(2, map.UnitObjectData!.NewUnits[0].NewId);
        Assert.NotNull(map.UnitSkinObjectData);
        Assert.NotNull(map.AbilityObjectData);
        Assert.Null(map.ItemObjectData);
        Assert.Equal("Gate", map.Regions!.Regions[0].Name);
        Assert.Equal("gg_snd_Horn", map.Sounds!.Sounds[0].Name);
        Assert.Equal("war3mapImported\\ui.toc", map.ImportedFiles!.Files[0].FullPath);
        Assert.Equal(32u, map.Environment!.Width);
        Assert.Single(map.Info!.Players);
        Assert.Null(map.Cameras);
        Assert.Null(map.Doodads);
        Assert.Equal("[Misc]\nDayHours=30\n", Encoding.UTF8.GetString(files.Misc!));
        Assert.Equal("war3mapImported\\ui.fdf\r\n", Encoding.UTF8.GetString(files.File("war3mapImported\\ui.toc")!));
        Assert.Equal("Frame \"TEXT\" \"Name\" {}\n", Encoding.UTF8.GetString(files.File("WAR3MAPIMPORTED\\UI.FDF")!));
        Assert.Null(files.File("war3mapImported\\none.fdf"));
    }

    // The install's (4)Roundabout, (10)DustwallowKeys and (1)SilverpineSprint each have one such file; the game loads them
    [Fact]
    public void A_file_that_cant_be_read_is_left_out_and_named()
    {
        var path = Path.Combine(Path.GetDirectoryName(_maps.Build("function main() end"))!, "broken.w3x");
        var archive = new MpqArchiveBuilder();
        archive.AddFile(MpqFile.New(new MemoryStream(Encoding.UTF8.GetBytes("not regions")), "war3map.w3r"));
        archive.AddFile(MpqFile.New(new MemoryStream(Encoding.UTF8.GetBytes("function main() end")), "war3map.lua"));
        archive.SaveTo(path);

        var map = MapArchive.Read(path);

        Assert.Null(map.Files.Regions);
        Assert.NotNull(map.Script);
        Assert.StartsWith("war3map.w3r: ", Assert.Single(map.Unread));
        Assert.Empty(MapArchive.Read(_maps.Build("function main() end")).Unread);
    }

    // docs/facts.md, "The game's data": TRIGSTR_1, TRIGSTR_0001 and "TRIGSTR_001 " are string 1, trigstr_001 and a string
    // the file doesn't have stay as they are
    [Fact]
    public void A_trigstr_is_the_maps_string_its_number_read_as_atoi_reads_it()
    {
        var files = MapArchive.Read(_maps.Build("function main() end", map: map =>
            map.TriggerStrings = new TriggerStrings { Strings = { new TriggerString { Key = 1, Value = "Probe Alpha" } } }));

        Assert.Equal(["Probe Alpha", "Probe Alpha", "Probe Alpha", "Probe Alpha", "TRIGSTR_999", "trigstr_001", "TRIGSTR_", "other"],
            new[] { "TRIGSTR_001", "TRIGSTR_1", "TRIGSTR_0001", "TRIGSTR_001 ", "TRIGSTR_999", "trigstr_001", "TRIGSTR_", "other" }.Select(files.Resolve));
    }
}
