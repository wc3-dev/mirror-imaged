using War3Net.Build;
using War3Net.Build.Info;
using War3Net.IO.Mpq;

namespace MirrorImaged.Tests;

/// <summary>
/// Small maps built with War3Net in a temporary folder, deleted on dispose. They have no minimap image, which the game
/// needs and MirrorImaged doesn't.
/// </summary>
public sealed class TestMaps : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    /// <summary>
    /// A 32 by 32 map whose script is <paramref name="script"/>, in Lua or JASS, its info changed by
    /// <paramref name="change"/>, its other files by <paramref name="map"/>, and with <paramref name="files"/> added by name.
    /// </summary>
    public string Build(string script, ScriptLanguage language = ScriptLanguage.Lua, Action<MapInfo>? change = null,
        Action<Map>? map = null, IReadOnlyDictionary<string, string>? files = null)
    {
        var path = Path.Combine(_folder, Path.GetRandomFileName() + ".w3x");
        var info = MapFactory.Info(32, 32);
        info.ScriptLanguage = language;
        change?.Invoke(info);
        var built = new Map { Info = info, Environment = MapFactory.Environment(info), Script = script };
        map?.Invoke(built);
        var builder = new MapBuilder(built);
        foreach (var (name, text) in files ?? new Dictionary<string, string>())
            builder.AddFiles(MpqFile.New(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)), name));
        builder.Build(path);
        return path;
    }

    public void Dispose() => Directory.Delete(_folder, true);
}
