namespace MirrorImaged.Natives;

// What happened to a unit, for the event responses: the unit, and the previous owner, order or state where one fits.
sealed record Happening(unit Unit, player? Previous = null, int Order = 0, unitstate? State = null);

// A unit state event's registration: its state and comparison, and whether the comparison held at the last frame's look
// at the unit's mana
sealed class UnitLimit(unitstate state, int op, float limit)
{
    public readonly unitstate State = state;
    public readonly int Op = op;
    public readonly float Limit = limit;
    public bool Held;
}

public partial class @event
{
    internal boolexpr? Filter;  // TriggerRegisterFilterUnitEvent's
    internal UnitLimit? Limit;  // TriggerRegisterUnitStateEvent's
}

// The unit events of wave 2 (death, decay, owner change, the order events of SetUnitOwner, KillUnit, RemoveUnit and
// PauseUnit, and the unit state events), fired inside the call, as the units probes measure them (docs/facts.md,
// "Units"). The other unit events register and wait for their waves.
public sealed partial class GameNatives
{
    const string UnitsProbe = "facts.md, Units: probe units (11-08)";
    const string MoreProbe = "facts.md, Units: probes units-more and units-more-2 (11-08)";
    const int UnitDeath = 53, UnitDecay = 54, UnitStateLimit = 59, UnitIssuedOrder = 75, UnitIssuedPointOrder = 76, UnitChangeOwner = 287, WidgetDeath = 89; // EVENT_UNIT_*, EVENT_WIDGET_DEATH
    const int PlayerUnitDeath = 20, PlayerUnitDecay = 21, PlayerUnitIssuedOrder = 38, PlayerUnitIssuedPointOrder = 39, PlayerUnitChangeOwner = 270; // EVENT_PLAYER_UNIT_*

    // The unit and widget death registrations of each widget, and the player-unit registrations of each player, in the
    // order made, so an event reads only its own unit's and owner's; a list goes once it is empty, or with its unit
    readonly Dictionary<widget, List<(int Kind, @event Event)>> _widgetEvents = [];
    readonly Dictionary<player, List<(int Kind, @event Event)>> _playerUnitEvents = [];

