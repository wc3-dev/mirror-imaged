namespace MirrorImaged.Natives;

public partial class force
{
    internal int Players; // bit n: the player in slot n
}

// Forces as the players probe measures them (docs/facts.md, "Players and the game"): ForForce goes in slot order, and
// the enumerations clear the force first and take the playing players of slots 0 to 23, not the neutral ones, though
// those play too. A filter waits for the boolexprs (W1-9) and stops the run as Missing until then.
public sealed partial class GameNatives
{
    player? _enumPlayer;

    [Native(Level.Close)]
    public force? CreateForce() => Make(new force());

    [Native(Level.Close)]
    public void DestroyForce(force? whichForce)
    {
        if (whichForce is not null) _game.Remove(whichForce.Key);
    }

    [Native(Level.Close)]
    public void ForceAddPlayer(force? whichForce, player? whichPlayer)
    {
        if (whichForce is not null && whichPlayer is not null) whichForce.Players |= 1 << whichPlayer.Number;
    }

    [Native(Level.Close)]
    public void ForceRemovePlayer(force? whichForce, player? whichPlayer)
    {
        if (whichForce is not null && whichPlayer is not null) whichForce.Players &= ~(1 << whichPlayer.Number);
    }

    [Native(Level.Close)]
    public void ForceClear(force? whichForce)
    {
        if (whichForce is not null) whichForce.Players = 0;
    }

    [Native(Level.Close)]
    public bool IsPlayerInForce(player? whichPlayer, force? whichForce) =>
        whichPlayer is not null && whichForce is not null && (whichForce.Players & (1 << whichPlayer.Number)) != 0;

    [Native(Level.Close)]
    public bool BlzForceHasPlayer(force? whichForce, player? whichPlayer) => IsPlayerInForce(whichPlayer, whichForce);

    /// <summary>Close until filters (W1-9): without one it is as the players probe measures it.</summary>
    [Native(Level.Close)]
    public void ForceEnumPlayers(force? whichForce, boolexpr? filter) => Enumerate(whichForce, filter, nameof(ForceEnumPlayers), _ => true);

    [Native(Level.Close)]
    public void ForceEnumPlayersCounted(force? whichForce, boolexpr? filter, int countLimit) =>
        Enumerate(whichForce, filter, nameof(ForceEnumPlayersCounted), _ => countLimit-- > 0);

    /// <summary>The players <paramref name="whichPlayer"/> is passive to, itself among them (not measured beyond itself).</summary>
    [Native(Level.Close)]
    public void ForceEnumAllies(force? whichForce, player? whichPlayer, boolexpr? filter) =>
        Enumerate(whichForce, filter, nameof(ForceEnumAllies), p => IsPlayerAlly(whichPlayer, p));

    [Native(Level.Close)]
    public void ForceEnumEnemies(force? whichForce, player? whichPlayer, boolexpr? filter) =>
        Enumerate(whichForce, filter, nameof(ForceEnumEnemies), p => IsPlayerEnemy(whichPlayer, p));

    void Enumerate(force? whichForce, boolexpr? filter, string native, Func<player, bool> take)
    {
        if (filter is not null) throw new MissingNativeException(native);
        if (whichForce is null) return;
        whichForce.Players = 0;
        foreach (var p in _players.Take(24).Where(p => p.SlotState == Playing && take(p)))
            whichForce.Players |= 1 << p.Number;
    }

    /// <summary>
    /// Runs <paramref name="callback"/> for each player the force holds as it starts, in slot order (the players probe),
    /// with the enum player set; then lets go of it. Close: players added or removed by the callback don't change the
    /// run (not measured), and the facts' wait that never ends after a force looped with ForForce is destroyed isn't
    /// modelled (W1-9).
    /// </summary>
    [Native(Level.Close)]
    public void ForForce(force? whichForce, Action? callback)
    {
        var outer = _enumPlayer;
        try
        {
            for (int slot = 0, players = whichForce?.Players ?? 0; slot < _players.Length; slot++)
            {
                if ((players & (1 << slot)) == 0) continue;
                _enumPlayer = _players[slot];
                callback?.Invoke();
            }
        }
        finally
        {
            _enumPlayer = outer;
            Release(callback);
        }
    }

    /// <summary>The player ForForce is running for; null outside it.</summary>
    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game: nil after ForForce")]
    public player? GetEnumPlayer() => _enumPlayer;
}
