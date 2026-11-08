/*
** The game's float arithmetic, measured on 3.0.1.24371 (docs/decisions.md, "The game's Lua numbers"). All on
** the bits with integers, or in doubles with + - * / only, so every platform gives the same bits.
*/
#define LUA_CORE

#include <stdint.h>
#include <string.h>

#include "lua.h"

static uint32_t bits (float f) { uint32_t u; memcpy(&u, &f, sizeof u); return u; }
static float flt (uint32_t u) { float f; memcpy(&f, &u, sizeof f); return f; }

/* Or-ed together with no range check, as the game packs a result: an exponent past the range carries into the
   sign bit (1e38 * 10 is -5.5e-39) and one below it borrows from it (2^-126 - (2^-126 + 2^-149) is -1.6e32). */
static float pack (uint32_t sign, int32_t e, uint32_t m) {
  return flt((sign << 31) | ((uint32_t)e << 23) | (m & 0x7fffff));
}

/* The exact sum or difference, cut toward zero; a number 23 or more octaves smaller, or a zero or subnormal one,
   is dropped. Of two of the same size the second is kept, so 0 + -0 is -0. */
float mi_add (float x, float y) {
  uint32_t a = bits(x), b = bits(y);
  uint64_t ma, mb, m;
  int32_t ea, eb;
  if ((a & 0x7fffffff) <= (b & 0x7fffffff)) { uint32_t t = a; a = b; b = t; }
  ea = (a >> 23) & 0xff;
  eb = (b >> 23) & 0xff;
  if (eb == 0 || ea - eb >= 23) return flt(a);
  ma = (uint64_t)((a & 0x7fffff) | 0x800000) << 23;
  mb = ((uint64_t)((b & 0x7fffff) | 0x800000) << 23) >> (ea - eb);
  if ((a ^ b) >> 31) {
    m = ma - mb;
    if (m == 0) return 0.0f;
    while (m < (1ull << 46)) { m <<= 1; ea--; }
  }
  else {
    m = ma + mb;
    if (m >> 47) { m >>= 1; ea++; }
  }
  return pack(a >> 31, ea, (uint32_t)(m >> 23));
}

float mi_sub (float a, float b) {
  return mi_add(a, flt(bits(b) ^ 0x80000000));
}

/* The top 24 bits of the product. A zero or subnormal operand, or a biased exponent outside 1..256 before the
   mantissas' carry, gives +0 (1e20 * 1e20 is 0; 2^127 * 4 is -0 and 1.5 * 2^127 * 6 is -1.3e-38, packed). */
float mi_mul (float x, float y) {
  uint32_t a = bits(x), b = bits(y);
  int32_t ea = (a >> 23) & 0xff, eb = (b >> 23) & 0xff, e = ea + eb - 127;
  uint64_t m;
  if (ea == 0 || eb == 0 || (uint32_t)(e - 1) > 255) return 0.0f;
  m = (uint64_t)((a & 0x7fffff) | 0x800000) * ((b & 0x7fffff) | 0x800000);
  if (m >> 47) { m >>= 24; e++; }
  else m >>= 23;
  return pack((a ^ b) >> 31, e, (uint32_t)m);
}

/* The reciprocal of a divisor's mantissa m (2^23 to 2^24 - 1) in 2^-47. Measured at the 1024 knots: the k-th is
   2^47 / (m - 1), but the first, 1/1, which is 2^24. Between them a fit, not the game's way (Close, a step off for
   about 1 in 5): straight lines to 2^47 / m less a little at the next knot (2^23 at the last), a step up from below. */
static uint64_t reciprocal (uint32_t m) {
  uint64_t i = (m >> 13) & 0x3ff, f = m & 0x1fff, knot = (0x400 + i) << 13;
  uint64_t lo = i ? (1ull << 47) / (knot - 1) : 1ull << 24;
  uint64_t hi = i == 0x3ff ? 1ull << 23 : ((1ull << 47) - (1ull << 16)) / (knot + 0x2000);
  return f ? (((lo << 13) - (lo - hi) * f) >> 13) + 1 : lo;
}

/* The dividend times the divisor's reciprocal. A number divided by the same bits is 1, so 0/0 and -0/-0 are 1;
   another zero or subnormal dividend gives +0. A zero divisor reads as 1.0 * 2^-127, so 1/0 is 2^127 and 100/0
   is past the range, 0. */
float mi_div (float x, float y) {
  uint32_t a = bits(x), b = bits(y);
  int32_t e = (int32_t)((a >> 23) & 0xff) - (int32_t)((b >> 23) & 0xff) + 126;
  uint64_t r, m;
  if (a == b) return 1.0f;
  if (!(a & 0x7f800000)) return 0.0f;
  r = reciprocal((b & 0x7fffff) | 0x800000);
  if (r >> 24) { r >>= 1; e++; }
  if ((uint32_t)(e - 1) > 255) return 0.0f;
  m = ((a & 0x7fffff) | 0x800000) * r;
  if (m >> 47) { m >>= 24; e++; }
  else m >>= 23;
  return pack((a ^ b) >> 31, e, (uint32_t)m);
}

