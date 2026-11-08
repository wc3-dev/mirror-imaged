/*
** A Lua state with the game's libraries, and what the C# side needs to call into it and be called from it.
** Everything that can raise a Lua error (anything that allocates can, through a failing __gc) runs here in C
** under lua_pcall, so Lua's longjmp never crosses a managed frame.
*/
#define LUA_LIB

#include "lua.h"
#include "lauxlib.h"
#include "lualib.h"
#include "lgc.h"
#include "lstate.h"
#include "mitime.h"

void *mirror_arena_new (void);
void mirror_arena_free (void *arena);
void *mirror_arena_alloc (void *ud, void *ptr, size_t osize, size_t nsize);
size_t mirror_arena_committed (void *arena);

/* A value crossing to or from C#; strings are not owned. A handle is its id and its declared type's name. */
typedef struct ManagedValue {
  int type;  /* VNIL, VBOOLEAN, VINTEGER, VFLOAT, VSTRING or VHANDLE */
  int integer;
  float number;
  const char *string;
  size_t length;
} ManagedValue;

enum { VNIL, VBOOLEAN, VINTEGER, VFLOAT, VSTRING, VHANDLE };

/* A handle's userdata: its id, and its declared type's name, which the type's metatable keeps alive */
typedef struct Handle {
  int id;
  const char *type;
} Handle;

#define HANDLES "mirror.handles"
#define HALT "mirror.halt"

/* what C#'s callback returns for an error that halts the run */
#define HALTS (-2)

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
  *mirror_clockms(L) = 0;
  lua_pushcfunction(L, openlibs);
  if (lua_pcall(L, 0, 0, 0) != LUA_OK) {
    lua_close(L);
    mirror_arena_free(arena);
    return NULL;
  }
  return L;
}

/* the memory the state's arena has committed: its most at any time, since the arena gives none back */
LUALIB_API size_t mirror_committed (lua_State *L) {
  void *arena;
  lua_getallocf(L, &arena);
  return mirror_arena_committed(arena);
}

LUALIB_API void mirror_close (lua_State *L) {
  void *arena;
  lua_getallocf(L, &arena);
  lua_close(L);
  mirror_arena_free(arena);
}

/* Pushes the userdata standing for handle id as the declared type named type: the same one while Lua holds it, with
   a metatable named after the type, so tostring writes "timer: <address>" as the game's does. */
static void pushhandle (lua_State *L, int id, const char *type, size_t length) {
  Handle *h;
  lua_pushlstring(L, type, length);
  luaL_getsubtable(L, LUA_REGISTRYINDEX, HANDLES);  /* type, handles */
  lua_pushvalue(L, -2);
  if (lua_rawget(L, -2) != LUA_TTABLE) {  /* the type's userdata by id, held weakly */
    lua_pop(L, 1);
    lua_newtable(L);
    lua_createtable(L, 0, 1);
    lua_pushliteral(L, "v");
    lua_setfield(L, -2, "__mode");
    lua_setmetatable(L, -2);
    lua_pushvalue(L, -3);
    lua_pushvalue(L, -2);
    lua_rawset(L, -4);
  }  /* type, handles, cache */
  if (lua_rawgeti(L, -1, id) == LUA_TNIL) {
    lua_pop(L, 1);
    h = (Handle *)lua_newuserdata(L, sizeof(Handle));
    h->id = id;
    h->type = lua_tostring(L, -4);  /* a short string, kept by the metatable's __name and the registry */
    luaL_newmetatable(L, h->type);
    lua_setmetatable(L, -2);
    lua_pushvalue(L, -1);
    lua_rawseti(L, -3, id);
  }
  lua_replace(L, -4);
  lua_pop(L, 2);
}

static void pushvalues (lua_State *L, const ManagedValue *v, int n) {
  luaL_checkstack(L, n, "too many values");
  for (; n > 0; v++, n--) {
    switch (v->type) {
      case VBOOLEAN: lua_pushboolean(L, v->integer); break;
      case VINTEGER: lua_pushinteger(L, v->integer); break;
      case VFLOAT: lua_pushnumber(L, v->number); break;
      case VSTRING: lua_pushlstring(L, v->string, v->length); break;
      case VHANDLE: pushhandle(L, v->integer, v->string, v->length); break;
      default: lua_pushnil(L); break;
    }
  }
}

static int pushall (lua_State *L) {
  const ManagedValue *v = (const ManagedValue *)lua_touserdata(L, 1);
  int n = (int)lua_tointeger(L, 2);
  lua_settop(L, 0);
  pushvalues(L, v, n);
  return n;
}

/* Pushes values C# wrote with the collector held, since a __gc calling back into C# would write over them midway.
   What the push allocated is then owed to the collector, which takes its step once the values are in, so it keeps
   its pace; lua_gc's stop and restart would forget the debt. */
static void pushcsharp (lua_State *L, const ManagedValue *v, int n) {
  global_State *g = G(L);
  lu_byte running = g->gcrunning;
  l_mem debt = g->GCdebt;
  lu_mem total = gettotalbytes(g);
  int status;
  g->gcrunning = 0;
  lua_pushcfunction(L, pushall);
  lua_pushlightuserdata(L, (void *)v);
  lua_pushinteger(L, n);
  status = lua_pcall(L, 2, LUA_MULTRET, 0);
  g->gcrunning = running;
  luaE_setdebt(g, debt + (l_mem)(gettotalbytes(g) - total));
  if (status != LUA_OK) lua_error(L);
  luaC_checkGC(L);
}

static int (*callback) (lua_State *L, const ManagedValue **results);

LUALIB_API void mirror_setcallback (int (*f) (lua_State *L, const ManagedValue **results)) {
  callback = f;
}

