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
    static readonly Dictionary<string, JassCompilationUnitSyntax> Parsed = [];

    /// <summary>
    /// blizzard.j as Lua, as the game's own Lua runs it in a Lua map, or with <paramref name="jass"/> JASS's rules, as
    /// the game runs it in a JASS map (decisions D131): transpiled once a process for each build of the game, and kept
    /// between processes in the user's cache: a transpile takes about 1.6 s in a new process.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">No game is installed.</exception>
    public static string Lua(bool jass = false)
    {
        var folder = Folder();
        var build = GameInstall.Build(folder).Key + (jass ? " jass" : "");
        lock (Transpiled)
        {
            if (!Transpiled.TryGetValue(build, out var lua))
                Transpiled[build] = lua = Cached(CacheFile(build), () => Transpile(folder, jass));
            return lua;
        }
    }

    /// <summary>blizzard.j as War3Net parses it, which a JASS map's script is transpiled against; once a process.</summary>
    /// <exception cref="DirectoryNotFoundException">No game is installed.</exception>
    public static JassCompilationUnitSyntax Syntax()
    {
        var folder = Folder();
        var build = GameInstall.Build(folder).Key;
        lock (Parsed)
        {
            if (!Parsed.TryGetValue(build, out var syntax))
            {
                using var storage = CascStorage.Open(folder);
                Parsed[build] = syntax = JassSyntaxFactory.ParseCompilationUnit(Read(storage, BlizzardJ));
            }
            return syntax;
        }
    }

    static string Folder() => GameInstall.Find() ?? throw new DirectoryNotFoundException(
        $"A run needs blizzard.j from an installed Warcraft III, and there is none in {GameInstall.Place()}. Set {GameInstall.Variable} to the game's folder.");

    // Named by a hash of the game's build (with " jass" for JASS's rules) and of the assemblies that make the Lua, so a
    // change to any of them makes a new file; null when there is no folder for it or no assembly file to hash (a
    // single-file app).
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

    static string Transpile(string folder, bool jass)
    {
        using var storage = CascStorage.Open(folder);
        return Jass.ToLua([JassSyntaxFactory.ParseCompilationUnit(Read(storage, CommonJ))], Read(storage, BlizzardJ), jass);
    }

    static string Read(CascStorage storage, string path) =>
        Encoding.UTF8.GetString(storage.Read(path) ?? throw new FileNotFoundException($"{path} is not stored in the install", path));

    /// <summary>
    /// <paramref name="jass"/> as Lua, as the game's own Lua runs blizzard.j. <paramref name="commonJ"/> gives the
    /// natives' return types, by which the transpiler tells joining strings from adding numbers.
    /// </summary>
    internal static string ToLua(string commonJ, string jass) => ToLua(JassSyntaxFactory.ParseCompilationUnit(commonJ), jass);

    /// <inheritdoc cref="ToLua(string, string)"/>
    internal static string ToLua(JassCompilationUnitSyntax commonJ, string jass) => Jass.ToLua([commonJ], jass, rules: false);
}
