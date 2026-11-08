using War3Net.Build;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>
/// Small maps built with War3Net in a temporary folder, deleted on dispose. They have no minimap image, which the game
/// needs and MirrorImaged doesn't.
/// </summary>
public sealed class TestMaps : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    /// <summary>A 32 by 32 map whose script is <paramref name="script"/>, in Lua or JASS, its info changed by <paramref name="change"/>.</summary>
    public string Build(string script, ScriptLanguage language = ScriptLanguage.Lua, Action<MapInfo>? change = null)
    {
        var path = Path.Combine(_folder, Path.GetRandomFileName() + ".w3x");
        var info = MapFactory.Info(32, 32);
        info.ScriptLanguage = language;
        change?.Invoke(info);
        new MapBuilder(new Map { Info = info, Environment = MapFactory.Environment(info), Script = script }).Build(path);
        return path;
    }

    public void Dispose() => Directory.Delete(_folder, true);
}
