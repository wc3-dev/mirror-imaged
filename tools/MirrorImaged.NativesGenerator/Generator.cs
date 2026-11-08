using System.Text;
using System.Text.RegularExpressions;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;

namespace MirrorImaged.NativesGenerator;

public sealed record JassType(string Name, string Parent);

public sealed record JassParameter(string Type, string Name);

public sealed record JassNative(string Name, IReadOnlyList<JassParameter> Parameters, string Returns);

public sealed record JassScript(IReadOnlyList<JassType> Types, IReadOnlyList<JassNative> Natives);

/// <summary>
/// Writes <c>src/MirrorImaged.Natives/Generated</c> from the game's <c>common.j</c> and <c>common.ai</c>: a class per
/// handle type and a Missing stub per native, sorted by name, leaving out the natives written by hand.
/// </summary>
public static partial class Generator
{
    /// <summary>The scripts the natives come from: each one's name, the file its stubs go to and its storage path.</summary>
    public static readonly (string Name, string File, string Path)[] Scripts =
    [
        ("common.j", "CommonJNatives.g.cs", "War3.w3mod:scripts/common.j"),
        ("common.ai", "CommonAiNatives.g.cs", "War3.w3mod:scripts/common.ai"),
    ];

    // The natives whose code returns a value the game reads: a JASS function passed to them must return that type.
    static readonly Dictionary<string, string> CodeReturns = new(StringComparer.Ordinal)
    {
        ["Condition"] = "Func<bool>?",
        ["Filter"] = "Func<bool>?",
        ["SetHeroLevels"] = "Func<int>?",
    };

    static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
        "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    };

    /// <summary>The handle types and natives a JASS script declares, in its order.</summary>
    public static JassScript Read(string jass)
    {
        var declarations = JassSyntaxFactory.ParseCompilationUnit(jass).Declarations;
        return new JassScript(
            declarations.OfType<JassTypeDeclarationSyntax>()
                .Select(type => new JassType(type.IdentifierName.Token.Text, type.BaseType.GetFirstToken().Text))
                .ToList(),
            declarations.OfType<JassNativeFunctionDeclarationSyntax>()
                .Select(native => new JassNative(
                    native.IdentifierName.Token.Text,
                    native.ParameterList is JassParameterListSyntax list
                        ? list.ParameterList.Items.Select(parameter => new JassParameter(parameter.Type.GetFirstToken().Text, parameter.IdentifierName.Token.Text)).ToList()
                        : [],
                    native.ReturnClause.ReturnType.GetFirstToken().Text))
                .ToList());
    }

    /// <summary>A script's text, read with <paramref name="read"/> from its storage path.</summary>
    /// <exception cref="FileNotFoundException">The install does not store it.</exception>
    public static string Text(Func<string, byte[]?> read, string path) =>
        Encoding.UTF8.GetString(read(path) ?? throw new FileNotFoundException($"{path} is not stored in the install", path));

    /// <summary>
    /// The generated files by name: the types, then each script's natives but those in <paramref name="handWritten"/>.
    /// </summary>
    public static SortedDictionary<string, string> Generate(Func<string, byte[]?> read, string gameVersion, IReadOnlySet<string> handWritten)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var types = new List<JassType>();
        foreach (var (name, file, path) in Scripts)
        {
            var script = Read(Text(read, path));
            types.AddRange(script.Types);
            files[file] = WriteNatives(script.Natives.Where(native => !handWritten.Contains(native.Name)), $"{name}, game {gameVersion}");
        }

        files["Types.g.cs"] = WriteTypes(types, $"{string.Join(" and ", Scripts.Select(script => script.Name))}, game {gameVersion}");
        return files;
    }

    /// <summary>
    /// The natives written by hand in the natives project's folder: the names of the public methods declared in its
    /// C# files outside <c>Generated</c>, <c>bin</c> and <c>obj</c>.
    /// </summary>
    public static IReadOnlySet<string> HandWritten(string nativesFolder) =>
        Directory.EnumerateFiles(nativesFolder, "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetRelativePath(nativesFolder, path).Split(Path.DirectorySeparatorChar)[0] is not ("Generated" or "bin" or "obj"))
            .SelectMany(path => PublicMethod().Matches(File.ReadAllText(path)).Select(match => match.Groups["name"].Value))
            .ToHashSet(StringComparer.Ordinal);

    static string WriteTypes(IEnumerable<JassType> types, string source)
    {
        var text = new StringBuilder(Header(source));
        text.Append("\n#pragma warning disable CS8981 // The types keep common.j's names, which are lower case.\n");
        text.Append("\nnamespace MirrorImaged.Natives;\n\npublic class handle;\n");
        foreach (var type in types.OrderBy(type => type.Name, StringComparer.Ordinal))
            text.Append($"public class {Identifier(type.Name)} : {Identifier(type.Parent)};\n");
        return text.ToString();
    }

    static string WriteNatives(IEnumerable<JassNative> natives, string source)
    {
        var stubs = natives.OrderBy(native => native.Name, StringComparer.Ordinal).Select(native =>
        {
            var parameters = string.Join(", ", native.Parameters.Select(parameter => $"{CsType(parameter.Type, native.Name)} {Identifier(parameter.Name)}"));
            return $"""
                    [Native(Level.Missing)]
                    public {CsType(native.Returns, native.Name)} {Identifier(native.Name)}({parameters}) => throw new MissingNativeException("{native.Name}");

                """;
        });
        return $"{Header(source)}\n#nullable enable\n\nnamespace MirrorImaged.Natives;\n\npublic sealed partial class GameNatives\n{{\n{string.Join("\n", stubs)}}}\n";
    }

    static string Header(string source) => $"// Generated by MirrorImaged.NativesGenerator from {source}. Do not edit.\n";

    // JASS's handles, strings and code can be null.
    static string CsType(string jassType, string native) => jassType switch
    {
        "integer" => "int",
        "real" => "float",
        "boolean" => "bool",
        "string" => "string?",
        "nothing" => "void",
        "code" => CodeReturns.GetValueOrDefault(native, "Action?"),
        _ => Identifier(jassType) + "?",
    };

    static string Identifier(string name) => Keywords.Contains(name) ? "@" + name : name;

    [GeneratedRegex(@"\bpublic\s+(?:static\s+)?[@\w<>?,]+\s+(?<name>\w+)\s*\(")]
    private static partial Regex PublicMethod();
}
