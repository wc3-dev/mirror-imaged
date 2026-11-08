using MirrorImaged.Data;
using War3Net.Build;
using War3Net.Build.Object;
using War3Net.Build.Script;
using War3Net.Common.Extensions;

namespace MirrorImaged.Tests;

/// <summary>
/// The map's changes to the standard data, read from maps built here: its object data and skin files, its strings and
/// its gameplay constants (game-facts, "Object data"; docs/facts.md, "The game's data").
/// </summary>
public sealed class GameDataTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static int Id(string id) => Ids.Of(id);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", new Dictionary<string, string> { ["uhpm"] = "420", ["usca"] = "0.8", ["unam"] = "Footman" })
        .Add(ObjectKind.Unit, "hkni", new Dictionary<string, string> { ["unam"] = "Knight" })
        .Add(ObjectKind.Ability, "AHhb", new Dictionary<string, string> { ["alev"] = "3", ["Hhb1:1"] = "200", ["Hhb1:2"] = "400", ["Hhb1:3"] = "600" })
        .Add(ObjectKind.Upgrade, "Rhme", new Dictionary<string, string> { ["glvl"] = "3" });

    // The data probe's map, and a footman made larger as the facts' moonwell-0.2.3-inherit made it.
    static void Changed(Map map)
    {
        map.TriggerStrings = new TriggerStrings { Strings = { new TriggerString { Key = 3, Value = "Probe Footman" } } };
        map.UnitObjectData = new UnitObjectData(ObjectDataFormatVersion.v3)
        {
            BaseUnits =
            {
                Change("hfoo", ("unam", "TRIGSTR_003"), ("uhpm", 500), ("usca", 1.5f)),
                Change("hkni", ("unam", "Main Knight")),
                Change("zzzz", ("unam", "Nothing")),
            },
            NewUnits = { new SimpleObjectModification { OldId = "hfoo".FromRawcode(), NewId = "h000".FromRawcode(), Unk = { 0 } } },
        };
        map.UnitSkinObjectData = new UnitObjectData(ObjectDataFormatVersion.v3) { BaseUnits = { Change("hkni", ("unam", "Skin Knight")) } };
        map.AbilityObjectData = new AbilityObjectData(ObjectDataFormatVersion.v3)
        {
            BaseAbilities =
            {
                new LevelObjectModification
                {
                    OldId = "AHhb".FromRawcode(),
                    Unk = { 0 },
                    Modifications =
                    {
                        new LevelObjectDataModification { Id = "alev".FromRawcode(), Type = ObjectDataType.Int, Value = 5 },
                        new LevelObjectDataModification { Id = "Hhb1".FromRawcode(), Type = ObjectDataType.Unreal, Value = 250f, Level = 1, Pointer = 1 },
                        new LevelObjectDataModification { Id = "Hhb1".FromRawcode(), Type = ObjectDataType.Unreal, Value = 1000f, Level = 5, Pointer = 1 },
                    },
                },
            },
        };
        map.UpgradeObjectData = new UpgradeObjectData(ObjectDataFormatVersion.v3)
        {
            BaseUpgrades =
            {
                new LevelObjectModification
                {
                    OldId = "Rhme".FromRawcode(),
                    Unk = { 0 },
                    Modifications = { new LevelObjectDataModification { Id = "glvl".FromRawcode(), Type = ObjectDataType.Int, Value = 5 } },
                },
            },
            NewUpgrades = { new LevelObjectModification { OldId = "Rhme".FromRawcode(), NewId = "R000".FromRawcode(), Unk = { 0 } } },
        };
    }

    static SimpleObjectModification Change(string id, params (string Field, object Value)[] changes)
    {
        var change = new SimpleObjectModification { OldId = id.FromRawcode(), Unk = { 0 } };
        foreach (var (field, value) in changes)
            change.Modifications.Add(new SimpleObjectDataModification
            {
                Id = field.FromRawcode(),
                Type = value switch { int => ObjectDataType.Int, float => ObjectDataType.Real, _ => ObjectDataType.String },
                Value = value,
            });
        return change;
    }

    GameData Load(IReadOnlyDictionary<string, string>? files = null) =>
        new(Standard(), MapArchive.Read(_maps.Build("function main() end", map: Changed, files: files)));

    [Fact]
    public void A_map_changes_a_standard_object_with_its_strings_resolved()
    {
        var footman = Load().Object(ObjectKind.Unit, Id("hfoo"))!;

        Assert.Equal("Probe Footman", footman.Text(Id("unam")));
        Assert.Equal(500, footman.Integer(Id("uhpm")));
        Assert.Equal(1.5f, footman.Real(Id("usca")));
    }

    // game-facts, "Object data": h001 based on a changed hfoo has 420 life and scale 0.8; the data probe: R000 from a
    // Rhme of 5 levels has 3
    [Fact]
    public void A_new_object_starts_from_its_standard_base_not_the_maps_change_to_it()
    {
        var data = Load();
        var made = data.Object(ObjectKind.Unit, Id("h000"))!;

        Assert.Equal(Id("hfoo"), made.BaseId);
        Assert.Equal(420, made.Integer(Id("uhpm")));
        Assert.Equal(0.8f, made.Real(Id("usca")));
        Assert.Equal("Footman", made.Text(Id("unam")));
        Assert.Equal(5, data.Object(ObjectKind.Upgrade, Id("Rhme"))!.Levels);
        Assert.Equal(3, data.Object(ObjectKind.Upgrade, Id("R000"))!.Levels);
    }

    // The data probe: hkni renamed in war3map.w3u and in war3mapSkin.w3u reads the skin file's name
    [Fact]
    public void The_skin_files_change_comes_after_the_main_files()
    {
        Assert.Equal("Skin Knight", Load().Object(ObjectKind.Unit, Id("hkni"))!.Text(Id("unam")));
    }

    [Fact]
    public void A_change_to_an_object_the_game_doesnt_have_is_dropped()
    {
        Assert.Null(Load().Object(ObjectKind.Unit, Id("zzzz")));
    }

    [Fact]
    public void A_change_of_the_level_count_reaches_the_levels_past_the_standard_ones()
    {
        var holyLight = Load().Object(ObjectKind.Ability, Id("AHhb"))!;

        Assert.Equal(5, holyLight.Levels);
        Assert.Equal(["250", "400", "600", null, "1000", null], new[] { 1, 2, 3, 4, 5, 6 }.Select(level => holyLight.Text(Id("Hhb1"), level)));
    }

    // game-facts, "Time of day": a map's war3mapMisc.txt sets DayHours=30
    [Fact]
    public void The_maps_misc_file_changes_the_constants_it_names()
    {
        var data = Load(new Dictionary<string, string> { ["war3mapMisc.txt"] = "[Misc]\nDayHours=30\n" });

        Assert.Equal("30", data.Constant("DayHours"));
        Assert.Equal(20, new GameData(new StandardData()).Constant("DayHours", 20));
    }

    // A real written with an exponent would read as atoi reads it: 1E-05 as 1
    [Fact]
    public void A_changed_real_is_its_text_without_an_exponent()
    {
        var data = new GameData(Standard(), MapArchive.Read(_maps.Build("function main() end", map: map =>
            map.UnitObjectData = new UnitObjectData(ObjectDataFormatVersion.v3)
            {
                BaseUnits = { Change("hfoo", ("usca", 0.00001f), ("ucol", 3e30f), ("uhpm", 1.234567e-21f), ("umpm", -2.5e-38f), ("usid", float.Epsilon), ("usin", -123.45f)) },
            })));
        var footman = data.Object(ObjectKind.Unit, Id("hfoo"))!;

        Assert.Equal("0.00001", footman.Text(Id("usca")));
        Assert.Equal(0.00001f, footman.Real(Id("usca")));
        Assert.Equal(0, footman.Integer(Id("usca")));
        Assert.Equal("3000000000000000000000000000000", footman.Text(Id("ucol")));
        Assert.Equal(3e30f, footman.Real(Id("ucol")));
        Assert.Equal(int.MaxValue, footman.Integer(Id("ucol")));
        // every digit kept down to the smallest floats, which decimal text cut at 1e-21
        Assert.Equal("0.000000000000000000001234567", footman.Text(Id("uhpm")));
        Assert.Equal(1.234567e-21f, footman.Real(Id("uhpm")));
        Assert.Equal(-2.5e-38f, footman.Real(Id("umpm")));
        Assert.Equal(float.Epsilon, footman.Real(Id("usid")));
        Assert.Equal("-123.45", footman.Text(Id("usin")));
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData(" +42abc", 42)]
    [InlineData("-7.9", -7)]
    [InlineData("1E-05", 1)]
    [InlineData("abc", 0)]
    [InlineData("99999999999999999999", int.MaxValue)]
    [InlineData("-99999999999", int.MinValue)]
    public void A_whole_number_is_read_as_atoi_reads_it_held_in_the_integers_range(string text, int number)
    {
        Assert.Equal(number, Numbers.Integer(text));
    }
}