static void rehalt (lua_State *L, lua_Debug *ar) {
  (void)ar;
  L->allowhook = 1;  /* the error leaves the hook without turning hooks back on, and an xpcall handler runs next */
  lua_getfield(L, LUA_REGISTRYINDEX, HALT);
  lua_error(L);
}

/* Raises the error on top, after which every Lua instruction in every thread raises the run's first such error
   again, so no pcall, xpcall, coroutine or load reader can carry on past it. */
static int halt (lua_State *L) {
  GCObject *o;
  if (lua_getfield(L, LUA_REGISTRYINDEX, HALT) == LUA_TNIL) {
    lua_pop(L, 1);
    lua_pushvalue(L, -1);
    lua_setfield(L, LUA_REGISTRYINDEX, HALT);
    lua_sethook(G(L)->mainthread, rehalt, LUA_MASKCOUNT, 1);
    for (o = G(L)->allgc; o != NULL; o = o->next)  /* a new thread takes its creator's hook */
      if (o->tt == LUA_TTHREAD) lua_sethook(gco2th(o), rehalt, LUA_MASKCOUNT, 1);
  }
  return lua_error(L);
}

/* The position of the nearest Lua function calling in, in this thread or else the main thread, so a halt called
   straight from pcall or a coroutine still names its line */
static void pushwhere (lua_State *L) {
  lua_State *threads[2];
  lua_Debug ar;
  int t, level;
  threads[0] = L;
  threads[1] = G(L)->mainthread;
  for (t = 0; t < 2; t++) {
    for (level = t == 0; lua_getstack(threads[t], level, &ar); level++) {
      lua_getinfo(threads[t], "Sl", &ar);
      if (ar.currentline > 0) {
        lua_pushfstring(L, "%s:%d: ", ar.short_src, ar.currentline);
        return;
      }
    }
  }
  lua_pushliteral(L, "");
}

/* Calls back into C#, which only reads the arguments and returns its results (or its error message, as a
   negative count) for this to push, before anything else can call C#. A function argument reaches C# as a
   reference in the registry, a light userdata holding its number, which C# owns. */
static int trampoline (lua_State *L) {
  const ManagedValue *results;
  int n = lua_gettop(L), i;
  for (i = 1; i <= n; i++) {
    if (lua_type(L, i) == LUA_TFUNCTION) {
      lua_pushvalue(L, i);
      lua_pushlightuserdata(L, (void *)(ptrdiff_t)luaL_ref(L, LUA_REGISTRYINDEX));
      lua_replace(L, i);
    }
  }
  n = callback(L, &results);
  if (n >= 0) {
    pushcsharp(L, results, n);
    return n;
  }
  pushcsharp(L, results, 1);
  if (n == HALTS) pushwhere(L);
  else luaL_where(L, 1);
  lua_insert(L, -2);
  lua_concat(L, 2);
  return n == HALTS ? halt(L) : lua_error(L);
}

static int traceback (lua_State *L) {
  const char *msg;
  if (lua_getfield(L, LUA_REGISTRYINDEX, HALT) == LUA_TNIL) lua_pop(L, 1);
  else lua_replace(L, 1);  /* a halted run reports its first error, which coroutine.wrap may have prefixed again */
  msg = lua_tostring(L, 1);
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
  const ManagedValue *values;
  int n;
  void *state;
  int ref;
} Request;

/* calls the global name, or the function ref when there is no name */
static int docall (lua_State *L) {
  const Request *r = (const Request *)lua_touserdata(L, 1);
  lua_settop(L, 0);
  pushcsharp(L, r->values, r->n);  /* before anything that can run Lua, and so C# */
  if (r->name == NULL) lua_rawgeti(L, LUA_REGISTRYINDEX, r->ref);
  else if (lua_getglobal(L, r->name) == LUA_TNIL)
    return luaL_error(L, "attempt to call a nil value (global '%s')", r->name);
  lua_insert(L, 1);
  lua_call(L, r->n, LUA_MULTRET);
  return lua_gettop(L);
}

static int dounref (lua_State *L) {
  luaL_unref(L, LUA_REGISTRYINDEX, ((const Request *)lua_touserdata(L, 1))->ref);
  return 0;
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
LUALIB_API int mirror_call (lua_State *L, const char *name, const ManagedValue *args, int n) {
  Request r = {name, args, n, NULL, LUA_NOREF};
  return protect(L, docall, &r, LUA_MULTRET);
}

/* the function a C# function was given as ref, called with args, leaving its results */
LUALIB_API int mirror_callref (lua_State *L, int ref, const ManagedValue *args, int n) {
  Request r = {NULL, args, n, NULL, ref};
  return protect(L, docall, &r, LUA_MULTRET);
}

/* lets go of a function C# was given */
LUALIB_API int mirror_unref (lua_State *L, int ref) {
  Request r = {NULL, NULL, 0, NULL, ref};
  return protect(L, dounref, &r, 0);
}

/* leaves the global */
LUALIB_API int mirror_getglobal (lua_State *L, const char *name) {
  Request r = {name, NULL, 0, NULL, LUA_NOREF};
  return protect(L, doget, &r, 1);
}

LUALIB_API int mirror_setglobal (lua_State *L, const char *name, const ManagedValue *value) {
  Request r = {name, value, 1, NULL, LUA_NOREF};
  return protect(L, doset, &r, 0);
}

/* the global name calls C# function id of state */
LUALIB_API int mirror_register (lua_State *L, const char *name, void *state, int id) {
  Request r = {name, NULL, id, state, LUA_NOREF};
  return protect(L, doregister, &r, 0);
}
