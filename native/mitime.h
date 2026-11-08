/*
** os.clock, os.time, os.date and os.difftime on the state's simulated clock, in UTC, with nothing from the C library
** that reads the machine's clock or time zone or differs between platforms. Included by loslib.c.
*/
#ifndef mitime_h
#define mitime_h

#include <time.h>

#include "lua.h"

#define l_gmtime(t,r)		mirror_gmtime(*(t),r)
#define l_localtime(t,r)	mirror_gmtime(*(t),r)

/* the state's clock in milliseconds since the run started, which C# sets */
LUALIB_API long long *mirror_clockms (lua_State *L);

LUAI_FUNC lua_Number mirror_clock (lua_State *L);
LUAI_FUNC time_t mirror_time (lua_State *L);
LUAI_FUNC struct tm *mirror_gmtime (time_t t, struct tm *r);
LUAI_FUNC time_t mirror_mktime (struct tm *ts);
LUAI_FUNC size_t mirror_strftime (char *s, size_t max, const char *conversion, const struct tm *tm);

#endif
