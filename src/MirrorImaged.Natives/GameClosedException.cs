namespace MirrorImaged.Natives;

/// <summary>Thrown where the game closes, as the facts measure it doing: the run stops there.</summary>
public sealed class GameClosedException(string message) : Exception(message);
