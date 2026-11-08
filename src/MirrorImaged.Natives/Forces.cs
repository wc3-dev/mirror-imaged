namespace MirrorImaged.Natives;

public partial class force
{
    internal int Players; // bit n: the player in slot n
    internal bool Looped; // ForForce has run over it
}

// Forces as the players and events probes measure them (docs/facts.md, "Players and the game", "Player and game
// events"): ForForce goes in slot order, and the enumerations clear the force first and take the playing players of
// slots 0 to 23 that their filter passes, not the neutral ones, though those play too.
public sealed partial class GameNatives
{
    player? _enumPlayer, _filterPlayer;
    long _waitLost = -1; // the step a force ForForce ran over was destroyed in, until a wait is lost to it

    [Native(Level.Close)]
    public force? CreateForce() => Make(new force());

    /// <summary>
    /// Destroying a force that ForForce has run over makes the next wait started in the same step never end, as the
    /// forforce-wait probe and the facts' three probes saw; a wait started 1 s later, or one already running, ends.
    /// Why isn't known.
    /// </summary>
    [Native(Level.Close)]
    public void DestroyForce(force? whichForce)
    {
        if (whichForce is null) return;
        if (whichForce.Looped) _waitLost = _game.Step;
        _game.Remove(whichForce.Key);
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

    /// <summary>
    /// The playing players of slots 0 to 23 that the filter passes, as a trigger's condition passes, called for each in
    /// slot order with GetFilterPlayer set. Close: in the game a destroyed filter's handle ran a condition made after it
    /// (handle ids, W1-6).
    /// </summary>
    [Native(Level.Close)]
    public void ForceEnumPlayers(force? whichForce, boolexpr? filter) => Enumerate(whichForce, filter, _ => true);

    /// <summary>As ForceEnumPlayers, until it holds <paramref name="countLimit"/> players: the filter isn't called after.</summary>
    [Native(Level.Close)]
    public void ForceEnumPlayersCounted(force? whichForce, boolexpr? filter, int countLimit) => Enumerate(whichForce, filter, _ => true, countLimit);

    /// <summary>
    /// The players <paramref name="whichPlayer"/> is passive to, itself among them (not measured beyond itself), that the
    /// filter passes: it is called for every playing player, ally or not.
    /// </summary>
    [Native(Level.Close)]
    public void ForceEnumAllies(force? whichForce, player? whichPlayer, boolexpr? filter) =>
        Enumerate(whichForce, filter, p => IsPlayerAlly(whichPlayer, p));

    [Native(Level.Close)]
    public void ForceEnumEnemies(force? whichForce, player? whichPlayer, boolexpr? filter) =>
        Enumerate(whichForce, filter, p => IsPlayerEnemy(whichPlayer, p));

    void Enumerate(force? whichForce, boolexpr? filter, Func<player, bool> take, int count = int.MaxValue)
    {
        if (whichForce is null) return;
        whichForce.Players = 0;
        foreach (var p in _players.Take(24))
        {
            if (count <= 0) break;
            if (p.SlotState != Playing || !Filtered(p, filter) || !take(p)) continue;
            whichForce.Players |= 1 << p.Number;
            count--;
        }
    }

    bool Filtered(player p, boolexpr? filter)
    {
        if (filter is null) return true;
        var outer = _filterPlayer;
        _filterPlayer = p;
        try { return Holds(filter, alone: true); }
        finally { _filterPlayer = outer; }
    }

    /// <summary>The player an enumeration's filter is called for; nil outside one.</summary>
    [Native(Level.Exact, Fact = Events)]
    public player? GetFilterPlayer() => _filterPlayer;

    /// <summary>
    /// Runs <paramref name="callback"/> for each player the force holds as it starts, in slot order (the players probe),
    /// with the enum player set; then lets go of it. Close: players added or removed by the callback don't change the
    /// run (not measured). Destroying the force afterwards loses the next wait (DestroyForce).
    /// </summary>
    [Native(Level.Close)]
    public void ForForce(force? whichForce, Action? callback)
    {
        if (whichForce is not null) whichForce.Looped = true;
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
