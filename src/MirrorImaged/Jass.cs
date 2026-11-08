using System.Collections.Immutable;
using System.Globalization;
using CSharpLua;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using MirrorImaged.World;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Extensions;
using War3Net.CodeAnalysis.Jass.Syntax;
using War3Net.CodeAnalysis.Transpilers;

namespace MirrorImaged;

/// <summary>
/// JASS made into Lua by War3Net's transpiler (decisions D65, D130): as the game's own Lua runs blizzard.j in a Lua map,
/// or with JASS's rules, as the game's JASS machine runs a JASS map and its blizzard.j (D131 to D137).
/// </summary>
static class Jass
{
    // The Lua functions transpiled JASS calls under JASS's rules, declared so the transpiler knows what they give.
    // MirrorImaged_Read gives a real to the transpiler, whatever it reads: what JASS's types change in the Lua (an
    // integer's / and a string's +) is rewritten before the transpiler sees it.
    static readonly JassCompilationUnitSyntax Helpers = JassSyntaxFactory.ParseCompilationUnit("""
        globals
            constant handle MirrorImaged_Unset = null
        endglobals
        native MirrorImaged_Divide takes integer dividend, integer divisor returns integer
        native MirrorImaged_DivideReal takes real dividend, real divisor returns real
        native MirrorImaged_Join takes string a, string b returns string
        native MirrorImaged_Equal takes real a, real b returns boolean
        native MirrorImaged_Real takes integer i returns real
        native MirrorImaged_Bits takes integer i returns real
        native MirrorImaged_Read takes real value, string name returns real
        """);

    /// <summary>
    /// The Lua of the functions JASS's rules call, run before blizzard.j in a JASS map's state;
    /// <c>MirrorImaged_Closes</c>, which closes the game, is C#'s. Each error names the JASS line calling it.
    /// </summary>
    internal const string Prelude = """
        local error, rawequal, pack, unpack, lowest, I2R, closes = error, rawequal, string.pack, string.unpack, math.mininteger, I2R, MirrorImaged_Closes
        local unset = {}
        MirrorImaged_Unset, MirrorImaged_Closes = unset, nil
        function MirrorImaged_Read(value, name)
          if rawequal(value, unset) then error(name .. " was read before it was set, which ends the thread", 2) end
          return value
        end
        function MirrorImaged_Divide(dividend, divisor)
          if divisor == 0 then error("an integer was divided by zero, which ends the thread", 2) end
          if divisor == -1 and dividend == lowest then closes("The game closes: -2147483648 was divided by -1") end
          local quotient = dividend // divisor
          if quotient < 0 and quotient * divisor ~= dividend then quotient = quotient + 1 end
          return quotient
        end
        function MirrorImaged_DivideReal(dividend, divisor)
          if divisor == 0 then error("a real was divided by zero, which ends the thread", 2) end
          return dividend / divisor
        end
        function MirrorImaged_Join(a, b)
          if a == nil then return b elseif b == nil then return a end
          return a .. b
        end
        function MirrorImaged_Equal(a, b)
          local d = a - b
          if d < 0 then d = -d end
          if d < 0.001 then return true end
          if a < 0 then a = -a end
          if b < 0 then b = -b end
          if b > a then a = b end
          local exponent = unpack('<I4', pack('<f', a)) & 0x7f800000
          return exponent > 0x0b000000 and d < unpack('<f', pack('<I4', exponent - 0x0b000000))
        end
        function MirrorImaged_Real(i)
          if i > -16777217 and i < 16777217 then return i + 0.0 end
          return I2R(i)
        end
        function MirrorImaged_Bits(i)
          return (unpack('<f', pack('<i4', i)))
        end
        """;

