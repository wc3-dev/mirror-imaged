namespace MirrorImaged.Natives;

public partial class gamecache
{
    internal string Name = "";
    internal Dictionary<(char Type, string Mission, string Key), object> Values = [];
}

// Game caches, as the files probe measured them (decisions D103, D104): one cache a name, which every InitGameCache of
// it gives a new handle to; each type's values apart, missions and keys in any case; a game starts with the caches
// saved in its documents, SaveGameCache writes them all, and ReloadGameCachesFromDisk puts the saved ones back in
// place. A handle whose cache was flushed, or dropped by a reload, reads and stores nothing. The unit natives wait
// for the units (wave 2).
public sealed partial class GameNatives
{
    const string CacheFact = "docs/facts.md, Files and game cache (the files probe, 11-08)";

    // The game's caches, taken from the documents' saved ones as the game starts (Documents).
    Dictionary<string, Dictionary<(char, string, string), object>> _caches = new(StringComparer.OrdinalIgnoreCase);

    static Dictionary<string, Dictionary<(char, string, string), object>> CopyCaches(Dictionary<string, Dictionary<(char Type, string Mission, string Key), object>> caches) =>
        new(caches.Select(cache => KeyValuePair.Create(cache.Key, new Dictionary<(char, string, string), object>(cache.Value))), StringComparer.OrdinalIgnoreCase);

    [Native(Level.Exact, Fact = "game-facts.md, Handles and identity: InitGameCache makes a new handle every call (10-01); " + CacheFact)]
    public gamecache? InitGameCache(string? campaignFile)
    {
        var name = campaignFile ?? "";
        if (!_caches.TryGetValue(name, out var values)) _caches[name] = values = [];
        return Make(new gamecache { Name = name, Values = values });
    }

    /// <summary>
    /// Writes every cache, not only <paramref name="whichCache"/>, to the documents, for the next game, and gives true;
    /// false for null (not measured). Close: measured in one run, which a probe can't repeat (it rewrites the account's
    /// saved caches).
    /// </summary>
    [Native(Level.Close)]
    public bool SaveGameCache(gamecache? whichCache)
    {
        if (whichCache is null) return false;
        Documents.GameCaches.Clear();
        foreach (var (name, values) in _caches) Documents.GameCaches[name] = new(values);
        return true;
    }

    /// <summary>
    /// The caches as last saved, each in place, so its handles read it; a cache never saved is gone. True, as in a live
    /// game. Close, as <see cref="SaveGameCache"/>.
    /// </summary>
    [Native(Level.Close)]
    public bool ReloadGameCachesFromDisk()
    {
        foreach (var name in _caches.Keys.Where(name => !Documents.GameCaches.ContainsKey(name)).ToList()) _caches.Remove(name);
        foreach (var (name, saved) in Documents.GameCaches)
        {
            if (_caches.TryGetValue(name, out var values)) values.Clear();
            else _caches[name] = values = [];
            foreach (var (key, value) in saved) values[key] = value;
        }
        return true;
    }

    [Native(Level.Exact, Fact = CacheFact)]
    public void FlushGameCache(gamecache? cache)
    {
        if (CacheLive(cache)) _caches.Remove(cache!.Name);
    }

    [Native(Level.Exact, Fact = CacheFact)]
    public void FlushStoredMission(gamecache? cache, string? missionKey)
    {
        if (!CacheLive(cache)) return;
        var mission = CacheKey(missionKey);
        foreach (var key in cache!.Values.Keys.Where(key => key.Mission == mission).ToList()) cache.Values.Remove(key);
    }

    [Native(Level.Exact, Fact = CacheFact)]
    public void StoreInteger(gamecache? cache, string? missionKey, string? key, int value) => CacheStore(cache, 'i', missionKey, key, value);

    [Native(Level.Exact, Fact = CacheFact)]
    public void StoreReal(gamecache? cache, string? missionKey, string? key, float value) => CacheStore(cache, 'r', missionKey, key, value);

