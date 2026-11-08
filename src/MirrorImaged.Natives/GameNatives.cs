using MirrorImaged.Data;
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
        _players = MakePlayers();
        _local = _players[0];
    }

    /// <summary>The game's data the natives read: the standard objects and constants with the map's changes, and its files.</summary>
    public GameData Data { get; init; } = new(new StandardData());

    T Make<T>(T handle) where T : handle
    {
        handle.Id = _game.NewId();
        handle.Key = _game.Add(handle);
        return handle;
    }
}
