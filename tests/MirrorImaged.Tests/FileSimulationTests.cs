using System.Text;
using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>Maps that write files and game caches and read them back, as the facts' "Files and Preload" measure them.</summary>
public sealed class FileSimulationTests : IDisposable
{
    readonly TestMaps _maps = new();
    readonly Documents _documents = new();

    public void Dispose() => _maps.Dispose();

    // A map whose main defines write(path, texts...) and runs the Lua given.
    Simulation Load(string main) => Simulation.Load(_maps.Build($$"""
        function write(path, ...)
          PreloadGenClear()
          PreloadGenStart()
          for _, text in ipairs({...}) do Preload(text) end
          PreloadGenEnd(path)
        end
        function config() end
        function main()
        {{main}}
        end
        """), "", new SimulationOptions { Documents = _documents });

    [Fact]
    public void A_run_writes_its_files_in_the_documents_it_was_given()
    {
        using var simulation = Load("""write("MirrorImaged\\hello.pld", "\")\nhello from the game\n//")""");

        Assert.Same(_documents, simulation.Documents);
        Assert.Equal(
            "function PreloadFiles takes nothing returns nothing\n\r\n\tcall PreloadStart()\r\n" +
            "\tcall Preload( \"\")\nhello from the game\n//\" )\r\n\tcall PreloadEnd( 0.0 )\r\n\nendfunction\n\n\r\n",
            Encoding.UTF8.GetString(_documents.CustomMapData[@"MirrorImaged\hello.pld"]));
    }

    // facts: after Preloader of the file the pieces come back at once, in the same call; the files probe's game cache
    // carrier gave 42. A rawcode, a nested call and a common.j global work as in JASS.
    [Fact]
    public void Preloader_runs_the_files_JASS_at_once()
    {
        using var simulation = Load("""
            write("cache.pld",
              "\")\ncall StoreInteger(InitGameCache(\"files-probe.w3v\"), \"pre\", \"k\", 42)\n//",
              "\")\ncall StoreInteger(InitGameCache(\"files-probe.w3v\"), \"pre\", \"id\", 'AHbz')\n//",
              "\")\ncall StoreInteger(InitGameCache(\"files-probe.w3v\"), \"pre\", \"slot\", PLAYER_NEUTRAL_AGGRESSIVE)\n//")
            Preloader("cache.pld")
            local cache = InitGameCache("files-probe.w3v")
            print(GetStoredInteger(cache, "pre", "k"), GetStoredInteger(cache, "pre", "id"), GetStoredInteger(cache, "pre", "slot"))
            """);

        Assert.Null(simulation.Stopped);
        Assert.Equal(["0.000 print: 42\t1095262842\t24"], simulation.Log);
    }

    // facts: printable ASCII but the quote, UTF-8 letters and colour codes come back whole, and a backslash, written
    // doubled, reads back as one
    [Fact]
    public void Text_comes_back_whole_through_a_file()
    {
        using var simulation = Load("""
            local texts = {"C:\\maps\\", "Åsa ö é € 中 😀 ~|", "|cffff0000red|r", "!#$%&'()*+,-./09:;<=>?@AZ[]^_`az{|}~"}
            local lines = {}
            for i, text in ipairs(texts) do
              lines[i] = "\")\ncall StoreString(InitGameCache(\"c\"), \"m\", \"" .. i .. "\", \"" .. text .. "\")\n//"
            end
            write("text.pld", table.unpack(lines))
            Preloader("text.pld")
            for i, text in ipairs(texts) do print(GetStoredString(InitGameCache("c"), "m", tostring(i)) == text) end
            """);

        Assert.Null(simulation.Stopped);
        Assert.Equal(Enumerable.Repeat("0.000 print: true", 4), simulation.Log);
    }

