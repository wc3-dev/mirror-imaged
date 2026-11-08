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

/* A biased exponent outside 1..256 gives +0. 255 and 256 are packed as they are: the exponent carries into
   the sign bit, as the game's does (1e38 * 10 is -5.5e-39, MaxValue * 2 is a nan). */
static float pack (uint32_t sign, int32_t e, uint32_t m) {
  if ((uint32_t)(e - 1) > 255) return 0.0f;
  return flt((sign << 31) + ((uint32_t)e << 23) + (m & 0x7fffff));
}

/* Aligned with one guard bit, so it rounds toward zero (1 - (1 - 2^-24) is exact); a number 23 or more
   octaves smaller is dropped. */
float mi_add (float x, float y) {
  uint32_t a = bits(x), b = bits(y), ma, mb, m;
  int32_t ea, eb;
  if ((a & 0x7fffffff) < (b & 0x7fffffff)) { uint32_t t = a; a = b; b = t; }
  ea = (a >> 23) & 0xff;
  eb = (b >> 23) & 0xff;
  if (eb == 0 || ea - eb >= 23) return flt(a);
  ma = ((a & 0x7fffff) | 0x800000) << 1;
  mb = (((b & 0x7fffff) | 0x800000) << 1) >> (ea - eb);
  if ((a ^ b) >> 31) {
    m = ma - mb;
    if (m == 0) return 0.0f;
    while (m < 0x1000000) { m <<= 1; ea--; }
  }
  else {
    m = ma + mb;
    if (m >> 25) { m >>= 1; ea++; }
  }
  return pack(a >> 31, ea, m >> 1);
}

float mi_sub (float a, float b) {
  return mi_add(a, flt(bits(b) ^ 0x80000000));
}

float mi_mul (float x, float y) {
  uint32_t a = bits(x), b = bits(y);
  int32_t ea = (a >> 23) & 0xff, eb = (b >> 23) & 0xff, e = ea + eb - 127;
  uint64_t m;
  if (ea == 0 || eb == 0) return 0.0f;
  m = (uint64_t)((a & 0x7fffff) | 0x800000) * ((b & 0x7fffff) | 0x800000);
  if (m >> 47) { m >>= 24; e++; }
  else m >>= 23;
  return pack((a ^ b) >> 31, e, (uint32_t)m);
}

/* A reciprocal of the divisor without its lowest bit, a little high, times the dividend. Zero is read as
   1.0 * 2^-127, so 0/0 is 1, 1/0 is 2^127 and 100/0 is past the range, 0. */