static float trunc_ (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  if (e < 0) return 0.0f;
  if (e >= 23) return x;
  return flt(a & ~(0x7fffffu >> e));
}

float mi_floorf (float x) {
  float t = trunc_(x);
  return x < t ? t - 1.0f : t;
}

/* A zero exponent reads as a fraction (1.0 * 2^-127), so ceil(0.0) and ceil(-0.0) are 1 as in the game. */
float mi_ceilf (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  if (e >= 23 || (e >= 0 && !(a & (0x7fffffu >> e)))) return x;
  return mi_floorf(x) + 1.0f;
}

float mi_fmodf (float a, float b) {
  return mi_sub(a, mi_mul(trunc_(mi_div(a, b)), b));
}

static uint64_t isqrt (uint64_t n) {
  uint64_t r = 0, bit = 1ull << 62;
  while (bit > n) bit >>= 2;
  while (bit) {
    if (n >= r + bit) { n -= r + bit; r = (r >> 1) + bit; }
    else r >>= 1;
    bit >>= 2;
  }
  return r;
}

/* One more than the integer root of the mantissa m times 2^8 (so 46341 to 65536), made a number from 1 below 2 by
   1 + 46341 (r - 46341) / 2^31, cut toward zero; for an odd exponent, times the float nearest sqrt(2). A zero,
   subnormal or negative number gives 0. Close: 6,156 of the 6,162 roots measured; the game's integer root is a step
   higher for some m * 2^8 just below a square (49 and 68 below, not 25 or 145), by a rule not found. */
float mi_sqrtf (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  uint64_t r;
  float s;
  if ((a >> 31) || e == -127) return 0.0f;
  r = isqrt((uint64_t)((a & 0x7fffff) | 0x800000) << 8) + 1;
  s = pack(0, (e - (e & 1)) / 2 + 127, (uint32_t)((46341 * r - 4633) >> 8));
  return (e & 1) ? mi_mul(s, flt(0x3fb504f3)) : s;
}

/*
** Doubles with + - * / only, the same on every platform, cut to a float toward zero as the game's arithmetic
** cuts.
*/

static const double LN2_HI = 6.93147180369123816490e-01, LN2_LO = 1.90821492927058770002e-10;
static const double PI = 3.14159265358979311600e+00, PIO2 = 1.57079632679489655800e+00;

/* Toward zero, after a nudge of 2^-45 so that a result that should be exact (2^10, 10^3) stays exact. A
   nan is 0: its sign differs between x64 and arm64, and the game makes none. */
static float to_float (double d) {
  uint64_t u;
  int32_t e;
  uint32_t s;
  if (d != d) return 0.0f;
  d *= 1.0 + 0x1p-45;
  memcpy(&u, &d, sizeof u);
  e = (int32_t)((u >> 52) & 0x7ff) - 1023;
  s = (uint32_t)(u >> 63) << 31;
  if (e < -126) return 0.0f;
  if (e > 127) return flt(s | 0x7f800000);
  return flt(s | (uint32_t)(e + 127) << 23 | ((uint32_t)(u >> 29) & 0x7fffff));
}

static double scale2 (double x, int32_t k) {  /* x * 2^k, k in -1022..1023 */
  uint64_t u = (uint64_t)(k + 1023) << 52;
  double p;
  memcpy(&p, &u, sizeof p);
  return x * p;
}

static double sin_d (double x) {  /* x from 0 to pi */
  double k, r, z, ps, pc;
  int32_t n = (int32_t)(x * (2 / PI) + 0.5);
  k = n;
  r = ((x - k * 1.57079632673412561417e+00) - k * 6.07710050650619224932e-11) - k * 2.02226624879595063154e-21;
  z = r * r;
  ps = r + r * z * (-1.0 / 6 + z * (1.0 / 120 + z * (-1.0 / 5040 + z * (1.0 / 362880 + z * (-1.0 / 39916800
       + z * (1.0 / 6227020800.0 + z * (-1.0 / 1307674368000.0 + z * (1.0 / 355687428096000.0))))))));
  pc = 1 + z * (-1.0 / 2 + z * (1.0 / 24 + z * (-1.0 / 720 + z * (1.0 / 40320 + z * (-1.0 / 3628800
       + z * (1.0 / 479001600 + z * (-1.0 / 87178291200.0 + z * (1.0 / 20922789888000.0
       + z * (-1.0 / 6402373705728000.0)))))))));
  return n == 0 ? ps : n == 1 ? pc : -ps;
}

