using MirrorImaged.Lua;

namespace MirrorImaged.Natives;

// In the game the maths natives give Lua's math.* numbers (facts, "Groups and performance"; probe strings), so they
// share its model (native/mifloat.c) and are Close as it is (decisions D31, D32), but where the two differ in the game:
// SquareRoot is 0 below 0.001, and Asin, Acos and Atan2 give 0 outside their domains where math.* doesn't.
public sealed partial class GameNatives
{
    /// <summary>As math.sqrt; 0 below 0.001, as the game gives.</summary>
    [Native(Level.Close)]
    public float SquareRoot(float x) => x < 0.001f ? 0 : Native.mi_sqrtf(x);

    [Native(Level.Close)]
    public float Sin(float radians) => Native.mi_sinf(radians);

    [Native(Level.Close)]
    public float Cos(float radians) => Native.mi_cosf(radians);

    [Native(Level.Close)]
    public float Tan(float radians) => Native.mi_tanf(radians);

    /// <summary>As math.asin; 0 outside -1 to 1.</summary>
    [Native(Level.Close)]
    public float Asin(float y) => Native.mi_asinf(y);

    /// <summary>As math.acos; 0 outside -1 to 1.</summary>
    [Native(Level.Close)]
    public float Acos(float x) => Native.mi_acosf(x);

    [Native(Level.Close)]
    public float Atan(float x) => Native.mi_atan2f(x, 1);

    /// <summary>As math.atan(y, x); Atan2(0, 0) is 0.</summary>
    [Native(Level.Close)]
    public float Atan2(float y, float x) => Native.mi_atan2f(y, x);

    /// <summary>As Lua's x ^ power.</summary>
    [Native(Level.Close)]
    public float Pow(float x, float power) => Native.mi_powf(x, power);

    /// <summary>Times the float nearest pi/180, in the game's arithmetic: Deg2Rad(180) is 3.1415925.</summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public float Deg2Rad(float degrees) => Native.mi_mul(degrees, (float)(Math.PI / 180));

    /// <summary>Times the float nearest 180/pi, in the game's arithmetic: Rad2Deg(1) is 57.2957802.</summary>
    [Native(Level.Exact, Fact = StringsFact)]
    public float Rad2Deg(float radians) => Native.mi_mul(radians, (float)(180 / Math.PI));
}
