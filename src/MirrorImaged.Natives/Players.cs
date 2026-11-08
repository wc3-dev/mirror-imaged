namespace MirrorImaged.Natives;

public partial class player
{
    /// <summary>The player's slot, 0 to 27.</summary>
    public int Number { get; internal init; }
}

public sealed partial class GameNatives
{
    /// <summary>The player in slot <paramref name="number"/>, 0 to 27; null for any other number, not measured.</summary>
    [Native(Level.Close)]
    public player? Player(int number) => (uint)number < _players.Length ? _players[number] : null;

    /// <summary>Slot 0: a run is single player.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, C# through CSharp.lua: in single player, GetLocalPlayer inside Local.For is slot 0 (10-19)")]
    public player? GetLocalPlayer() => _players[0];

    /// <summary>The player's slot; 0 for null, not measured.</summary>
    [Native(Level.Close)]
    public int GetPlayerId(player? whichPlayer) => whichPlayer?.Number ?? 0;

    /// <summary>Text for <paramref name="toPlayer"/>, which goes in the run's log: <c>2.000 player 0: text</c>.</summary>
    [Native(Level.Recorded, Reason = "Shows text on the player's screen; the run's log records it with the player")]
    public void DisplayTextToPlayer(player? toPlayer, float x, float y, string? message) => Show(toPlayer, message);

    /// <summary>As <see cref="DisplayTextToPlayer"/>; the duration isn't kept.</summary>
    [Native(Level.Recorded, Reason = "Shows text on the player's screen for a time; the run's log records it with the player")]
    public void DisplayTimedTextToPlayer(player? toPlayer, float x, float y, float duration, string? message) => Show(toPlayer, message);

    void Show(player? toPlayer, string? message)
    {
        if (toPlayer is not null) _game.Write($"player {toPlayer.Number}", message ?? "");
    }
}