/* sin(pi k / 2048) in 2^-31, cut toward zero, for k 0 to 2048: the game's table, whose top is 2^31 - 1 and whose
   last is 1. */
static int64_t sine_at (uint32_t k) {
  if (k == 0 || k == 1024 || k == 2048) return k == 1024 ? 2147483647 : k == 2048;
  return (int64_t)(sin_d(k * (PI / 2048)) * 2147483648.0);
}

/* A whole number of 2^-31 as a float, cut toward zero. */
static float q31 (int64_t v) {
  uint32_t sign = v < 0, u = (uint32_t)(sign ? -v : v);
  int32_t top = 30;
  if (!u) return 0.0f;
  while (!(u >> top)) top--;
  return pack(sign, top - 31 + 127, top > 23 ? u >> (top - 23) : u << (23 - top));
}

/* The game's sine: the angle in 2^-20 turns, x times the float nearest 1/(2 pi) in its arithmetic and cut toward
   zero, a quarter turn more for the cosine; then the table above for half a turn, the 256 steps between two of its
   values on a straight line divided into 255, negated for the other half. Close: 2,333 of the 2,336 measured; the three
   near pi and its multiples are a 2^-31 step off. */
static float sine (float x, uint32_t quarter) {
  uint32_t u = bits(mi_mul(x, flt(0x3e22f983))), p, i, f;
  uint64_t m = (u & 0x7fffff) | 0x800000;
  int32_t e = (int32_t)((u >> 23) & 0xff) - 127 - 3;  /* |u| * 2^20 = m * 2^e */
  int64_t lo, hi, v;
  if (!(u & 0x7f800000) || e >= 20) p = 0;
  else p = (uint32_t)((e >= 0 ? m << e : e > -32 ? m >> -e : 0) & 0xfffff);
  if (u >> 31) p = (0x100000 - p) & 0xfffff;
  p = (p + quarter) & 0xfffff;
  i = (p >> 8) & 0x7ff;
  f = p & 0xff;
  lo = sine_at(i);
  hi = sine_at(i + 1);
  v = lo + (hi - lo) * (int64_t)f / 255;
  return q31(p >> 19 ? -v : v);
}

float mi_sinf (float x) { return sine(x, 0); }
float mi_cosf (float x) { return sine(x, 0x40000); }

/* The game's tan(0.5) is its sine over its cosine. */
float mi_tanf (float x) { return mi_div(mi_sinf(x), mi_cosf(x)); }

/*
** The rest is not pinned down by the measurements (Close), but for what each says.
*/

static double exp_d (double x) {  /* x of 0 and up */
  double r, s = 1, t = 1;
  int32_t k, i;
  if (!(x <= 91)) return 0x1p200;  /* and nan */
  k = (int32_t)(x * 1.44269504088896338700e+00 + 0.5);
  r = (x - k * LN2_HI) - k * LN2_LO;
  for (i = 1; i <= 17; i++) { t *= r / i; s += t; }
  return scale2(s, k);
}

/* log(x) = e ln 2 + log(m), for x > 0 */
static double log_d (double x) {
  uint64_t u;
  double m, s, z, t, sum = 0;
  int32_t e, i;
  memcpy(&u, &x, sizeof u);
  e = (int32_t)((u >> 52) & 0x7ff) - 1023;
  u = (u & 0x000fffffffffffffull) | 0x3ff0000000000000ull;
  memcpy(&m, &u, sizeof m);
  if (m > 1.41421356237309514547) { m *= 0.5; e++; }
  s = (m - 1) / (m + 1);
  z = s * s;
  t = s;
  for (i = 1; i <= 41; i += 2) { sum += t / i; t *= z; }
  return e * LN2_HI + (2 * sum + e * LN2_LO);
}

/* exp(x) for x of 0 and up, past the float range packed as a product is (exp(89) is a nan, exp(100) is 0) */
static float exp_up (float x) {
  double d = exp_d(x) * (1.0 + 0x1p-45);
  uint64_t u;
  int32_t e;
  memcpy(&u, &d, sizeof u);
  e = (int32_t)((u >> 52) & 0x7ff) - 1023 + 127;
  if ((uint32_t)(e - 1) > 255) return 0.0f;
  return pack(0, e, (uint32_t)(u >> 29));
}

/* Below 0 the game's exp is 1 over exp(-x) in its division, so exp(-104) is 2^127, as 1/0 is. */
float mi_expf (float x) { return x < 0 ? mi_div(1.0f, exp_up(-x)) : exp_up(x); }

/* The logarithm reads a zero or subnormal number as 1.m * 2^-127, as division reads a zero divisor, so log(0)
   is -88.0296860; below 0 it is 0, as sqrt(-1) is. inf and nan go through the double's exponent (log(inf) is 709.78),
   not measured. */
