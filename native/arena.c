/*
** Each state's memory: one arena of address space aligned to its size, filled the same way every run, so an
** object's offset in it is the same every run. Tables hash an object by the low 32 bits of its address, which
** are the offset's, so pairs visits table keys in the same order; tostring writes the offset.
*/
#define LUA_CORE

#include <stdint.h>
#include <stdio.h>
#include <string.h>

#if defined(_WIN32)
#include <windows.h>
#else
#include <sys/mman.h>
#if !defined(MAP_NORESERVE)
#define MAP_NORESERVE 0
#endif
#endif

#include "lua.h"

#define ARENA_SIZE ((size_t)1 << 36)  /* address space only; memory is committed as it is used */
#define COMMIT_STEP ((size_t)1 << 20)
#define SMALL 1024                     /* up to here, classes 16 bytes apart; then powers of two */
#define CLASSES (SMALL / 16 + 36 - 10)

typedef struct Arena {  /* at the arena's start */
  size_t top, committed;
  void *free[CLASSES];  /* blocks given back, by size class, last in first out */
} Arena;

static int sizeclass (size_t n, size_t *size) {
  int c;
  if (n <= SMALL) {
    *size = (n + 15) & ~(size_t)15;
    return (int)(*size >> 4) - 1;
  }
  for (c = SMALL / 16, *size = 2 * SMALL; *size < n; c++) *size <<= 1;
  return c;
}

static int commit (char *base, size_t from, size_t to) {
#if defined(_WIN32)
  return VirtualAlloc(base + from, to - from, MEM_COMMIT, PAGE_READWRITE) != NULL;
#else
  return mprotect(base + from, to - from, PROT_READ | PROT_WRITE) == 0;
#endif
}

static void *take (Arena *a, int c, size_t size) {
  char *p = a->free[c];
  if (p != NULL) {
    memcpy(&a->free[c], p, sizeof(void *));
    return p;
  }
  if (size > ARENA_SIZE - a->top) return NULL;
  if (a->top + size > a->committed) {
    size_t to = (a->top + size + COMMIT_STEP - 1) & ~(COMMIT_STEP - 1);
    if (!commit((char *)a, a->committed, to)) return NULL;
    a->committed = to;
  }
  p = (char *)a + a->top;
  a->top += size;
  return p;
}

static void give (Arena *a, void *p, size_t osize) {
  size_t size;
  int c = sizeclass(osize, &size);
  memcpy(p, &a->free[c], sizeof(void *));
  a->free[c] = p;
}

void *mirror_arena_alloc (void *ud, void *ptr, size_t osize, size_t nsize) {
  Arena *a = (Arena *)ud;
  size_t size, osz;
  void *q;
  int c;
  if (nsize == 0) {
    if (ptr != NULL) give(a, ptr, osize);
    return NULL;
  }
  if (nsize > ARENA_SIZE) return NULL;
  c = sizeclass(nsize, &size);
  if (ptr != NULL && sizeclass(osize, &osz) == c) return ptr;
  q = take(a, c, size);
  if (q == NULL) return (ptr != NULL && nsize <= osize) ? ptr : NULL;  /* Lua expects shrinking to work */
  if (ptr != NULL) {
    memcpy(q, ptr, osize < nsize ? osize : nsize);
    give(a, ptr, osize);
  }
  return q;
}

void mirror_arena_free (void *arena) {
#if defined(_WIN32)
  VirtualFree(arena, 0, MEM_RELEASE);
#else
  munmap(arena, ARENA_SIZE);
#endif
}
void *mirror_arena_new (void) {
  char *base = NULL;
#if defined(_WIN32)
  int i;
  for (i = 0; i < 8 && base == NULL; i++) {  /* another thread may take the space between the two calls */
    char *p = VirtualAlloc(NULL, 2 * ARENA_SIZE, MEM_RESERVE, PAGE_NOACCESS);
    if (p == NULL) return NULL;
    VirtualFree(p, 0, MEM_RELEASE);
    base = VirtualAlloc((void *)(((uintptr_t)p + ARENA_SIZE - 1) & ~(ARENA_SIZE - 1)), ARENA_SIZE, MEM_RESERVE,
                        PAGE_NOACCESS);
  }
  if (base == NULL) return NULL;
#else
  char *p = mmap(NULL, 2 * ARENA_SIZE, PROT_NONE, MAP_PRIVATE | MAP_ANONYMOUS | MAP_NORESERVE, -1, 0);
  if (p == MAP_FAILED) return NULL;
  base = (char *)(((uintptr_t)p + ARENA_SIZE - 1) & ~(ARENA_SIZE - 1));
  if (base > p) munmap(p, (size_t)(base - p));
  munmap(base + ARENA_SIZE, (size_t)(p + ARENA_SIZE - base));
#endif
  if (!commit(base, 0, COMMIT_STEP)) {
    mirror_arena_free(base);
    return NULL;
  }
  memset(base, 0, sizeof(Arena));
  ((Arena *)base)->top = (sizeof(Arena) + 15) & ~(size_t)15;
  ((Arena *)base)->committed = COMMIT_STEP;
  return base;
}


/* Written as Windows writes %p, 16 hex digits */
int mirror_pointer2str (char *buff, size_t sz, const void *p) {
  return snprintf(buff, sz, "%016llX", (unsigned long long)((uintptr_t)p & (ARENA_SIZE - 1)));
}
