using MirrorImaged.Data;
using MirrorImaged.NativesGenerator;

if (args is not ([_] or [_, "--game", _]))
{
    Console.Error.WriteLine("""
        usage: MirrorImaged.NativesGenerator <repository root> [--game <game folder>]

        Writes src/MirrorImaged.Natives/Generated from the install's common.j and common.ai. Without --game, the install
        is the folder MIRROR_GAME names, else the usual folder.
        """);
    return 2;
}

CascStorage storage;
try
{
    storage = CascStorage.Open(GameInstall.Require(args.Length == 3 ? args[2] : null));
}
catch (IOException error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

using (storage)
{
    var natives = Path.Combine(args[0], "src", "MirrorImaged.Natives");
    var handWritten = Generator.HandWritten(natives);
    var scripts = Generator.ReadScripts(storage.Read);

    var generated = Directory.CreateDirectory(Path.Combine(natives, "Generated")).FullName;
    foreach (var (name, text) in Generator.Generate(scripts, storage.Build.Version, handWritten))
        File.WriteAllText(Path.Combine(generated, name), text);

    foreach (var ((name, _, _), script) in Generator.Scripts.Zip(scripts))
        Console.WriteLine($"{name}: {script.Types.Count} types, {script.Natives.Count} natives, {script.Natives.Count(native => handWritten.Contains(native.Name))} of them written by hand");
}

return 0;