    /// <summary>
    /// <paramref name="jass"/> as Lua. <paramref name="declarations"/> are the files it calls into, whose types tell
    /// joining strings from adding numbers, and with <paramref name="rules"/>, integers from reals; they aren't made
    /// into Lua. With <paramref name="rules"/>, the Lua keeps JASS's rules, needing <see cref="Prelude"/>.
    /// </summary>
    internal static string ToLua(IReadOnlyList<JassCompilationUnitSyntax> declarations, string jass, bool rules)
    {
        var unit = JassSyntaxFactory.ParseCompilationUnit(jass);
        var transpiler = new JassToLuaTranspiler();
        // A map may declare a native itself, as many declare common.ai's UnitAlive, which the transpiler takes once
        var natives = unit.Declarations.OfType<JassNativeFunctionDeclarationSyntax>().Select(native => native.IdentifierName.Token.Text).ToHashSet();
        foreach (var declaration in declarations)
            transpiler.RegisterJassFile(natives.Count == 0 ? declaration : JassSyntaxFactory.CompilationUnit(declaration.Declarations.Where(d =>
                d is not JassNativeFunctionDeclarationSyntax native || !natives.Contains(native.IdentifierName.Token.Text))));
        if (rules) transpiler.RegisterJassFile(Helpers);
        var lua = new StringWriter();
        new LuaRenderer(new LuaSyntaxGenerator.SettingInfo(), lua)
            .RenderCompilationUnit(transpiler.Transpile(new Rewriter([.. declarations, unit], rules).Rewrite(unit)));
        return lua.ToString();
    }

    // What the JASS needs before War3Net transpiles it (decisions D65, D102), and with JASS's rules, what keeps them
    // (D131 to D136). It knows each variable's and function's type from the files, as the transpiler does.
    sealed class Rewriter : JassSyntaxRewriter
    {
        static readonly HashSet<string> LuaKeywords = ["break", "do", "end", "for", "goto", "in", "nil", "repeat", "until", "while"];

        // S2R, by which JASS's real literals are read; it reads no game state
        static readonly GameNatives Numbers = new(new Game(0));

        readonly bool _rules;
        readonly Dictionary<string, string> _globals = [];
        readonly Dictionary<string, (string Returns, string[] Takes)> _functions = [];
        readonly HashSet<string> _unsetGlobals = [];
        Dictionary<string, string>? _locals;
        string _returns = "nothing";

        // The variables that may not be set yet where the rewriter is: those declared without a value, until a set.
        HashSet<string> _unset;

        public Rewriter(IEnumerable<JassCompilationUnitSyntax> units, bool rules)
        {
            _rules = rules;
            foreach (var declaration in units.SelectMany(unit => unit.Declarations))
                switch (declaration)
                {
                    case JassGlobalsDeclarationSyntax globals:
                        foreach (var global in globals.GlobalDeclarations)
                            switch (global)
                            {
                                case JassGlobalConstantDeclarationSyntax constant:
                                    _globals[constant.IdentifierName.Token.Text] = Type(constant.Type);
                                    break;
                                case JassGlobalVariableDeclarationSyntax { Declarator: JassArrayDeclaratorSyntax array }:
                                    _globals[array.IdentifierName.Token.Text] = Type(array.Type);
                                    break;
                                case JassGlobalVariableDeclarationSyntax { Declarator: JassVariableDeclaratorSyntax variable }:
                                    _globals[variable.IdentifierName.Token.Text] = Type(variable.Type);
                                    if (variable.Value is null) _unsetGlobals.Add(variable.IdentifierName.Token.Text);
                                    break;
                            }
                        break;
                    case JassNativeFunctionDeclarationSyntax native:
                        _functions[native.IdentifierName.Token.Text] = (Type(native.ReturnClause.ReturnType), [.. Parameters(native.ParameterList).Select(p => p.Type)]);
                        break;
                    case JassFunctionDeclarationSyntax function:
                        var declarator = function.FunctionDeclarator;
                        _functions[declarator.IdentifierName.Token.Text] = (Type(declarator.ReturnClause.ReturnType), [.. Parameters(declarator.ParameterList).Select(p => p.Type)]);
                        break;
                }
            _unset = rules ? [.. _unsetGlobals] : [];
        }

        public JassCompilationUnitSyntax Rewrite(JassCompilationUnitSyntax jass) => RewriteCompilationUnit(jass, out var rewritten) ? rewritten : jass;

        static string Type(JassTypeSyntax type) => type.GetToken().Text;

        static IEnumerable<(string Name, string Type)> Parameters(JassParameterListOrEmptyParameterListSyntax parameters) =>
            parameters is JassParameterListSyntax list ? list.ParameterList.Items.Select(p => (p.IdentifierName.Token.Text, Type(p.Type))) : [];

        string VariableType(string name) => _locals?.GetValueOrDefault(name) ?? _globals.GetValueOrDefault(name) ?? "";

