namespace MirrorImaged.Natives;

// Handle ids as the probes handles to handles-3 measure them (docs/facts.md, "Handles and errors")
public sealed partial class GameNatives
{
    const string Handles = "facts.md, Handles and errors: probes handles, handles-2, handles-3 and arguments (11-08)";

    /// <summary>
    /// The id the game shows: each handle a native makes takes the next, whatever its type, a trigger's actions,
    /// conditions and events too; a Convert handle's is its value; nil is 0, and a destroyed handle keeps its id. Close:
    /// the game's first ids differ, as it makes one handle before blizzard.j's and its players when first asked for, and
    /// it gives a destroyed handle's id again once Lua has collected it (decisions D110, D111).
    /// </summary>
    [Native(Level.Close)]
    public int GetHandleId(handle? h) => h?.Id ?? 0;
}
