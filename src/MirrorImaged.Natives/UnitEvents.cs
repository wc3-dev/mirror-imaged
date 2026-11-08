namespace MirrorImaged.Natives;

// What happened to a unit, for the event responses: the unit, and the killer, previous owner or order where one fits.
sealed record Happening(unit Unit, unit? Killer = null, player? Previous = null, int Order = 0);

// The unit events of wave 2 (death, decay, owner change and SetUnitOwner's stop), fired inside the call, as the units
// probe measures them (docs/facts.md, "Units"). The other unit events register and wait for their waves.
public sealed partial class GameNatives
{
    const string UnitsProbe = "facts.md, Units: probe units (11-08)";
    const int UnitDeath = 53, UnitDecay = 54, UnitIssuedOrder = 75, UnitChangeOwner = 287, WidgetDeath = 89; // EVENT_UNIT_*, EVENT_WIDGET_DEATH
    const int PlayerUnitDeath = 20, PlayerUnitDecay = 21, PlayerUnitIssuedOrder = 38, PlayerUnitChangeOwner = 270; // EVENT_PLAYER_UNIT_*

    // Every unit, player-unit and widget death registration, in the order made
    readonly List<(widget? Widget, player? Player, int Kind, @event Event)> _unitEvents = [];

    /// <summary>
    /// Fires for the unit's death (inside KillUnit and a set of its life below 0.405), its decay, its owner changing and
    /// the stop SetUnitOwner gives it; the other unit events fire once their waves bring them.
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public @event? TriggerRegisterUnitEvent(trigger? whichTrigger, unit? whichUnit, unitevent? whichEvent) =>
        RegisterUnit(whichTrigger, whichUnit, null, whichEvent);

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
        _unitEvents.Add((w, p, kind.Id, registered));
        return registered;
    }

    /// <summary>The unit an event is about, in its widget death event too; nil outside a unit's event.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public unit? GetTriggerUnit() => Current.Unit?.Unit;

    /// <summary>The unit of a widget death event, seen as a widget (Lua's == holds with the unit); nil in its other events.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public widget? GetTriggerWidget() => Response(WidgetDeath)?.Unit;

    /// <summary>The unit of a unit or player-unit death event; nil in the widget death event.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public unit? GetDyingUnit() => Response(UnitDeath, PlayerUnitDeath)?.Unit;

    /// <summary>Nil for a death from KillUnit or a set of its life or most life (game-facts, Orders and events; probe units).</summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; game-facts.md, Orders and events: GetKillingUnit is nil for KillUnit (10-15)")]
    public unit? GetKillingUnit() => Response(UnitDeath, PlayerUnitDeath, WidgetDeath)?.Killer;

    [Native(Level.Exact, Fact = DecayProbe + "; " + UnitsProbe)]
    public unit? GetDecayingUnit() => Response(UnitDecay, PlayerUnitDecay)?.Unit;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public unit? GetChangingUnit() => Response(UnitChangeOwner, PlayerUnitChangeOwner)?.Unit;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public player? GetChangingUnitPrevOwner() => Response(UnitChangeOwner, PlayerUnitChangeOwner)?.Previous;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public unit? GetOrderedUnit() => Response(UnitIssuedOrder, PlayerUnitIssuedOrder)?.Unit;

    /// <summary>The order of an order event; 0 outside one.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetIssuedOrderId() => Response(UnitIssuedOrder, PlayerUnitIssuedOrder)?.Order ?? 0;

    Happening? Response(params int[] kinds) => Current.Unit is { } happening && kinds.Contains(Current.Event.Id) ? happening : null;

    // Fires a unit's event (probe units): for a death its widget registrations first, then its owner's player-unit
    // registrations, then its own unit registrations, each kind in the order registered; GetTriggerPlayer is the owner,
    // but in the widget event
    void FireUnit(Happening happening, int unitKind, int playerKind)
    {
        var (u, owner) = (happening.Unit, happening.Unit.Owner);
        var registrations = _unitEvents.ToArray();
        if (unitKind == UnitDeath)
            foreach (var (w, _, kind, registered) in registrations)
                if (w == u && kind == WidgetDeath) Fire(registered, Convert<widgetevent>(kind), unit: happening);
        foreach (var (_, p, kind, registered) in registrations)
            if (p == owner && kind == playerKind) Fire(registered, Convert<playerunitevent>(kind), player: owner, unit: happening);
        foreach (var (w, _, kind, registered) in registrations)
            if (w == u && kind == unitKind) Fire(registered, Convert<unitevent>(kind), player: owner, unit: happening);
    }
}
