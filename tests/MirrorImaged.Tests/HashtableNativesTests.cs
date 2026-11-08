using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>The hashtable natives against the probes hashtables and strings-2 (docs/facts.md, "Strings, numbers and hashtables").</summary>
public sealed class HashtableNativesTests : IDisposable
{
    readonly GameNatives _natives = new(new Game(0));
    readonly hashtable _table;
    readonly TestMaps _maps = new();

    public HashtableNativesTests() => _table = _natives.InitHashtable()!;

    public void Dispose() => _maps.Dispose();

    [Fact]
    public void A_missing_value_loads_as_0_false_an_empty_string_or_null()
    {
        Assert.Equal(0, _natives.LoadInteger(_table, 1, 1));
        Assert.Equal(0f, _natives.LoadReal(_table, 1, 1));
        Assert.False(_natives.LoadBoolean(_table, 1, 1));
        Assert.Equal("", _natives.LoadStr(_table, 1, 1));
        Assert.Null(_natives.LoadTimerHandle(_table, 1, 1));
        Assert.False(_natives.HaveSavedInteger(_table, 1, 1));
        Assert.False(_natives.HaveSavedHandle(_table, 1, 1));
    }

    [Fact]
    public void A_key_holds_one_value_of_each_kind_apart()
    {
        _natives.SaveInteger(_table, 1, 1, 5);
        _natives.SaveReal(_table, 1, 1, 2.5f);
        _natives.SaveBoolean(_table, 1, 1, true);
        Assert.True(_natives.SaveStr(_table, 1, 1, "x"));

        Assert.Equal(5, _natives.LoadInteger(_table, 1, 1));
        Assert.Equal(2.5f, _natives.LoadReal(_table, 1, 1));
        Assert.True(_natives.LoadBoolean(_table, 1, 1));
        Assert.Equal("x", _natives.LoadStr(_table, 1, 1));
        Assert.False(_natives.HaveSavedHandle(_table, 1, 1));

        _natives.RemoveSavedInteger(_table, 1, 1);

        Assert.False(_natives.HaveSavedInteger(_table, 1, 1));
        Assert.Equal(0, _natives.LoadInteger(_table, 1, 1));
        Assert.True(_natives.HaveSavedReal(_table, 1, 1));
        Assert.Equal(2.5f, _natives.LoadReal(_table, 1, 1));
    }

    [Fact]
    public void False_0_and_an_empty_string_are_saved_values()
    {
        _natives.SaveBoolean(_table, 1, 3, false);
        _natives.SaveInteger(_table, 1, 4, 0);
        Assert.True(_natives.SaveStr(_table, 7, 7, ""));

        Assert.True(_natives.HaveSavedBoolean(_table, 1, 3));
        Assert.True(_natives.HaveSavedInteger(_table, 1, 4));
        Assert.True(_natives.HaveSavedString(_table, 7, 7));
        Assert.Equal("", _natives.LoadStr(_table, 7, 7));
    }

    [Fact]
    public void A_key_holds_one_handle_whatever_its_type_and_loads_it_only_as_its_type()
    {
        var timer = _natives.CreateTimer()!;
        var group = _natives.CreateGroup()!;

        Assert.True(_natives.SaveTimerHandle(_table, 3, 3, timer));
        Assert.Same(timer, _natives.LoadTimerHandle(_table, 3, 3));
        Assert.Null(_natives.LoadGroupHandle(_table, 3, 3));
        Assert.Null(_natives.LoadWidgetHandle(_table, 3, 3));
        Assert.True(_natives.HaveSavedHandle(_table, 3, 3));
        Assert.False(_natives.HaveSavedInteger(_table, 3, 3));

        Assert.True(_natives.SaveGroupHandle(_table, 3, 3, group));
        Assert.Null(_natives.LoadTimerHandle(_table, 3, 3));
        Assert.Same(group, _natives.LoadGroupHandle(_table, 3, 3));

        Assert.True(_natives.SaveAgentHandle(_table, 4, 4, timer));
        Assert.Same(timer, _natives.LoadTimerHandle(_table, 4, 4));
        _natives.RemoveSavedHandle(_table, 4, 4);
        Assert.False(_natives.HaveSavedHandle(_table, 4, 4));

        Assert.True(_natives.SavePlayerHandle(_table, 5, 5, _natives.Player(0)));
        Assert.Same(_natives.Player(0), _natives.LoadPlayerHandle(_table, 5, 5));
    }

    [Fact]
    public void Saving_a_null_or_destroyed_handle_saves_nothing_and_gives_false()
    {
        var group = _natives.CreateGroup()!;
        var timer = _natives.CreateTimer()!;
        _natives.SaveGroupHandle(_table, 3, 3, group);
        _natives.DestroyTimer(timer);

        Assert.False(_natives.SaveTimerHandle(_table, 3, 3, null));
        Assert.Same(group, _natives.LoadGroupHandle(_table, 3, 3));
        Assert.False(_natives.SaveTimerHandle(_table, 2, 2, timer));
        Assert.False(_natives.HaveSavedHandle(_table, 2, 2));
    }

    [Fact]
    public void A_handle_destroyed_after_its_save_loads_as_null_but_is_still_had()
    {
        var timer = _natives.CreateTimer()!;
        _natives.SaveTimerHandle(_table, 8, 8, timer);
        _natives.DestroyTimer(timer);

        Assert.Null(_natives.LoadTimerHandle(_table, 8, 8));
        Assert.True(_natives.HaveSavedHandle(_table, 8, 8));
    }

