using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>
/// The string and number natives against what the probes strings, strings-2 and strings-3 measured in the game
/// (docs/facts.md, "Strings, numbers and hashtables"). Where the model differs (Close), the game's value is given
/// beside the model's and the model's is asserted, so the recorded gap can't grow unseen.
/// </summary>
public sealed class StringNativesTests : IDisposable
{
    readonly GameNatives _natives = new(new Game(0));
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static float F(uint bits) => BitConverter.UInt32BitsToSingle(bits);

    static uint Bits(float f) => BitConverter.SingleToUInt32Bits(f);

    // The Lua map's main prints what the test asks, through the natives' Lua binding.
    IReadOnlyList<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n"), "");
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ") + 2)..])];
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("a", -1587459251)]
    [InlineData("A", -1587459251)]
    [InlineData("abc", 1043745117)]
    [InlineData("ABC", 1043745117)]
    [InlineData("/", 1838906452)]
    [InlineData("\\", 1838906452)]
    [InlineData("a/b", -2092314359)]
    [InlineData("a\\b", -2092314359)]
    [InlineData("hello world", -1563733934)]
    [InlineData("Hello World", -1563733934)]
    [InlineData("abcdefghijk", -1733605089)]
    [InlineData("abcdefghijkl", -358854790)]
    [InlineData("abcdefghijklm", -911692467)]
    [InlineData("abcdefghijklmnopqrstuvwx", 583294010)]
    [InlineData("abcdefghijklmnopqrstuvwxy", 74245078)]
    [InlineData("war3map.lua", -1655558825)]
    [InlineData("Units\\Human\\Footman", 736755776)]
    [InlineData("z{|}~", -1517250396)]
    [InlineData("ä", 159638553)]
    [InlineData("Ä", 159638553)]
    [InlineData("é", -1499360152)]
    [InlineData("0", -242600650)]
    [InlineData("123", 1101291013)]
    [InlineData("中", -234949316)]
    [InlineData("-", 2000040631)]
    [InlineData("ÿ", -842383544)]
    [InlineData("Ÿ", 184478878)]
    [InlineData("x", -1449975558)]
    [InlineData("ß", 847928083)]
    [InlineData("ẞ", -1090827626)]
    [InlineData("€", -1318684164)]
    [InlineData("¬", -1504205738)]
    [InlineData("αβ", -154141055)]
    [InlineData("Α", 1295812739)]
    [InlineData("Ω", 255223544)]
    [InlineData("а", 1688666443)]
    [InlineData("А", 1688666443)]
    [InlineData("😀", -1471725602)]
    [InlineData("aä", 530046118)]
    [InlineData("äa", -1686530025)]
    [InlineData("ää", -1179506023)]
    [InlineData("aB/c\\D", -2048350672)]
    public void StringHash_folds_case_and_slashes_and_hashes_with_lookup2(string s, int game) =>
        Assert.Equal(game, _natives.StringHash(s));

    // probe strings-3: each pair hashed alike where the game folds the letter, apart where it doesn't
    [Theory]
    [InlineData("ö", "Ö", true)]
    [InlineData("þ", "Þ", true)]
    [InlineData("÷", "×", true)]
    [InlineData("œ", "Œ", true)]
    [InlineData("ё", "Ё", true)]
    [InlineData("я", "Я", true)]
    [InlineData("ł", "Ł", false)]
    [InlineData("ą", "Ą", false)]
    [InlineData("ž", "Ž", false)]
    [InlineData("š", "Š", false)]
    [InlineData("ő", "Ő", false)]
    [InlineData("ı", "I", false)]
    [InlineData("ա", "Ա", false)]
    [InlineData("ａ", "Ａ", false)]
    [InlineData("ǆ", "Ǆ", false)]
    [InlineData("α", "Α", false)]
    [InlineData("µ", "Μ", false)]
    [InlineData("ÿ", "Ÿ", false)]
    public void StringHash_folds_the_letters_the_game_folds(string lower, string upper, bool same) =>
        Assert.Equal(same, _natives.StringHash(lower) == _natives.StringHash(upper));

    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    [InlineData("ä", 2)]
    [InlineData("中", 3)]
    public void StringLength_counts_bytes(string s, int game) => Assert.Equal(game, _natives.StringLength(s));

    [Theory]
    [InlineData("abcdef", 0, 3, "abc")]
    [InlineData("abcdef", 2, 2, "")]
    [InlineData("abcdef", 4, 2, "ef")]
    [InlineData("abcdef", 2, 100, "cdef")]
    [InlineData("abcdef", 6, 7, "")]
    [InlineData("abcdef", 10, 12, "")]
    [InlineData("abcdef", -1, 3, "")]
    [InlineData("abcdef", -3, -1, "")]
    [InlineData("abcdef", 0, -1, "abcdef")]
    [InlineData("aäb", 1, 3, "ä")]
    [InlineData("", 0, 0, "")]
    public void SubString_takes_bytes_from_start_to_end_and_the_rest_for_an_end_before_the_start(string s, int start, int end, string game) =>
        Assert.Equal(game, _natives.SubString(s, start, end));

    // A cut inside a letter: the game gives the byte (c3, and a4 then b); C# holds no lone byte (decisions D38).
    [Fact]
    public void SubString_inside_a_letter_gives_U_FFFD_where_the_game_gives_the_byte()
    {
        Assert.Equal("\uFFFD", _natives.SubString("aäb", 1, 2));
        Assert.Equal("\uFFFDb", _natives.SubString("aäb", 2, 4));
    }

    [Theory]
    [InlineData("aBc1!", true, "ABC1!")]
    [InlineData("aBc1!", false, "abc1!")]
    [InlineData("äÖé", true, "äÖé")]
    [InlineData("äÖé", false, "äÖé")]
    [InlineData("z{@[`", true, "Z{@[`")]
    [InlineData("z{@[`", false, "z{@[`")]
    [InlineData("", true, "")]
    [InlineData("ёЁ", true, "ёЁ")]
    public void StringCase_changes_ASCII_letters_only(string s, bool upper, string game) =>
        Assert.Equal(game, _natives.StringCase(s, upper));

    [Theory]
    [InlineData(0, "0")]
    [InlineData(-5, "-5")]
    [InlineData(2147483647, "2147483647")]
    [InlineData(-2147483648, "-2147483648")]
    public void I2S_writes_the_integer(int i, string game) => Assert.Equal(game, _natives.I2S(i));

    [Theory]
    [InlineData("123", 123)]
    [InlineData("-45", -45)]
    [InlineData("+7", 7)]
    [InlineData(" 12", 12)]
    [InlineData("\t5", 5)]
    [InlineData("12abc", 12)]
    [InlineData("abc", 0)]
    [InlineData("", 0)]
    [InlineData("2147483647", 2147483647)]
    [InlineData("2147483648", 2147483647)]
    [InlineData("-2147483648", -2147483648)]
    [InlineData("-2147483649", -2147483648)]
    [InlineData("99999999999", 2147483647)]
    [InlineData("1.9", 1)]
    [InlineData("0x10", 0)]
    [InlineData("1e3", 1)]
    [InlineData("--5", 0)]
    [InlineData("0012", 12)]
    [InlineData("- 5", 0)]
    public void S2I_reads_leading_digits_after_spaces_and_a_sign_held_at_the_integers_ends(string s, int game) =>
        Assert.Equal(game, _natives.S2I(s));

    [Theory]
    [InlineData("1.5", 0x3fc00001u)]
    [InlineData("-2.25", 0xc0100000u)]
    [InlineData("abc", 0x00000000u)]
    [InlineData("", 0x00000000u)]
    [InlineData("1e3", 0x3f800000u)]
    [InlineData("1.5e2", 0x3fc00001u)]
    [InlineData(" 2.5", 0x00000000u)]
    [InlineData("3.0x", 0x40400001u)]
    [InlineData(".5", 0x3f000000u)]
    [InlineData("5.", 0x40a00000u)]
    [InlineData("0x10", 0x00000000u)]
    [InlineData("1e40", 0x3f800000u)]
    [InlineData("-1e40", 0xbf800000u)]
    [InlineData("1e-50", 0x3f800000u)]
    [InlineData("inf", 0x00000000u)]
    [InlineData("nan", 0x00000000u)]
    [InlineData("0.1", 0x3dccccceu)]
    [InlineData("16777217", 0x4b800000u)]
    [InlineData("16777219", 0x4b800001u)]
    [InlineData("123456789", 0x4ceb79a2u)]
    [InlineData("1,5", 0x3f800000u)]
    [InlineData("+.5e1", 0x3f000000u)]
    [InlineData("0.25", 0x3e800000u)]
    [InlineData("0.75", 0x3f400000u)]
    [InlineData("0.125", 0x3e000000u)]
    [InlineData("0.3", 0x3e99999au)]
    [InlineData("0.7", 0x3f333334u)]
    [InlineData("2.5", 0x40200000u)]
    [InlineData("7.2", 0x40e66667u)]
    [InlineData("1.1", 0x3f8ccccdu)]
    [InlineData("1.01", 0x3f8147aeu)]
    [InlineData("1.001", 0x3f8020c5u)]
    [InlineData("1.0001", 0x3f800347u)]
    [InlineData("1.2345", 0x3f9e0419u)]
    [InlineData("0.001", 0x3a83126fu)]
    [InlineData("0.0001", 0x38d1b718u)]
    [InlineData("123.456", 0x42f6e979u)]
    [InlineData("0.123456789", 0x3dfcd6eau)]
    [InlineData("99999.99", 0x47c34fffu)]
    [InlineData("1234567.8", 0x4996b43fu)]
    [InlineData("100.01", 0x42c8051fu)]
    [InlineData("-0.5", 0xbf000000u)]
    [InlineData("-0", 0x00000000u)]
    [InlineData("00.5", 0x3f000000u)]
    [InlineData("1..5", 0x3f800000u)]
    [InlineData("1.5.5", 0x3fc00001u)]
    [InlineData(".", 0x00000000u)]
    [InlineData("-", 0x00000000u)]
    [InlineData("-.5", 0xbf000000u)]
    [InlineData("+-1", 0x00000000u)]
    [InlineData("4294967296", 0x4f7fffffu)]
    [InlineData("4294967297.5", 0x4f7fffffu)]
    [InlineData("2147483648", 0x4effffffu)]
    [InlineData("0.30000001", 0x3e99999au)]
    [InlineData("1.5x", 0x3fc00001u)]
    [InlineData("25000000000.5", 0x50ba43b7u)]
    [InlineData("1e5", 0x3f800000u)]
    [InlineData("1 ", 0x3f800000u)]
    [InlineData("16777216.5", 0x4b800000u)]
    [InlineData("1048576.25", 0x49800002u)]
    [InlineData("0.0000000001", 0x2edbe701u)]
    [InlineData("1.10", 0x3f8ccccdu)]
    [InlineData("1.100", 0x3f8ccccdu)]
    [InlineData("1.1000", 0x3f8ccccdu)]
    [InlineData("10.5", 0x41280000u)]
    [InlineData("100.5", 0x42c90001u)]
    [InlineData("1000.5", 0x447a2001u)]
    public void S2R_reads_a_sign_digits_and_a_point_in_the_games_arithmetic(string s, uint game) =>
        Assert.Equal(game, Bits(_natives.S2R(s)));

    [Theory]
    [InlineData("1.00001", 0x3f800055u, 0x3f800054u)]
    [InlineData("1.000001", 0x3f800009u, 0x3f800008u)]
    [InlineData("3.14159", 0x40490fd1u, 0x40490fd0u)]
    [InlineData("0.000001", 0x358637beu, 0x358637bdu)]
    [InlineData("12345678901234567890", 0x5f2b54a8u, 0x5f2b54a5u)]
    [InlineData("2.50000", 0x40200001u, 0x40200000u)]
    [InlineData("2.500000000000", 0x40200000u, 0x401fffffu)]
    [InlineData("0.1000000000", 0x3dcccccdu, 0x3dccccceu)]
    [InlineData("0.5555555555555", 0x3f0e38e3u, 0x3f0e38e2u)]
    [InlineData("9.999999999", 0x41200000u, 0x411fffffu)]
    public void S2R_is_a_float_step_off_the_game_on_these(string s, uint game, uint model)
    {
        Assert.NotEqual(game, model);
        Assert.Equal(model, Bits(_natives.S2R(s)));
    }

    [Theory]
    [InlineData(0, 0x00000000u)]
    [InlineData(16777217, 0x4b800000u)]
    [InlineData(16777219, 0x4b800001u)]
    [InlineData(-16777219, 0xcb800001u)]
    [InlineData(2147483647, 0x4effffffu)]
    public void I2R_cuts_toward_zero(int i, uint game) => Assert.Equal(game, Bits(_natives.I2R(i)));

    [Theory]
    [InlineData(0x3ff33333u, 1)]
    [InlineData(0xbff33333u, -1)]
    [InlineData(0x3f7fffffu, 0)]
    [InlineData(0x80000000u, 0)]
    [InlineData(0x4effffffu, 2147483520)]
    [InlineData(0x4f000000u, 2147483647)]
    [InlineData(0x501502f9u, 2147483647)]
    [InlineData(0xd01502f9u, -2147483648)]
    [InlineData(0x7f800000u, 2147483647)]
    public void R2I_cuts_toward_zero_held_at_the_integers_ends(uint r, int game) => Assert.Equal(game, _natives.R2I(F(r)));

    [Theory]
    [InlineData(0x3f000000u, 1)]
    [InlineData(0x3fc00000u, 2)]
    [InlineData(0x40200000u, 3)]
    [InlineData(0xbf000000u, -1)]
    [InlineData(0xbfc00000u, -2)]
    [InlineData(0xc0200000u, -3)]
    [InlineData(0x3effffffu, 0)]
    [InlineData(0x501502f9u, 1410065408)]
    public void MathRound_rounds_half_away_from_zero_through_64_bits(uint r, int game) => Assert.Equal(game, _natives.MathRound(F(r)));

    [Theory]
    [InlineData(0x00000000u, "0.000")]
    [InlineData(0x80000000u, "0.000")]
    [InlineData(0x3fc00000u, "1.500")]
    [InlineData(0x3d800000u, "0.063")]
    [InlineData(0x3e400000u, "0.188")]
    [InlineData(0x40040000u, "2.063")]
    [InlineData(0xbd800000u, "-0.063")]
    [InlineData(0x3a03126fu, "0.001")]
    [InlineData(0x3f7fdf3bu, "0.999")]
    [InlineData(0x3f7fdfe3u, "1.000")]
    [InlineData(0x3eaaaaabu, "0.333")]
    [InlineData(0x3f2aaaabu, "0.667")]
    [InlineData(0x47f12065u, "123456.789")]
    [InlineData(0x3dcccccdu, "0.100")]
    [InlineData(0xb8d1b717u, "-0.000")]
    [InlineData(0x2edbe6ffu, "0.000")]
    [InlineData(0x4b800000u, "16777216.000")]
    [InlineData(0x7f800000u, "inf")]
    [InlineData(0xff800000u, "-inf")]
    [InlineData(0x3ea00000u, "0.313")]
    [InlineData(0x3ee00000u, "0.438")]
    [InlineData(0x3f100000u, "0.563")]
    [InlineData(0x3f300000u, "0.688")]
    [InlineData(0x3f500000u, "0.813")]
    [InlineData(0x3f700000u, "0.938")]
    [InlineData(0x3d000000u, "0.031")]
    [InlineData(0x3dc00000u, "0.094")]
    [InlineData(0x3e200000u, "0.156")]
    [InlineData(0x3e000000u, "0.125")]
    [InlineData(0x3e4ccccdu, "0.200")]
    [InlineData(0x3e99999au, "0.300")]
    [InlineData(0x3f333333u, "0.700")]
    [InlineData(0x3f8ccccdu, "1.100")]
    [InlineData(0x402b3333u, "2.675")]
    [InlineData(0x3a030bb9u, "0.000")]
    [InlineData(0x3f801062u, "1.000")]
    [InlineData(0x3f803127u, "1.002")]
    [InlineData(0x40000831u, "2.000")]
    [InlineData(0x3ac49ba6u, "0.002")]
    [InlineData(0x3b23d70au, "0.002")]
    [InlineData(0x3b656042u, "0.004")]
    [InlineData(0x447a0000u, "1000.000")]
    [InlineData(0x4640e400u, "12345.000")]
    [InlineData(0x47c35000u, "100000.000")]
    [InlineData(0x49742400u, "1000000.000")]
    [InlineData(0x4996b438u, "1234567.000")]
    [InlineData(0x4b189680u, "10000000.000")]
    [InlineData(0x4b7fffffu, "16777215.000")]
    [InlineData(0x4c000000u, "33554432.000")]
    [InlineData(0x4cbebc20u, "100000000.000")]
    [InlineData(0x4ceb79a3u, "123456792.000")]
    [InlineData(0x4e6e6b28u, "1000000000.000")]
    [InlineData(0x42f6e979u, "123.456")]
    [InlineData(0x40490fdbu, "3.142")]
    [InlineData(0x402df854u, "2.718")]
    [InlineData(0x449a522bu, "1234.568")]
    [InlineData(0x42c7ffbeu, "99.999")]
    [InlineData(0x45800001u, "4096.000")]
    [InlineData(0x4affffffu, "8388607.500")]
    [InlineData(0xbfc00000u, "-1.500")]
    [InlineData(0xba03126fu, "-0.001")]
    [InlineData(0xc2f6e9bau, "-123.456")]
    [InlineData(0x3a83126fu, "0.001")]
    [InlineData(0x38d1b717u, "0.000")]
    [InlineData(0x3727c5acu, "0.000")]
    [InlineData(0x3f7fffefu, "1.000")]
    [InlineData(0x411ffdf4u, "10.000")]
    [InlineData(0x419ffefau, "20.000")]
    [InlineData(0x3c1ba5e3u, "0.009")]
    [InlineData(0x3f7ffffeu, "1.000")]
    public void R2S_writes_three_decimals_rounded_half_up_in_the_games_arithmetic(uint r, string game) =>
        Assert.Equal(game, _natives.R2S(F(r)));

    [Theory]
    [InlineData(0x3fc00000u, 8, 2, "     1.50")]
    [InlineData(0x3fc00000u, 0, 0, "2.0")]
    [InlineData(0x40200000u, 0, 0, "3.0")]
    [InlineData(0x3f000000u, 0, 0, "1.0")]
    [InlineData(0xbfc00000u, 0, 0, "-2.0")]
    [InlineData(0x3d800000u, 1, 3, "0.063")]
    [InlineData(0xbfc00000u, -8, 2, "-1.50")]
    [InlineData(0x3fc00000u, 0, -1, "1.5")]
    [InlineData(0x3dcccccdu, 0, 12, "0.100000000")]
    [InlineData(0x42f6e979u, 2, 1, "123.5")]
    [InlineData(0x3fc00000u, 3, 30, "1.500000000")]
    [InlineData(0x3fc00000u, -1, 2, "1.50")]
    [InlineData(0x3fc00000u, 0, 2, "1.50")]
    [InlineData(0x3fc00000u, 1, 2, "1.50")]
    [InlineData(0x3fc00000u, 2, 2, "1.50")]
    [InlineData(0x3fc00000u, 3, 2, "1.50")]
    [InlineData(0x3fc00000u, 4, 2, " 1.50")]
    [InlineData(0x3fc00000u, 5, 2, "  1.50")]
    [InlineData(0x3fc00000u, 6, 2, "   1.50")]
    [InlineData(0x3fc00000u, 10, 2, "       1.50")]
    [InlineData(0x3fc00000u, 20, 2, "                 1.50")]
    [InlineData(0x3f9e064bu, 0, 0, "1.0")]
    [InlineData(0x3f9e064bu, 0, 1, "1.2")]
    [InlineData(0x3f9e064bu, 0, 2, "1.23")]
    [InlineData(0x3f9e064bu, 0, 3, "1.235")]
    [InlineData(0x3f9e064bu, 0, 4, "1.2346")]
    [InlineData(0x3f9e064bu, 0, 5, "1.23457")]
    [InlineData(0x3f9e064bu, 0, 6, "1.234567")]
    [InlineData(0x3f9e064bu, 0, 7, "1.2345670")]
    [InlineData(0x3f9e064bu, 0, 8, "1.23456704")]
    [InlineData(0x3f9e064bu, 0, 9, "1.234567040")]
    [InlineData(0x3f9e064bu, 0, 10, "1.234567040")]
    [InlineData(0x3f9e064bu, 0, 11, "1.234567040")]
    [InlineData(0x3f9e064bu, 0, 12, "1.234567040")]
    [InlineData(0xbfc00000u, 8, 2, "-     1.50")]
    [InlineData(0xbfc00000u, 4, 2, "- 1.50")]
    [InlineData(0x41440000u, 8, 1, "     12.3")]
    [InlineData(0x3fc00000u, 8, 0, "       2.0")]
    [InlineData(0x42c80000u, 4, 0, " 100.0")]
    [InlineData(0x42c80000u, 6, 3, "100.000")]
    [InlineData(0x3a03126fu, 0, 3, "0.001")]
    [InlineData(0x47f12065u, 0, 5, "123456.78906")]
    [InlineData(0x3f000000u, 0, 1, "0.5")]
    [InlineData(0x3d4ccccdu, 0, 1, "0.1")]
    [InlineData(0x3ee66666u, 0, 1, "0.4")]
    [InlineData(0x3f733333u, 0, 1, "0.9")]
    [InlineData(0x41180000u, 0, 0, "10.0")]
    [InlineData(0x42c70000u, 0, 0, "100.0")]
    [InlineData(0x00000000u, 5, 2, "  0.00")]
    [InlineData(0x80000000u, 0, 2, "0.00")]
    [InlineData(0xbb83126fu, 0, 2, "-0.00")]
    [InlineData(0xbbc49ba6u, 0, 2, "-0.01")]
    [InlineData(0x40200000u, 3, -1, " 2.5")]
    [InlineData(0x3fa00000u, 0, -2, "1.25")]
    [InlineData(0x449a5000u, 2, 0, "1235.0")]
    public void R2SW_pads_after_the_sign_and_writes_at_most_nine_decimals(uint r, int width, int precision, string game) =>
        Assert.Equal(game, _natives.R2SW(F(r), width, precision));

    [Theory]
    [InlineData(0x60ad78ecu, "100000057600000000000.000", "100000002004087734272.000")]
    [InlineData(0xe0ad78ecu, "-100000057600000000000.000", "-100000002004087734272.000")]
    [InlineData(0x7f7fc99eu, "340000512000000000000000000000000000000.000", "339999995214436424907732413799364296704.000")]
    [InlineData(0x4f000000u, "2147483840.000", "2147483648.000")]
    [InlineData(0x4f800000u, "4294967680.000", "4294967296.000")]
    [InlineData(0x501502f9u, "10000000640.000", "10000000000.000")]
    [InlineData(0x5368d4a5u, "1000000128000.000", "999999995904.000")]
    [InlineData(0x58635fa9u, "1000000256000000.000", "999999986991104.000")]
    [InlineData(0x7f7fffffu, "inf", "340282346638528859811704183484516925440.000")]
    public void R2S_from_2_to_the_31_writes_the_floats_digits_where_the_game_writes_its_own(uint r, string game, string model)
    {
        Assert.NotEqual(game, model);
        Assert.Equal(model, _natives.R2S(F(r)));
    }

    [Theory]
    [InlineData(0x60ad78ecu, 0, 2, "100000057600000000000.00", "100000002004087734272.00")]
    [InlineData(0x60ad78ecu, 30, 2, "100000057600000000000.00", "100000002004087734272.00")]
    public void R2SW_from_2_to_the_31_writes_the_floats_digits_unpadded(uint r, int width, int precision, string game, string model)
    {
        Assert.NotEqual(game, model);
        Assert.Equal(model, _natives.R2SW(F(r), width, precision));
    }

    [Fact]
    public void The_bit_natives_are_and_or_and_xor()
    {
        Assert.Equal(5, _natives.BlzBitAnd(-1, 5));
        Assert.Equal(7, _natives.BlzBitOr(5, 3));
        Assert.Equal(6, _natives.BlzBitXor(5, 3));
        Assert.Equal(-2147483648, _natives.BlzBitXor(-1, 2147483647));
    }

    // probes strings and strings-3: a string parameter refuses nil and booleans, takes a number as tostring writes it,
    // and is read up to its first NUL
    [Fact]
    public void A_string_parameter_takes_a_number_as_tostring_writes_it_and_stops_at_a_NUL()
    {
        Assert.Equal(
            ["5\t3\t42", "5 1e+20", "3 3.0", "4 -0.0", "3 0.1", "9 123456789", "12 2.147484e+09", "9 0.3333333",
             "1\ttrue\ta\t"],
            Run("""
                print(StringLength(12345), StringLength(1.5), S2I(42))
                for _, v in ipairs({1e20, 3.0, -0.0, 0.1, 123456789, 2 ^ 31, 1 / 3}) do
                  print(StringLength(v) .. ' ' .. SubString(v, 0, 20))
                end
                print(StringLength('a\0b'), StringHash('a\0b') == StringHash('a'), SubString('a\0bc', 0, 4), SubString('a\0bc', 2, 4))
                """));
    }

    [Fact]
    public void A_string_parameter_refuses_nil_and_a_boolean()
    {
        Assert.Equal(
            ["bad argument #1 to 'StringHash' (string expected, got nil)",
             "bad argument #1 to 'StringLength' (string expected, got boolean)",
             "bad argument #1 to 'SubString' (string expected, got nil)"],
            Run("""
                for _, f in ipairs({function() return StringHash(nil) end, function() return StringLength(true) end,
                    function() return SubString(nil, 0, 1) end}) do
                  print((select(2, pcall(f)):gsub('^.-: ', '', 1)))
                end
                """));
    }
}