    /// <summary>
    /// Fires for the unit's death (inside KillUnit and a set of its life below 0.405), its decay, its owner changing and
    /// the orders SetUnitOwner, KillUnit, RemoveUnit and PauseUnit give it (stop, undefend, stunned); the other unit
    /// events fire once their waves bring them.
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + MoreProbe)]
    public @event? TriggerRegisterUnitEvent(trigger? whichTrigger, unit? whichUnit, unitevent? whichEvent) =>
        RegisterUnit(whichTrigger, whichUnit, null, whichEvent);

    /// <summary>
    /// As TriggerRegisterUnitEvent, but the filter runs first, as a trigger's condition does, and the event fires only
    /// when it passes (nil passes); a death registered so never fires, with a filter or without (probes units-more and
    /// units-more-2: an owner change fired with nil and a passing filter, not with a failing one).
    /// </summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public @event? TriggerRegisterFilterUnitEvent(trigger? whichTrigger, unit? whichUnit, unitevent? whichEvent, boolexpr? filter)
    {
        if (whichEvent?.Id == UnitDeath) return whichTrigger is null || whichUnit is null ? null : Register(whichTrigger);
        var registered = RegisterUnit(whichTrigger, whichUnit, null, whichEvent);
        registered?.Filter = filter;
        return registered;
    }

    /// <summary>
    /// Fires inside the call when the unit's life or mana is set, or it dies, from a value that fails the comparison to
    /// one that passes it, and the unit's mana again a frame later if it still passes and didn't at the last frame (here
    /// the next step); never for EQUAL, NOT_EQUAL or the most life and mana, nor for a unit that already passes when
    /// registered (probes units-more and units-more-2). Close: the game's frame comes 0.005 s later, and its
    /// regeneration, which can pass a limit, comes with combat (wave 3).
    /// </summary>
    [Native(Level.Close)]
    public @event? TriggerRegisterUnitStateEvent(trigger? whichTrigger, unit? whichUnit, unitstate? whichState, limitop? opcode, float limitval)
    {
        if (whichState is null || opcode is null) return null;
        var registered = RegisterUnit(whichTrigger, whichUnit, null, Convert<unitevent>(UnitStateLimit));
        if (registered is null) return null;
        registered.Limit = new(whichState, opcode.Id, limitval) { Held = Compare(opcode.Id, State(whichUnit!, whichState.Id), limitval) };
        return registered;
    }

    float State(unit u, int state) => state == ManaState ? u.Mana : u.Life;

    // A life or mana changed: its state events whose comparison now passes and didn't fire (probes units-more and
    // units-more-2), and for mana the frame's look a step later
    void StateChanged(unit u, int state, float old, float now)
    {
        if (!_widgetEvents.TryGetValue(u, out var list)) return;
        var limits = list.Where(e => e.Event.Limit is { Op: not (2 or 5) } l && l.State.Id == state).ToArray();
        foreach (var (_, registered) in limits)
            if (registered.Limit is { } l && !Compare(l.Op, old, l.Limit) && Compare(l.Op, now, l.Limit))
                Fire(registered, Convert<unitevent>(UnitStateLimit), player: u.Owner, unit: new(u, State: l.State));
        if (state != ManaState || limits.Length == 0) return;
        _game.At(_game.Step + 1, () =>
        {
            foreach (var (_, registered) in limits)
            {
                if (registered.Limit is not { } l || _game.Handle(u.Key) is null) continue;
                var holds = Compare(l.Op, u.Mana, l.Limit);
                if (holds && !l.Held) Fire(registered, Convert<unitevent>(UnitStateLimit), player: u.Owner, unit: new(u, State: l.State));
                l.Held = holds;
            }
        });
    }

    /// <summary>The state of a unit state event; outside one the state with id 0, UNIT_STATE_LIFE (probe units-more).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public unitstate? GetEventUnitState() => Response(UnitStateLimit)?.State ?? Convert<unitstate>(LifeState);

    /// <summary>
    /// As TriggerRegisterUnitEvent, for each unit the player owns when it happens; for an owner change, the new owner.
    /// The filter never runs, and one that would fail doesn't hold the event back (probe units).
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public @event? TriggerRegisterPlayerUnitEvent(trigger? whichTrigger, player? whichPlayer, playerunitevent? whichPlayerUnitEvent, boolexpr? filter) =>
        whichPlayer is null ? null : RegisterUnit(whichTrigger, null, whichPlayer, whichPlayerUnitEvent);

    /// <summary>Fires inside the unit's death, before its player-unit and unit events; destructables' come with them (W2-4).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public @event? TriggerRegisterDeathEvent(trigger? whichTrigger, widget? whichWidget) =>
        RegisterUnit(whichTrigger, whichWidget, null, Convert<widgetevent>(WidgetDeath));

    @event? RegisterUnit(trigger? t, widget? w, player? p, eventid? kind)
    {
        if (t is null || w is null && p is null || kind is null) return null;
        var registered = Register(t);
        registered.Of = (agent?)w ?? p;
        var list = w is not null ? Registrations(_widgetEvents, w) : Registrations(_playerUnitEvents, p!);
        list.Add((kind.Id, registered));
        return registered;
    }

    static List<(int Kind, @event Event)> Registrations<T>(Dictionary<T, List<(int Kind, @event Event)>> lists, T key) where T : notnull =>
        lists.TryGetValue(key, out var list) ? list : lists[key] = [];

    // DestroyTrigger lets go of a unit registration: only its own widget's or player's list is read
    void ForgetUnitEvent(@event registered)
    {
        if (registered.Of is widget w) Forget(_widgetEvents, w, registered);
        else if (registered.Of is player p) Forget(_playerUnitEvents, p, registered);
    }

    static void Forget<T>(Dictionary<T, List<(int Kind, @event Event)>> lists, T key, @event registered) where T : notnull
    {
        if (!lists.TryGetValue(key, out var list)) return;
        list.RemoveAll(e => e.Event == registered);
        if (list.Count == 0) lists.Remove(key);
    }

    /// <summary>How many unit event lists are kept, a widget's or a player's each: for the tests.</summary>
    internal int UnitEventLists => _widgetEvents.Count + _playerUnitEvents.Count;

    /// <summary>The unit an event is about, in its widget death event too; nil outside a unit's event.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public unit? GetTriggerUnit() => Current.Unit?.Unit;

    /// <summary>The unit of a widget death event, seen as a widget (Lua's == holds with the unit); nil in its other events.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public widget? GetTriggerWidget() => Response(WidgetDeath)?.Unit;

    /// <summary>The unit of a unit or player-unit death event; nil in the widget death event.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public unit? GetDyingUnit() => Response(UnitDeath, PlayerUnitDeath)?.Unit;

    /// <summary>
    /// Nil for a death from KillUnit or a set of its life or most life (game-facts, Orders and events; probe units), the
    /// only deaths before damage (W2-5).
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; game-facts.md, Orders and events: GetKillingUnit is nil for KillUnit (10-15)")]
    public unit? GetKillingUnit() => null;

    [Native(Level.Exact, Fact = DecayProbe + "; " + UnitsProbe)]
    public unit? GetDecayingUnit() => Response(UnitDecay, PlayerUnitDecay)?.Unit;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public unit? GetChangingUnit() => Response(UnitChangeOwner, PlayerUnitChangeOwner)?.Unit;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public player? GetChangingUnitPrevOwner() => Response(UnitChangeOwner, PlayerUnitChangeOwner)?.Previous;

    [Native(Level.Exact, Fact = UnitsProbe + "; " + MoreProbe)]
    public unit? GetOrderedUnit() => Response(UnitIssuedOrder, PlayerUnitIssuedOrder, UnitIssuedPointOrder, PlayerUnitIssuedPointOrder)?.Unit;

    /// <summary>The order of an order event; 0 outside one.</summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + MoreProbe)]
    public int GetIssuedOrderId() => Response(UnitIssuedOrder, PlayerUnitIssuedOrder, UnitIssuedPointOrder, PlayerUnitIssuedPointOrder)?.Order ?? 0;

    Happening? Response(params int[] kinds) => Current.Unit is { } happening && kinds.Contains(Current.Event.Id) ? happening : null;

    // Fires a unit's event (probe units): for a death its widget registrations first, then its owner's player-unit
    // registrations, then its own unit registrations, each kind in the order registered, a filter's only when the
    // filter passes; GetTriggerPlayer is the owner, but in the widget event
    void FireUnit(Happening happening, int unitKind, int playerKind)
    {
        var (u, owner) = (happening.Unit, happening.Unit.Owner);
        var (own, players) = (_widgetEvents.GetValueOrDefault(u)?.ToArray() ?? [], _playerUnitEvents.GetValueOrDefault(owner)?.ToArray() ?? []);
        if (unitKind == UnitDeath)
            foreach (var (kind, registered) in own)
                if (kind == WidgetDeath) Fire(registered, Convert<widgetevent>(kind), unit: happening);
        foreach (var (kind, registered) in players)
            if (kind == playerKind) Fire(registered, Convert<playerunitevent>(kind), player: owner, unit: happening);
        foreach (var (kind, registered) in own)
            if (kind == unitKind && (registered.Filter is not { } filter
                || In(new Firing(registered.Trigger, Convert<unitevent>(kind), Player: owner, Unit: happening), () => Holds(filter, alone: true))))
                Fire(registered, Convert<unitevent>(kind), player: owner, unit: happening);
    }
}
