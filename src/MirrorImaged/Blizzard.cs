using System.Text;
using CSharpLua;
using MirrorImaged.Data;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;
using War3Net.CodeAnalysis.Transpilers;

namespace MirrorImaged;

// The installed game's blizzard.j, turned into Lua by War3Net's transpiler and cached (decisions D52, D53).
static class Blizzard
{
    const string CommonJ = "War3.w3mod:scripts/common.j", BlizzardJ = "War3.w3mod:scripts/blizzard.j";

    /// <summary>Where the transpiled Lua is kept between processes.</summary>
    public static string Cache => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MirrorImaged", "cache");

    static readonly Dictionary<string, string> Transpiled = [];

    /// <summary>
    /// blizzard.j as Lua, transpiled once for each build of the game and of MirrorImaged, whose build changes with the
    /// transpiling code, and kept in <see cref="Cache"/>: a transpile takes about 1.6 s in a new process.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">No game is installed.</exception>
    public static string Lua()
    {
        var folder = GameInstall.Find() ?? throw new DirectoryNotFoundException(
            $"A run needs blizzard.j from an installed Warcraft III, and there is none in {GameInstall.Place()}. Set {GameInstall.Variable} to the game's folder.");
        var file = Path.Combine(Cache, $"blizzard-{GameInstall.Build(folder).Key}-{typeof(Blizzard).Assembly.ManifestModule.ModuleVersionId:N}.lua");
        lock (Transpiled)
        {
            if (!Transpiled.TryGetValue(file, out var lua))
                Transpiled[file] = lua = Cached(file, () => Transpile(folder));
            return lua;
        }
    }

    /// <summary>
    /// The Lua in <paramref name="file"/>, else <paramref name="transpile"/>'s, written there in place of the other
    /// builds' files; written whole under another name first, so another process never reads half of it. A file that
    /// can't be read or written is left to the next run.
    /// </summary>
    internal static string Cached(string file, Func<string> transpile)
    {
        try { return File.ReadAllText(file); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        var lua = transpile();
        var folder = Path.GetDirectoryName(file)!;
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText($"{file}.{Environment.ProcessId}", lua);
            File.Move($"{file}.{Environment.ProcessId}", file, overwrite: true);
            foreach (var other in Directory.EnumerateFiles(folder, "blizzard-*"))
                if (other != file) File.Delete(other);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return lua;
    }

    static string Transpile(string folder)
    {
        using var storage = CascStorage.Open(folder);
        return ToLua(Read(storage, CommonJ), Read(storage, BlizzardJ));
    }

    static string Read(CascStorage storage, string path) =>
        Encoding.UTF8.GetString(storage.Read(path) ?? throw new FileNotFoundException($"{path} is not stored in the install", path));

    /// <summary>
    /// <paramref name="jass"/> as Lua. <paramref name="commonJ"/> gives the natives' return types, by which the
    /// transpiler tells joining strings from adding numbers.
    /// </summary>
    internal static string ToLua(string commonJ, string jass)
    {
        var transpiler = new JassToLuaTranspiler();
        transpiler.RegisterJassFile(JassSyntaxFactory.ParseCompilationUnit(commonJ));
        var lua = new StringWriter();
        new LuaRenderer(new LuaSyntaxGenerator.SettingInfo(), lua)
            .RenderCompilationUnit(transpiler.Transpile(LuaKeywords.Rename(JassSyntaxFactory.ParseCompilationUnit(jass))));
        return lua.ToString();
    }

    // War3Net writes a JASS name that is a Lua keyword as it is (SubStringBJ's parameter end), which Lua can't load, so
    // such a name gets _lua after it; JASS refuses the plainer end_. shortcut: a JASS name end_lua beside end would clash.
    sealed class LuaKeywords : JassSyntaxRewriter
    {
        static readonly HashSet<string> Keywords = ["break", "do", "end", "for", "goto", "in", "nil", "repeat", "until", "while"];

        public static JassCompilationUnitSyntax Rename(JassCompilationUnitSyntax jass) =>
            new LuaKeywords().RewriteCompilationUnit(jass, out var renamed) ? renamed : jass;

        protected override bool RewriteIdentifierName(JassIdentifierNameSyntax name, out JassIdentifierNameSyntax result)
        {
            if (!Keywords.Contains(name.Token.Text)) return base.RewriteIdentifierName(name, out result);
            result = JassSyntaxFactory.IdentifierName(name.Token.Text + "_lua");
            return true;
        }

        protected override bool RewriteIdentifierNameAsExpression(JassIdentifierNameSyntax name, out JassExpressionSyntax result)
        {
            if (!RewriteIdentifierName(name, out var renamed)) return base.RewriteIdentifierNameAsExpression(name, out result);
            result = renamed;
            return true;
        }
    }
}
