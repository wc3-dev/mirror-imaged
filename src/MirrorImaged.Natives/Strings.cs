using System.Globalization;
using System.Text;
using MirrorImaged.Lua;

namespace MirrorImaged.Natives;

// The string and number natives as the probes strings to strings-6 measured them (docs/facts.md, "Strings,
// numbers and hashtables"). A string is the game's bytes as UTF-8: one that isn't UTF-8 reaches C# changed (decisions
// D38), which is where SubString and StringHash can differ from the game.
public sealed partial class GameNatives
{
    const string StringsFact = "docs/facts.md, Strings, numbers and hashtables (probes strings to strings-6)";

    /// <summary>
    /// The string's length in bytes. A string that isn't UTF-8 reaches C# with U+FFFD, three bytes, in place of each
    /// bad byte (decisions D38), so its length can differ from the game's.
    /// </summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public int StringLength(string? s) => Encoding.UTF8.GetByteCount(s ?? "");

    /// <summary>
    /// The bytes from <paramref name="start"/> up to <paramref name="end"/>; "" for a start below 0 or past the
    /// end, and the rest of the string for an end before the start; null for null, which JASS gives (probe
    /// jass-threads). A cut inside a letter gives U+FFFD (decisions D38), where the game gives the byte.
    /// </summary>
    [Native(Level.Close)]
    public string? SubString(string? source, int start, int end)
    {
        if (source is null) return null;
        var bytes = Encoding.UTF8.GetBytes(source);
        if (start < 0 || start >= bytes.Length) return "";
        return Encoding.UTF8.GetString(bytes, start, (end < start ? bytes.Length : Math.Min(end, bytes.Length)) - start);
    }

    /// <summary>
    /// The string with its ASCII letters in upper or lower case; other letters stay as they are; null for null, which
    /// JASS gives (probe jass-threads). A string that isn't UTF-8 reaches C# with U+FFFD in place of each bad byte
    /// (decisions D38), and comes back so.
    /// </summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public string? StringCase(string? source, bool upper) => source is null ? null :
        string.Concat(source.Select(c =>
            upper && char.IsAsciiLetterLower(c) ? (char)(c - 32) : !upper && char.IsAsciiLetterUpper(c) ? (char)(c + 32) : c));

    /// <summary>
    /// Bob Jenkins's lookup2 hash, with 0 as its seed, of the string's letters: each folded to upper case where the
    /// game folds it, '/' made '\', then written as one byte below 256 and two bytes, low first, from there; a letter
    /// past U+FFFF is U+FFFD. "" is 0. A string that isn't UTF-8 reaches C# changed (decisions D38), so its hash
    /// differs from the game's.
    /// </summary>
    [Native(Level.Close)]
    public int StringHash(string? s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        var bytes = new List<byte>();
        foreach (var rune in s.EnumerateRunes())
        {
            var c = rune.IsBmp ? Upper((char)rune.Value) : (char)0xFFFD;
            if (c == '/') c = '\\';
            bytes.Add((byte)c);
            if (c > 0xFF) bytes.Add((byte)(c >> 8));
        }
        return (int)Lookup2(bytes);
    }

    // Measured folded (strings, strings-2, strings-3): a, b, c, h, w, ä, é, ö, þ, ÷, œ, а, ё and я; measured left: ÿ, ß,
    // ł, ą, ž, š, ő, ı, α, β, ա, ａ, ǆ and µ. The ranges a-z, à-þ, а-я and ѐ-џ are inferred from those; every other
    // letter is left as it is, measured or not.
    static char Upper(char c) => c switch
    {
        >= 'a' and <= 'z' or >= 'à' and <= 'þ' or >= 'а' and <= 'я' => (char)(c - 32),
        >= 'ѐ' and <= 'џ' => (char)(c - 80),
        'œ' => 'Œ',
        _ => c,
    };

