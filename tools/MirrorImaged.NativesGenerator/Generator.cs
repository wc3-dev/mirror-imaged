using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Extensions;
using War3Net.CodeAnalysis.Jass.Syntax;

namespace MirrorImaged.NativesGenerator;

public sealed record JassType(string Name, string Parent);

public sealed record JassParameter(string Type, string Name);

public sealed record JassNative(string Name, IReadOnlyList<JassParameter> Parameters, string Returns);

/// <summary>A global and the expression it is given, if any.</summary>
public sealed record JassGlobal(string Name, string Type, JassExpressionSyntax? Value);

public sealed record JassScript(IReadOnlyList<JassType> Types, IReadOnlyList<JassNative> Natives, IReadOnlyList<JassGlobal> Globals);

/// <summary>
/// Writes <c>src/MirrorImaged.Natives/Generated</c> from the game's <c>common.j</c> and <c>common.ai</c>: a class per
/// handle type and a Missing stub per native, sorted by name, leaving out the natives written by hand, and
/// <c>common.j</c>'s globals with their values.
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

    /// <summary>The handle types, natives and globals a JASS script declares, in its order.</summary>
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
                .ToList(),
            declarations.OfType<JassGlobalsDeclarationSyntax>()
                .SelectMany(globals => globals.GlobalDeclarations)
                .Select(global => global switch
                {
                    JassGlobalConstantDeclarationSyntax constant => new JassGlobal(constant.IdentifierName.Token.Text, constant.Type.GetFirstToken().Text, constant.Value.Expression),
                    JassGlobalVariableDeclarationSyntax { Declarator: JassVariableDeclaratorSyntax variable } => new JassGlobal(variable.IdentifierName.Token.Text, variable.Type.GetFirstToken().Text, variable.Value?.Expression),
                    JassGlobalVariableDeclarationSyntax { Declarator: JassArrayDeclaratorSyntax array } => new JassGlobal(array.IdentifierName.Token.Text, array.Type.GetFirstToken().Text, null),
                    _ => throw new InvalidDataException($"A global the generator can't read: {global}"),
                })
                .ToList());
    }

    /// <summary>
    /// The installed scripts, read with <paramref name="read"/> from their storage paths, in the order of
    /// <see cref="Scripts"/>.
    /// </summary>
    /// <exception cref="FileNotFoundException">The install does not store one of them.</exception>
    public static IReadOnlyList<JassScript> ReadScripts(Func<string, byte[]?> read) =>
        Scripts.Select(script => Read(Encoding.UTF8.GetString(
                read(script.Path) ?? throw new FileNotFoundException($"{script.Path} is not stored in the install", script.Path))))
            .ToList();

    /// <summary>
    /// The generated files by name, from <paramref name="scripts"/> in the order of <see cref="Scripts"/>: the types,
    /// each script's natives but those in <paramref name="handWritten"/>, and <c>common.j</c>'s globals.
    /// </summary>
    /// <exception cref="InvalidDataException">A global of <c>common.j</c> has a value the generator can't write.</exception>
    public static SortedDictionary<string, string> Generate(IReadOnlyList<JassScript> scripts, string gameVersion, IReadOnlySet<string> handWritten)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var ((name, file, _), script) in Scripts.Zip(scripts))
            files[file] = WriteNatives(script.Natives.Where(native => !handWritten.Contains(native.Name)), $"{name}, game {gameVersion}");

        var source = $"{string.Join(" and ", Scripts.Select(script => script.Name))}, game {gameVersion}";
        files["Types.g.cs"] = WriteTypes(scripts.SelectMany(script => script.Types), source);
        files["Globals.g.cs"] = WriteGlobals(scripts[0].Globals, $"{Scripts[0].Name}, game {gameVersion}");
        return files;
    }

    /// <summary>
    /// The natives written by hand in the natives project's folder: the methods marked <c>[Native(...)]</c> right
    /// before <c>public</c>, in its C# files outside <c>Generated</c>, <c>bin</c> and <c>obj</c>, comments and strings
    /// left out.
    /// </summary>
    public static IReadOnlySet<string> HandWritten(string nativesFolder) =>
        Directory.EnumerateFiles(nativesFolder, "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetRelativePath(nativesFolder, path).Split(Path.DirectorySeparatorChar)[0] is not ("Generated" or "bin" or "obj"))
            .SelectMany(path => NativeMethod().Matches(File.ReadAllText(path)))
            .Where(match => match.Groups["name"].Success)
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

    static string WriteTypes(IEnumerable<JassType> types, string source)
    {
        var text = new StringBuilder(Header(source));
        text.Append("\n#pragma warning disable CS8981 // The types keep common.j's names, which are lower case.\n");
        text.Append("\nnamespace MirrorImaged.Natives;\n\npublic partial class handle;\n");
        foreach (var type in types.OrderBy(type => type.Name, StringComparer.Ordinal))
            text.Append($"public partial class {Identifier(type.Name)} : {Identifier(type.Parent)};\n");
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

        // The raw string above takes this file's line endings, which depend on how git checked it out.
        return $"{Header(source)}\n#nullable enable\n\nnamespace MirrorImaged.Natives;\n\npublic sealed partial class GameNatives\n{{\n{string.Join("\n", stubs)}}}\n"
            .ReplaceLineEndings("\n");
    }

    // The values are C# calling the natives as common.j does, so each Convert* handle is made as the game makes it.
    static string WriteGlobals(IEnumerable<JassGlobal> globals, string source)
    {
        var values = globals.OrderBy(global => global.Name, StringComparer.Ordinal).Select(global =>
            $"        (\"{global.Name}\", {Value(global) ?? throw new InvalidDataException($"The generator can't write the value of the global {global.Name}: {global.Value?.ToString() ?? "none"}")}),\n");
        return $"{Header(source)}\n#nullable enable\n\nnamespace MirrorImaged.Natives;\n\npublic sealed partial class GameNatives\n{{\n"
            + "    /// <summary>The globals common.j declares, sorted by name, with the values it gives them.</summary>\n"
            + $"    public (string Name, object? Value)[] Globals() =>\n    [\n{string.Concat(values)}    ];\n}}\n";
    }

    // The forms common.j's globals take: an integer, a boolean, or a native called with integers; null for any other.
    static string? Value(JassGlobal global)
    {
        if (global.Value is JassInvocationExpressionSyntax call)
        {
            var arguments = call.ArgumentList.ArgumentList.Items.Select(Integer).ToList();
            return arguments.Contains(null) ? null : $"{call.IdentifierName.Token.Text}({string.Join(", ", arguments)})";
        }
        return global.Type switch
        {
            "integer" when global.Value is { } value => Integer(value),
            "boolean" when global.Value is { } value && value.TryGetBooleanExpressionValue(out var boolean) => boolean ? "true" : "false",
            _ => null,
        };
    }

    static string? Integer(JassExpressionSyntax expression) => IntegerValue(expression)?.ToString(CultureInfo.InvariantCulture);

    // An integer literal in any of JASS's forms, or a product of them (8192*2).
    static int? IntegerValue(JassExpressionSyntax expression) => expression switch
    {
        JassBinaryExpressionSyntax { OperatorToken.SyntaxKind: JassSyntaxKind.AsteriskToken } product => unchecked(IntegerValue(product.Left) * IntegerValue(product.Right)),
        _ when expression.TryGetIntegerExpressionValue(out var value) => value,
        _ => null,
    };

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

    // Comments and strings are matched first, so a native named inside one is passed over.
    [GeneratedRegex("""
        //[^\n]*
        | /\*.*?\*/
        | @"(?:[^"]|"")*"
        | "(?:[^"\\\n]|\\.)*"
        | '(?:[^'\\\n]|\\.)*'
        | \[Native\((?:[^()"]|"(?:[^"\\\n]|\\.)*"|\([^()]*\))*\)\]\s*public\s+(?:static\s+)?[@\w<>?,]+\s+@?(?<name>\w+)\s*\(
        """, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline)]
    private static partial Regex NativeMethod();
}
