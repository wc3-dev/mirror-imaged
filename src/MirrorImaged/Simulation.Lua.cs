using System.Globalization;
using System.Reflection;
using MirrorImaged.Lua;
using MirrorImaged.Natives;

namespace MirrorImaged;

// The natives and the game's own Lua functions as Lua globals (decisions D49, D113).
public sealed partial class Simulation
{
    // An argument the call left out, which Lua's messages call "no value"
    static readonly object None = new();

    Func<object?[], object?[]> Bind(MethodInfo native)
    {
        if (native.GetCustomAttribute<NativeAttribute>()!.Level == Level.Missing)
            return _ => throw new MissingNativeException(native.Name);
        var parameters = native.GetParameters();
        return args =>
        {
            var values = new object?[parameters.Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = ToCSharp(i < args.Length ? args[i] : None, parameters[i].ParameterType, i + 1, native.Name);
            var result = native.Invoke(_natives, BindingFlags.DoNotWrapExceptions, null, values, null);
            return native.ReturnType == typeof(void) ? [] : [result is handle h ? ToLua(h, native.ReturnType) : result];
        };
    }

    // A handle as Lua holds it, seen as the type declared, whose == compares the game's ids (decisions D63, D112)
    static LuaHandle ToLua(handle h, Type declared) => new(declared.Name, h.Key, h.Id, typeof(agent).IsAssignableFrom(declared));

    // The natives a TRIGSTR_n given to reads as the map's string n (probe data: StringHash, S2I, S2R, SubString, SaveStr
    // and ParseTags don't); the two that show text are inferred, as World Editor writes their text so (decisions D206).
    static readonly HashSet<string> ReadsTrigstr =
        ["GetLocalizedString", "StringCase", "StringLength", "SetPlayerName", "DisplayTextToPlayer", "DisplayTimedTextToPlayer"];

    internal object? ToCSharp(object? value, Type type, int position, string function)
    {
        if (type == typeof(int)) return Integer(value, position, function);
        if (type == typeof(float))
            return Number(value) switch
            {
                int n => (float)n,
                float f => f,
                _ => throw BadArgument(position, function, $"number expected, got {TypeName(value)}"),
            };
        if (type == typeof(bool)) return value is not (null or false) && value != None;
        // As luaL_checklstring: a number is written as tostring writes it, nil is refused, and the native reads up to
        // the first NUL (docs/facts.md, "Strings, numbers and hashtables"). A TRIGSTR_n given to a native that reads it
        // is the map's string n (decisions D206). A JASS map gives natives null, as its JASS does (probe jass-threads,
        // decisions D136).
        if (type == typeof(string))
            return value switch
            {
                null when _jass => null,
                string s when (s.IndexOf('\0') is >= 0 and var end ? s[..end] : s) is var text => ReadsTrigstr.Contains(function) ? _natives.Data.Map?.Resolve(text) ?? text : text,
                int n => n.ToString(CultureInfo.InvariantCulture),
                float f => Native.Text(f),
                _ => throw BadArgument(position, function, $"string expected, got {TypeName(value)}"),
            };
        if (value is null || value == None) return null;
        if (type == typeof(Action) && value is LuaFunction code) return (Action)new LuaCode(this, code.Keep()).Run;
        if (type == typeof(Func<bool>) && value is LuaFunction condition) return (Func<bool>)new LuaCode(this, condition.Keep()).Evaluate;
        // Found by its key, whatever type Lua sees it as; one of another type is refused, as the type Lua sees is. A
        // destroyed one reads as null, but GetHandleId, the one native taking any handle, reads its id (probe handles),
        // and a destroyed Condition or Filter is still found, as its place's (decisions D115).
        if (value is LuaHandle h && typeof(handle).Assembly.GetType($"{typeof(handle).Namespace}.{h.Type}") is { } declared && type.IsAssignableFrom(declared)
            && _game.Handle(h.Key) is var found && (found is null || type.IsInstanceOfType(found)))
            return found ?? (type == typeof(handle) ? new handle { Id = h.Id, Key = h.Key } : null);
        // The game's message names no type given for a handle or a function (probe arguments)
        throw BadArgument(position, function, $"{(type.IsSubclassOf(typeof(Delegate)) ? "function" : type.Name)} expected");
    }

    // A whole number held as a float is taken (facts, "Lua runtime"), and so is a string Lua reads as a number (probe
    // arguments), as luaL_checkinteger and luaL_checknumber take them.
    static object? Number(object? value) => value is string s ? LuaState.ToNumber(s) : value;

    static int Integer(object? value, int position, string function) => Number(value) switch
    {
        int n => n,
        float f when f == MathF.Floor(f) && f >= int.MinValue && f < 2147483648f => (int)f,
        float => throw BadArgument(position, function, "number has no integer representation"),
        _ => throw BadArgument(position, function, $"number expected, got {TypeName(value)}"),
    };

    static ArgumentException BadArgument(int position, string function, string problem) =>
        new($"bad argument #{position} to '{function}' ({problem})");

    static string TypeName(object? value) => value switch
    {
        null => "nil",
        _ when value == None => "no value",
        bool => "boolean",
        int or float => "number",
        string => "string",
        LuaFunction => "function",
        LuaHandle h => h.Type,
        LuaOpaque o => o.Type,
        _ => value.GetType().Name,
    };

    // The game's own Lua functions (decisions D49): print writes to the log; FourCC reads a string's bytes, as C# can't
    // when they aren't UTF-8; __jarray, which World Editor's scripts make their array variables with; math.random and
    // math.randomseed on the game's generator, which GetRandomInt shares (facts, "Lua runtime").
    void AddLuaExtras()
    {
        _lua.Register("print", args =>
        {
            _game.Write("print", (string)args[0]!);
            return [];
        });
        _lua.Register("random", Random);
        _lua.Register("randomseed", args =>
        {
            _game.Seed(args.FirstOrDefault() switch
            {
                int n => n,
                float f => (int)f,
                var other => throw BadArgument(1, "randomseed", $"number expected, got {TypeName(other)}"),
            });
            return [];
        });
        _lua.Run("""
            local write, select, concat, type, error, byte, setmetatable = print, select, table.concat, type, error, string.byte, setmetatable
            function print(...)
              local parts = {}
              for i = 1, select('#', ...) do parts[i] = tostring((select(i, ...))) end
              write(concat(parts, '\t'))
            end
            function FourCC(id)
              if type(id) ~= 'string' then error("bad argument #1 to 'FourCC' (string expected, got " .. type(id) .. ")", 2) end
              local n = 0
              for i = 1, #id do n = n * 256 + byte(id, i) end
              return n
            end
            function __jarray(default)
              return setmetatable({}, {__index = function() return default end})
            end
            math.random, math.randomseed, random, randomseed = random, randomseed, nil, nil
            """, "mirror");
    }

    // As Lua 5.3's math.random: a float from 0 to 1, or an integer from 1 to m, or from m to n.
    object?[] Random(object?[] args)
    {
        if (args.Length == 0) return [_game.RandomReal(0, 1)];
        if (args.Length > 2) throw new ArgumentException("wrong number of arguments");
        int low = args.Length == 1 ? 1 : Integer(args[0], 1, "random"), high = Integer(args[^1], args.Length, "random");
        if (low > high) throw BadArgument(1, "random", "interval is empty");
        if (low < 0 && high > int.MaxValue + low) throw BadArgument(1, "random", "interval too large");
        return [_game.RandomInt(low, high)];
    }
}
