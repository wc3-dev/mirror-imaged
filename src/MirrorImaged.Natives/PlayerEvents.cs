namespace MirrorImaged.Natives;

// Player and game events as the events probe measures them (docs/facts.md, "Player and game events"): RemovePlayer,
// SetPlayerAlliance and SetPlayerState fire them inside the call; leaving, chat, cinematics, keys and the mouse come with
// players and input (wave 6), so their events never fire yet.
public sealed partial class GameNatives
{
    const string Events = "facts.md, Player and game events: probe events (11-08)";
    const int PlayerStateLimit = 11, PlayerAllianceChanged = 12, PlayerDefeat = 13, PlayerVictory = 14; // EVENT_PLAYER_*

    readonly List<(player Player, int Kind, @event Event)> _playerEvents = [];
    readonly List<(player Player, int Alliance, @event Event)> _allianceEvents = [];
    readonly List<(player Player, playerstate State, limitop Op, float Limit, @event Event)> _playerStateEvents = [];

    /// <summary>
    /// Fires for the player's victory and defeat (RemovePlayer, for a player that isn't a user) and its alliance
    /// changes of <c>ALLIANCE_PASSIVE</c>; <c>EVENT_PLAYER_STATE_LIMIT</c> never fires here, nor <c>EVENT_PLAYER_LEAVE</c>
    /// for RemovePlayer. The input events wait for input (wave 6).
    /// </summary>
    [Native(Level.Exact, Fact = Events)]
    public @event? TriggerRegisterPlayerEvent(trigger? whichTrigger, player? whichPlayer, playerevent? whichPlayerEvent)
    {
        if (whichTrigger is null || whichPlayer is null || whichPlayerEvent is null) return null;
        var registered = Register(whichTrigger);
        _playerEvents.Add((whichPlayer, whichPlayerEvent.Id, registered));
        return registered;
    }

    /// <summary>
    /// Fires inside SetPlayerState when the player's state changes to a value that meets the comparison, NOT_EQUAL
    /// included, with the event id <c>EVENT_PLAYER_STATE_LIMIT</c>, the player and the state.
    /// </summary>
    [Native(Level.Exact, Fact = Events)]
    public @event? TriggerRegisterPlayerStateEvent(trigger? whichTrigger, player? whichPlayer, playerstate? whichState, limitop? opcode, float limitval)
    {
        if (whichTrigger is null || whichPlayer is null || whichState is null || opcode is null) return null;
        var registered = Register(whichTrigger);
        _playerStateEvents.Add((whichPlayer, whichState, opcode, limitval, registered));
        return registered;
    }

    /// <summary>
    /// Fires inside SetPlayerAlliance when the player's alliance of this type with anyone changes, after its
    /// <c>EVENT_PLAYER_ALLIANCE_CHANGED</c>, with that event id and no trigger player.
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Orders and events: an alliance change fires on the giving player's registration only (11-06); " + Events)]
    public @event? TriggerRegisterPlayerAllianceChange(trigger? whichTrigger, player? whichPlayer, alliancetype? whichAlliance)
    {
        if (whichTrigger is null || whichPlayer is null || whichAlliance is null) return null;
        var registered = Register(whichTrigger);
        _allianceEvents.Add((whichPlayer, whichAlliance.Id, registered));
        return registered;
    }

    /// <summary>
    /// Never fires in a run: the events probe saw none of the 16 game events fire for its timers, game state sets,
    /// RemovePlayer's victory or EndGame. Close: the game fires <c>EVENT_GAME_LOADED</c> once a saved game has loaded
    /// (facts, "Initialisation"), which a run doesn't do.
    /// </summary>
    [Native(Level.Close)]
    public @event? TriggerRegisterGameEvent(trigger? whichTrigger, gameevent? whichGameEvent) =>
        whichTrigger is null || whichGameEvent is null ? null : Register(whichTrigger);

    /// <summary>The player of the event that fired the trigger; nil in alliance events, game state events and outside one.</summary>
    [Native(Level.Exact, Fact = Events)]
    public player? GetTriggerPlayer() => Current.Player;

    /// <summary>The state of the player state event that fired the trigger; outside one, the state with id 0.</summary>
    [Native(Level.Exact, Fact = Events)]
    public playerstate? GetEventPlayerState() => Current.PlayerState ?? Convert<playerstate>(0);

    void FirePlayerEvent(player p, int kind, player? triggerPlayer)
    {
        foreach (var (player, k, registered) in _playerEvents.ToArray())
            if (player == p && k == kind) Fire(registered, Convert<playerevent>(kind), player: triggerPlayer);
    }

    // EVENT_PLAYER_ALLIANCE_CHANGED fires for ALLIANCE_PASSIVE only, and before the alliance type's own events
    void AllianceChanged(player source, int alliance)
    {
        if (alliance == 0) FirePlayerEvent(source, PlayerAllianceChanged, null);
        foreach (var (player, a, registered) in _allianceEvents.ToArray())
            if (player == source && a == alliance) Fire(registered, Convert<playerevent>(PlayerAllianceChanged));
    }

    void PlayerStateChanged(player p, playerstate state, int value)
    {
        foreach (var (player, s, op, limit, registered) in _playerStateEvents.ToArray())
            if (player == p && s == state && Compare(op.Id, value, limit))
                Fire(registered, Convert<playerevent>(PlayerStateLimit), player: p, playerState: state);
    }

    // LESS_THAN to NOT_EQUAL, limitop's ids 0 to 5
    static bool Compare(int op, float value, float limit) => op switch
    {
        0 => value < limit,
        1 => value <= limit,
        2 => value == limit,
        3 => value >= limit,
        4 => value > limit,
        5 => value != limit,
        _ => false,
    };
}
