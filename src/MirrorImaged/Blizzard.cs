using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using CSharpLua;
using MirrorImaged.Data;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;
using War3Net.CodeAnalysis.Transpilers;

namespace MirrorImaged;

// The installed game's blizzard.j, turned into Lua by War3Net's transpiler and cached (decisions D65, D66).
static class Blizzard
{
    const string CommonJ = "War3.w3mod:scripts/common.j", BlizzardJ = "War3.w3mod:scripts/blizzard.j";

    static readonly Dictionary<string, string> Transpiled = [];

    /// <summary>
    /// blizzard.j as Lua, transpiled once a process for each build of the game, and kept between processes in the
    /// user's cache: a transpile takes about 1.6 s in a new process.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">No game is installed.</exception>
    public static string Lua()
    {
        var folder = GameInstall.Find() ?? throw new DirectoryNotFoundException(
            $"A run needs blizzard.j from an installed Warcraft III, and there is none in {GameInstall.Place()}. Set {GameInstall.Variable} to the game's folder.");
        var build = GameInstall.Build(folder).Key;
        lock (Transpiled)
        {
            if (!Transpiled.TryGetValue(build, out var lua))
                Transpiled[build] = lua = Cached(CacheFile(build), () => Transpile(folder));
            return lua;
        }
    }

    // Named by a hash of the game's build and of the assemblies that make the Lua, so a change to any of them makes a
    // new file; null when there is no folder for it or no assembly file to hash (a single-file app).
    internal static string? CacheFile(string build)
    {
        var cache = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var assemblies = new[] { typeof(Blizzard), typeof(JassSyntaxFactory), typeof(JassToLuaTranspiler), typeof(LuaRenderer) }
            .Select(type => type.Assembly.Location)
            .ToList();
        if (cache == "" || assemblies.Contains("")) return null;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes(build));
        foreach (var assembly in assemblies) hash.AppendData(File.ReadAllBytes(assembly));
        return Path.Combine(cache, "MirrorImaged", "cache", $"blizzard-{Convert.ToHexStringLower(hash.GetHashAndReset())[..32]}.lua");
    }

    /// <summary>
    /// The Lua in <paramref name="file"/>, whose write time is moved to now, as it is used; else
    /// <paramref name="transpile"/>'s, written there whole under another name first, so another process never reads half
    /// of it, and the other <c>blizzard-*</c> files there not used for 7 days removed. A file that can't be read or
    /// written is passed over, and with no file the Lua is transpiled.
    /// </summary>
    internal static string Cached(string? file, Func<string> transpile)
    {
        if (file is null) return transpile();
        string? cached = null;
        try
        {
            cached = File.ReadAllText(file);
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        if (cached is not null) return cached;
        var lua = transpile();
        var folder = Path.GetDirectoryName(file)!;
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText($"{file}.{Environment.ProcessId}", lua);
            File.Move($"{file}.{Environment.ProcessId}", file, overwrite: true);
            foreach (var other in new DirectoryInfo(folder).EnumerateFiles("blizzard-*"))
                if (other.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7)) other.Delete();
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
    internal static string ToLua(string commonJ, string jass) => ToLua(JassSyntaxFactory.ParseCompilationUnit(commonJ), jass);

    /// <inheritdoc cref="ToLua(string, string)"/>
    internal static string ToLua(JassCompilationUnitSyntax commonJ, string jass)
    {
        var transpiler = new JassToLuaTranspiler();
        transpiler.RegisterJassFile(commonJ);
        var lua = new StringWriter();
        new LuaRenderer(new LuaSyntaxGenerator.SettingInfo(), lua)
            .RenderCompilationUnit(transpiler.Transpile(Rewriter.Rewrite(JassSyntaxFactory.ParseCompilationUnit(jass))));
        return lua.ToString();
    }

    // What the JASS needs before War3Net transpiles it (decisions D65).
    sealed class Rewriter : JassSyntaxRewriter
    {
        static readonly HashSet<string> LuaKeywords = ["break", "do", "end", "for", "goto", "in", "nil", "repeat", "until", "while"];

        public static JassCompilationUnitSyntax Rewrite(JassCompilationUnitSyntax jass) =>
            new Rewriter().RewriteCompilationUnit(jass, out var rewritten) ? rewritten : jass;

        // War3Net writes a JASS name that is a Lua keyword as it is (SubStringBJ's parameter end), which Lua can't load,
        // so it gets _lua after it; JASS refuses the plainer end_. shortcut: a JASS name end_lua beside end would clash.
        protected override bool RewriteIdentifierName(JassIdentifierNameSyntax name, out JassIdentifierNameSyntax result)
        {
            if (!LuaKeywords.Contains(name.Token.Text)) return base.RewriteIdentifierName(name, out result);
            result = JassSyntaxFactory.IdentifierName(name.Token.Text + "_lua");
            return true;
        }

        protected override bool RewriteIdentifierNameAsExpression(JassIdentifierNameSyntax name, out JassExpressionSyntax result)
        {
            if (!RewriteIdentifierName(name, out var renamed)) return base.RewriteIdentifierNameAsExpression(name, out result);
            result = renamed;
            return true;
        }

        // War3Net writes a rawcode as FourCC("...") with its text as it is, so a quote in it breaks out of the string and
        // a backslash starts a Lua escape; its value is written instead (decisions D102), a negative one in parentheses,
        // since -'éab' would otherwise read --1029086878, a Lua comment.
        protected override bool RewriteLiteralExpression(JassLiteralExpressionSyntax literal, out JassExpressionSyntax result)
        {
            if (literal.SyntaxKind != JassSyntaxKind.FourCCLiteralExpression) return base.RewriteLiteralExpression(literal, out result);
            var value = JassLiteral.ParseFourCC(literal.Token.Text);
            result = JassSyntaxFactory.LiteralExpression(JassSyntaxFactory.Literal(value));
            if (value < 0) result = JassSyntaxFactory.ParenthesizedExpression(result);
            return true;
        }

        // War3Net transpiles a debug statement as if it had no debug; the game never runs one (claim: the JASS manual).
        protected override bool RewriteStatementList(ImmutableArray<JassStatementSyntax> statements, out ImmutableArray<JassStatementSyntax> result)
        {
            var rewritten = base.RewriteStatementList(statements, out result);
            if (!rewritten) result = statements;
            var kept = result.RemoveAll(statement => statement is JassDebugStatementSyntax);
            if (kept.Length == result.Length) return rewritten;
            result = kept;
            return true;
        }
    }
}
