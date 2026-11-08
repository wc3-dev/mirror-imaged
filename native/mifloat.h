/*
** The game's float arithmetic and maths in place of the CPU's and the C library's, included at the end of
** luaconf.h. The models and the measurements behind them: docs/decisions.md, "The game's Lua numbers".
*/
#ifndef mifloat_h
#define mifloat_h

#include <math.h>
#include <stdio.h>
#include <stdlib.h>

LUA_API float mi_add (float a, float b);
LUA_API float mi_sub (float a, float b);
LUA_API float mi_mul (float a, float b);
LUA_API float mi_div (float a, float b);
LUA_API float mi_floorf (float x);
LUA_API float mi_ceilf (float x);
LUA_API float mi_fmodf (float a, float b);
LUA_API float mi_sqrtf (float x);
LUA_API float mi_sinf (float x);
LUA_API float mi_cosf (float x);
LUA_API float mi_tanf (float x);
LUA_API float mi_asinf (float x);
LUA_API float mi_acosf (float x);
LUA_API float mi_atan2f (float y, float x);
LUA_API float mi_expf (float x);
LUA_API float mi_logf (float x);
LUA_API float mi_log10f (float x);
LUA_API float mi_powf (float x, float y);
LUA_API int mi_snprintf (char *s, size_t sz, const char *f, double n);

#define luai_numadd(L,a,b)	mi_add(a,b)
#define luai_numsub(L,a,b)	mi_sub(a,b)
#define luai_nummul(L,a,b)	mi_mul(a,b)
#define luai_numdiv(L,a,b)	mi_div(a,b)
#define luai_numunm(L,a)	(-(a))
#define luai_numeq(a,b)		((a)==(b))
#define luai_numlt(a,b)		((a)<(b))
#define luai_numle(a,b)		((a)<=(b))
#define luai_numisnan(a)	(!luai_numeq((a), (a)))
#define luai_nummod(L,a,b,m)  \
  { (m) = mi_fmodf(a,b); if (mi_mul(m,b) < 0) (m) = mi_add(m,b); }

/* every library function the game measures; fabs and frexp stay the C library's, being exact. luaconf.h
   falls back to the double functions where math.h has no HUGE_VALF (MinGW): the float ones are wanted. */
#undef l_mathop
#define l_mathop(op)	op##f
#undef floorf
#define floorf mi_floorf
#undef ceilf
#define ceilf mi_ceilf
#undef fmodf
#define fmodf mi_fmodf
#undef sqrtf
#define sqrtf mi_sqrtf
#undef sinf
#define sinf mi_sinf
#undef cosf
#define cosf mi_cosf
#undef tanf
#define tanf mi_tanf
#undef asinf
#define asinf mi_asinf
#undef acosf
#define acosf mi_acosf
#undef atan2f
#define atan2f mi_atan2f
#undef expf
#define expf mi_expf
#undef logf
#define logf mi_logf
#undef log10f
#define log10f mi_log10f
#undef powf
#define powf mi_powf

/* strtof rounds differently between C libraries; strtod rounds correctly on all of them */
#undef lua_str2number
#define lua_str2number(s,p)	((lua_Number)strtod((s), (p)))

/* floats are printed by mi_snprintf, which writes inf and nan the same on every platform */
#undef l_sprintf
#define l_sprintf(s,sz,f,i)	_Generic((i), double: mi_snprintf, default: snprintf)(s,sz,f,i)

#endif
