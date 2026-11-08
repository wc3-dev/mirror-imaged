using System.Text;

namespace MirrorImaged.Data;

/// <summary>The game's two table formats: SLK tables and profile text (<c>[section]</c>, <c>key=value</c>).</summary>
static class Tables
{
    /// <summary>
    /// An SLK table's rows, top to bottom: the first row names the columns and the first column is the id. Each cell
    /// is the file's text, a quoted one without its quotes; a cell the file doesn't have is left out, and so is a row
    /// without an id.
    /// </summary>
    public static IEnumerable<(string Id, Dictionary<string, string> Cells)> Slk(byte[] file)
    {
        var cells = new Dictionary<int, Dictionary<int, string>>();
        int x = 0, y = 0;
        foreach (var raw in Text(file).Split('\n'))
        {
            if (!raw.StartsWith("C;", StringComparison.Ordinal)) continue;
            // A semicolon in a value is written twice.
            var line = raw.TrimEnd('\r');
            var parts = line.Contains(";;", StringComparison.Ordinal) ? line.Replace(";;", "\u0001").Split(';') : line.Split(';');
            string? value = null;
            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length == 0) continue;
                switch (part[0])
                {
                    case 'X': x = int.Parse(part.AsSpan(1)); break;
                    case 'Y': y = int.Parse(part.AsSpan(1)); break;
                    case 'K': value = (part.Length >= 3 && part[1] == '"' && part[^1] == '"' ? part[2..^1] : part[1..]).Replace('\u0001', ';'); break;
                }
            }
            if (value is null) continue;
            if (!cells.TryGetValue(y, out var row)) cells[y] = row = [];
            row[x] = value;
        }

        if (cells.Count == 0) yield break;
        var rows = cells.Keys.Order().ToList();
        var header = cells[rows[0]];
        var idColumn = header.Keys.Min();
        foreach (var y2 in rows.Skip(1))
        {
            var row = cells[y2];
            if (row.GetValueOrDefault(idColumn) is not { Length: > 0 } id) continue;
            var values = new Dictionary<string, string>(row.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var (column, value) in row.OrderBy(cell => cell.Key))
                if (header.TryGetValue(column, out var name)) values[name] = value;
            yield return (id, values);
        }
    }

    /// <summary>
    /// Profile text's sections in the order they first come, each with its keys (any case; a later one replaces an
    /// earlier) and values as written, quotes kept. Blank lines, <c>//</c> comments and lines without <c>=</c> are
    /// passed over; a section given twice is one.
    /// </summary>
    public static List<(string Name, Dictionary<string, string> Keys)> Profile(byte[] file)
    {
        var sections = new List<(string, Dictionary<string, string>)>();
        var byName = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        Dictionary<string, string>? section = null;
        foreach (var raw in Text(file).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;
            if (line[0] == '[' && line[^1] == ']')
            {
                var name = line[1..^1].Trim();
                if (!byName.TryGetValue(name, out section))
                {
                    byName[name] = section = new(StringComparer.OrdinalIgnoreCase);
                    sections.Add((name, section));
                }
                continue;
            }
            var equals = line.IndexOf('=');
            if (section is not null && equals > 0) section[line[..equals].Trim()] = line[(equals + 1)..].Trim();
        }
        return sections;
    }

    /// <summary>
    /// The entries of a comma list, each trimmed; one that starts with a quote ends at the next quote, so it holds
    /// commas, and loses its quotes.
    /// </summary>
    public static List<string> Entries(string text)
    {
        var entries = new List<string>();
        for (var at = 0; ; at++)
        {
            while (at < text.Length && text[at] is ' ' or '\t') at++;
            var close = at < text.Length && text[at] == '"' ? text.IndexOf('"', at + 1) : -1;
            var comma = text.IndexOf(',', close >= 0 ? close : at);
            var end = comma < 0 ? text.Length : comma;
            entries.Add(close >= 0 ? text[(at + 1)..close] : text[at..end].Trim());
            if (comma < 0) return entries;
            at = comma;
        }
    }

    /// <summary>A whole value without the pair of quotes around it, if it has one and no other quote.</summary>
    public static string Unquoted(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' && value.IndexOf('"', 1, value.Length - 2) < 0 ? value[1..^1] : value;

    static string Text(byte[] file) => Encoding.UTF8.GetString(file).TrimStart('﻿');
}