    static uint Lookup2(List<byte> k)
    {
        uint a = 0x9e3779b9, b = a, c = 0;
        int i = 0;
        uint At(int j) => j < k.Count ? k[j] : 0u;
        uint Word(int j) => At(j) | At(j + 1) << 8 | At(j + 2) << 16 | At(j + 3) << 24;
        for (; k.Count - i >= 12; i += 12)
        {
            a += Word(i);
            b += Word(i + 4);
            c += Word(i + 8);
            Mix(ref a, ref b, ref c);
        }
        // The last bytes, with c's lowest byte kept for the length; bytes past the end read as 0.
        a += Word(i);
        b += Word(i + 4);
        c += (uint)k.Count + (Word(i + 8) << 8);
        Mix(ref a, ref b, ref c);
        return c;
    }

    static void Mix(ref uint a, ref uint b, ref uint c)
    {
        a -= b; a -= c; a ^= c >> 13;
        b -= c; b -= a; b ^= a << 8;
        c -= a; c -= b; c ^= b >> 13;
        a -= b; a -= c; a ^= c >> 12;
        b -= c; b -= a; b ^= a << 16;
        c -= a; c -= b; c ^= b >> 5;
        a -= b; a -= c; a ^= c >> 3;
        b -= c; b -= a; b ^= a << 10;
        c -= a; c -= b; c ^= b >> 15;
    }

    [Native(Level.Exact, Fact = StringsFact)]
    public string? I2S(int i) => i.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The number in the string's leading digits, after spaces and a sign, held at the integer's ends: S2I("12abc") is
    /// 12, S2I("2147483648") 2147483647, S2I("abc") 0.
    /// </summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public int S2I(string? s)
    {
        s ??= "";
        int i = 0;
        while (i < s.Length && s[i] is ' ' or '\t' or '\n' or '\v' or '\f' or '\r') i++;
        var negative = i < s.Length && s[i] == '-';
        if (i < s.Length && s[i] is '-' or '+') i++;
        long n = 0;
        for (; i < s.Length && char.IsAsciiDigit(s[i]); i++) n = Math.Min(n * 10 + (s[i] - '0'), 1L << 31);
        return (int)Math.Clamp(negative ? -n : n, int.MinValue, int.MaxValue);
    }

    /// <summary>
    /// A sign, digits and one point, read up to anything else, with no spaces before and no exponent: S2R(" 2.5") and
    /// S2R("abc") are 0, S2R("1e3") 1. The digits make an integer, then a float in the game's arithmetic once past an
    /// integer's range, divided by 10 to the number of digits after the point: so S2R("1.5") is 1.50000012, as in the
    /// game. Close: 10 of the 77 measured strings, most with five or more digits after the point, come out a float step
    /// from the game's; past the float range the sum wraps as the game's products do and the result is extrapolated
    /// (decisions D83: 40 decimals give -1.814 where the game gives 0.123456791).
    /// </summary>
    [Native(Level.Close)]
    public float S2R(string? s)
    {
        s ??= "";
        int i = 0, decimals = 0;
        var negative = i < s.Length && s[i] == '-';
        if (i < s.Length && s[i] is '-' or '+') i++;
        long whole = 0;
        float big = 0;
        bool point = false, past = false;
        for (; i < s.Length && (char.IsAsciiDigit(s[i]) || s[i] == '.' && !point); i++)
        {
            if (s[i] == '.')
            {
                point = true;
                continue;
            }
            if (point) decimals++;
            if (!past && whole * 10 + (s[i] - '0') <= int.MaxValue) whole = whole * 10 + (s[i] - '0');
            else
            {
                if (!past) big = TowardZero(whole);
                past = true;
                big = Native.mi_add(Native.mi_mul(big, 10), s[i] - '0');
            }
        }
        var v = past ? big : TowardZero(whole);
        if (decimals > 0) v = Native.mi_div(v, Power(10, decimals));
        return negative ? Native.mi_mul(v, -1) : v;
    }

    // Whole powers in the game's arithmetic: exact up to 10^10.
    static float Power(float x, int n)
    {
        float p = 1;
        for (int i = 0; i < n; i++) p = Native.mi_mul(p, x);
        return p;
    }