        // The JASS type of an expression, as the transpiler works it out
        string TypeOf(JassExpressionSyntax expression) => expression switch
        {
            JassLiteralExpressionSyntax literal => literal.SyntaxKind switch
            {
                JassSyntaxKind.RealLiteralExpression => "real",
                JassSyntaxKind.StringLiteralExpression => "string",
                JassSyntaxKind.TrueLiteralExpression or JassSyntaxKind.FalseLiteralExpression => "boolean",
                JassSyntaxKind.NullLiteralExpression => "null",
                _ => "integer",
            },
            JassIdentifierNameSyntax name => VariableType(name.Token.Text),
            JassElementAccessExpressionSyntax element => VariableType(element.IdentifierName.Token.Text),
            JassInvocationExpressionSyntax call => _functions.GetValueOrDefault(call.IdentifierName.Token.Text).Returns ?? "",
            JassFunctionReferenceExpressionSyntax => "code",
            JassParenthesizedExpressionSyntax parenthesized => TypeOf(parenthesized.Expression),
            JassUnaryExpressionSyntax unary => unary.SyntaxKind == JassSyntaxKind.LogicalNotExpression ? "boolean" : TypeOf(unary.Expression),
            JassBinaryExpressionSyntax binary when binary.SyntaxKind is JassSyntaxKind.AddExpression or JassSyntaxKind.SubtractExpression
                or JassSyntaxKind.MultiplyExpression or JassSyntaxKind.DivideExpression => Sum(TypeOf(binary.Left), TypeOf(binary.Right)),
            _ => "boolean",
        };

        static string Sum(string left, string right) => left == "string" || right == "string" ? "string" : left == "real" || right == "real" ? "real" : left;

        // A whole number written out (a literal, maybe signed or in parentheses), as JASS reads it; null for one past 32 bits
        static int? Whole(JassExpressionSyntax expression) => expression switch
        {
            JassLiteralExpressionSyntax literal => literal.SyntaxKind switch
            {
                JassSyntaxKind.DecimalLiteralExpression when int.TryParse(literal.Token.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var n) => n,
                JassSyntaxKind.HexadecimalLiteralExpression when uint.TryParse(literal.Token.Text[(literal.Token.Text[0] == '$' ? 1 : 2)..],
                    NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var n) => (int)n,
                JassSyntaxKind.OctalLiteralExpression when literal.Token.Text.Length <= 11 => (int)Convert.ToUInt32(literal.Token.Text, 8),
                JassSyntaxKind.CharacterLiteralExpression => JassLiteral.ParseChar(literal.Token.Text),
                JassSyntaxKind.FourCCLiteralExpression => JassLiteral.ParseFourCC(literal.Token.Text),
                _ => null,
            },
            JassParenthesizedExpressionSyntax parenthesized => Whole(parenthesized.Expression),
            JassUnaryExpressionSyntax { SyntaxKind: JassSyntaxKind.UnaryMinusExpression } unary => unchecked(-Whole(unary.Expression)),
            JassUnaryExpressionSyntax { SyntaxKind: JassSyntaxKind.UnaryPlusExpression } unary => Whole(unary.Expression),
            _ => null,
        };

        static bool NonZero(JassExpressionSyntax expression) =>
            Whole(expression) is not (null or 0)
            || expression is JassLiteralExpressionSyntax { SyntaxKind: JassSyntaxKind.RealLiteralExpression } real && JassLiteral.ParseReal(real.Token.Text) != 0;

        // A number as a JASS literal, a negative one in parentheses, since the Lua of -(-1.0) would otherwise read --1.0,
        // a comment
        static JassExpressionSyntax Number(JassSyntaxToken token) =>
            token.Text.StartsWith('-') ? JassSyntaxFactory.ParenthesizedExpression(JassSyntaxFactory.LiteralExpression(token)) : JassSyntaxFactory.LiteralExpression(token);

        static JassExpressionSyntax Call(string helper, params JassExpressionSyntax[] arguments) => JassSyntaxFactory.InvocationExpression(helper, arguments);

        // The integer as JASS gives it to a real: cut toward zero (probe jass-numbers)
        static float TowardZero(int i)
        {
            var f = (float)i;
            return Math.Abs((double)f) > Math.Abs((double)i) ? i > 0 ? MathF.BitDecrement(f) : MathF.BitIncrement(f) : f;
        }

        // The value given where a JASS type takes it: an integer where a real goes becomes a real, a whole number written
        // out at once (decisions D131), else, with JASS's rules, as JASS cuts it
        JassExpressionSyntax Converted(JassExpressionSyntax original, JassExpressionSyntax rewritten, string type)
        {
            if (type != "real" || TypeOf(original) != "integer") return rewritten;
            if (Whole(original) is { } whole) return Number(JassSyntaxFactory.Literal(TowardZero(whole), 1));
            return _rules ? Call("MirrorImaged_Real", rewritten) : rewritten;
        }