float mi_div (float x, float y) {
  uint32_t a = bits(x), b = bits(y);
  int32_t e = (int32_t)((a >> 23) & 0xff) - (int32_t)((b >> 23) & 0xff) + 126;
  uint64_t r = ((1ull << 47) + 0xcc0000) / (((b & 0x7fffff) | 0x800000) & ~1u), m;
  if (r >> 24) { r >>= 1; e++; }
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

/* The game's error of the square root, the same every two octaves: by the mantissa m in [1, 4) times 2^23,
   in 2^-40, at each measured m. Between them it is interpolated (Close). */
static const struct { uint32_t m; int32_t e; } sqrt_errors[] = {
  {0x0800000, 0}, {0x0a20000, 8213845}, {0x0c80000, 14732492}, {0x0d1b717, 10178400}, {0x1000000, 27877},
  {0x1200000, 21189973}, {0x1388000, 20426260}, {0x147ae14, 3182720}, {0x1800000, 2288561},
  {0x1880000, 18312630}, {0x1e84800, 16542335}, {0x2000000, 0}
};

float mi_sqrtf (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  uint64_t m, r;
  int i = 1;
  if ((a >> 31) || e == -127) return 0.0f;
  m = (uint64_t)((a & 0x7fffff) | 0x800000) << (e & 1);
  r = isqrt(m << 39);  /* sqrt(m) * 2^31 */
  while (sqrt_errors[i].m <= m) i++;
  r += r * (uint64_t)(sqrt_errors[i - 1].e + (int64_t)(sqrt_errors[i].e - sqrt_errors[i - 1].e)
       * (int64_t)(m - sqrt_errors[i - 1].m) / (int64_t)(sqrt_errors[i].m - sqrt_errors[i - 1].m)) >> 40;
  return pack(0, (e >> 1) + 127, (uint32_t)(r >> 8));
}

/*
** The rest is not pinned down by the measurements (Close): computed in doubles and cut to a
** float toward zero, as the game's arithmetic does.
*/

static const double LN2_HI = 6.93147180369123816490e-01, LN2_LO = 1.90821492927058770002e-10;
static const double PI = 3.14159265358979311600e+00, PIO2 = 1.57079632679489655800e+00;

/* Toward zero, after a nudge of 2^-45 so that a result that should be exact (2^10, log2(8)) stays exact. A
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

/* The game's sine and cosine are 31-bit fractions: cos(0) is 1 - 2^-24. */
static float q31 (double v) {
  double t = v * 2147483648.0 * (1.0 + 0x1p-45);
  if (t > 2147483647.0) t = 2147483647.0;
  if (t < -2147483647.0) t = -2147483647.0;
  return to_float((double)(int32_t)t / 2147483648.0);
}

static double scale2 (double x, int32_t k) {  /* x * 2^k, k in -1022..1023 */
  uint64_t u = (uint64_t)(k + 1023) << 52;
  double p;
  memcpy(&p, &u, sizeof p);
  return x * p;
}

static void sincos_d (float x, double *s, double *c) {
  double k, r, z, ps, pc;
  int32_t n;
  if (((bits(x) >> 23) & 0xff) >= 127 + 31) { *s = 0; *c = 1; return; }  /* 2^31 and up, inf and nan */
  n = (int32_t)(x * (2 / PI) + (x < 0 ? -0.5 : 0.5));
  k = n;
  r = ((x - k * 1.57079632673412561417e+00) - k * 6.07710050650619224932e-11) - k * 2.02226624879595063154e-21;
  z = r * r;
  ps = r + r * z * (-1.0 / 6 + z * (1.0 / 120 + z * (-1.0 / 5040 + z * (1.0 / 362880 + z * (-1.0 / 39916800
       + z * (1.0 / 6227020800.0 + z * (-1.0 / 1307674368000.0 + z * (1.0 / 355687428096000.0))))))));
  pc = 1 + z * (-1.0 / 2 + z * (1.0 / 24 + z * (-1.0 / 720 + z * (1.0 / 40320 + z * (-1.0 / 3628800
       + z * (1.0 / 479001600 + z * (-1.0 / 87178291200.0 + z * (1.0 / 20922789888000.0
       + z * (-1.0 / 6402373705728000.0)))))))));
  switch (n & 3) {
    case 0: *s = ps; *c = pc; break;
    case 1: *s = pc; *c = -ps; break;
    case 2: *s = -ps; *c = -pc; break;
    default: *s = -pc; *c = ps; break;
  }
}

float mi_sinf (float x) { double s, c; sincos_d(x, &s, &c); return q31(s); }
float mi_cosf (float x) { double s, c; sincos_d(x, &s, &c); return q31(c); }

/* The game's tan(0.5) is its sine over its cosine. */
float mi_tanf (float x) { return mi_div(mi_sinf(x), mi_cosf(x)); }

static double exp_d (double x) {
  double r, s = 1, t = 1;
  int32_t k, i;
  if (!(x <= 89)) return 0x1p200;  /* and nan */
  if (x < -104) return 0;
  k = (int32_t)(x * 1.44269504088896338700e+00 + (x < 0 ? -0.5 : 0.5));
  r = (x - k * LN2_HI) - k * LN2_LO;
  for (i = 1; i <= 17; i++) { t *= r / i; s += t; }
  return scale2(s, k);
}

/* log(x) = e ln 2 + log(m) for x > 0; returns log(m) and sets e */
static double logm_d (double x, int32_t *e) {
  uint64_t u;
  double m, s, z, t, sum = 0;
  int32_t i;
  memcpy(&u, &x, sizeof u);
  *e = (int32_t)((u >> 52) & 0x7ff) - 1023;
  u = (u & 0x000fffffffffffffull) | 0x3ff0000000000000ull;
  memcpy(&m, &u, sizeof m);
  if (m > 1.41421356237309514547) { m *= 0.5; (*e)++; }
  s = (m - 1) / (m + 1);
  z = s * s;
  t = s;
  for (i = 1; i <= 41; i += 2) { sum += t / i; t *= z; }
  return 2 * sum;
}

static double log_d (double x) {
  int32_t e;
  double lm = logm_d(x, &e);
  return e * LN2_HI + (lm + e * LN2_LO);
}

/* sqrt, log and asin outside their domains give 0, as sqrt(-1) does in the game */
static int bad_log (float x) { return (bits(x) >> 31) || !(bits(x) & 0x7f800000); }

float mi_expf (float x) { return to_float(exp_d(x)); }
float mi_logf (float x) { return bad_log(x) ? (x == 0 ? -HUGE_VALF : 0.0f) : to_float(log_d(x)); }
float mi_log10f (float x) {
  return bad_log(x) ? (x == 0 ? -HUGE_VALF : 0.0f) : to_float(log_d(x) * 4.34294481903251816668e-01);
}
float mi_log2f (float x) {
  int32_t e;
  double lm;
  if (bad_log(x)) return x == 0 ? -HUGE_VALF : 0.0f;
  lm = logm_d(x, &e);
  return to_float(e + lm * 1.44269504088896338700e+00);
}

float mi_powf (float x, float y) {
  double b = x < 0 ? -(double)x : x, r = 1;
  int integral = mi_floorf(y) == y;
  if (y == 0) return 1.0f;
  if (!(bits(x) & 0x7f800000)) return y > 0 ? 0.0f : HUGE_VALF;
  if (x < 0 && !integral) return 0.0f;
  if (integral && y <= 0x1p24f && y >= -0x1p24f) {  /* exact as far as a double goes: 2^10 is 1024 */
    uint32_t n = (uint32_t)(y < 0 ? -y : y);
    while (n) { if (n & 1) r *= b; b *= b; n >>= 1; }
    if (y < 0) r = 1 / r;
  }
  else r = exp_d(y * log_d(b));
  if (x < 0 && integral && y <= 0x1p24f && y >= -0x1p24f && ((uint32_t)(y < 0 ? -y : y) & 1)) r = -r;
  return to_float(r);
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

float mi_atan2f (float y, float x) { return to_float(atan2_d(y, x)); }

float mi_asinf (float x) {
  if (x > 1 || x < -1) return 0.0f;
  return to_float(atan2_d(x, __builtin_sqrt((1.0 - x) * (1.0 + x))));
}

float mi_acosf (float x) {
  if (x > 1 || x < -1) return 0.0f;
  return to_float(atan2_d(__builtin_sqrt((1.0 - x) * (1.0 + x)), x));
}

int mi_snprintf (char *s, size_t sz, const char *f, double n) {
  uint64_t u;
  memcpy(&u, &n, sizeof u);
  if (((u >> 52) & 0x7ff) == 0x7ff)
    return snprintf(s, sz, "%s%s", (u >> 63) ? "-" : "", (u << 12) ? "nan" : "inf");
  return snprintf(s, sz, f, n);
}
