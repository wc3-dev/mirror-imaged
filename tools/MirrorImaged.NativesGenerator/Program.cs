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

string folder;
try
{
    folder = GameInstall.Require(args.Length == 3 ? args[2] : null);
}
catch (DirectoryNotFoundException missing)
{
    Console.Error.WriteLine(missing.Message);
    return 1;
}

using var storage = CascStorage.Open(folder);
var natives = Path.Combine(args[0], "src", "MirrorImaged.Natives");
var handWritten = Generator.HandWritten(natives);
var files = Generator.Generate(storage.Read, storage.Build.Version, handWritten);

var generated = Directory.CreateDirectory(Path.Combine(natives, "Generated")).FullName;
foreach (var (name, text) in files)
    File.WriteAllText(Path.Combine(generated, name), text);

foreach (var (name, _, path) in Generator.Scripts)
{
    var script = Generator.Read(Generator.Text(storage.Read, path));
    Console.WriteLine($"{name}: {script.Types.Count} types, {script.Natives.Count} natives, {script.Natives.Count(native => handWritten.Contains(native.Name))} of them written by hand");
}

return 0;