    /// <summary>The integer cut toward zero to a float, as the game cuts: I2R(16777219) is 16777218.</summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public float I2R(int i) => TowardZero(i);

    /// <summary>Cut toward zero, held at the integer's ends: R2I(1e10) is 2147483647.</summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public int R2I(float r) => (int)r;

    /// <summary>Rounded half away from zero, through a 64-bit integer cut to 32 bits: MathRound(1e10) is 1410065408.</summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public int MathRound(float r) => unchecked((int)(long)MathF.Round(r, MidpointRounding.AwayFromZero));

    /// <summary>As <see cref="R2SW"/> with no width and 3 decimals: R2S(0.0625) is "0.063".</summary>
    [Native(Level.Close)]
    public string? R2S(float r) => R2SW(r, 0, 3);

    /// <summary>
    /// The number with <paramref name="precision"/> decimals (at most 9; below 0, 6 with the zeros at the end dropped but
    /// one), rounded half up in the game's arithmetic, and spaces after the sign until the integer part and decimals fill
    /// <paramref name="width"/>; precision 0 writes ".0". At most 255 bytes, the rest cut; "" when the length the width
    /// asks for is past a 32-bit integer. The game's own formatting: R2SW(1.5, 8, 2) is "     1.50", R2SW(-1.5, 4, 2)
    /// "- 1.50", R2SW(1.25, 0, -1) "1.25". A number below 0 whose text passes 255 bytes closes the game; one whose length
    /// wraps past a 32-bit integer gives "", as above 0 (not measured below 0). Close from 2^31 up, where the game writes
    /// about ten digits of its own and then zeros (R2S(1e20) is "100000057600000000000.000") and this writes the float's
    /// digits, and for the largest floats, which the game writes "inf".
    /// </summary>
    /// <exception cref="GameClosedException">The number is below 0 and its text passes 255 bytes, its length not wrapped.</exception>
    [Native(Level.Close)]
    public string? R2SW(float r, int width, int precision)
    {
        if (float.IsInfinity(r)) return r > 0 ? "inf" : "-inf";
        var sign = r < 0 ? "-" : "";
        r = Math.Abs(r);
        var decimals = precision < 0 ? 6 : Math.Min(precision, 9);
        string digits, fraction;
        if (r >= 2147483648f)
        {
            digits = ((double)r).ToString("F0", CultureInfo.InvariantCulture);
            fraction = new string('0', Math.Max(decimals, 1));
        }
        else
        {
            var whole = (int)r;
            var scale = (int)Power(10, decimals);
            var part = (int)Native.mi_add(Native.mi_mul(Native.mi_sub(r, whole), scale), 0.5f);
            if (part >= scale)
            {
                whole++;
                part -= scale;
            }
            digits = whole.ToString(CultureInfo.InvariantCulture);
            fraction = decimals == 0 ? "0" : part.ToString(CultureInfo.InvariantCulture).PadLeft(decimals, '0');
        }
        if (precision < 0) fraction = fraction.TrimEnd('0') is { Length: > 0 } kept ? kept : "0";
        var body = $"{digits}.{fraction}";
        // From 2^31 the game pads nothing; the pad and the length are 32-bit integers that wrap, as the game's.
        var pad = r >= 2147483648f ? 0 : Math.Max(0, unchecked(width - (precision == 0 ? 0 : fraction.Length) - digits.Length));
        var length = unchecked(sign.Length + pad + body.Length);
        if (sign.Length > 0 && length > 255)
            throw new GameClosedException($"The game closes: R2SW's text of {sign}{body} at width {width} passes 255 bytes");
        return length < 0 ? "" : (sign + new string(' ', Math.Min(pad, 255)) + body)[..Math.Min(length, 255)];
    }

    [Native(Level.Exact, Fact = StringsFact)]
    public int BlzBitAnd(int x, int y) => x & y;

    [Native(Level.Exact, Fact = StringsFact)]
    public int BlzBitOr(int x, int y) => x | y;

    [Native(Level.Exact, Fact = StringsFact)]
    public int BlzBitXor(int x, int y) => x ^ y;
}