    // facts: pieces of 200 to 213 characters (text of 259 with the wrapping) came back whole. With SetPlayerName the
    // wrapping is 38 bytes, so 221 still fits.
    [Theory]
    [InlineData(213)]
    [InlineData(221)]
    public void A_piece_comes_back_whole_while_its_text_fits_in_259_bytes(int length)
    {
        using var simulation = Load($$"""
            write("name.pld", "\")\ncall SetPlayerName(Player(0),\"" .. string.rep("a", {{length}}) .. "\")\n//")
            Preloader("name.pld")
            print(#GetPlayerName(Player(0)))
            """);

        Assert.Equal([$"0.000 print: {length}"], simulation.Log);
    }

    // facts: reading a file whose 214-character piece Preload had cut closed the game; so did a line that isn't JASS
    // and a file that ends PreloadFiles and goes on in Lua, as some maps' save files do (the files probe), and a file
    // naming a blizzard.j global. A piece of 222 cut through SetPlayerName, a JASS error, is taken to close it too.
    [Theory]
    [InlineData("""write("bad.pld", "\")\nnot jass\n//")""")]
    [InlineData("""write("bad.pld", "\")\nendfunction\n//!beginusercode\nFilesProbeLua={}--[[", "]]FilesProbeLua[1]=1--[[")""")]
    [InlineData("""write("bad.pld", "\")\ncall SetPlayerName(Player(0),\"" .. string.rep("a", 222) .. "\")\n//")""")]
    [InlineData("""write("bad.pld", "\")\ncall BlzSetAbilityTooltip('A0P1', \"" .. string.rep("a", 214) .. "\", 0)\n//")""")]
    [InlineData("""write("bad.pld", "\")\ncall DisplayTextToForce(GetPlayersAll(), \"x\")\n//")""")]
    [InlineData("""write("bad.pld", "\")\ncall StoreInteger(InitGameCache(\"c\"), \"m\", \"k\", bj_MAX_INVENTORY)\n//")""")]
    public void A_file_the_game_cant_run_stops_the_run_where_the_game_closes(string write)
    {
        using var simulation = Load(write + "\nprint('before')\nPreloader('bad.pld')\nprint('after')");

        var stop = simulation.Stopped!;
        Assert.StartsWith("war3map.lua:11: The game closes: Preloader ran bad.pld", stop.Message);
        Assert.IsType<GameClosedException>(stop.InnerException);
        Assert.Equal(["0.000 print: before"], simulation.Log);
    }

    // Not measured: an error as the file runs (here a missing argument) is taken to close the game too. The stop names
    // the map's line and the file's.
    [Fact]
    public void An_error_as_a_file_runs_stops_the_run_naming_the_file()
    {
        using var simulation = Load("""
            write("bad.pld", "\")\ncall StoreInteger(InitGameCache(\"c\"), \"m\", \"k\")\n//")
            Preloader("bad.pld")
            """);

        Assert.StartsWith("war3map.lua:10: The game closes: Preloader ran bad.pld, which it can't run (bad.pld:", simulation.Stopped!.Message);
        Assert.Contains("bad argument #4 to 'StoreInteger'", simulation.Stopped.Message);
    }

    // facts: Lua can't be run from a Preload file and //!beginusercode does nothing (10-01; the file isn't recorded): a
    // block of it whose lines are JASS comments runs nothing and the game goes on.
    [Fact]
    public void A_usercode_block_in_a_file_runs_nothing()
    {
        using var simulation = Load("""
            write("usercode.pld", "\")\n//!beginusercode\n//FilesProbeLua = 1\n//!endusercode\n//")
            Preloader("usercode.pld")
            print(FilesProbeLua)
            """);

        Assert.Null(simulation.Stopped);
        Assert.Equal(["0.000 print: nil"], simulation.Log);
    }

    // War3Net's transpiler makes a JASS array with __jarray.
    [Fact]
    public void A_file_can_use_a_JASS_array()
    {
        using var simulation = Load("""
            write("array.pld", "\")\nlocal integer array a\nset a[1] = 5\ncall StoreInteger(InitGameCache(\"c\"), \"m\", \"k\", a[1] + a[2])\n//")
            Preloader("array.pld")
            print(GetStoredInteger(InitGameCache("c"), "m", "k"))
            """);

        Assert.Null(simulation.Stopped);
        Assert.Equal(["0.000 print: 5"], simulation.Log);
    }