static int bad_log (float x) { return (bits(x) >> 31) && (bits(x) & 0x7f800000); }
static double log_arg (float x) {
  uint32_t a = bits(x);
  return (a & 0x7f800000) ? x : ((a & 0x7fffff) | 0x800000) * 0x1p-150;
}

float mi_logf (float x) { return bad_log(x) ? 0.0f : to_float(log_d(log_arg(x))); }
float mi_log10f (float x) { return bad_log(x) ? 0.0f : to_float(log_d(log_arg(x)) * 4.34294481903251816668e-01); }

/* A whole power from 1 up is multiplied out in the game's arithmetic, squaring as it goes (1000^10 is 7149f2c9,
   one by one would give 7149f2c8), so (-8)^3 is -512; any other is exp(y log|x|), so (-8)^(1/3) is 1.99999964 and
   (-8)^-1 is 0.125. 0^0 is 1, and 0 to any other power 0. */
float mi_powf (float x, float y) {
  float r = 1.0f, b = x;
  uint32_t n;
  if (y == 0) return 1.0f;
  if (!(bits(x) & 0x7f800000)) return 0.0f;
  if (y >= 1 && y < 0x1p31f && mi_floorf(y) == y) {
    for (n = (uint32_t)y; n; n >>= 1) {
      if (n & 1) r = mi_mul(r, b);
      b = mi_mul(b, b);
    }
    return r;
  }
  return mi_expf(mi_mul(y, mi_logf(flt(bits(x) & 0x7fffffff))));
}

static double atan_d (double t) {  /* t >= 0 */
  int inv = t > 1, off;
  double z, p, s = 0;
  int32_t i;
  if (inv) t = 1 / t;
  off = t > 0.26794919243112270;
  if (off) t = (t * 1.73205080756887719318 - 1) / (t + 1.73205080756887719318);
  z = t * t;
  p = t;
  for (i = 1; i <= 33; i += 2) { s += (i & 2 ? -p : p) / i; p *= z; }
  if (off) s += PI / 6;
  return inv ? PIO2 - s : s;
}

static double atan2_d (double y, double x) {
  double ay = y < 0 ? -y : y, ax = x < 0 ? -x : x, a;
  if (ax == 0 && ay == 0) a = 0;
  else if (ax >= ay) a = atan_d(ay / ax);
  else a = PIO2 - atan_d(ax / ay);
  if (x < 0) a = PI - a;
  return y < 0 ? -a : a;
}

/* On an axis the game gives the floats nearest pi/2 and pi, and atan(0, 0) is pi/2. */
float mi_atan2f (float y, float x) {
  if (y == 0) return x < 0 ? flt(0x40490fdb) : x > 0 ? 0.0f : flt(0x3fc90fdb);
  if (x == 0) return flt(y < 0 ? 0xbfc90fdb : 0x3fc90fdb);
  return to_float(atan2_d(y, x));
}

/* From 1 and past it asin is the float nearest pi/2 (asin(2) too) and acos 0; from -1 down, -pi/2 and pi. */
float mi_asinf (float x) {
  if (x >= 1 || x <= -1) return flt(x < 0 ? 0xbfc90fdb : 0x3fc90fdb);
  return to_float(atan2_d(x, __builtin_sqrt((1.0 - x) * (1.0 + x))));
}

float mi_acosf (float x) {
  if (x >= 1 || x <= -1) return x < 0 ? flt(0x40490fdb) : 0.0f;
  return to_float(atan2_d(__builtin_sqrt((1.0 - x) * (1.0 + x)), x));
}

/* inf and nan are written here, as the C library writes them but the same on every platform: the sign, or + or
   a space for those flags, in capitals for %E %F %G %A, padded with spaces to the width */
int mi_snprintf (char *s, size_t sz, const char *f, double n) {
  uint64_t u;
  const char *flags = f + 1, *p = flags;
  char text[8];
  int width = 0, upper;
  memcpy(&u, &n, sizeof u);
  if (((u >> 52) & 0x7ff) != 0x7ff) return snprintf(s, sz, f, n);
  while (*p && strchr("-+ #0", *p)) p++;
  while (*p >= '0' && *p <= '9') width = width * 10 + (*p++ - '0');
  while (*p == '.' || (*p >= '0' && *p <= '9')) p++;
  upper = *p >= 'A' && *p <= 'Z';
  snprintf(text, sizeof text, "%s%s", (u >> 63) ? "-" : memchr(flags, '+', (size_t)(p - flags)) ? "+"
           : memchr(flags, ' ', (size_t)(p - flags)) ? " " : "",
           (u << 12) ? (upper ? "NAN" : "nan") : (upper ? "INF" : "inf"));
  return snprintf(s, sz, memchr(flags, '-', (size_t)(p - flags)) ? "%-*s" : "%*s", width, text);
}
