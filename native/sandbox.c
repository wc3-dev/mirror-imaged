/*
** A Lua state with the game's libraries, and what the C# side needs to call into it and be called from it.
** Everything that can raise a Lua error (anything that allocates can, through a failing __gc) runs here in C
** under lua_pcall, so Lua's longjmp never crosses a managed frame.
*/
#define LUA_LIB

#include "lua.h"
#include "lauxlib.h"
#include "lualib.h"

void *mirror_arena_new (void);
void mirror_arena_free (void *arena);
void *mirror_arena_alloc (void *ud, void *ptr, size_t osize, size_t nsize);

/* A value crossing to or from C#; strings are not owned */
typedef struct Value {
  int type;  /* VNIL, VBOOLEAN, VINTEGER, VFLOAT or VSTRING */
  int integer;
  float number;
  const char *string;
  size_t length;
} Value;

enum { VNIL, VBOOLEAN, VINTEGER, VFLOAT, VSTRING };

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
static int openlibs (lua_State *L) {
  static const char *const os_kept[] = {"clock", "date", "difftime", "time", NULL};
  const luaL_Reg *lib;
  const char *const *name;
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
  return 0;
}

LUALIB_API lua_State *mirror_newstate (void) {
  void *arena = mirror_arena_new();
  lua_State *L = arena != NULL ? lua_newstate(mirror_arena_alloc, arena) : NULL;
  if (L == NULL) {
    if (arena != NULL) mirror_arena_free(arena);
    return NULL;
  }
  lua_pushcfunction(L, openlibs);
  if (lua_pcall(L, 0, 0, 0) != LUA_OK) {
    lua_close(L);
    mirror_arena_free(arena);
    return NULL;
  }
  return L;
}

LUALIB_API void mirror_close (lua_State *L) {
  void *arena;
  lua_getallocf(L, &arena);
  lua_close(L);
  mirror_arena_free(arena);
}

static void pushvalues (lua_State *L, const Value *v, int n) {
  luaL_checkstack(L, n, "too many values");
  for (; n > 0; v++, n--) {
    switch (v->type) {
      case VBOOLEAN: lua_pushboolean(L, v->integer); break;
      case VINTEGER: lua_pushinteger(L, v->integer); break;
      case VFLOAT: lua_pushnumber(L, v->number); break;
      case VSTRING: lua_pushlstring(L, v->string, v->length); break;
      default: lua_pushnil(L); break;
    }
  }
}

static int pushall (lua_State *L) {
  const Value *v = (const Value *)lua_touserdata(L, 1);
  int n = (int)lua_tointeger(L, 2);
  lua_settop(L, 0);
  pushvalues(L, v, n);
  return n;
}

/* Pushes values C# wrote, with the collector stopped: a __gc calling back into C# would write over them midway */
static void pushcsharp (lua_State *L, const Value *v, int n) {
  int running = lua_gc(L, LUA_GCISRUNNING, 0), status;
  lua_gc(L, LUA_GCSTOP, 0);
  lua_pushcfunction(L, pushall);
  lua_pushlightuserdata(L, (void *)v);
  lua_pushinteger(L, n);
  status = lua_pcall(L, 2, LUA_MULTRET, 0);
  if (running) lua_gc(L, LUA_GCRESTART, 0);
  if (status != LUA_OK) lua_error(L);
}

static int (*callback) (lua_State *L, const Value **results);

LUALIB_API void mirror_setcallback (int (*f) (lua_State *L, const Value **results)) {
  callback = f;
}

/* Calls back into C#, which only reads the arguments and returns its results (or its error message, as a
   negative count) for this to push, before anything else can call C#. */
static int trampoline (lua_State *L) {
  const Value *results;
  int n = callback(L, &results);
  if (n >= 0) {
    pushcsharp(L, results, n);
    return n;
  }
  pushcsharp(L, results, 1);
  luaL_where(L, 1);
  lua_insert(L, -2);
  lua_concat(L, 2);
  return lua_error(L);
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

typedef struct Request {
  const char *name;
  const Value *values;
  int n;
  void *state;
} Request;

static int docall (lua_State *L) {
  const Request *r = (const Request *)lua_touserdata(L, 1);
  lua_settop(L, 0);
  pushcsharp(L, r->values, r->n);  /* before anything that can run Lua, and so C# */
  lua_getglobal(L, r->name);
  lua_insert(L, 1);
  lua_call(L, r->n, LUA_MULTRET);
  return lua_gettop(L);
}

static int doget (lua_State *L) {
  lua_getglobal(L, ((const Request *)lua_touserdata(L, 1))->name);
  return 1;
}

static int doset (lua_State *L) {
  const Request *r = (const Request *)lua_touserdata(L, 1);
  pushcsharp(L, r->values, 1);
  lua_setglobal(L, r->name);
  return 0;
}

static int doregister (lua_State *L) {
  const Request *r = (const Request *)lua_touserdata(L, 1);
  lua_pushlightuserdata(L, r->state);
  lua_pushinteger(L, r->n);
  lua_pushcclosure(L, trampoline, 2);
  lua_setglobal(L, r->name);
  return 0;
}

static int protect (lua_State *L, lua_CFunction f, const Request *r, int nresults) {
  lua_pushlightuserdata(L, (void *)r);
  lua_pushcfunction(L, f);
  lua_insert(L, -2);
  return mirror_pcall(L, 1, nresults);
}

/* name(args...), leaving its results */
LUALIB_API int mirror_call (lua_State *L, const char *name, const Value *args, int n) {
  Request r = {name, args, n, NULL};
  return protect(L, docall, &r, LUA_MULTRET);
}

/* leaves the global */
LUALIB_API int mirror_getglobal (lua_State *L, const char *name) {
  Request r = {name, NULL, 0, NULL};
  return protect(L, doget, &r, 1);
}

LUALIB_API int mirror_setglobal (lua_State *L, const char *name, const Value *value) {
  Request r = {name, value, 1, NULL};
  return protect(L, doset, &r, 0);
}

/* the global name calls C# function id of state */
LUALIB_API int mirror_register (lua_State *L, const char *name, void *state, int id) {
  Request r = {name, NULL, id, state};
  return protect(L, doregister, &r, 0);
}
