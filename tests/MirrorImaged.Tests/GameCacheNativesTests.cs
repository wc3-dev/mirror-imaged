using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>The game cache natives, against the files probe (docs/facts.md, "Files and game cache").</summary>
public sealed class GameCacheNativesTests
{
    readonly Documents _documents = new();
    readonly GameNatives _natives;

    public GameCacheNativesTests() => _natives = NewGame();

    GameNatives NewGame() => new(new Game(0)) { Documents = _documents };

    // facts: InitGameCache makes a new handle every call
    [Fact]
    public void Caches_of_one_name_are_one_cache_through_new_handles()
    {
        var first = _natives.InitGameCache("files-probe.w3v")!;
        _natives.StoreInteger(first, "m", "k", 5);
        var second = _natives.InitGameCache("files-probe.w3v")!;

        Assert.NotSame(first, second);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(5, _natives.GetStoredInteger(second, "m", "k"));
        Assert.Equal(0, _natives.GetStoredInteger(_natives.InitGameCache("files-probe-other.w3v"), "m", "k"));
    }

    [Fact]
    public void A_missing_value_reads_0_false_and_an_empty_string()
    {
        var cache = _natives.InitGameCache("c.w3v");

        Assert.Equal(0, _natives.GetStoredInteger(cache, "m", "none"));
        Assert.Equal(0f, _natives.GetStoredReal(cache, "m", "none"));
        Assert.False(_natives.GetStoredBoolean(cache, "m", "none"));
        Assert.Equal("", _natives.GetStoredString(cache, "m", "none"));
        Assert.False(_natives.HaveStoredInteger(cache, "m", "none"));
    }

    [Fact]
    public void Each_type_has_its_own_values()
    {
        var cache = _natives.InitGameCache("c.w3v");
        _natives.StoreInteger(cache, "m", "k", 5);
        _natives.StoreReal(cache, "m", "k", 1.5f);

        Assert.Equal(5, _natives.GetStoredInteger(cache, "m", "k"));
        Assert.Equal(1.5f, _natives.GetStoredReal(cache, "m", "k"));
        Assert.Equal([true, true, false, false],
            [_natives.HaveStoredInteger(cache, "m", "k"), _natives.HaveStoredReal(cache, "m", "k"),
             _natives.HaveStoredBoolean(cache, "m", "k"), _natives.HaveStoredString(cache, "m", "k")]);

        _natives.FlushStoredInteger(cache, "m", "k");

        Assert.False(_natives.HaveStoredInteger(cache, "m", "k"));
        Assert.True(_natives.HaveStoredReal(cache, "m", "k"));
    }

    [Fact]
    public void Each_flush_takes_its_own_type()
    {
        var cache = _natives.InitGameCache("c.w3v");
        _natives.StoreReal(cache, "m", "k", 1);
        _natives.StoreBoolean(cache, "m", "k", true);
        _natives.StoreString(cache, "m", "k", "s");

        _natives.FlushStoredReal(cache, "m", "k");
        _natives.FlushStoredBoolean(cache, "m", "k");

        Assert.False(_natives.HaveStoredReal(cache, "m", "k"));
        Assert.False(_natives.HaveStoredBoolean(cache, "m", "k"));
        Assert.True(_natives.HaveStoredString(cache, "m", "k"));
        _natives.FlushStoredString(cache, "m", "k");
        Assert.False(_natives.HaveStoredString(cache, "m", "k"));
    }

    [Fact]
    public void Empty_false_and_0_are_stored_values()
    {
        var cache = _natives.InitGameCache("c.w3v");

        Assert.True(_natives.StoreString(cache, "m", "s", "text"));
        Assert.True(_natives.StoreString(cache, "m", "e", ""));
        _natives.StoreBoolean(cache, "m", "b", false);
        _natives.StoreInteger(cache, "m", "zero", 0);

        Assert.Equal("text", _natives.GetStoredString(cache, "m", "s"));
        Assert.True(_natives.HaveStoredString(cache, "m", "e"));
        Assert.True(_natives.HaveStoredBoolean(cache, "m", "b"));
        Assert.True(_natives.HaveStoredInteger(cache, "m", "zero"));
    }

    [Fact]
    public void Missions_and_keys_are_found_in_any_case()
    {
        var cache = _natives.InitGameCache("c.w3v");
        _natives.StoreInteger(cache, "Case", "Key", 3);

        Assert.Equal(3, _natives.GetStoredInteger(cache, "case", "key"));
        Assert.Equal(3, _natives.GetStoredInteger(cache, "CASE", "KEY"));
    }

