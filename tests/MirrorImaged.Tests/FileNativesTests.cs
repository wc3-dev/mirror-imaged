using System.Text;
using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>Preload's files and Preloader's reads, against the facts' "Files and Preload" and the files probe.</summary>
public sealed class FileNativesTests
{
    const string Start = "function PreloadFiles takes nothing returns nothing\n\r\n\tcall PreloadStart()\r\n";
    const string End = "\tcall PreloadEnd( 0.0 )\r\n\nendfunction\n\n\r\n";

    readonly Documents _documents = new();
    readonly List<string> _ran = [];
    readonly GameNatives _natives;

    public FileNativesTests() => _natives = NewGame();

    GameNatives NewGame() => new(new Game(0)) { Documents = _documents, RunPreloadFile = (_, text) => _ran.Add(text) };

    void Write(string path, params string[] texts)
    {
        _natives.PreloadGenClear();
        _natives.PreloadGenStart();
        foreach (var text in texts) _natives.Preload(text);
        _natives.PreloadGenEnd(path);
    }

    string? Written(string path) =>
        _documents.CustomMapData.TryGetValue(path, out var file) ? Encoding.UTF8.GetString(file) : null;

    static string Line(string text) => $"\tcall Preload( \"{text}\" )\r\n";

    // The probe map's two lines, and hello.pld as the game wrote it (3.0.1.24371).
    [Fact]
    public void A_file_is_written_as_the_game_writes_it()
    {
        Write(@"MirrorImaged\hello.pld", "\")\nhello from the game\n//", "\")\n3\n//");

        Assert.Equal(
            "function PreloadFiles takes nothing returns nothing\n\r\n\tcall PreloadStart()\r\n" +
            "\tcall Preload( \"\")\nhello from the game\n//\" )\r\n" +
            "\tcall Preload( \"\")\n3\n//\" )\r\n" +
            "\tcall PreloadEnd( 0.0 )\r\n\nendfunction\n\n\r\n",
            Written(@"MirrorImaged\hello.pld"));
    }

    [Fact]
    public void A_file_with_no_lines_has_its_start_and_end()
    {
        Write(@"MirrorImaged\loaded.pld");

        Assert.Equal(Start + End, Written(@"MirrorImaged\loaded.pld"));
    }

    // facts: wrapped as ")\n + a line + \n//, a line of 253 is kept whole, one of 254 to 256 loses the closing // or its
    // line end, and a longer one is cut, at 1,000 and 4,000 too, with no error
    [Theory]
    [InlineData(253, "\")\n{0}\n//")]
    [InlineData(254, "\")\n{0}\n/")]
    [InlineData(255, "\")\n{0}\n")]
    [InlineData(256, "\")\n{0}")]
    [InlineData(1000, "\")\n{0}")]
    [InlineData(4000, "\")\n{0}")]
    public void A_text_keeps_its_first_259_bytes(int length, string kept)
    {
        Write("cut.pld", "\")\n" + new string('a', length) + "\n//");

        Assert.Equal(Start + Line(string.Format(kept, new string('a', Math.Min(length, 256)))) + End, Written("cut.pld"));
    }

    // facts: ")\n, 140 letters é (280 bytes) and \n// kept its first 259 bytes, 128 é
    [Fact]
    public void The_259_are_bytes()
    {
        Write("bytes.pld", "\")\n" + new string('é', 140) + "\n//");

        Assert.Equal(Start + Line("\")\n" + new string('é', 128)) + End, Written("bytes.pld"));
    }

    // facts: quotes are written as they are, a backslash doubled; |, a tab, a carriage return, a line break and UTF-8
    // letters as they are
    [Fact]
    public void A_backslash_is_doubled_and_the_rest_written_as_it_is()
    {
        Write("text.pld", "C:\\maps \"quoted\" | \t \r \n Åsa ö é € 中");

        Assert.Equal(Start + Line("C:\\\\maps \"quoted\" | \t \r \n Åsa ö é € 中") + End, Written("text.pld"));
    }

    // The files probe: Preload keeps every text until PreloadGenClear, before PreloadGenStart and after PreloadGenEnd too,
    // and PreloadGenEnd writes them all and keeps them.
    [Fact]
    public void Preload_keeps_its_texts_until_PreloadGenClear()
    {
        _natives.PreloadGenClear();
        _natives.Preload("before start");
        _natives.PreloadGenStart();
        _natives.Preload("after start");
        _natives.PreloadGenEnd("order.pld");
        _natives.PreloadGenStart();
        _natives.Preload("second start");
        _natives.PreloadGenEnd("noclear.pld");
        _natives.PreloadGenEnd("again.pld");
        _natives.Preload("after end");
        _natives.PreloadGenEnd("afterend.pld");
        _natives.PreloadGenClear();
        _natives.PreloadGenEnd("cleared.pld");

        Assert.Equal(Start + Line("before start") + Line("after start") + End, Written("order.pld"));
        var three = Start + Line("before start") + Line("after start") + Line("second start") + End;
        Assert.Equal(three, Written("noclear.pld"));
        Assert.Equal(three, Written("again.pld"));
        Assert.Equal(three[..^End.Length] + Line("after end") + End, Written("afterend.pld"));
        Assert.Equal(Start + End, Written("cleared.pld"));
    }