    [Fact]
    public void A_converted_value_is_saved_but_gives_false()
    {
        var masked = _natives.ConvertFogState(1);

        Assert.False(_natives.SaveFogStateHandle(_table, 6, 6, masked));
        Assert.Same(masked, _natives.LoadFogStateHandle(_table, 6, 6));
        Assert.Null(_natives.LoadPlayerHandle(_table, 6, 6));
        Assert.True(_natives.HaveSavedHandle(_table, 6, 6));
    }

    [Fact]
    public void Flushing_a_parent_key_empties_only_it()
    {
        _natives.SaveReal(_table, 1, 1, 1);
        _natives.SaveBoolean(_table, 1, 1, true);
        _natives.SaveStr(_table, 1, 1, "x");
        _natives.SaveInteger(_table, -1, int.MinValue, 11);
        _natives.SavePlayerHandle(_table, 5, 5, _natives.Player(0));

        _natives.FlushChildHashtable(_table, 1);

        Assert.False(_natives.HaveSavedReal(_table, 1, 1));
        Assert.False(_natives.HaveSavedBoolean(_table, 1, 1));
        Assert.False(_natives.HaveSavedString(_table, 1, 1));
        Assert.True(_natives.HaveSavedHandle(_table, 5, 5));
        Assert.Equal(11, _natives.LoadInteger(_table, -1, int.MinValue));
        Assert.Equal(0, _natives.LoadInteger(_table, -1, int.MaxValue));
    }

    [Fact]
    public void No_table_loads_nothing_and_saves_nothing()
    {
        Assert.Equal(0, _natives.LoadInteger(null, 1, 1));
        Assert.Equal("", _natives.LoadStr(null, 1, 1));
        Assert.False(_natives.HaveSavedInteger(null, 1, 1));
        _natives.SaveInteger(null, 1, 1, 1);
        Assert.False(_natives.SaveStr(null, 1, 1, "x"));
        Assert.False(_natives.SaveTimerHandle(null, 1, 1, _natives.CreateTimer()));
        _natives.FlushChildHashtable(null, 1);
        _natives.FlushParentHashtable(null);
    }

    [Fact]
    public void Flushing_a_hashtable_destroys_it_and_leaves_the_others()
    {
        var other = _natives.InitHashtable();
        _natives.SaveInteger(other, 1, 1, 21);
        _natives.SavePlayerHandle(_table, 5, 5, _natives.Player(0));

        _natives.FlushParentHashtable(_table);
        _natives.SaveInteger(_table, 9, 9, 99);

        Assert.False(_natives.HaveSavedHandle(_table, 5, 5));
        Assert.Equal(0, _natives.LoadInteger(_table, 9, 9));
        Assert.Equal(21, _natives.LoadInteger(other, 1, 1));
        Assert.False(_natives.SaveStr(_table, 9, 9, "x"));
        Assert.False(_natives.SaveTimerHandle(_table, 9, 9, _natives.CreateTimer()));
        Assert.False(_natives.HaveSavedString(_table, 9, 9));
    }

    [Fact]
    public void There_are_at_most_256_hashtables_and_a_flushed_one_gives_its_place_back()
    {
        var made = Enumerable.Range(0, 255).Select(_ => _natives.InitHashtable()).ToList();

        Assert.DoesNotContain(null, made);
        Assert.Null(_natives.InitHashtable());

        _natives.FlushParentHashtable(made[9]);

        Assert.NotNull(_natives.InitHashtable());
        Assert.Null(_natives.InitHashtable());
    }

    // Through Lua: the handle comes back as the same userdata, a real slot takes an integer as a float, LoadStr of a
    // missing key is "", a nil string is refused and leaves what the key held, and a destroyed timer loads as nil.
    [Fact]
    public void Lua_gets_back_what_it_saved()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
              local ht, t = InitHashtable(), CreateTimer()
              SaveTimerHandle(ht, 3, 3, t)
              SaveReal(ht, 1, 2, 3)
              SaveFogStateHandle(ht, 6, 6, FOG_OF_WAR_MASKED)
              print(LoadTimerHandle(ht, 3, 3) == t, math.type(LoadReal(ht, 1, 2)), LoadStr(ht, 1, 1) == '',
                LoadFogStateHandle(ht, 6, 6) == FOG_OF_WAR_MASKED, tostring(ht):match('^%a+'))
              print(select(2, pcall(SaveStr, ht, 7, 8, nil)))
              SaveStr(ht, 7, 9, 'a')
              pcall(SaveStr, ht, 7, 9, nil)
              print(HaveSavedString(ht, 7, 8), HaveSavedString(ht, 7, 9), LoadStr(ht, 7, 9))
              DestroyTimer(t)
              print(LoadTimerHandle(ht, 3, 3), HaveSavedHandle(ht, 3, 3))
            end
            """), "");

        Assert.Equal(
            ["0.000 print: true\tfloat\ttrue\ttrue\thashtable",
             "0.000 print: bad argument #4 to 'SaveStr' (string expected, got nil)", "0.000 print: false\ttrue\ta",
             "0.000 print: nil\ttrue"],
            simulation.Log);
    }
}
