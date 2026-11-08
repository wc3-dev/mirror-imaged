using System.Text;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The SLK and profile text readers, on files written as the game's are.</summary>
public sealed class TablesTests
{
    static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public void An_slk_table_is_its_rows_by_the_first_columns_id_with_the_headers_names()
    {
        var rows = Tables.Slk(Bytes("""
            ID;PWXL;N;E
            B;X3;Y4;D0
            C;X1;Y1;K"unitID"
            C;X2;K"HP"
            C;X3;K"Name;;s"
            C;X1;Y2;K"hfoo"
            C;X2;K420
            C;X3;K"a;;b"
            C;Y3;X1;K"hkni"
            C;X3;K"-"
            C;X2;Y4;K800
            E
            """)).ToList();

        Assert.Equal(["hfoo", "hkni"], rows.Select(row => row.Id));
        Assert.Equal("420", rows[0].Cells["HP"]);
        Assert.Equal("420", rows[0].Cells["hp"]);
        Assert.Equal("a;b", rows[0].Cells["Name;s"]);
        Assert.Equal("-", rows[1].Cells["Name;s"]);
        Assert.False(rows[1].Cells.ContainsKey("HP"));
    }

    [Fact]
    public void Profile_text_is_its_sections_and_keys_a_later_key_replacing_an_earlier()
    {
        var sections = Tables.Profile(Bytes("﻿// a comment\n[hfoo]\nName=Footman\r\nArt = x.blp \nnot a key\n[Hpal]\nName=\"Paladin\"\n[hfoo]\nname=Changed\n"));

        Assert.Equal(["hfoo", "Hpal"], sections.Select(section => section.Name));
        Assert.Equal("Changed", sections[0].Keys["Name"]);
        Assert.Equal("x.blp", sections[0].Keys["art"]);
        Assert.Equal("\"Paladin\"", sections[1].Keys["Name"]);
    }

    [Fact]
    public void A_comma_list_keeps_a_quoted_entrys_commas()
    {
        Assert.Equal(["a", "b, c", "", "d"], Tables.Entries("a, \"b, c\",, d"));
        Assert.Equal(["only"], Tables.Entries("only"));
        Assert.Equal("a, b", Tables.Unquoted("\"a, b\""));
        Assert.Equal("\"a\",\"b\"", Tables.Unquoted("\"a\",\"b\""));
    }
}
