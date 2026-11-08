using System.Reflection;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;

namespace MirrorImaged;

// Preloader runs a file's JASS as the game does (decisions D102): turned into Lua by War3Net's transpiler, as blizzard.j
// is, and run with common.j's natives and globals only, as they were before the map ran. A file that doesn't parse or
// names anything else (a blizzard.j function or global) closes the game (facts), and so stops the run.
public sealed partial class Simulation
{
    static JassCompilationUnitSyntax? _commonJ;

    LuaFunction? _preloadFile;

    static IEnumerable<MethodInfo> NativeMethods => typeof(GameNatives).GetMethods().Where(method => method.GetCustomAttribute<NativeAttribute>() is not null);

    // common.j's globals and natives as JASS declarations, which a file is transpiled against: War3Net needs a function's
    // declaration to call it inside an expression.
    static string Declarations(GameNatives natives) =>
        string.Join('\n', [
            "globals",
            .. natives.Globals().Select(global => $"constant {JassType(global.Value!.GetType())} {global.Name} = null"),
            "endglobals",
            .. NativeMethods.Select(native =>
            {
                var parameters = native.GetParameters();
                var takes = parameters.Length == 0 ? "nothing" : string.Join(", ", parameters.Select(p => $"{JassType(p.ParameterType)} {p.Name}"));
                return $"native {native.Name} takes {takes} returns {JassType(native.ReturnType)}";
            }),
        ]);

    static string JassType(Type type) =>
        type == typeof(void) ? "nothing"
        : type == typeof(int) ? "integer"
        : type == typeof(float) ? "real"
        : type == typeof(bool) ? "boolean"
        : type == typeof(string) ? "string"
        : typeof(Delegate).IsAssignableFrom(type) ? "code"
        : type.Name;

    // Keeps a Lua function that runs a file's Lua, as the chunk named by its path, with common.j's natives and globals
    // and __jarray, which War3Net makes a JASS array with, taken now, before blizzard.j and the map can change them; any
    // other name it reads is an error.
    void AddPreloader()
    {
        _lua["preloaderNames"] = string.Join(' ', _natives.Globals().Select(global => global.Name).Concat(NativeMethods.Select(native => native.Name)).Append("__jarray"));
        _lua.Register("preloader", args =>
        {
            _preloadFile = ((LuaFunction)args[0]!).Keep();
            return [];
        });
        _lua.Run("""
            local load, assert, error, setmetatable, jass = load, assert, error, setmetatable, {}
            for name in preloaderNames:gmatch('%S+') do jass[name] = _G[name] end
            setmetatable(jass, {__index = function(_, name) error(name .. " is not a native or a global of common.j", 2) end})
            preloader(function(lua, path)
              local file = setmetatable({}, {__index = jass})
              assert(load(lua, '@' .. path, 't', file))()
              file.PreloadFiles()
            end)
            preloaderNames, preloader = nil, nil
            """, "mirror");
    }

    void RunPreloadFile(string path, string jass)
    {
        try
        {
            _preloadFile!.Call(Blizzard.ToLua(_commonJ ??= JassSyntaxFactory.ParseCompilationUnit(Declarations(_natives)), jass), path);
        }
        catch (Exception e) when (_lua.Halted is null)
        {
            throw new GameClosedException($"The game closes: Preloader ran {path}, which it can't run ({e.Message.Split("\nstack traceback:")[0].ReplaceLineEndings(" ")})");
        }
    }
}