    // The files probe: .txt and .pld are written; .dat, .json, no extension, .ini, .log, .w3v and .PLD aren't; nor a path
    // through .., an absolute one, or one of more than 201 characters. A sub-folder, a name with Å, . and / are.
    [Theory]
    [InlineData(@"B15Probe\ext.txt", @"B15Probe\ext.txt")]
    [InlineData(@"B15Probe\ext.pld", @"B15Probe\ext.pld")]
    [InlineData(@"B15Probe\sub\ext.pld", @"B15Probe\sub\ext.pld")]
    [InlineData(@"B15Probe\Åsa.pld", @"B15Probe\Åsa.pld")]
    [InlineData(@"files-probe\.\dot.pld", @"files-probe\dot.pld")]
    [InlineData("MirrorImaged/files-probe/slash.pld", @"MirrorImaged\files-probe\slash.pld")]
    [InlineData(@"B15Probe\ext.dat", null)]
    [InlineData(@"B15Probe\ext.json", null)]
    [InlineData(@"B15Probe\ext", null)]
    [InlineData(@"files-probe\ext.ini", null)]
    [InlineData(@"files-probe\ext.log", null)]
    [InlineData(@"files-probe\ext.w3v", null)]
    [InlineData(@"files-probe\caps.PLD", null)]
    [InlineData(@"files-probe\sub\..\dots.pld", null)]
    [InlineData(@"..\escape.pld", null)]
    [InlineData(@"C:\Users\someone\abs.pld", null)]
    [InlineData(@"\rooted.pld", null)]
    [InlineData(null, null)]
    public void PreloadGenEnd_writes_where_the_game_does(string? name, string? path)
    {
        Write(name!, "x");

        Assert.Equal(path is null ? [] : [path], _documents.CustomMapData.Keys);
    }

    [Theory]
    [InlineData(201, true)]
    [InlineData(202, false)]
    public void A_path_of_more_than_201_characters_is_written_nowhere(int length, bool written)
    {
        var path = @"MirrorImaged\files-probe\" + new string('a', length - 29) + ".pld";

        Write(path, "x");

        Assert.Equal(written, _documents.CustomMapData.ContainsKey(path));
    }

    // facts: a file is read back with Preloader, at once, in the same call; .txt and .pld are, .dat, .json and no
    // extension not, even when an outside program wrote them; a sub-folder and Å are
    [Theory]
    [InlineData(@"B15Probe\ext.txt", true)]
    [InlineData(@"B15Probe\ext.pld", true)]
    [InlineData(@"B15Probe\sub\ext.pld", true)]
    [InlineData(@"B15Probe\Åsa.pld", true)]
    [InlineData(@"B15Probe\ext.dat", false)]
    [InlineData(@"B15Probe\ext.json", false)]
    [InlineData(@"B15Probe\ext", false)]
    [InlineData(@"B15Probe\ext.PLD", false)]
    public void Preloader_reads_pld_and_txt_files(string path, bool read)
    {
        _documents.CustomMapData[path] = Encoding.UTF8.GetBytes(path);

        _natives.Preloader(path);

        Assert.Equal(read ? [path] : [], _ran);
    }

    // The files probe: an outside file read through sub\.., folders in capitals, a name in another case.
    [Theory]
    [InlineData(@"files-probe\outside2.pld", @"files-probe\sub\..\outside2.pld")]
    [InlineData(@"MirrorImaged\files-probe\own.pld", @"MIRRORIMAGED\FILES-PROBE\own.pld")]
    [InlineData(@"files-probe\Mixed.pld", @"files-probe\mixed.pld")]
    [InlineData(@"files-probe\slash.pld", "files-probe/slash.pld")]
    public void Preloader_finds_a_file_as_Windows_does(string path, string name)
    {
        _documents.CustomMapData[path] = "content"u8.ToArray();

        _natives.Preloader(name);

        Assert.Equal(["content"], _ran);
    }

    [Theory]
    [InlineData(@"..\x.pld")]
    [InlineData(@"C:\x.pld")]
    [InlineData(null)]
    public void Preloader_reads_nothing_outside_CustomMapData(string? name)
    {
        _documents.CustomMapData["x.pld"] = "content"u8.ToArray();

        _natives.Preloader(name);

        Assert.Empty(_ran);
    }

    // facts: a missing file changes nothing
    [Fact]
    public void A_missing_file_runs_nothing()
    {
        _natives.Preloader("missing.pld");

        Assert.Empty(_ran);
    }

    // facts: a path keeps the content it had when the game first read it, also when the map writes it again (the files
    // probe); a new path is read as it is on disk; files a game wrote are read in the next game
    [Fact]
    public void A_path_keeps_the_content_the_game_first_read()
    {
        Write("own.pld", "first");
        _natives.Preloader("own.pld");
        Write("own.pld", "second");
        _natives.Preloader("own.pld");
        _documents.CustomMapData["new.pld"] = "outside"u8.ToArray();
        _natives.Preloader("new.pld");
        _documents.CustomMapData["new.pld"] = "changed"u8.ToArray();
        _natives.Preloader("NEW.pld");
        NewGame().Preloader("own.pld");

        Assert.Equal([Start + Line("first") + End, Start + Line("first") + End, "outside", "outside", Start + Line("second") + End], _ran);
    }

    // Not measured: whether a path that was missing when read is read again once it is there.
    [Fact]
    public void A_path_read_while_missing_is_read_once_it_is_there()
    {
        _natives.Preloader("later.pld");
        _documents.CustomMapData["later.pld"] = "there"u8.ToArray();
        _natives.Preloader("later.pld");

        Assert.Equal(["there"], _ran);
    }

    // The loading natives a Preload file calls change nothing a map reads, and aren't in the log (D98).
    [Fact]
    public void The_loading_natives_do_nothing()
    {
        var game = new Game(0);
        var natives = new GameNatives(game) { Documents = _documents };

        natives.PreloadStart();
        natives.PreloadRefresh();
        natives.PreloadEnd(1.5f);
        natives.PreloadEndEx();
        natives.PreloadGenClear();
        natives.PreloadGenEnd("empty.pld");

        Assert.Empty(game.Log);
        Assert.Equal(Start + End, Written("empty.pld"));
    }
}
