using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using static MirrorImaged.Lua.Native;

namespace MirrorImaged.Lua;

/// <summary>
/// A Lua 5.3 state as the game has it: 32-bit integers, the game's float arithmetic and maths, and its sandbox.
/// Values cross as <see langword="null"/> (nil), <see cref="bool"/>, <see cref="int"/> (integer),
/// <see cref="float"/> (float) and <see cref="string"/>; a Lua string is read as UTF-8, so bytes that aren't UTF-8
/// come back as U+FFFD. Dispose it to free the state. Not thread-safe.
/// </summary>
public sealed unsafe class LuaState : IDisposable
{
    static readonly string[] TypeNames = ["nil", "boolean", "light userdata", "number", "string", "table", "function", "userdata", "thread"];
    static readonly Value* NoMessage = (Value*)NativeMemory.AllocZeroed((nuint)sizeof(Value));

    static LuaState() => mirror_setcallback(&Dispatch);

    readonly List<Func<object?[], object?[]>> _functions = [];
    readonly StateHandle _handle;

    public LuaState()
    {
        _handle = new StateHandle(mirror_newstate());
        if (_handle.IsInvalid) throw new OutOfMemoryException("Lua could not make a state.");
        _handle.Self = GCHandle.Alloc(this, GCHandleType.Weak);
    }

    /// <summary>Runs a chunk and returns what it returns. Error messages name it: <c>war3map.lua:3: ...</c>.</summary>
    public object?[] Run(string code, string chunkName = "chunk")
    {
        var l = L;
        int top = lua_gettop(l), status;
        var bytes = Encoding.UTF8.GetBytes(code);
        fixed (byte* p = bytes) status = luaL_loadbufferx(l, p, (nuint)bytes.Length, "@" + chunkName, "t");
        if (status != 0) throw Error(l, top);
        return Results(l, top, mirror_pcall(l, 0, MultipleResults));
    }

    /// <summary>Calls the global function <paramref name="name"/> and returns its results.</summary>
    public object?[] Call(string name, params object?[] args)
    {
        var l = L;
        int top = lua_gettop(l);
        return Results(l, top, mirror_call(l, name, _handle.Write(args), args.Length));
    }

    /// <summary>Reads or writes a global.</summary>
    public object? this[string name]
    {
        get
        {
            var l = L;
            return Results(l, lua_gettop(l), mirror_getglobal(l, name))[0];
        }
        set
        {
            var l = L;
            Results(l, lua_gettop(l), mirror_setglobal(l, name, _handle.Write([value])));
        }
    }

    /// <summary>
    /// Makes <paramref name="function"/> the global <paramref name="name"/>. An exception it throws becomes a Lua
    /// error at the calling line.
    /// </summary>
    public void Register(string name, Func<object?[], object?[]> function)
    {
        var l = L;
        Results(l, lua_gettop(l), mirror_register(l, name, GCHandle.ToIntPtr(_handle.Self), _functions.Count));
        _functions.Add(function);
    }

    public void Dispose() => _handle.Dispose();

    nint L => !_handle.IsClosed ? _handle.DangerousGetHandle() : throw new ObjectDisposedException(nameof(LuaState));

    static object?[] Results(nint l, int top, int status)
    {
        if (status != 0) throw Error(l, top);
        try
        {
            var results = new object?[lua_gettop(l) - top];
            for (int i = 0; i < results.Length; i++) results[i] = Read(l, top + 1 + i);
            return results;
        }
        finally { lua_settop(l, top); }
    }

    static LuaException Error(nint l, int top)
    {
        var message = lua_type(l, -1) == 4 ? ReadString(l, -1) : "(error object is not a string)";
        lua_settop(l, top);
        return new LuaException(message);
    }

    // Only reads from Lua: what it returns, or its error message, sandbox.c's trampoline pushes.
    [UnmanagedCallersOnly]
    static int Dispatch(nint l, Value** results)
    {
        LuaState? state = null;
        try
        {
            state = (LuaState)GCHandle.FromIntPtr(lua_touserdata(l, RegistryIndex - 1)).Target!;
            var function = state._functions[lua_tointegerx(l, RegistryIndex - 2, null)];
            var args = new object?[lua_gettop(l)];
            for (int i = 0; i < args.Length; i++) args[i] = Read(l, i + 1);
            var values = function(args);
            *results = state._handle.Write(values);
            return values.Length;
        }
        catch (Exception e)
        {
            *results = state is null ? NoMessage : state._handle.Write([e.Message]);
            return -1;
        }
    }

    static object? Read(nint l, int index) => lua_type(l, index) switch
    {
        0 => null,
        1 => lua_toboolean(l, index) != 0,
        3 when lua_isinteger(l, index) != 0 => lua_tointegerx(l, index, null),
        3 => lua_tonumberx(l, index, null),
        4 => ReadString(l, index),
        var type => throw new InvalidOperationException($"A Lua {TypeNames[type]} can't be read in C#."),
    };

    static string ReadString(nint l, int index)
    {
        nuint length;
        var p = lua_tolstring(l, index, &length);
        return Encoding.UTF8.GetString(p, (int)length);
    }

    /// <summary>The state, and the buffer that values cross to C in, freed together.</summary>
    sealed class StateHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public GCHandle Self;
        byte* _buffer;
        int _capacity;

        public StateHandle(nint l) : base(true) => SetHandle(l);

        /// <summary>The values as C reads them, valid until the next write; a string's bytes follow the values.</summary>
        public Value* Write(object?[] values)
        {
            int size = values.Length * sizeof(Value);
            foreach (var value in values) size += value is string s ? Encoding.UTF8.GetByteCount(s) : 0;
            if (size > _capacity)
            {
                _buffer = (byte*)NativeMemory.Realloc(_buffer, (nuint)size);
                _capacity = size;
            }
            var result = (Value*)_buffer;
            var text = _buffer + values.Length * sizeof(Value);
            for (int i = 0; i < values.Length; i++)
            {
                switch (values[i])
                {
                    case null: result[i] = new() { Type = Value.Nil }; break;
                    case bool b: result[i] = new() { Type = Value.Boolean, IntegerValue = b ? 1 : 0 }; break;
                    case int n: result[i] = new() { Type = Value.Integer, IntegerValue = n }; break;
                    case float f: result[i] = new() { Type = Value.Float, FloatValue = f }; break;
                    case double d: result[i] = new() { Type = Value.Float, FloatValue = (float)d }; break;
                    case string s:
                        int length = Encoding.UTF8.GetBytes(s, new Span<byte>(text, _capacity - (int)(text - _buffer)));
                        result[i] = new() { Type = Value.String, StringValue = text, Length = (nuint)length };
                        text += length;
                        break;
                    case var other: throw new ArgumentException($"A {other.GetType().Name} can't be passed to Lua.");
                }
            }
            return result;
        }

        protected override bool ReleaseHandle()
        {
            mirror_close(handle);
            if (Self.IsAllocated) Self.Free();
            NativeMemory.Free(_buffer);
            return true;
        }
    }
}
