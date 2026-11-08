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
#include "lobject.h"
#include "lstate.h"
#include <string.h>
#include "mitime.h"

void *mirror_arena_new (void);
void mirror_arena_free (void *arena);
void *mirror_arena_alloc (void *ud, void *ptr, size_t osize, size_t nsize);
size_t mirror_arena_committed (void *arena);

/* A value crossing to or from C#; strings are not owned. A handle (VHANDLE, or VAGENT for an agent) is its key in
   integer, its game id in id and its declared type's name in string. */
typedef struct ManagedValue {
  int type;  /* VNIL, VBOOLEAN, VINTEGER, VFLOAT, VSTRING, VHANDLE or VAGENT */
  int integer;
  float number;
  const char *string;
  size_t length;
  int id;
} ManagedValue;

enum { VNIL, VBOOLEAN, VINTEGER, VFLOAT, VSTRING, VHANDLE, VAGENT };

/* A handle's userdata: its key, by which C# finds it, the id the game shows, whether its declared type is an agent,
   and that type's name, which the type's metatable keeps alive */
typedef struct Handle {
  int key;
  int id;
  int agent;
  const char *type;
} Handle;

#define HANDLES "mirror.handles"
#define HALT "mirror.halt"
#define THREADS "mirror.threads"

/* what C#'s callback returns for an error that halts the run */
#define HALTS (-2)

/* what C#'s callback returns to make the thread calling it wait (yield) */
#define YIELDS (-3)

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

/* whether L is a thread the natives started (mirror_start): the game runs its callbacks in threads the script can't
   yield, though a wait (a C# function's YIELDS) can */
static int ownthread (lua_State *L) {
  int own;
  lua_getfield(L, LUA_REGISTRYINDEX, THREADS);
  lua_pushthread(L);
  own = lua_rawget(L, -2) != LUA_TNIL;
  lua_pop(L, 2);
  return own;
}

static int coyield (lua_State *L) {
  if (ownthread(L)) {
    lua_pushliteral(L, "attempt to yield across a C-call boundary");
    return lua_error(L);
  }
  return lua_yield(L, lua_gettop(L));
}

static int coisyieldable (lua_State *L) {
  lua_pushboolean(L, lua_isyieldable(L) && !ownthread(L));
  return 1;
}

/* coroutine.resume, which refuses a thread of the natives' own: only they run it on after its wait (not measured) */
static int coresume (lua_State *L) {
  int own;
  luaL_argcheck(L, lua_type(L, 1) == LUA_TTHREAD, 1, "thread expected");  /* as stock Lua's, at the caller's line */
  lua_getfield(L, LUA_REGISTRYINDEX, THREADS);
  lua_pushvalue(L, 1);
  own = lua_rawget(L, -2) != LUA_TNIL;
  lua_pop(L, 2);
  if (own) {
    lua_pushboolean(L, 0);
    lua_pushliteral(L, "cannot resume non-suspended coroutine");
    return 2;
  }
  lua_pushvalue(L, lua_upvalueindex(1));
  lua_insert(L, 1);
  lua_call(L, lua_gettop(L) - 1, LUA_MULTRET);
  return lua_gettop(L);
}

/* No io, debug or package; no dofile, loadfile, collectgarbage or string.dump; os with only clock, date, difftime and
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
  lua_getfield(L, -1, LUA_STRLIBNAME);
  drop(L, "dump");
  lua_pop(L, 1);
  lua_getfield(L, -1, LUA_COLIBNAME);
  lua_getfield(L, -1, "resume");
  lua_pushcclosure(L, coresume, 1);
  lua_setfield(L, -2, "resume");
  lua_pushcfunction(L, coyield);
  lua_setfield(L, -2, "yield");
  lua_pushcfunction(L, coisyieldable);
  lua_setfield(L, -2, "isyieldable");
  lua_pop(L, 1);
  lua_newtable(L);  /* the natives' threads, held weakly */
  lua_createtable(L, 0, 1);
  lua_pushliteral(L, "k");
  lua_setfield(L, -2, "__mode");
  lua_setmetatable(L, -2);
  lua_setfield(L, LUA_REGISTRYINDEX, THREADS);
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

static Handle *tohandle (lua_State *L, int index) {
  return lua_type(L, index) == LUA_TUSERDATA && lua_rawlen(L, index) == sizeof(Handle)
         ? (Handle *)lua_touserdata(L, index) : NULL;
}

/* == as the game's: by the game ids, whatever the types (GetTriggerEventId() == ConvertPlayerColor(4)). Lua calls the
   first operand's, and an agent's refuses a second that isn't an agent, at the comparing line. */
