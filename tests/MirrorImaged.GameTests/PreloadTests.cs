using MirrorImaged.Probes;

namespace MirrorImaged.GameTests;

public sealed class PreloadTests
{
    // The bytes the game writes (3.0.1.24371): its own lines end in \r\n, the text's line breaks stay \n, and a backslash
    // in the text is written doubled.
    const string Written =
        "function PreloadFiles takes nothing returns nothing\n\r\n" +
        "\tcall PreloadStart()\r\n" +
        "\tcall Preload( \"\")\nfirst line\n//\" )\r\n" +
        "\tcall Preload( \"\")\nC:\\\\maps \"quoted\" \n//\" )\r\n" +
        "\tcall Preload( \"\")\n\n//\" )\r\n" +
        "\tcall PreloadEnd( 0.0 )\r\n\nendfunction\n\n\r\n";

    [Fact]
    public void Lines_are_read_from_a_file_the_game_wrote()
    {
        Assert.Equal(["first line", "C:\\maps \"quoted\" ", ""], Preload.Lines(Written));
    }

    [Fact]
    public void A_file_still_being_written_has_no_lines()
    {
        Assert.Null(Preload.Lines(Written[..Written.IndexOf("\tcall PreloadEnd", StringComparison.Ordinal)]));
        Assert.Null(Preload.Lines(""));
    }

    [Fact]
    public void A_request_sets_the_tooltip_the_hub_reads_to_the_maps_path()
    {
        Assert.Equal(
            "function PreloadFiles takes nothing returns nothing\n\r\n" +
            "\tcall PreloadStart()\r\n" +
            "\tcall Preload( \"\")\ncall BlzSetAbilityTooltip('AHbz', \"C:\\\\maps\\\\1-hello.w3x\", 0)\n//\" )\r\n" +
            "\tcall PreloadEnd( 0.0 )\r\n\nendfunction\n\n\r\n",
            Preload.Request(@"C:\maps\1-hello.w3x"));
    }
}
