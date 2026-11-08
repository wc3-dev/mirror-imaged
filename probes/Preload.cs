namespace MirrorImaged.Probes;

/// <summary>The game's Preload file, as <c>PreloadGenEnd</c> writes it (3.0.1.24371).</summary>
public static class Preload
{
    const string End = "\tcall PreloadEnd( 0.0 )";
    const string Opening = "\tcall Preload( \"\")";
    const string Closing = "//\" )";

    /// <summary>
    /// The lines a probe wrote, each with <c>Preload("\")\n" .. line .. "\n//")</c>; null while the file isn't complete.
    /// The game writes a backslash doubled, so it is read back single.
    /// </summary>
    public static IReadOnlyList<string>? Lines(string text)
    {
        if (!text.Contains(End, StringComparison.Ordinal))
            return null;

        var lines = new List<string>();
        var inside = false;
        foreach (var line in text.Split('\n').Select(line => line.TrimEnd('\r')))
        {
            if (line == Opening)
                inside = true;
            else if (line == Closing)
                inside = false;
            else if (inside)
                lines.Add(line.Replace("\\\\", "\\"));
        }

        return lines;
    }
}