        // War3Net writes a JASS name that is a Lua keyword as it is (SubStringBJ's parameter end), which Lua can't load,
        // so it gets _lua after it; JASS refuses the plainer end_. shortcut: a JASS name end_lua beside end would clash.
        protected override bool RewriteIdentifierName(JassIdentifierNameSyntax name, out JassIdentifierNameSyntax result)
        {
            if (!LuaKeywords.Contains(name.Token.Text)) return base.RewriteIdentifierName(name, out result);
            result = JassSyntaxFactory.IdentifierName(name.Token.Text + "_lua");
            return true;
        }

        // A name the files don't declare, which the game's JASS refuses as it loads the map, where Lua would call nil
        void Declared(string name, bool function)
        {
            if (!(function ? _functions.ContainsKey(name) : _locals?.ContainsKey(name) == true || _globals.ContainsKey(name)))
                throw new InvalidDataException($"{name} is not declared");
        }

        protected override bool RewriteFunctionReferenceExpression(JassFunctionReferenceExpressionSyntax reference, out JassExpressionSyntax result)
        {
            Declared(reference.IdentifierName.Token.Text, function: true);
            return base.RewriteFunctionReferenceExpression(reference, out result);
        }

        // A variable read; with JASS's rules, one that may not be set yet is checked (probe jass-threads)
        protected override bool RewriteIdentifierNameAsExpression(JassIdentifierNameSyntax name, out JassExpressionSyntax result)
        {
            Declared(name.Token.Text, function: false);
            var renamed = RewriteIdentifierName(name, out var identifier);
            result = identifier;
            if (!_unset.Contains(name.Token.Text)) return renamed;
            result = Call("MirrorImaged_Read", identifier, JassSyntaxFactory.LiteralExpression(JassSyntaxFactory.Literal(name.Token.Text)));
            return true;
        }

        // War3Net writes a rawcode as FourCC("...") with its text as it is, so a quote in it breaks out of the string and
        // a backslash starts a Lua escape; its value is written instead (decisions D102), as the game reads it (probe
        // jass-rawcodes), a negative one in parentheses. With JASS's rules, a real is read as the game's JASS reads it,
        // and written so Lua reads that float back.
        protected override bool RewriteLiteralExpression(JassLiteralExpressionSyntax literal, out JassExpressionSyntax result)
        {
            if (literal.SyntaxKind == JassSyntaxKind.FourCCLiteralExpression)
                result = Number(JassSyntaxFactory.Literal(JassLiteral.ParseFourCC(literal.Token.Text)));
            else if (_rules && literal.SyntaxKind == JassSyntaxKind.RealLiteralExpression && Real(literal.Token.Text) is var real && float.IsFinite(real))
                result = JassSyntaxFactory.LiteralExpression(JassSyntaxFactory.Token(JassSyntaxKind.RealLiteralToken, Numeral(real)));
            else return base.RewriteLiteralExpression(literal, out result);
            return true;
        }

        // A real literal as the game's JASS reads it: its whole part and its decimals each as S2R reads them, added in the
        // game's arithmetic (probe jass-literals: 0.1 is S2R's, a float step above the nearest, and 1.001 a step below
        // S2R's)
        static float Real(string text) =>
            text.Split('.') is [var whole, var decimals] ? Native.mi_add(Numbers.S2R(whole), Numbers.S2R("." + decimals)) : Numbers.S2R(text);

        // The float's double written so Lua reads it back exactly, with a point or exponent so it reads as a float
        static string Numeral(float real) =>
            ((double)real).ToString("R", CultureInfo.InvariantCulture) is var text && (text.Contains('.') || text.Contains('E')) ? text : text + ".0";

        // A debug statement never runs (probe jass-debug), nor does what follows a return in its block, which War3Net
        // writes after Lua's return, where Lua can't load it (docs/upstream.md). A block's sets may not have happened
        // after it.
        protected override bool RewriteStatementList(ImmutableArray<JassStatementSyntax> statements, out ImmutableArray<JassStatementSyntax> result)
        {
            var unset = _unset.ToHashSet();
            var rewritten = base.RewriteStatementList(statements, out result);
            _unset = unset;
            if (!rewritten) result = statements;
            var kept = new List<JassStatementSyntax>();
            foreach (var statement in result)
            {
                if (statement is not JassDebugStatementSyntax) kept.Add(statement);
                if (statement is JassReturnStatementSyntax) break;
            }
            if (kept.Count == result.Length) return rewritten;
            result = [.. kept];
            return true;
        }