    // A rawcode is its four bytes as War3Net reads them, a quote and a backslash among them (the game's reading of a
    // backslash isn't measured). Written by an outside program, as Preload would double the backslash.
    [Fact]
    public void A_rawcode_with_a_quote_or_a_backslash_is_its_four_bytes()
    {
        _documents.CustomMapData["raw.pld"] = Encoding.UTF8.GetBytes("""
            function PreloadFiles takes nothing returns nothing
            call StoreInteger(InitGameCache("c"), "m", "quote", 'ab"c')
            call StoreInteger(InitGameCache("c"), "m", "backslash", '\abc')
            endfunction
            """);

        using var simulation = Load("""
            Preloader("raw.pld")
            print(GetStoredInteger(InitGameCache("c"), "m", "quote"), GetStoredInteger(InitGameCache("c"), "m", "backslash"))
            """);

        Assert.Null(simulation.Stopped);
        Assert.Equal(["0.000 print: 1633821283\t1549886051"], simulation.Log);
    }

    [Fact]
    public void A_player_name_carries_a_piece_out_of_a_file()
    {
        using var simulation = Load("""
            write("name.pld", "\")\ncall SetPlayerName(Player(1), \"piece\")\n//")
            Preloader("name.pld")
            print(GetPlayerName(Player(1)))
            """);

        Assert.Equal(["0.000 print: piece"], simulation.Log);
    }

    // The tooltip carrier needs the abilities (wave 2): until then its native stops the run as Missing.
    [Fact]
    public void A_Missing_native_in_a_file_stops_the_run_as_Missing()
    {
        using var simulation = Load("""
            write("tooltip.pld", "\")\ncall BlzSetAbilityTooltip('AHbz', \"piece\", 0)\n//")
            Preloader("tooltip.pld")
            """);

        Assert.Equal("BlzSetAbilityTooltip", Assert.IsType<MissingNativeException>(simulation.Stopped!.InnerException).Native);
        Assert.StartsWith("tooltip.pld:", simulation.Stopped.Message);
    }

    // Inferred from the facts (a Preload file runs as JASS, without blizzard.j or Lua): it calls the natives themselves,
    // not what the map put in their globals, and its functions aren't the map's.
    [Fact]
    public void A_file_calls_the_natives_not_the_maps_globals()
    {
        using var simulation = Load("""
            write("cache.pld", "\")\ncall StoreInteger(InitGameCache(\"c\"), \"m\", \"k\", 7)\n//")
            StoreInteger = function() print("replaced") end
            Preloader("cache.pld")
            print(GetStoredInteger(InitGameCache("c"), "m", "k"), PreloadFiles)
            """);

        Assert.Equal(["0.000 print: 7\tnil"], simulation.Log);
    }

    // facts: files a game wrote were read back in the next game; the files probe: the next game has the saved caches
    [Fact]
    public void Files_and_saved_caches_carry_into_the_next_run()
    {
        using (Load("""
                   write("save.pld", "\")\ncall StoreInteger(InitGameCache(\"from-file.w3v\"), \"m\", \"k\", 5)\n//")
                   local cache = InitGameCache("campaign.w3v")
                   StoreInteger(cache, "m", "gold", 300)
                   SaveGameCache(cache)
                   StoreInteger(cache, "m", "unsaved", 1)
                   """)) { }

        using var next = Load("""
            Preloader("save.pld")
            print(GetStoredInteger(InitGameCache("from-file.w3v"), "m", "k"),
              GetStoredInteger(InitGameCache("campaign.w3v"), "m", "gold"),
              GetStoredInteger(InitGameCache("campaign.w3v"), "m", "unsaved"))
            """);

        Assert.Equal(["0.000 print: 5\t300\t0"], next.Log);
    }
}
