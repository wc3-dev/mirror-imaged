using System.Runtime.InteropServices;
using System.Text;

namespace MirrorImaged.Lua;

/// <summary>
/// A Lua 5.3 state as the game has it: 32-bit integers, the game's float arithmetic and maths, and its sandbox.
/// Values cross as <see langword="null"/> (nil), <see cref="bool"/>, <see cref="int"/> (integer),
/// <see cref="float"/> (float) and <see cref="string"/>. Not thread-safe.
/// </summary>
public sealed unsafe class LuaState : IDisposable
{
    static readonly string[] TypeNames = ["nil", "boolean", "light userdata", "number", "string", "table", "function", "userdata", "thread"];

    static LuaState() => Native.mirror_setcallback(&Dispatch);

    readonly List<Func<object?[], object?[]>> _functions = [];
    GCHandle _self;
    nint _l;

    public LuaState()
    {
        _l = Native.mirror_newstate();
        if (_l == 0) throw new OutOfMemoryException("Lua could not make a state.");
        _self = GCHandle.Alloc(this);
    }

    /// <summary>Runs a chunk and returns what it returns. Error messages name it: <c>war3map.lua:3: ...</c>.</summary>
    public object?[] Run(string code, string chunkName = "chunk")
    {
        var l = L;
        int top = Native.lua_gettop(l);
        var bytes = Encoding.UTF8.GetBytes(code);
        fixed (byte* p = bytes)
        {
            if (Native.luaL_loadbufferx(l, p, (nuint)bytes.Length, "@" + chunkName, "t") != 0) throw Error(l, top);
        }
        return CallAt(l, top, 0);
    }

    /// <summary>Calls the global function <paramref name="name"/> and returns its results.</summary>
    public object?[] Call(string name, params object?[] args)
    {
        var l = L;
        int top = Native.lua_gettop(l);
        Native.lua_getglobal(l, name);
        PushAll(l, args);
        return CallAt(l, top, args.Length);
    }

    /// <summary>Reads or writes a global.</summary>
    public object? this[string name]
    {
        get
        {
            var l = L;
            Native.lua_getglobal(l, name);
            try { return Read(l, -1); }
            finally { Native.lua_settop(l, -2); }
        }
        set
        {
            var l = L;
            PushAll(l, [value]);
            Native.lua_setglobal(l, name);
        }
    }

    /// <summary>
    /// Makes <paramref name="function"/> the global <paramref name="name"/>. An exception it throws becomes a Lua
    /// error at the calling line.
    /// </summary>
    public void Register(string name, Func<object?[], object?[]> function)
    {
        var l = L;
        Native.lua_pushlightuserdata(l, GCHandle.ToIntPtr(_self));
        Native.lua_pushinteger(l, _functions.Count);
        _functions.Add(function);
        Native.mirror_pushfunction(l, 2);
        Native.lua_setglobal(l, name);
    }

    public void Dispose()
    {
        if (_l == 0) return;
        Native.lua_close(_l);
        _l = 0;
        _self.Free();
    }

    nint L => _l != 0 ? _l : throw new ObjectDisposedException(nameof(LuaState));

    static object?[] CallAt(nint l, int top, int args)
    {
        if (Native.mirror_pcall(l, args, Native.MultipleResults) != 0) throw Error(l, top);
        try
        {
            var results = new object?[Native.lua_gettop(l) - top];
            for (int i = 0; i < results.Length; i++) results[i] = Read(l, top + 1 + i);
            return results;
        }
        finally { Native.lua_settop(l, top); }
    }

    static LuaException Error(nint l, int top)
    {
        var message = Native.lua_type(l, -1) == 4 ? ReadString(l, -1) : "(error object is not a string)";
        Native.lua_settop(l, top);
        return new LuaException(message);
    }

    [UnmanagedCallersOnly]
    static int Dispatch(nint l)
    {
        try
        {
            var state = (LuaState)GCHandle.FromIntPtr(Native.lua_touserdata(l, Native.RegistryIndex - 1)).Target!;
            var function = state._functions[Native.lua_tointegerx(l, Native.RegistryIndex - 2, null)];
            var args = new object?[Native.lua_gettop(l)];
            for (int i = 0; i < args.Length; i++) args[i] = Read(l, i + 1);
            var results = function(args);
            PushAll(l, results);
            return results.Length;
        }
        catch (Exception e)
        {
            PushAll(l, [e.Message]);
            return -1;
        }
    }

    static object? Read(nint l, int index) => Native.lua_type(l, index) switch
    {
        0 => null,
        1 => Native.lua_toboolean(l, index) != 0,
        3 when Native.lua_isinteger(l, index) != 0 => Native.lua_tointegerx(l, index, null),
        3 => Native.lua_tonumberx(l, index, null),
        4 => ReadString(l, index),
        var type => throw new InvalidOperationException($"A Lua {TypeNames[type]} can't be read in C#."),
    };

    static string ReadString(nint l, int index)
    {
        nuint length;
        var p = Native.lua_tolstring(l, index, &length);
        return Encoding.UTF8.GetString(p, (int)length);
    }

    static void PushAll(nint l, object?[] values)
    {
        if (Native.lua_checkstack(l, values.Length) == 0) throw new InvalidOperationException("Lua's stack is full.");
        foreach (var value in values) Push(l, value);
    }

    static void Push(nint l, object? value)
    {
        switch (value)
        {
            case null: Native.lua_pushnil(l); break;
            case bool b: Native.lua_pushboolean(l, b ? 1 : 0); break;
            case int i: Native.lua_pushinteger(l, i); break;
            case float f: Native.lua_pushnumber(l, f); break;
            case double d: Native.lua_pushnumber(l, (float)d); break;
            case string s:
                var bytes = Encoding.UTF8.GetBytes(s);
                fixed (byte* p = bytes) Native.lua_pushlstring(l, p, (nuint)bytes.Length);
                break;
            default: throw new ArgumentException($"A {value.GetType().Name} can't be passed to Lua.");
        }
    }
}
