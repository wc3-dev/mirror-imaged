using System.Reflection;
using MirrorImaged.Lua;
using MirrorImaged.Natives;

namespace MirrorImaged;

// The natives and the game's own Lua functions as Lua globals (decisions D49).
public sealed partial class Simulation
{
    Func<object?[], object?[]> Bind(MethodInfo native)
    {
        if (native.GetCustomAttribute<NativeAttribute>()!.Level == Level.Missing)
            return _ => throw new MissingNativeException(native.Name);
        var parameters = native.GetParameters();
        return args =>
        {
            var values = new object?[parameters.Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = ToCSharp(i < args.Length ? args[i] : null, parameters[i].ParameterType, i + 1, native.Name);
            var result = native.Invoke(_natives, BindingFlags.DoNotWrapExceptions, null, values, null);
            return native.ReturnType == typeof(void) ? [] : [result is handle h ? new LuaHandle(native.ReturnType.Name, h.Key) : result];
        };
    }

    internal object? ToCSharp(object? value, Type type, int position, string function)
    {
        if (type == typeof(int)) return Integer(value, position, function);
        if (type == typeof(float))
            return value switch
            {
                int n => (float)n,
                float f => f,
                _ => throw BadArgument(position, function, $"number expected, got {TypeName(value)}"),
            };
        if (type == typeof(bool)) return value is not (null or false);
        if (value is null) return null;
        if (type == typeof(string) && value is string s) return s;
        if (type == typeof(Action) && value is LuaFunction code) return (Action)new LuaCode(this, code.Keep()).Run;
        if (type == typeof(Func<bool>) && value is LuaFunction condition) return (Func<bool>)new LuaCode(this, condition.Keep()).Evaluate;
        // Found by its key, whatever type Lua sees it as; one of another type is refused, as the type Lua sees is.
        if (value is LuaHandle h && typeof(handle).Assembly.GetType($"{typeof(handle).Namespace}.{h.Type}") is { } declared && type.IsAssignableFrom(declared)
            && _game.Handle(h.Key) is var found && (found is null || type.IsInstanceOfType(found)))
            return found;
        throw BadArgument(position, function, $"{(type.IsSubclassOf(typeof(Delegate)) ? "function" : type.Name.ToLowerInvariant())} expected, got {TypeName(value)}");
    }

    // A whole number held as a float is taken (facts, "Lua runtime").
    static int Integer(object? value, int position, string function) => value switch
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
