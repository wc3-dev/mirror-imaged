using System.Runtime.InteropServices;
using System.Text;

namespace MirrorImaged.Lua;

// The game's float arithmetic and maths (native/mifloat.c), which Lua's operators and math library use, for the
// natives that compute as the game does.
internal static unsafe partial class Native
{
    [LibraryImport(Lib)] public static partial float mi_add(float a, float b);
    [LibraryImport(Lib)] public static partial float mi_sub(float a, float b);
    [LibraryImport(Lib)] public static partial float mi_mul(float a, float b);
    [LibraryImport(Lib)] public static partial float mi_div(float a, float b);
    [LibraryImport(Lib)] public static partial float mi_sqrtf(float x);
    [LibraryImport(Lib)] public static partial float mi_sinf(float x);
    [LibraryImport(Lib)] public static partial float mi_cosf(float x);
    [LibraryImport(Lib)] public static partial float mi_tanf(float x);
    [LibraryImport(Lib)] public static partial float mi_asinf(float x);
    [LibraryImport(Lib)] public static partial float mi_acosf(float x);
    [LibraryImport(Lib)] public static partial float mi_atan2f(float y, float x);
    [LibraryImport(Lib)] public static partial float mi_powf(float x, float y);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int mi_snprintf(byte* s, nuint size, string format, double n);

    /// <summary>A float as Lua's <c>tostring</c> writes it: <c>%.7g</c>, with <c>.0</c> after what reads as a whole number.</summary>
    public static string Text(float f)
    {
        var buffer = stackalloc byte[64];
        var text = Encoding.ASCII.GetString(buffer, mi_snprintf(buffer, 64, "%.7g", f));
        return text.AsSpan().IndexOfAnyExcept("-0123456789") < 0 ? text + ".0" : text;
    }
}
