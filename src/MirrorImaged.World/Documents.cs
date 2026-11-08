namespace MirrorImaged.World;

/// <summary>
/// A simulated <c>Documents\Warcraft III</c>: the files in <c>CustomMapData</c>, which maps write with Preload and read
/// with <c>Preloader</c>, and the game caches they save. A run reads it and writes to it; give the same one to the next
/// run to keep them, as the game keeps them on disk. It isn't thread-safe: give it to one run at a time.
/// </summary>
public sealed class Documents
{
    /// <summary>
    /// The files in <c>CustomMapData</c>, by their path in it with <c>\</c> between folders (<c>MyMap\save.pld</c>), in
    /// any case, as Windows finds them. A test can read them, add its own and remove them.
    /// </summary>
    public Dictionary<string, byte[]> CustomMapData { get; } = new(StringComparer.OrdinalIgnoreCase);

    // The game caches as SaveGameCache last wrote them (the account's Campaigns.w3v), by name: a game starts with them.
    internal Dictionary<string, Dictionary<(char Type, string Mission, string Key), object>> GameCaches { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}
