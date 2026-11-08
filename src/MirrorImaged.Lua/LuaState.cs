using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using static MirrorImaged.Lua.Native;

namespace MirrorImaged.Lua;

/// <summary>
/// A Lua 5.3 state as the game has it: 32-bit integers, the game's float arithmetic and maths, and its sandbox.
/// Values cross as <see langword="null"/> (nil), <see cref="bool"/>, <see cref="int"/> (integer),
/// <see cref="float"/> (float), <see cref="string"/> and <see cref="LuaHandle"/> (userdata); a Lua string is read as
/// UTF-8, so bytes that aren't UTF-8 come back as U+FFFD. A C# function is also given Lua functions, as
/// <see cref="LuaFunction"/>, and anything else as <see cref="LuaOpaque"/>. Dispose it to free the state. Not
/// thread-safe.
/// </summary>
public sealed unsafe class LuaState : IDisposable
{
    static readonly string[] TypeNames = ["nil", "boolean", "light userdata", "number", "string", "table", "function", "userdata", "thread"];
    static readonly Value* NoState = Message("The Lua state calling this C# function is gone.");

    static LuaState() => mirror_setcallback(&Dispatch);

    readonly List<Func<object?[], object?[]>> _functions = [];
    readonly StateHandle _handle;
    bool _yield;

    public LuaState()
    {
        _handle = new StateHandle(mirror_newstate());
        if (_handle.IsInvalid) throw new OutOfMemoryException("Lua could not make a state.");
        _handle.Self = GCHandle.Alloc(this, GCHandleType.Weak);
    }

    /// <summary>
    /// Which exceptions from a C# function halt the state: no <c>pcall</c>, <c>xpcall</c> or coroutine in Lua can
    /// catch one, and every Lua call from C# fails from then on with a <see cref="LuaException"/> whose inner
    /// exception is it.
    /// </summary>
    public Func<Exception, bool>? Halts { get; set; }

    /// <summary>The exception that halted the state, if one has.</summary>
    public Exception? Halted { get; private set; }

    /// <summary>
    /// The simulated time, in whole milliseconds, that <c>os.clock</c> reads, and that <c>os.time</c> and
    /// <c>os.date</c> add to 2026-01-01 00:00:00 UTC; local time is UTC. Zero when the state is made.
    /// </summary>
    public TimeSpan Clock
    {
        get => Use(l => TimeSpan.FromMilliseconds(*mirror_clockms(l)));
        set => Use(l => *mirror_clockms(l) = value.Ticks / TimeSpan.TicksPerMillisecond);
    }

    /// <summary>Runs a chunk and returns what it returns. Error messages name it: <c>war3map.lua:3: ...</c>.</summary>
    public object?[] Run(string code, string chunkName = "chunk") => Run(Encoding.UTF8.GetBytes(code), chunkName);

    /// <summary>Runs a chunk given as its bytes, as a map stores it.</summary>
    public object?[] Run(byte[] code, string chunkName = "chunk") =>
        Use(l =>
        {
            int top = lua_gettop(l), status;
            fixed (byte* p = code) status = luaL_loadbufferx(l, p, (nuint)code.Length, "@" + chunkName, "t");
            if (status != 0) throw Error(l, top);
            return Results(l, top, mirror_pcall(l, 0, MultipleResults));
        });

    /// <summary>Calls the global function <paramref name="name"/> and returns its results.</summary>
    public object?[] Call(string name, params object?[] args) =>
        Use(l => Results(l, lua_gettop(l), mirror_call(l, name, _handle.Write(args), args.Length)));

    /// <summary>
    /// Calls the global function <paramref name="name"/> in a new Lua thread, as <see cref="LuaFunction.Start"/> does.
    /// </summary>
    public LuaThread Start(string name) => Start(name, 0);

    internal LuaThread Start(string? name, int reference)
    {
        var thread = new LuaThread(this, Use(l =>
        {
            int top = lua_gettop(l), made, status = mirror_start(l, name, reference, &made);
            if (status != 0) throw Error(l, top);
            return made;
        }));
        thread.Resume();
        return thread;
    }

    internal (object?[] Results, bool Yielded) Resume(int thread) =>
        Use(l =>
        {
            int top = lua_gettop(l), yielded, status = mirror_resume(l, thread, &yielded);
            return yielded != 0 ? ([], true) : (Results(l, top, status, argument: true), false);
        });

    /// <summary>
    /// Makes the Lua thread calling the C# function now running yield when that function returns, until it is resumed
    /// (<see cref="LuaThread.Resume"/>); the last thing the function does. Where the thread can't yield (the main
    /// thread, or past a C function), Lua raises its own error.
    /// </summary>
    public void Yield() => _yield = true;

    internal void Release(int reference)
    {
        if (_handle.IsClosed) return;
        Use(l => Results(l, lua_gettop(l), mirror_unref(l, reference)));
    }

    /// <summary>Reads or writes a global.</summary>
    public object? this[string name]
    {
        get => Use(l => Results(l, lua_gettop(l), mirror_getglobal(l, name)))[0];
        set => Use(l => Results(l, lua_gettop(l), mirror_setglobal(l, name, _handle.Write([value]))));
    }