    [Native(Level.Exact, Fact = CacheFact)]
    public void StoreBoolean(gamecache? cache, string? missionKey, string? key, bool value) => CacheStore(cache, 'b', missionKey, key, value);

    /// <summary>True once stored, an empty string too; false where nothing is stored (not measured).</summary>
    [Native(Level.Exact, Fact = CacheFact)]
    public bool StoreString(gamecache? cache, string? missionKey, string? key, string? value) => CacheStore(cache, 's', missionKey, key, value ?? "");

    [Native(Level.Exact, Fact = CacheFact)]
    public int GetStoredInteger(gamecache? cache, string? missionKey, string? key) => (int)(CacheGet(cache, 'i', missionKey, key) ?? 0);

    [Native(Level.Exact, Fact = CacheFact)]
    public float GetStoredReal(gamecache? cache, string? missionKey, string? key) => (float)(CacheGet(cache, 'r', missionKey, key) ?? 0f);

    [Native(Level.Exact, Fact = CacheFact)]
    public bool GetStoredBoolean(gamecache? cache, string? missionKey, string? key) => (bool)(CacheGet(cache, 'b', missionKey, key) ?? false);

    [Native(Level.Exact, Fact = CacheFact)]
    public string? GetStoredString(gamecache? cache, string? missionKey, string? key) => (string)(CacheGet(cache, 's', missionKey, key) ?? "");

    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredInteger(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 'i', missionKey, key) is not null;

    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredReal(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 'r', missionKey, key) is not null;

    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredBoolean(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 'b', missionKey, key) is not null;

    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredString(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 's', missionKey, key) is not null;

    [Native(Level.Exact, Fact = CacheFact)]
    public void FlushStoredInteger(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 'i', missionKey, key);

    /// <summary>As <see cref="FlushStoredInteger"/>, the one measured.</summary>
    [Native(Level.Close)]
    public void FlushStoredReal(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 'r', missionKey, key);

    /// <summary>As <see cref="FlushStoredInteger"/>, the one measured.</summary>
    [Native(Level.Close)]
    public void FlushStoredBoolean(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 'b', missionKey, key);

    /// <summary>As <see cref="FlushStoredInteger"/>, the one measured.</summary>
    [Native(Level.Close)]
    public void FlushStoredString(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 's', missionKey, key);

    // Syncing sends the local player's value to the others; a run has one machine, whose caches are every player's.
    [Native(Level.Close)]
    public void SyncStoredInteger(gamecache? cache, string? missionKey, string? key) { }

    [Native(Level.Close)]
    public void SyncStoredReal(gamecache? cache, string? missionKey, string? key) { }

    [Native(Level.Close)]
    public void SyncStoredBoolean(gamecache? cache, string? missionKey, string? key) { }

    [Native(Level.Close)]
    public void SyncStoredString(gamecache? cache, string? missionKey, string? key) { }

    bool CacheLive(gamecache? cache) => cache is not null && _caches.TryGetValue(cache.Name, out var values) && values == cache.Values;

    // The game finds missions and keys in any case (measured with ASCII letters).
    static string CacheKey(string? key) => (key ?? "").ToUpperInvariant();

    bool CacheStore(gamecache? cache, char type, string? missionKey, string? key, object value)
    {
        if (!CacheLive(cache)) return false;
        cache!.Values[(type, CacheKey(missionKey), CacheKey(key))] = value;
        return true;
    }

    object? CacheGet(gamecache? cache, char type, string? missionKey, string? key) =>
        CacheLive(cache) ? cache!.Values.GetValueOrDefault((type, CacheKey(missionKey), CacheKey(key))) : null;

    void CacheFlush(gamecache? cache, char type, string? missionKey, string? key)
    {
        if (CacheLive(cache)) cache!.Values.Remove((type, CacheKey(missionKey), CacheKey(key)));
    }
}
