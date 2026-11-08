using System.Globalization;

namespace MirrorImaged.Data;

/// <summary>
/// A walkable destructable's deck as <c>GetLocationZ</c> reads it: its height above the ground beneath its place every
/// 32 along and across it, at facing 270 and scale 1, about the place the game reports, in whole units (<c>Decks.txt</c>,
/// measured in the game on 3.0.1.24371 for each standard walkable type's variation 0; decisions D255).
/// </summary>
public sealed class Deck
{
    const float Cell = 32;
    static readonly Lazy<Dictionary<int, Deck>> All = new(Read);

    readonly float[] _values; // NaN where it has none
    readonly int _columns, _rows, _column, _row;

    Deck(float[] values, int columns, int rows, int column, int row) =>
        (_values, _columns, _rows, _column, _row) = (values, columns, rows, column, row);

    /// <summary>The deck of the type <paramref name="id"/> (as <c>FourCC</c> gives it); null for a type without one.</summary>
    public static Deck? Of(int id) => All.Value.GetValueOrDefault(id);

    /// <summary>
    /// Its height above the ground beneath, <paramref name="x"/> east and <paramref name="y"/> north of its place in its
    /// own frame; null where the nearest point measured has none. Between points, the blend of the four around, or the
    /// nearest's where one of them has none, as Moonwell reads it.
    /// </summary>
    public float? Height(float x, float y)
    {
        float u = x / Cell + _column, v = y / Cell + _row;
        if (!(u > -0.5f && u < _columns - 0.5f && v > -0.5f && v < _rows - 0.5f)) return null;
        var nearest = _values[(int)(v + 0.5f) * _columns + (int)(u + 0.5f)];
        if (float.IsNaN(nearest)) return null;
        int c = (int)MathF.Floor(u), r = (int)MathF.Floor(v);
        if (c < 0 || r < 0 || c + 1 >= _columns || r + 1 >= _rows) return nearest;
        float At(int dc, int dr) => _values[(r + dr) * _columns + c + dc];
        var (across, up) = (u - c, v - r);
        var blend = (1 - up) * ((1 - across) * At(0, 0) + across * At(1, 0)) + up * ((1 - across) * At(0, 1) + across * At(1, 1));
        return float.IsNaN(blend) ? nearest : blend;
    }

    // A block is "type variation cell columns rows originColumn originRow", then its rows from the south, each a value a
    // column from the west, "." where there is no deck.
    static Dictionary<int, Deck> Read()
    {
        using var stream = typeof(Deck).Assembly.GetManifestResourceStream("Decks.txt")!;
        var lines = new StreamReader(stream).ReadToEnd().Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && line[0] != '#').ToList();
        var decks = new Dictionary<int, Deck>();
        for (var at = 0; at < lines.Count;)
        {
            var head = lines[at++].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int Number(int i) => int.Parse(head[i], CultureInfo.InvariantCulture);
            var (columns, rows) = (Number(3), Number(4));
            var values = lines.Skip(at).Take(rows)
                .SelectMany(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(value => value == "." ? float.NaN : float.Parse(value, CultureInfo.InvariantCulture)))
                .ToArray();
            at += rows;
            if (Number(1) == 0) decks[Ids.Of(head[0])] = new Deck(values, columns, rows, Number(5), Number(6));
        }
        return decks;
    }
}