static int handleeq (lua_State *L) {
  Handle *a = tohandle(L, 1), *b = tohandle(L, 2);
  if (a == NULL) return luaL_argerror(L, 1, "handle expected");
  if (b == NULL || (a->agent && !b->agent)) return luaL_argerror(L, 2, a->agent ? "agent expected" : "handle expected");
  lua_pushboolean(L, a->id == b->id);
  return 1;
}

/* Pushes the userdata standing for the handle with key as the declared type named type: the same one while Lua holds
   it, with a metatable named after the type, so tostring writes "timer: <address>" as the game's does, and which
   getmetatable reads as false, as the game's. */
static void pushhandle (lua_State *L, int key, int id, int agent, const char *type, size_t length) {
  Handle *h;
  lua_pushlstring(L, type, length);
  luaL_getsubtable(L, LUA_REGISTRYINDEX, HANDLES);  /* type, handles */
  lua_pushvalue(L, -2);
  if (lua_rawget(L, -2) != LUA_TTABLE) {  /* the type's userdata by key, held weakly */
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
  if (lua_rawgeti(L, -1, key) == LUA_TNIL) {
    lua_pop(L, 1);
    h = (Handle *)lua_newuserdata(L, sizeof(Handle));
    h->key = key;
    h->id = id;
    h->agent = agent;
    h->type = lua_tostring(L, -4);  /* a short string, kept by the metatable's __name and the registry */
    if (luaL_newmetatable(L, h->type)) {
      lua_pushcfunction(L, handleeq);
      lua_setfield(L, -2, "__eq");
      lua_pushboolean(L, 0);
      lua_setfield(L, -2, "__metatable");
    }
    lua_setmetatable(L, -2);
    lua_pushvalue(L, -1);
    lua_rawseti(L, -3, key);
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
      case VHANDLE: case VAGENT: pushhandle(L, v->integer, v->id, v->type == VAGENT, v->string, v->length); break;
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
   straight from pcall or a coroutine still names its line; MirrorImaged's own Lua (the chunk "mirror") is passed over,
   so the line is the script's */
static void pushwhere (lua_State *L) {
  lua_State *threads[2];
  lua_Debug ar;
  int t, level;
  threads[0] = L;
  threads[1] = G(L)->mainthread;
  for (t = 0; t < 2; t++) {
    for (level = t == 0; lua_getstack(threads[t], level, &ar); level++) {
      lua_getinfo(threads[t], "Sl", &ar);
      if (ar.currentline > 0 && strcmp(ar.short_src, "mirror") != 0) {
        lua_pushfstring(L, "%s:%d: ", ar.short_src, ar.currentline);
        return;
      }
    }
  }
  lua_pushliteral(L, "");
}

/* As luaL_where, but passing over MirrorImaged's own Lua (the chunk "mirror"), so a position is the script's; a C
   function where it looks gives no position, as luaL_where's */
static void where (lua_State *L, int level) {
  lua_Debug ar;
  for (; lua_getstack(L, level, &ar); level++) {
    lua_getinfo(L, "Sl", &ar);
    if (strcmp(ar.short_src, "mirror") == 0) continue;
    if (ar.currentline > 0) {
      lua_pushfstring(L, "%s:%d: ", ar.short_src, ar.currentline);
      return;
    }
    break;
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
  if (n == YIELDS) return lua_yield(L, 0);
  if (n >= 0) {
    pushcsharp(L, results, n);
    return n;
  }
  pushcsharp(L, results, 1);
  if (n == HALTS) pushwhere(L);
  else where(L, 1);
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

/* calls the global name */
static int docall (lua_State *L) {
  const Request *r = (const Request *)lua_touserdata(L, 1);
  lua_settop(L, 0);
  pushcsharp(L, r->values, r->n);  /* before anything that can run Lua, and so C# */
  if (lua_getglobal(L, r->name) == LUA_TNIL)
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

/* a new thread of the natives' own, to call the global name, or the function ref when there is no name; the
   thread's reference is left in r->ref */
static int dostart (lua_State *L) {
  Request *r = (Request *)lua_touserdata(L, 1);
  lua_State *co = lua_newthread(L);
  lua_getfield(L, LUA_REGISTRYINDEX, THREADS);
  lua_pushvalue(L, -2);
  lua_pushboolean(L, 1);
  lua_rawset(L, -3);
  lua_pop(L, 1);
  if (r->name == NULL) lua_rawgeti(L, LUA_REGISTRYINDEX, r->ref);
  else lua_getglobal(L, r->name);
  lua_xmove(L, co, 1);
  r->ref = luaL_ref(L, LUA_REGISTRYINDEX);
  return 0;
}

/* Runs thread r->ref on. Ended, it is let go of and leaves what it returned, or raises its error; r->n is 1 when it
   yielded instead, and it is kept. */
static int doresume (lua_State *L) {
  Request *r = (Request *)lua_touserdata(L, 1);
  lua_State *co;
  int status, n;
  lua_rawgeti(L, LUA_REGISTRYINDEX, r->ref);
  co = lua_tothread(L, -1);
  status = lua_resume(co, L, 0);
  r->n = status == LUA_YIELD;
  if (r->n) {
    lua_settop(co, 0);  /* what it yielded, which nothing reads */
    return 0;
  }
  luaL_unref(L, LUA_REGISTRYINDEX, r->ref);  /* the thread stays on this stack until this returns */
  if (status != LUA_OK) {
    lua_xmove(co, L, 1);
    return lua_error(L);
  }
  n = lua_gettop(co);
  luaL_checkstack(L, n, "too many results");
  lua_xmove(co, L, n);
  return n;
}

/* starts the global name, or the function ref when there is no name, in a new thread kept as *thread, for
   mirror_resume to run */
LUALIB_API int mirror_start (lua_State *L, const char *name, int ref, int *thread) {
  Request r = {name, NULL, 0, NULL, ref};
  int status = protect(L, dostart, &r, 0);
  *thread = r.ref;
  return status;
}

/* runs the thread on until it ends, leaving what it returned, or yields (*yielded 1); an error ends it */
LUALIB_API int mirror_resume (lua_State *L, int thread, int *yielded) {
  Request r = {NULL, NULL, 0, NULL, thread};
  int status = protect(L, doresume, &r, LUA_MULTRET);
  *yielded = status == LUA_OK && r.n;
  return status;
}

/* JASS's operation limit: the thread raises once it has run its count of Lua instructions */
static void overlimit (lua_State *L, lua_Debug *ar) {
  (void)ar;
  L->allowhook = 1;  /* as rehalt's: the error leaves the hook without turning hooks back on */
  where(L, 0);  /* in a hook, level 0 is the Lua function running */
  lua_pushliteral(L, "the thread passed JASS's operation limit");
  lua_concat(L, 2);
  lua_error(L);
}

static int dolimit (lua_State *L) {
  const Request *r = (const Request *)lua_touserdata(L, 1);
  lua_State *t = G(L)->mainthread;
  if (lua_getfield(L, LUA_REGISTRYINDEX, HALT) != LUA_TNIL) return 0;  /* a halted state keeps rehalt's hooks */
  if (r->ref != LUA_NOREF) {
    lua_rawgeti(L, LUA_REGISTRYINDEX, r->ref);
    t = lua_tothread(L, -1);
  }
  lua_sethook(t, r->n > 0 ? overlimit : NULL, r->n > 0 ? LUA_MASKCOUNT : 0, r->n);
  return 0;
}

/* Gives the thread mirror_start left as thread, or the main thread for LUA_NOREF, count Lua instructions from now
   before it raises; a thread started later takes the main thread's count afresh. A count of 0 takes the limit away. */
LUALIB_API int mirror_limit (lua_State *L, int thread, int count) {
  Request r = {NULL, NULL, count, NULL, thread};
  return protect(L, dolimit, &r, 0);
}

/* What the string s of length bytes is as a number to Lua, as luaL_checkinteger and luaL_checknumber read one: 1 an
   integer, 2 a float, 0 none */
LUALIB_API int mirror_tonumber (const char *s, size_t length, int *integer, float *number) {
  TValue o;
  if (strlen(s) != length || luaO_str2num(s, &o) != length + 1) return 0;
  if (ttisinteger(&o)) {
    *integer = (int)ivalue(&o);
    return 1;
  }
  *number = fltvalue(&o);
  return 2;
}

/* JASS's rules for transpiled JASS (decisions D132 to D136), as C functions, so the operation limit counts only the
   instructions that call them (D135). Each error names the JASS line calling it. */
static int jasserror (lua_State *L, const char *message) {
  where(L, 1);
  lua_pushstring(L, message);
  lua_concat(L, 2);
  return lua_error(L);
}

/* MirrorImaged_Read(value, name): a variable declared without a value, read before it is set, ends the thread */
static int jassread (lua_State *L) {
  if (lua_rawequal(L, 1, lua_upvalueindex(1))) {
    lua_pushfstring(L, "%s was read before it was set, which ends the thread", luaL_checkstring(L, 2));
    return jasserror(L, lua_tostring(L, -1));
  }
  lua_settop(L, 1);
  return 1;
}

/* MirrorImaged_Divide(dividend, divisor): cut toward zero, as C's / does; by zero ends the thread, and -2147483648 by
   -1 closes the game, through C#'s MirrorImaged_Closes */
static int jassdivide (lua_State *L) {
  lua_Integer a = luaL_checkinteger(L, 1), b = luaL_checkinteger(L, 2);
  if (b == 0) return jasserror(L, "an integer was divided by zero, which ends the thread");
  if (b == -1 && a == LUA_MININTEGER) {
    lua_pushvalue(L, lua_upvalueindex(1));
    lua_pushliteral(L, "The game closes: -2147483648 was divided by -1");
    lua_call(L, 1, 0);
  }
  lua_pushinteger(L, a / b);
  return 1;
}

/* MirrorImaged_DivideReal(dividend, divisor): Lua's / in the game's arithmetic; by zero ends the thread */
static int jassdividereal (lua_State *L) {
  if (luaL_checknumber(L, 2) == 0) return jasserror(L, "a real was divided by zero, which ends the thread");
  lua_settop(L, 2);
  lua_arith(L, LUA_OPDIV);
  return 1;
}

/* MirrorImaged_Join(a, b): a string's +, null taken as "", and null + null null */
static int jassjoin (lua_State *L) {
  lua_settop(L, 2);
  if (lua_isnil(L, 1)) return 1;
  if (lua_isnil(L, 2)) lua_pop(L, 1);
  else lua_concat(L, 2);
  return 1;
}

/* MirrorImaged_Equal(a, b): == on reals, true when |a - b| (in the game's arithmetic) is under 0.001 or under two
   float steps of the larger magnitude */
static int jassequal (lua_State *L) {
  float d, m, n, step;
  unsigned int bits;
  lua_settop(L, 2);
  lua_pushvalue(L, 1);
  lua_pushvalue(L, 2);
  lua_arith(L, LUA_OPSUB);
  d = (float)lua_tonumber(L, -1);
  m = (float)lua_tonumber(L, 1);
  n = (float)lua_tonumber(L, 2);
  if (d < 0) d = -d;
  if (m < 0) m = -m;
  if (n < 0) n = -n;
  if (n > m) m = n;
  memcpy(&bits, &m, sizeof bits);
  bits &= 0x7f800000u;
  step = 0;
  if (bits > 0x0b000000u) {
    bits -= 0x0b000000u;
    memcpy(&step, &bits, sizeof step);
  }
  lua_pushboolean(L, d < 0.001f || d < step);
  return 1;
}

/* MirrorImaged_Real(i): an integer given to a real, cut toward zero to a float (probe jass-numbers), where C's
   conversion rounds */
static int jassreal (lua_State *L) {
  lua_Integer i = luaL_checkinteger(L, 1);
  unsigned int u = i < 0 ? 0u - (unsigned int)i : (unsigned int)i;
  int shift = 0;
  float f;
  while ((u >> shift) >= (1u << 24)) shift++;
  f = (float)((u >> shift) << shift);
  lua_pushnumber(L, i < 0 ? -f : f);
  return 1;
}

/* MirrorImaged_Bits(i): an integer a function returning real returned, its bits read as a float */
static int jassbits (lua_State *L) {
  int i = (int)luaL_checkinteger(L, 1);
  float f;
  memcpy(&f, &i, sizeof f);
  lua_pushnumber(L, f);
  return 1;
}

static int dojass (lua_State *L) {
  static const luaL_Reg plain[] = {
    {"MirrorImaged_DivideReal", jassdividereal}, {"MirrorImaged_Join", jassjoin}, {"MirrorImaged_Equal", jassequal},
    {"MirrorImaged_Real", jassreal}, {"MirrorImaged_Bits", jassbits}, {NULL, NULL}
  };
  const luaL_Reg *f;
  lua_newtable(L);  /* the mark of a variable not set yet */
  lua_pushvalue(L, -1);
  lua_setglobal(L, "MirrorImaged_Unset");
  lua_pushcclosure(L, jassread, 1);
  lua_setglobal(L, "MirrorImaged_Read");
  lua_getglobal(L, "MirrorImaged_Closes");
  lua_pushcclosure(L, jassdivide, 1);
  lua_setglobal(L, "MirrorImaged_Divide");
  lua_pushnil(L);
  lua_setglobal(L, "MirrorImaged_Closes");
  for (f = plain; f->name; f++) {
    lua_pushcfunction(L, f->func);
    lua_setglobal(L, f->name);
  }
  return 0;
}

/* Makes the functions JASS's rules call globals, taking C#'s MirrorImaged_Closes, which closes the game, and hiding it */
LUALIB_API int mirror_jass (lua_State *L) {
  Request r = {NULL, NULL, 0, NULL, LUA_NOREF};
  return protect(L, dojass, &r, 0);
}
