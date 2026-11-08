namespace MirrorImaged.GameTests;

public sealed class ProbeComparisonTests : IDisposable
{
    readonly string _file = Path.GetTempFileName();

    public void Dispose() => File.Delete(_file);

    public static TheoryData<string> Probes => [.. ProbeComparison.Names];

    // Without the game: MirrorImaged against the game's lines in probes/expected (decisions D120, D121).
    [Theory]
    [MemberData(nameof(Probes))]
    public void A_probe_gives_the_games_lines_in_MirrorImaged_but_for_its_recorded_differences(string name) =>
        ProbeComparison.AssertMirror(name);

    List<Difference> Compare(string expected, params string[] run)
    {
        File.WriteAllText(_file, expected);
        return ProbeComparison.Compare(_file, run);
    }

    [Fact]
    public void A_varying_line_matches_any_number_anywhere_and_the_other_lines_come_in_order()
    {
        const string expected = "# a note\na\n~ w {t} true\nb\n";

        Assert.Empty(Compare(expected, "a", "b", "w 1.5 true"));
        Assert.Empty(Compare(expected, "w -2 true", "a", "b"));
        Assert.Equal([new(0, null, "w 1.5 false"), new(3, "~ w {t} true", null)], Compare(expected, "w 1.5 false", "a", "b"));
        Assert.Equal([new(2, "a", null), new(4, null, "a")], Compare(expected, "b", "a", "w 1 true"));
        Assert.Equal([new(3, "~ w {t} true", null)], Compare(expected, "a", "b"));
    }

    [Fact]
    public void Lines_that_differ_between_the_same_common_lines_are_paired_in_order()
    {
        const string expected = "a\nb\nc\nd\ne\n";

        Assert.Equal([new(2, "b", "B"), new(3, "c", "C")], Compare(expected, "a", "B", "C", "d", "e"));
        Assert.Equal([new(2, "b", "B"), new(3, "c", null), new(5, null, "f")], Compare(expected, "a", "B", "d", "e", "f"));
        Assert.Equal([new(1, null, "x"), new(5, "e", null)], Compare(expected, "a", "x", "b", "c", "d"));
        Assert.Equal([new(1, "a", null), new(2, "b", null), new(3, "c", null), new(4, "d", null), new(5, "e", null)], Compare(expected));
    }

    [Theory]
    [InlineData("~ w done")]
    [InlineData("~ {t}")]
    [InlineData("~ {w} {t}")]
    public void A_moving_line_without_both_varying_and_fixed_text_is_refused(string line)
    {
        var error = Assert.Throws<InvalidDataException>(() => Compare($"a\n{line}\n", "a"));

        Assert.Equal($"{_file}:2: a line starting \"~ \" moves, and needs a {{t}} or {{w}} for what varies and some text that doesn't", error.Message);
    }

    [Fact]
    public void A_varying_word_is_ascii_letters_digits_and_underscores()
    {
        const string expected = "slot 0 RACE_{w} team 0\n";

        Assert.Empty(Compare(expected, "slot 0 RACE_UNDEAD team 0"));
        Assert.Empty(Compare(expected, "slot 0 RACE_night_elf2 team 0"));
        Assert.Equal([new(1, "slot 0 RACE_{w} team 0", "slot 0 RACE_UNDEAD x team 0")], Compare(expected, "slot 0 RACE_UNDEAD x team 0"));
        Assert.Equal([new(1, "slot 0 RACE_{w} team 0", "slot 0 RACE_é team 0")], Compare(expected, "slot 0 RACE_é team 0"));
    }

    [Fact]
    public void A_line_with_a_varying_part_that_does_not_move_keeps_its_place()
    {
        const string expected = "a\nw {t} {w}\nb\n";

        Assert.Empty(Compare(expected, "a", "w 1.5 true", "b"));
        Assert.Equal([new(2, "w {t} {w}", null), new(3, null, "w 1.5 true")], Compare(expected, "a", "b", "w 1.5 true"));
    }

    // Every expected and differences file belongs to a probe in probes/.
    [Fact]
    public void Every_record_has_its_probe()
    {
        var names = ProbeComparison.Names.ToHashSet();
        var records = new[] { "expected", "differences" }
            .SelectMany(folder => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "probes", folder), "*.txt"))
            .Select(Path.GetFileNameWithoutExtension);

        Assert.All(records, name => Assert.Contains(name!, names));
    }

    [Fact]
    public void A_difference_names_the_line_and_both_sides_and_is_recorded_as_a_diff()
    {
        Assert.Equal("line 2: the game gave \"b\", MirrorImaged \"B\"", new Difference(2, "b", "B").ToString());
        Assert.Equal("line 3: the game gave \"c\", MirrorImaged nothing", new Difference(3, "c", null).ToString());
        Assert.Equal("after line 5: the game gave nothing, MirrorImaged \"f\"", new Difference(5, null, "f").ToString());
        Assert.Equal("MirrorImaged stopped without lines: war3map.lua:3: no", new Difference(0, null, null, "war3map.lua:3: no").ToString());
        Assert.Equal(["- b", "+ B"], new Difference(2, "b", "B").Recorded);
        Assert.Equal(["- c"], new Difference(3, "c", null).Recorded);
        Assert.Equal(["+ f"], new Difference(5, null, "f").Recorded);
        Assert.Equal(["! war3map.lua:3: no"], new Difference(0, null, null, "war3map.lua:3: no").Recorded);
    }
}
