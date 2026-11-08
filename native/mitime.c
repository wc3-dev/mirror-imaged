/*
** The simulated clock behind os.clock, os.time and os.date (native/mitime.h). Every run starts at EPOCH, and local
** time is UTC.
*/
#define LUA_LIB

#include <stdio.h>

#include "lua.h"
#include "lstate.h"
#include "mitime.h"

/* 2026-01-01 00:00:00 UTC */
#define EPOCH 1767225600

static const char *const weekdays[] = {"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};
static const char *const months[] = {"January", "February", "March", "April", "May", "June", "July", "August",
                                     "September", "October", "November", "December"};

/* in the main thread's extra space, so every coroutine reads the one C# sets */
LUALIB_API long long *mirror_clockms (lua_State *L) {
  return (long long *)lua_getextraspace(G(L)->mainthread);
}

/* divided in the game's float arithmetic (decisions D44) */
lua_Number mirror_clock (lua_State *L) {
  return mi_div((lua_Number)*mirror_clockms(L), 1000);
}

time_t mirror_time (lua_State *L) {
  return (time_t)(EPOCH + *mirror_clockms(L) / 1000);
}

/* days from 1970-01-01 to y-m-d (Howard Hinnant's days_from_civil) */
static long long daysfrom (long long y, long long m, long long d) {
  long long era, yoe;
  y -= m <= 2;
  era = (y >= 0 ? y : y - 399) / 400;
  yoe = y - era * 400;
  return era * 146097 + yoe * 365 + yoe / 4 - yoe / 100 + (153 * (m > 2 ? m - 3 : m + 9) + 2) / 5 + d - 1 - 719468;
}

/* (Howard Hinnant's civil_from_days) NULL before 1970, as Windows's C library gives */
struct tm *mirror_gmtime (time_t t, struct tm *r) {
  long long days = (long long)t / 86400, secs = (long long)t % 86400, era, doe, yoe, doy, mp, y, m;
  if (t < 0) return NULL;
  r->tm_hour = (int)(secs / 3600);
  r->tm_min = (int)(secs / 60 % 60);
  r->tm_sec = (int)(secs % 60);
  r->tm_wday = (int)((days + 4) % 7);  /* 1970-01-01 was a Thursday */
  days += 719468;
  era = (days >= 0 ? days : days - 146096) / 146097;
  doe = days - era * 146097;
  yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
  doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
  mp = (5 * doy + 2) / 153;
  m = mp < 10 ? mp + 3 : mp - 9;
  y = yoe + era * 400 + (m <= 2);
  r->tm_year = (int)(y - 1900);
  r->tm_mon = (int)(m - 1);
  r->tm_mday = (int)(doy - (153 * mp + 2) / 5 + 1);
  r->tm_yday = (int)(daysfrom(y, m, r->tm_mday) - daysfrom(y, 1, 1));
  r->tm_isdst = 0;
  return r;
}

/* as mktime in UTC: fields out of range carry over, and come back put right; -1 before 1970, as on Windows */
time_t mirror_mktime (struct tm *ts) {
  long long y = ts->tm_year + 1900LL + ts->tm_mon / 12, m = ts->tm_mon % 12, t;
  if (m < 0) {
    m += 12;
    y--;
  }
  t = (daysfrom(y, m + 1, 1) + ts->tm_mday - 1) * 86400 + ts->tm_hour * 3600LL + ts->tm_min * 60LL + ts->tm_sec;
  if (t < 0) return (time_t)-1;
  mirror_gmtime((time_t)t, ts);
  return (time_t)t;
}

/* One conversion ("%x" or "%#x") as Windows's C library writes it in the "C" locale: '#' takes a number's leading
   zeros off and makes %c and %x long. */
size_t mirror_strftime (char *s, size_t max, const char *conversion, const struct tm *tm) {
  int alt = conversion[1] == '#', width = 2, n;
  const char *day = weekdays[tm->tm_wday], *month = months[tm->tm_mon];
  switch (conversion[1 + alt]) {
    case 'a': return (size_t)snprintf(s, max, "%.3s", day);
    case 'A': return (size_t)snprintf(s, max, "%s", day);
    case 'b': return (size_t)snprintf(s, max, "%.3s", month);
    case 'B': return (size_t)snprintf(s, max, "%s", month);
    case 'c':
      if (alt)
        return (size_t)snprintf(s, max, "%s, %s %02d, %d, %02d:%02d:%02d", day, month, tm->tm_mday,
                                tm->tm_year + 1900, tm->tm_hour, tm->tm_min, tm->tm_sec);
      return (size_t)snprintf(s, max, "%02d/%02d/%02d %02d:%02d:%02d", tm->tm_mon + 1, tm->tm_mday,
                              tm->tm_year % 100, tm->tm_hour, tm->tm_min, tm->tm_sec);
    case 'x':
      if (alt)
        return (size_t)snprintf(s, max, "%s, %s %02d, %d", day, month, tm->tm_mday, tm->tm_year + 1900);
      return (size_t)snprintf(s, max, "%02d/%02d/%02d", tm->tm_mon + 1, tm->tm_mday, tm->tm_year % 100);
    case 'X': return (size_t)snprintf(s, max, "%02d:%02d:%02d", tm->tm_hour, tm->tm_min, tm->tm_sec);
    case 'p': return (size_t)snprintf(s, max, "%s", tm->tm_hour < 12 ? "AM" : "PM");
    case 'z': return (size_t)snprintf(s, max, "+0000");
    case 'Z': return (size_t)snprintf(s, max, "UTC");
    case '%': return (size_t)snprintf(s, max, "%%");
    case 'd': n = tm->tm_mday; break;
    case 'H': n = tm->tm_hour; break;
    case 'I': n = (tm->tm_hour + 11) % 12 + 1; break;
    case 'j': n = tm->tm_yday + 1; width = 3; break;
    case 'm': n = tm->tm_mon + 1; break;
    case 'M': n = tm->tm_min; break;
    case 'S': n = tm->tm_sec; break;
    case 'U': n = (tm->tm_yday + 7 - tm->tm_wday) / 7; break;
    case 'w': n = tm->tm_wday; width = 1; break;
    case 'W': n = (tm->tm_yday + 7 - (tm->tm_wday + 6) % 7) / 7; break;
    case 'y': n = tm->tm_year % 100; break;
    default: n = tm->tm_year + 1900; width = 1; break;  /* 'Y', the last option loslib.c lets through */
  }
  return (size_t)snprintf(s, max, "%0*d", alt ? 1 : width, n);
}