    /// <summary>
    /// Makes <paramref name="function"/> the global <paramref name="name"/>. An exception it throws becomes a Lua
    /// error at the calling line.
    /// </summary>
    public void Register(string name, Func<object?[], object?[]> function)
    {
        // Added first, since a __newindex on _G can call it while it is registered, and kept if that fails, since the
        // __newindex may have stored it: an id is never given out twice.
        int id = _functions.Count;
        _functions.Add(function);
        Use(l => Results(l, lua_gettop(l), mirror_register(l, name, GCHandle.ToIntPtr(_handle.Self), id)));
    }

    /// <summary>The memory the state has committed, its most so far: its arena gives none back.</summary>
    internal long CommittedBytes => Use(l => (long)mirror_committed(l));

    /// <summary>Frees the state; when called from inside one of its calls, once that call is over.</summary>
    public void Dispose() => _handle.Dispose();

    // The handle is counted in use for the whole call, so neither Dispose nor the finalizer frees the state under it.
    T Use<T>(Func<nint, T> action)
    {
        bool added = false;
        try
        {
            _handle.DangerousAddRef(ref added);
            return action(_handle.DangerousGetHandle());
        }
        finally
        {
            if (added) _handle.DangerousRelease();
            GC.KeepAlive(this);
        }
    }

    static Value* Message(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var value = (Value*)NativeMemory.Alloc((nuint)(sizeof(Value) + bytes.Length));
        bytes.CopyTo(new Span<byte>(value + 1, bytes.Length));
        *value = new() { Type = Value.String, StringValue = (byte*)(value + 1), Length = (nuint)bytes.Length };
        return value;
    }

    object?[] Results(nint l, int top, int status, bool argument = false)
    {
        if (status != 0) throw Error(l, top);
        try
        {
            var results = new object?[lua_gettop(l) - top];
            for (int i = 0; i < results.Length; i++) results[i] = Read(l, top + 1 + i, argument);
            return results;
        }
        finally { lua_settop(l, top); }
    }

    LuaException Error(nint l, int top)
    {
        var message = lua_type(l, -1) == 4 ? ReadString(l, -1) : "(error object is not a string)";
        lua_settop(l, top);
        return new LuaException(message, Halted);
    }

    // Only reads from Lua: what it returns, or its error message, sandbox.c's trampoline pushes. The functions it was
    // given and didn't keep are let go of before the results are written, since that runs Lua. Once the state has
    // halted, no C# function runs: a finalizer or an xpcall handler can still call one without a Lua instruction.
    [UnmanagedCallersOnly]
    static int Dispatch(nint l, Value** results)
    {
        LuaState? state = null;
        try
        {
            state = (LuaState)GCHandle.FromIntPtr(lua_touserdata(l, RegistryIndex - 1)).Target!;
            if (state.Halted is not null)
            {
                *results = state._handle.Write([state.Halted.Message]);
                return Native.Halts;
            }
            var function = state._functions[lua_tointegerx(l, RegistryIndex - 2, null)];
            var args = new object?[lua_gettop(l)];
            for (int i = 0; i < args.Length; i++) args[i] = state.Read(l, i + 1, argument: true);
            object?[] values;
            state._yield = false;
            try { values = function(args); }
            finally
            {
                foreach (var arg in args)
                    if (arg is LuaFunction { Kept: false } given) given.Dispose();
            }
            if (state._yield)
            {
                state._yield = false;
                return Native.Yields;
            }
            *results = state._handle.Write(values);
            return values.Length;
        }
        catch (Exception e)
        {
            if (state is null)
            {
                *results = NoState;
                return -1;
            }
            state._yield = false;
            if (state.Halted is null && state.Halts?.Invoke(e) == true) state.Halted = e;
            *results = state._handle.Write([e.Message]);
            return state.Halted is null ? -1 : Native.Halts;
        }
    }

    // An argument C# can't read comes as its type, so the C# function can refuse it and every function is let go of; so
    // does what a thread returns.
    object? Read(nint l, int index, bool argument = false) => lua_type(l, index) switch
    {
        0 => null,
        1 => lua_toboolean(l, index) != 0,
        2 => new LuaFunction(this, (int)lua_touserdata(l, index)),  // a function given to C#, as sandbox.c's trampoline passes it
        3 when lua_isinteger(l, index) != 0 => lua_tointegerx(l, index, null),
        3 => lua_tonumberx(l, index, null),
        4 => ReadString(l, index),
        7 => ReadHandle((Native.Handle*)lua_touserdata(l, index)),
        var type when argument => new LuaOpaque(TypeNames[type]),
        var type => throw new InvalidOperationException($"A Lua {TypeNames[type]} can't be read in C#."),
    };

    static LuaHandle ReadHandle(Native.Handle* handle) =>
        new(Marshal.PtrToStringUTF8((nint)handle->Type)!, handle->Id);

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
            foreach (var value in values)
                size += value switch
                {
                    string s => Encoding.UTF8.GetByteCount(s),
                    LuaHandle h => Encoding.UTF8.GetByteCount(h.Type),
                    _ => 0,
                };
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
                    case LuaHandle h:
                        length = Encoding.UTF8.GetBytes(h.Type, new Span<byte>(text, _capacity - (int)(text - _buffer)));
                        result[i] = new() { Type = Value.Handle, IntegerValue = h.Key, StringValue = text, Length = (nuint)length };
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