    [Fact]
    public void Flushing_a_mission_takes_all_its_values_and_no_others()
    {
        var cache = _natives.InitGameCache("c.w3v");
        _natives.StoreReal(cache, "m", "k", 1.5f);
        _natives.StoreString(cache, "m", "s", "text");
        _natives.StoreBoolean(cache, "m", "b", false);
        _natives.StoreInteger(cache, "Case", "Key", 3);

        _natives.FlushStoredMission(cache, "M");

        Assert.False(_natives.HaveStoredReal(cache, "m", "k"));
        Assert.False(_natives.HaveStoredString(cache, "m", "s"));
        Assert.False(_natives.HaveStoredBoolean(cache, "m", "b"));
        Assert.Equal(3, _natives.GetStoredInteger(cache, "Case", "Key"));
    }

    // The files probe: after FlushGameCache its handle reads nothing and stores nothing, even once the name is made
    // again; a new InitGameCache of the name is an empty cache that works.
    [Fact]
    public void A_flushed_cache_is_gone()
    {
        var cache = _natives.InitGameCache("files-probe.w3v");
        _natives.StoreInteger(cache, "Case", "Key", 3);

        _natives.FlushGameCache(cache);

        Assert.Equal(0, _natives.GetStoredInteger(cache, "Case", "Key"));
        Assert.Equal(0, _natives.GetStoredInteger(_natives.InitGameCache("files-probe.w3v"), "Case", "Key"));
        _natives.StoreInteger(cache, "m", "after", 9);
        _natives.SyncStoredInteger(cache, "m", "after");
        Assert.Equal(0, _natives.GetStoredInteger(cache, "m", "after"));
        Assert.Equal(0, _natives.GetStoredInteger(_natives.InitGameCache("files-probe.w3v"), "m", "after"));
        _natives.StoreInteger(_natives.InitGameCache("files-probe.w3v"), "pre", "k", 42);
        Assert.Equal(42, _natives.GetStoredInteger(_natives.InitGameCache("files-probe.w3v"), "pre", "k"));
    }

    // The files probe: SaveGameCache and ReloadGameCachesFromDisk give true; the reload brings back what was saved, the
    // old handles included, and loses what wasn't, a cache never saved too.
    [Fact]
    public void A_reload_brings_back_the_saved_caches()
    {
        var saved = _natives.InitGameCache("files-probe-save.w3v");
        _natives.StoreInteger(saved, "m", "saved", 1);
        Assert.True(_natives.SaveGameCache(saved));
        _natives.StoreInteger(saved, "m", "unsaved", 2);

        Assert.True(_natives.ReloadGameCachesFromDisk());

        Assert.Equal(1, _natives.GetStoredInteger(saved, "m", "saved"));
        Assert.Equal(0, _natives.GetStoredInteger(saved, "m", "unsaved"));
        Assert.Equal(1, _natives.GetStoredInteger(_natives.InitGameCache("files-probe-save.w3v"), "m", "saved"));
        var never = _natives.InitGameCache("files-probe-never-saved.w3v");
        _natives.StoreInteger(never, "m", "k", 3);
        _natives.ReloadGameCachesFromDisk();
        Assert.Equal(0, _natives.GetStoredInteger(never, "m", "k"));
    }

    // The files probe: the next game starts with the caches saved, every cache in memory then, not only the one named;
    // what came after the last save isn't there.
    [Fact]
    public void The_next_game_starts_with_the_saved_caches()
    {
        _natives.StoreInteger(_natives.InitGameCache("other.w3v"), "m", "k", 7);
        var saved = _natives.InitGameCache("files-probe-save.w3v");
        _natives.StoreInteger(saved, "m", "saved", 1);
        _natives.SaveGameCache(saved);
        _natives.StoreInteger(saved, "m", "unsaved", 2);

        var next = NewGame();

        Assert.Equal(1, next.GetStoredInteger(next.InitGameCache("files-probe-save.w3v"), "m", "saved"));
        Assert.Equal(0, next.GetStoredInteger(next.InitGameCache("files-probe-save.w3v"), "m", "unsaved"));
        Assert.Equal(7, next.GetStoredInteger(next.InitGameCache("other.w3v"), "m", "k"));
    }

    [Fact]
    public void A_null_cache_reads_nothing_and_saves_nothing()
    {
        _natives.StoreInteger(null, "m", "k", 1);

        Assert.Equal(0, _natives.GetStoredInteger(null, "m", "k"));
        Assert.Equal("", _natives.GetStoredString(null, "m", "k"));
        Assert.False(_natives.StoreString(null, "m", "k", "s"));
        Assert.False(_natives.SaveGameCache(null));
    }
}
