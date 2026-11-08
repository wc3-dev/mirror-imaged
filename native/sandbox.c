/*
** A Lua state with the game's libraries, and what the C# side needs to call into it and be called from it.
*/
#define LUA_LIB

#include "lua.h"
#include "lauxlib.h"
#include "lualib.h"

static const luaL_Reg libs[] = {
  {"_G", luaopen_base},
  {LUA_COLIBNAME, luaopen_coroutine},
  {LUA_TABLIBNAME, luaopen_table},
  {LUA_STRLIBNAME, luaopen_string},
  {LUA_MATHLIBNAME, luaopen_math},
  {LUA_UTF8LIBNAME, luaopen_utf8},
  {LUA_OSLIBNAME, luaopen_os},
  {NULL, NULL}
};

static void drop (lua_State *L, const char *name) {
  lua_pushnil(L);
  lua_setfield(L, -2, name);
}

/* No io, debug or package; no dofile, loadfile or collectgarbage; os with only clock, date, difftime and
   time. math.random and math.randomseed are the game's generator, which the world provides. */
LUALIB_API lua_State *mirror_newstate (void) {
  static const char *const os_kept[] = {"clock", "date", "difftime", "time", NULL};
  const luaL_Reg *lib;
  const char *const *name;
  lua_State *L = luaL_newstate();
  if (L == NULL) return NULL;
  for (lib = libs; lib->func; lib++) {
    luaL_requiref(L, lib->name, lib->func, 1);
    lua_pop(L, 1);
  }
  lua_pushglobaltable(L);
  drop(L, "dofile");
  drop(L, "loadfile");
  drop(L, "collectgarbage");
  lua_getfield(L, -1, LUA_MATHLIBNAME);
  drop(L, "random");
  drop(L, "randomseed");
  lua_pop(L, 1);
  lua_getfield(L, -1, LUA_OSLIBNAME);
  lua_createtable(L, 0, 4);
  for (name = os_kept; *name; name++) {
    lua_getfield(L, -2, *name);
    lua_setfield(L, -2, *name);
  }
  lua_setfield(L, -3, LUA_OSLIBNAME);
  lua_pop(L, 2);
  return L;
}

static int (*callback) (lua_State *L);

LUALIB_API void mirror_setcallback (int (*f) (lua_State *L)) {
  callback = f;
}

/* Calls back into C#. A negative answer means the C# side failed and pushed its message: the error is
   raised here, so Lua's longjmp never crosses a managed frame. */
static int trampoline (lua_State *L) {
  int n = callback(L);
  if (n >= 0) return n;
  luaL_where(L, 1);
  lua_insert(L, -2);
  lua_concat(L, 2);
  return lua_error(L);
}

LUALIB_API void mirror_pushfunction (lua_State *L, int nup) {
  lua_pushcclosure(L, trampoline, nup);
}

static int traceback (lua_State *L) {
  const char *msg = lua_tostring(L, 1);
  if (msg == NULL) msg = lua_pushfstring(L, "(error object is a %s value)", luaL_typename(L, 1));
  luaL_traceback(L, L, msg, 1);
  return 1;
}

/* lua_pcall with a traceback added to the error message */
LUALIB_API int mirror_pcall (lua_State *L, int nargs, int nresults) {
  int base = lua_gettop(L) - nargs, status;
  lua_pushcfunction(L, traceback);
  lua_insert(L, base);
  status = lua_pcall(L, nargs, nresults, base);
  lua_remove(L, base);
  return status;
}
