using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class handle
{
    /// <summary>The handle's id in its game.</summary>
    public int Id { get; internal set; }

    /// <summary>Which handle this is in its game, unique whatever the type, by which Lua's handles find it (decisions D63).</summary>
    public int Key { get; internal set; }
}

public sealed partial class GameNatives
{
    readonly Game _game;
    readonly player[] _players;

    /// <summary>The natives of <paramref name="game"/>, whose players 0 to 27 are made first.</summary>
    public GameNatives(Game game)
    {
        _game = game;
        _players = [.. Enumerable.Range(0, 28).Select(number => Make(new player { Number = number }))];
    }

    T Make<T>(T handle) where T : handle
    {
        handle.Id = _game.NewId();
        handle.Key = _game.Add(handle);
        return handle;
    }
}