        protected override bool RewriteFunctionDeclaration(JassFunctionDeclarationSyntax function, out JassTopLevelDeclarationSyntax result)
        {
            var declarator = function.FunctionDeclarator;
            _returns = Type(declarator.ReturnClause.ReturnType);
            _locals = Parameters(declarator.ParameterList).ToDictionary(p => p.Name, p => p.Type);
            _unset.ExceptWith(_locals.Keys);
            var rewritten = base.RewriteFunctionDeclaration(function, out result);
            (_locals, _returns, _unset) = (null, "nothing", _rules ? [.. _unsetGlobals] : []);
            return rewritten;
        }

        // A variable's value given as its type takes it. With JASS's rules, one declared without a value is unset until
        // set, and reading it ends the thread (probe jass-threads), and a string array is a table whose elements read null
        // until set, as JASS's do, where War3Net's would read "".
        protected override bool RewriteVariableOrArrayDeclarator(JassVariableOrArrayDeclaratorSyntax declarator, out JassVariableOrArrayDeclaratorSyntax result)
        {
            var rewritten = base.RewriteVariableOrArrayDeclarator(declarator, out result);
            switch (declarator, result)
            {
                case (JassArrayDeclaratorSyntax array, JassArrayDeclaratorSyntax renamed):
                    if (_locals is not null) _locals[array.IdentifierName.Token.Text] = Type(array.Type);
                    if (!_rules || Type(array.Type) != "string") return rewritten;
                    result = JassSyntaxFactory.ArrayDeclarator(JassPredefinedTypeSyntax.Handle, renamed.IdentifierName);
                    return true;
                case (JassVariableDeclaratorSyntax variable, JassVariableDeclaratorSyntax renamed):
                    var name = variable.IdentifierName.Token.Text;
                    if (_locals is not null) _locals[name] = Type(variable.Type);
                    if (variable.Value is null)
                    {
                        if (!_rules) return rewritten;
                        _unset.Add(name);
                        result = JassSyntaxFactory.VariableDeclarator(renamed.Type, renamed.IdentifierName, JassSyntaxFactory.IdentifierName("MirrorImaged_Unset"));
                        return true;
                    }
                    _unset.Remove(name);
                    var value = Converted(variable.Value.Expression, renamed.Value!.Expression, Type(variable.Type));
                    if (ReferenceEquals(value, renamed.Value.Expression)) return rewritten;
                    result = JassSyntaxFactory.VariableDeclarator(renamed.Type, renamed.IdentifierName, value);
                    return true;
            }
            return rewritten;
        }

        protected override bool RewriteGlobalConstantDeclaration(JassGlobalConstantDeclarationSyntax constant, out JassGlobalDeclarationSyntax result)
        {
            var rewritten = base.RewriteGlobalConstantDeclaration(constant, out result);
            var renamed = (JassGlobalConstantDeclarationSyntax)result;
            var value = Converted(constant.Value.Expression, renamed.Value.Expression, Type(constant.Type));
            if (ReferenceEquals(value, renamed.Value.Expression)) return rewritten;
            result = JassSyntaxFactory.GlobalConstantDeclaration(renamed.Type, renamed.IdentifierName, value);
            return true;
        }

        protected override bool RewriteSetStatement(JassSetStatementSyntax set, out JassStatementSyntax result)
        {
            var rewritten = base.RewriteSetStatement(set, out result);
            var renamed = (JassSetStatementSyntax)result;
            if (set.ElementAccessClause is null) _unset.Remove(set.IdentifierName.Token.Text);
            var value = Converted(set.Value.Expression, renamed.Value.Expression, VariableType(set.IdentifierName.Token.Text));
            if (ReferenceEquals(value, renamed.Value.Expression)) return rewritten;
            result = renamed.ElementAccessClause is { } element
                ? JassSyntaxFactory.SetStatement(renamed.IdentifierName, element, value)
                : JassSyntaxFactory.SetStatement(renamed.IdentifierName, value);
            return true;
        }

