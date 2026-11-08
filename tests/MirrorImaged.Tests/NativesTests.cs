using System.Reflection;
using MirrorImaged.Data;
using MirrorImaged.Natives;
using MirrorImaged.NativesGenerator;

namespace MirrorImaged.Tests;

public sealed class NativesTests
{
    static readonly MethodInfo[] All = typeof(GameNatives).GetMethods()
        .Where(method => method.GetCustomAttribute<NativeAttribute>() is not null)
        .ToArray();

    [Fact]
    public void Every_missing_native_throws_an_exception_that_names_it()
    {
        var natives = new GameNatives();
        foreach (var method in All.Where(method => method.GetCustomAttribute<NativeAttribute>()!.Level == Level.Missing))
        {
            var arguments = method.GetParameters()
                .Select(parameter => parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null)
                .ToArray();

            var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(natives, arguments));

            var missing = Assert.IsType<MissingNativeException>(error.InnerException);
            Assert.Equal(method.Name, missing.Native);
            Assert.Contains(method.Name, missing.Message);
        }
    }

    [Fact]
    public void Every_recorded_native_names_why_and_every_exact_one_names_what_measures_it()
    {
        foreach (var method in All)
        {
            var native = method.GetCustomAttribute<NativeAttribute>()!;
            if (native.Level == Level.Recorded)
                Assert.False(string.IsNullOrWhiteSpace(native.Reason), $"{method.Name} is Recorded with no reason");
            if (native.Level == Level.Exact)
                Assert.False(string.IsNullOrWhiteSpace(native.Fact), $"{method.Name} is Exact with no fact or probe");
        }
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void The_natives_and_handle_types_match_the_installed_common_j_and_common_ai_one_to_one()
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var scripts = Generator.Scripts.Select(script => Generator.Read(Generator.Text(storage.Read, script.Path))).ToList();

        Assert.Equal(
            scripts.SelectMany(script => script.Natives).Select(Signature).Order(StringComparer.Ordinal),
            All.Select(Signature).Order(StringComparer.Ordinal));
        foreach (var type in scripts.SelectMany(script => script.Types))
            Assert.Equal(type.Parent, typeof(handle).Assembly.GetType($"MirrorImaged.Natives.{type.Name}")?.BaseType?.Name);
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Generating_from_the_install_gives_the_committed_files()
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var folder = Path.Combine(RepoRoot(), "src", "MirrorImaged.Natives");

        var files = Generator.Generate(storage.Read, storage.Build.Version, Generator.HandWritten(folder));

        var generated = Path.Combine(folder, "Generated");
        Assert.Equal(files.Keys, Directory.GetFiles(generated).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        foreach (var (name, text) in files)
            Assert.True(text == File.ReadAllText(Path.Combine(generated, name)).ReplaceLineEndings("\n"), $"{name} is not what the generator writes");
    }

    static string Signature(JassNative native) =>
        $"{native.Name}({string.Join(", ", native.Parameters.Select(parameter => $"{parameter.Type} {parameter.Name}"))}) {native.Returns}";

    static string Signature(MethodInfo method) =>
        $"{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => $"{Jass(parameter.ParameterType)} {parameter.Name}"))}) {Jass(method.ReturnType)}";

    static string Jass(Type type) =>
        type == typeof(void) ? "nothing"
        : type == typeof(int) ? "integer"
        : type == typeof(float) ? "real"
        : type == typeof(bool) ? "boolean"
        : type == typeof(string) ? "string"
        : type.IsSubclassOf(typeof(Delegate)) ? "code"
        : type.Name;

    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "MirrorImaged.slnx"))) dir = dir.Parent!;
        return dir.FullName;
    }
}
