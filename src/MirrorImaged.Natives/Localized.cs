namespace MirrorImaged.Natives;

// The game's own strings and hotkeys (docs/facts.md, "The game's data": probe data).
public sealed partial class GameNatives
{
    const string DataProbe = "docs/facts.md, The game's data: probe data";

    /// <summary>
    /// The string the game's string lists give <paramref name="source"/> (its case as written), else
    /// <paramref name="source"/>; a <c>TRIGSTR_n</c> has come as the map's string already, as every string argument does.
    /// </summary>
    [Native(Level.Exact, Fact = DataProbe)]
    public string? GetLocalizedString(string? source) => source is null ? null : Data.String(source) ?? source;

    /// <summary>The letter of <c>ui/miscui.txt</c>'s hotkey for <paramref name="source"/> (any case), as a number; 0 for none.</summary>
    [Native(Level.Exact, Fact = DataProbe)]
    public int GetLocalizedHotkey(string? source) =>
        source is not null && Data.Hotkey(source) is { Length: > 0 } key ? key[0] : 0;
}