        // A function returning real that returns an integer gives the integer's bits as a real (probes jass-numbers and
        // jass-reals); the game's own Lua is taken to give the number, as a real if written out.
        protected override bool RewriteReturnStatement(JassReturnStatementSyntax statement, out JassStatementSyntax result)
        {
            var rewritten = base.RewriteReturnStatement(statement, out result);
            if (statement.Value is null || _returns != "real" || TypeOf(statement.Value) != "integer") return rewritten;
            var value = ((JassReturnStatementSyntax)result).Value!;
            var returned = _rules ? Call("MirrorImaged_Bits", value) : Converted(statement.Value, value, "real");
            if (ReferenceEquals(returned, value)) return rewritten;
            result = JassSyntaxFactory.ReturnStatement(returned);
            return true;
        }

        protected override bool RewriteInvocationExpression(JassInvocationExpressionSyntax invocation, out JassExpressionSyntax result)
        {
            Declared(invocation.IdentifierName.Token.Text, function: true);
            var rewritten = base.RewriteInvocationExpression(invocation, out result);
            var renamed = (JassInvocationExpressionSyntax)result;
            if (!Arguments(invocation.IdentifierName, invocation.ArgumentList, renamed.ArgumentList, out var arguments)) return rewritten;
            result = JassSyntaxFactory.InvocationExpression(renamed.IdentifierName, arguments);
            return true;
        }

        protected override bool RewriteCallStatement(JassCallStatementSyntax call, out JassStatementSyntax result)
        {
            Declared(call.IdentifierName.Token.Text, function: true);
            var rewritten = base.RewriteCallStatement(call, out result);
            var renamed = (JassCallStatementSyntax)result;
            if (!Arguments(call.IdentifierName, call.ArgumentList, renamed.ArgumentList, out var arguments)) return rewritten;
            result = JassSyntaxFactory.CallStatement(renamed.IdentifierName, arguments);
            return true;
        }

        // Each argument given as its parameter's type takes it; false when none changes
        bool Arguments(JassIdentifierNameSyntax function, JassArgumentListSyntax original, JassArgumentListSyntax rewritten, out JassExpressionSyntax[] arguments)
        {
            var takes = _functions.GetValueOrDefault(function.Token.Text).Takes ?? [];
            var given = rewritten.ArgumentList.Items;
            arguments = [.. original.ArgumentList.Items.Select((argument, i) => i < takes.Length ? Converted(argument, given[i], takes[i]) : given[i])];
            return arguments.Where((argument, i) => !ReferenceEquals(argument, given[i])).Any();
        }

        // With JASS's rules: an integer meeting a real is cut toward zero to one first, where Lua rounds it (probe
        // jass-numbers); an integer's / cuts toward zero, where Lua's // floors, and by zero ends the thread; a real's /
        // by zero ends it too (probes jass-divide-integer and jass-divide-real); a string's + takes null as "" (null +
        // null is null; probe jass-threads); and == on reals holds within a margin (probes jass-reals and jass-equal),
        // where != compares exactly.
        protected override bool RewriteBinaryExpression(JassBinaryExpressionSyntax binary, out JassExpressionSyntax result)
        {
            var rewritten = base.RewriteBinaryExpression(binary, out result);
            if (!_rules) return rewritten;
            string left = TypeOf(binary.Left), right = TypeOf(binary.Right);
            var operands = (JassBinaryExpressionSyntax)result;
            var (first, second) = (left, right) switch
            {
                ("integer", "real") => (Converted(binary.Left, operands.Left, "real"), operands.Right),
                ("real", "integer") => (operands.Left, Converted(binary.Right, operands.Right, "real")),
                _ => (operands.Left, operands.Right),
            };
            var helper = binary.SyntaxKind switch
            {
                JassSyntaxKind.DivideExpression when left == "integer" && right == "integer" => "MirrorImaged_Divide",
                JassSyntaxKind.DivideExpression when !NonZero(binary.Right) => "MirrorImaged_DivideReal",
                JassSyntaxKind.AddExpression when (left == "string" || right == "string")
                    && !(binary.Left is JassLiteralExpressionSyntax && binary.Right is JassLiteralExpressionSyntax) => "MirrorImaged_Join",
                JassSyntaxKind.EqualsExpression when left is "real" or "integer" && right is "real" or "integer" && (left == "real" || right == "real") => "MirrorImaged_Equal",
                _ => null,
            };
            if (helper is not null) result = Call(helper, first, second);
            else if (!ReferenceEquals(first, operands.Left) || !ReferenceEquals(second, operands.Right))
                result = JassSyntaxFactory.BinaryExpression(first, operands.OperatorToken, second);
            else return rewritten;
            return true;
        }
    }
}
