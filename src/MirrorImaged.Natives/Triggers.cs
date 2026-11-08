using MirrorImaged.World;

namespace MirrorImaged.Natives;

/// <summary>
/// The script's threads, which can wait, as the game runs a trigger's actions (decisions D70). A simulation gives its
/// natives them; without them, a function runs straight through and nothing can wait.
/// </summary>
public interface IScriptThreads
{
    /// <summary>
    /// Runs the script's global function <paramref name="name"/> in a new thread: null once it has ended (an error in it
    /// is logged), else the thread, waiting.
    /// </summary>
    IScriptThread? Start(string name);

    /// <summary>Makes the thread calling the running native wait once the native returns.</summary>
    void Yield();
}

/// <summary>
/// A function the script gave a native: the target of the delegate the native holds, which runs it straight through.
/// </summary>
public interface IScriptFunction
{
    /// <summary>Runs it in a new thread: null once it has ended (an error in it is logged), else the thread, waiting.</summary>
    IScriptThread? Start();
}

/// <summary>A thread of the script's, waiting; disposing of it lets it go without running on.</summary>
public interface IScriptThread : IDisposable
{
    /// <summary>Runs it on from its wait: true when it waits again, false once it has ended.</summary>
    bool Resume();
}

public partial class trigger
{
    internal bool Enabled = true, WaitOnSleeps, Destroyed;
    internal int EvalCount, ExecCount;
    internal readonly List<triggercondition> Conditions = [];
    internal readonly List<triggeraction> Actions = [];
    internal int Running;  // its threads that haven't ended, which may still run an action it no longer has
    internal int Interrupts;  // BlzTriggerInterrupt's calls: a thread started before the last one stops
    internal readonly List<triggeraction> Removed = [];  // let go of once none is running
    internal readonly List<@event> Events = [];
}

public partial class triggeraction
{
    internal Action? Code;
}

public partial class triggercondition
{
    internal boolexpr? Condition;
}

internal enum Junction { None, And, Or, Not }

// Where a Condition's or a Filter's function lives: once its handle is destroyed (Code null), the next Condition or Filter
// made takes the place, the first destroyed first, and whatever still holds the destroyed one runs the new one
// (decisions D115).
internal sealed class Place
{
    internal Func<bool>? Code;
}

public partial class boolexpr
{
    internal Place? Place;  // a Condition's or a Filter's; none for Condition(nil), And, Or and Not
    internal Junction Junction;
    internal boolexpr? A, B;
}

public partial class @event
{
    internal trigger? Trigger;
    internal timer? Timer;  // an expiry event's
}

public partial class timer
{
    internal List<@event>? ExpireEvents;
}

// Triggers, conditions, threads and waits, as the probes triggers to triggers-4 measure them (docs/facts.md, "Triggers and
// threads"). Events that need units, players or regions come with them.
public sealed partial class GameNatives
{
    const string Probe1 = "facts.md, Triggers and threads: probe triggers (11-08)";
    const string Probe2 = "facts.md, Triggers and threads: probe triggers-2 (11-08)";
    const string Probe3 = "facts.md, Triggers and threads: probe triggers-3 (11-08)";
    const string Probe4 = "facts.md, Triggers and threads: probe triggers-4 (11-08)";
    const int TimerExpired = 4;  // EVENT_GAME_TIMER_EXPIRED

    // What the event responses read in a thread: the trigger it runs for and the event that started it (decisions D75)
    sealed record Firing(trigger? Trigger, eventid Event, gamestate? State = null, player? Player = null, playerstate? PlayerState = null, widget? Widget = null);

    readonly List<(gamestate State, limitop Op, float Limit, @event Event)> _stateEvents = [];
    Firing? _firing, _outside;
    readonly Queue<Place> _places = new();
    conditionfunc? _noCondition;
    filterfunc? _noFilter;
    float _sleep;

    /// <summary>The script's threads; without them, a trigger's actions run straight through and can't wait.</summary>
    public IScriptThreads? Threads { get; init; }

    // Outside any event, the event id is a handle with id 0 (probe triggers)
    Firing Current => _firing ?? (_outside ??= new(null, Convert<gameevent>(0)));

    [Native(Level.Close)]
    public trigger? CreateTrigger() => Make(new trigger());

    /// <summary>
    /// Its events stop and they, its actions and its conditions are let go of; a thread already running its actions runs
    /// on (the game's handle stack may break: a claim, not measured).
    /// </summary>
    [Native(Level.Close)]
    public void DestroyTrigger(trigger? whichTrigger)
    {
        if (whichTrigger is null) return;
        whichTrigger.Destroyed = true;
        TriggerClearActions(whichTrigger);
        TriggerClearConditions(whichTrigger);
        foreach (var registered in whichTrigger.Events)
        {
            _game.Remove(registered.Key);
            registered.Timer?.ExpireEvents?.Remove(registered);
        }
        whichTrigger.Events.Clear();
        _stateEvents.RemoveAll(state => state.Event.Trigger == whichTrigger);
        _playerEvents.RemoveAll(e => e.Event.Trigger == whichTrigger);
        _allianceEvents.RemoveAll(e => e.Event.Trigger == whichTrigger);
        _playerStateEvents.RemoveAll(e => e.Event.Trigger == whichTrigger);
        _deaths.RemoveAll(e => e.Event.Trigger == whichTrigger);
        _game.Remove(whichTrigger.Key);
    }

    /// <summary>
    /// Whether a run of its actions hasn't ended, waiting or not, also one interrupted that hasn't reached its end yet;
    /// its conditions in TriggerEvaluate are no run.
    /// </summary>
    [Native(Level.Exact, Fact = Probe4)]
    public bool BlzTriggerIsRunning(trigger? whichTrigger) => whichTrigger?.Running > 0;

    /// <summary>
    /// Stops its runs: the action running goes on, but no other action runs, and a run that waits never goes on after
    /// its wait, also one the action running starts after the interrupt (decisions D77).
    /// </summary>
    [Native(Level.Exact, Fact = Probe4)]
    public void BlzTriggerInterrupt(trigger? whichTrigger)
    {
        if (whichTrigger is not null) whichTrigger.Interrupts++;
    }

    /// <summary>A disabled trigger's events are counted but run nothing; TriggerExecute and TriggerEvaluate still run it.</summary>
    [Native(Level.Exact, Fact = Probe1)]
    public void DisableTrigger(trigger? whichTrigger)
    {
        if (whichTrigger is not null) whichTrigger.Enabled = false;
    }

    [Native(Level.Exact, Fact = Probe1)]
    public void EnableTrigger(trigger? whichTrigger)
    {
        if (whichTrigger is not null) whichTrigger.Enabled = true;
    }

    /// <summary>True for a new trigger.</summary>
    [Native(Level.Exact, Fact = Probe1)]
    public bool IsTriggerEnabled(trigger? whichTrigger) => whichTrigger?.Enabled ?? false;

    /// <summary>Kept and read back; TriggerExecuteWait doesn't wait either way.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public void TriggerWaitOnSleeps(trigger? whichTrigger, bool flag)
    {
        if (whichTrigger is not null) whichTrigger.WaitOnSleeps = flag;
    }

    /// <summary>False for a new trigger.</summary>
    [Native(Level.Exact, Fact = Probe1)]
    public bool IsTriggerWaitOnSleeps(trigger? whichTrigger) => whichTrigger?.WaitOnSleeps ?? false;

    /// <summary>Its events and TriggerExecute count; TriggerEvaluate doesn't.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public int GetTriggerEvalCount(trigger? whichTrigger) => whichTrigger?.EvalCount ?? 0;

    /// <summary>Its runs: TriggerExecute, and its events whose conditions passed, counted before the actions run.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public int GetTriggerExecCount(trigger? whichTrigger) => whichTrigger?.ExecCount ?? 0;

    [Native(Level.Exact, Fact = Probe1)]
    public void ResetTrigger(trigger? whichTrigger)
    {
        if (whichTrigger is null) return;
        whichTrigger.EvalCount = 0;
        whichTrigger.ExecCount = 0;
    }

    /// <summary>
    /// The actions run in the order added, each in turn after the one before has ended, waits and all; an error ends
    /// only its own action. A run takes the actions the trigger has as it starts: one added or removed meanwhile changes
    /// the next run.
    /// </summary>
    [Native(Level.Exact, Fact = Probe2)]
    public triggeraction? TriggerAddAction(trigger? whichTrigger, Action? actionFunc)
    {
        if (whichTrigger is null)
        {
            Release(actionFunc);
            return null;
        }
        var action = Make(new triggeraction { Code = actionFunc });
        whichTrigger.Actions.Add(action);
        return action;
    }

    [Native(Level.Exact, Fact = Probe2)]
    public void TriggerRemoveAction(trigger? whichTrigger, triggeraction? whichAction)
    {
        if (whichTrigger is not null && whichAction is not null && whichTrigger.Actions.Remove(whichAction)) LetGo(whichTrigger, whichAction);
    }

    [Native(Level.Exact, Fact = Probe2)]
    public void TriggerClearActions(trigger? whichTrigger)
    {
        if (whichTrigger is null) return;
        foreach (var action in whichTrigger.Actions) LetGo(whichTrigger, action);
        whichTrigger.Actions.Clear();
    }

    /// <summary>
    /// A nil condition makes the trigger's conditions fail. A destroyed condition fails, until a Condition or Filter made
    /// after it takes its place and runs there (decisions D115).
    /// </summary>
    [Native(Level.Exact, Fact = Probe1 + "; " + Handles)]
    public triggercondition? TriggerAddCondition(trigger? whichTrigger, boolexpr? condition)
    {
        if (whichTrigger is null) return null;
        var added = Make(new triggercondition { Condition = condition });
        whichTrigger.Conditions.Add(added);
        return added;
    }

    [Native(Level.Exact, Fact = Probe1 + "; " + Handles)]
    public void TriggerRemoveCondition(trigger? whichTrigger, triggercondition? whichCondition)
    {
        if (whichCondition is not null && whichTrigger?.Conditions.Remove(whichCondition) == true) _game.Remove(whichCondition.Key);
    }

    [Native(Level.Exact, Fact = Probe2)]
    public void TriggerClearConditions(trigger? whichTrigger)
    {
        if (whichTrigger is null) return;
        foreach (var condition in whichTrigger.Conditions) _game.Remove(condition.Key);
        whichTrigger.Conditions.Clear();
    }

    /// <summary>
    /// Runs every condition in order, also after one has failed, and answers whether all passed: true with none. A
    /// condition that raises, or waits, fails. Counts nothing; a disabled trigger's conditions run too. Close: with
    /// Condition(nil) in an And, Or or Not it gives the model's value, not the game's (decisions D114).
    /// </summary>
    [Native(Level.Close)]
    public bool TriggerEvaluate(trigger? whichTrigger) =>
        whichTrigger is not null && In(Current with { Trigger = whichTrigger }, () => Passes(whichTrigger));

    /// <summary>
    /// Runs the actions in a new thread, also for a disabled trigger, with the caller's event; the caller goes on when
    /// they end or wait. Counts an evaluation and a run.
    /// </summary>
    [Native(Level.Exact, Fact = Probe1)]
    public void TriggerExecute(trigger? whichTrigger)
    {
        if (whichTrigger is null) return;
        whichTrigger.EvalCount++;
        whichTrigger.ExecCount++;
        Execute(whichTrigger, Current with { Trigger = whichTrigger });
    }

    /// <summary>As TriggerExecute: the caller doesn't wait for the actions' waits, even with TriggerWaitOnSleeps.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public void TriggerExecuteWait(trigger? whichTrigger) => TriggerExecute(whichTrigger);

    /// <summary>
    /// Runs the script's global function in a new thread with the caller's trigger and event; the caller goes on when it
    /// ends or waits. Null, which a JASS map can give, runs nothing (probe jass-executefunc-null). A name that isn't a
    /// function raises in that thread, which logs it; the game's JASS runs nothing and says nothing (the same probe).
    /// </summary>
    [Native(Level.Exact, Fact = Probe2)]
    public void ExecuteFunc(string? funcName)
    {
        if (funcName is not null && Threads is { } threads) RunThread(Current, null, [() => threads.Start(funcName)]);
    }

    /// <summary>
    /// Waits in a trigger's actions or ExecuteFunc's function until the first tenth of a second of game time after the
    /// timeout (decisions D71). A wait in a timer's callback or a condition never ends; in the main thread, or past a C
    /// function, it raises. The first wait started in the step a force ForForce ran over was destroyed in never ends
    /// either (DestroyForce).
    /// </summary>
    [Native(Level.Close)]
    public void TriggerSleepAction(float timeout)
    {
        _sleep = _waitLost == _game.Step ? float.PositiveInfinity : timeout;
        _waitLost = -1;
        if (Threads is null) throw new InvalidOperationException("attempt to yield from outside a coroutine");
        Threads.Yield();
    }

    /// <summary>The trigger whose conditions or actions run; nil outside one, and ExecuteFunc keeps its caller's.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public trigger? GetTriggeringTrigger() => Current.Trigger;

    /// <summary>
    /// The event that fired the trigger, kept through TriggerExecute and ExecuteFunc: EVENT_GAME_TIMER_EXPIRED for timer
    /// events, EVENT_GAME_STATE_LIMIT for a float game state, the comparison less 255 for an integer one, and a handle
    /// with id 0 outside an event. Lua's == holds with any handle of its id, ConvertPlayerColor(4) too (decisions D112).
    /// </summary>
    [Native(Level.Exact, Fact = Probe1 + "; " + Probe3 + "; " + Handles)]
    public eventid? GetTriggerEventId() => Current.Event;

    /// <summary>
    /// A new handle each call, even for the same function, in a destroyed one's place if there is one (decisions D115).
    /// Condition(nil) and Filter(nil) give one handle every time, the same for both (decisions D114), which passes as a
    /// trigger's condition and is false in an And, Or or Not. Close: there the game's reads a value left from what ran
    /// before it, which this matches in 15 of the 21 cases measured (decisions D114).
    /// </summary>
    [Native(Level.Close)]
    public conditionfunc? Condition(Func<bool>? func) =>
        func is null ? _noCondition ??= Nil(new conditionfunc()) : Make(new conditionfunc { Place = Take(func) });

    /// <summary>As Condition.</summary>
    [Native(Level.Close)]
    public filterfunc? Filter(Func<bool>? func) =>
        func is null ? _noFilter ??= Nil(new filterfunc()) : Make(new filterfunc { Place = Take(func) });

    // Condition(nil) and Filter(nil) are one handle in the game: here two, seen as each declares, of one id (decisions D114)
    T Nil<T>(T made) where T : boolexpr
    {
        if ((_noCondition ?? (boolexpr?)_noFilter) is not { } other) return Make(made);
        made.Id = other.Id;
        made.Key = _game.Add(made);
        if (_game.Handle(other.Key) is null) _game.Remove(made.Key); // the other was destroyed, so this one is too
        return made;
    }

    Place Take(Func<bool> code)
    {
        var place = _places.Count > 0 ? _places.Dequeue() : new Place();
        place.Code = code;
        return place;
    }

    /// <summary>
    /// The second operand runs only when the first passes; a nil operand is false. Not matched: with Condition(nil) in
    /// it, 6 of the 21 cases measured differ, such as And(t, Condition(nil)) for a condition t that passes, which passes
    /// in the game and fails here (decisions D114).
    /// </summary>
    [Native(Level.Close)]
    public boolexpr? And(boolexpr? operandA, boolexpr? operandB) => Make(new boolexpr { Junction = Junction.And, A = operandA, B = operandB });

    /// <summary>The second operand runs only when the first fails; a nil operand is false. Close as And (decisions D114).</summary>
    [Native(Level.Close)]
    public boolexpr? Or(boolexpr? operandA, boolexpr? operandB) => Make(new boolexpr { Junction = Junction.Or, A = operandA, B = operandB });

    /// <summary>Not(nil) is false. Close as And (decisions D114).</summary>
    [Native(Level.Close)]
    public boolexpr? Not(boolexpr? operand) => Make(new boolexpr { Junction = Junction.Not, A = operand });

    /// <summary>A destroyed And, Or or Not still runs where it was added.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public void DestroyBoolExpr(boolexpr? e) => Destroy(e);

    /// <summary>
    /// A destroyed condition no longer runs, and fails where it was added, until a Condition or Filter made after it takes
    /// its place and runs there (decisions D115).
    /// </summary>
    [Native(Level.Exact, Fact = Probe2 + "; " + Handles)]
    public void DestroyCondition(conditionfunc? c) => Destroy(c);

    /// <summary>As DestroyCondition.</summary>
    [Native(Level.Exact, Fact = Probe2 + "; " + Handles)]
    public void DestroyFilter(filterfunc? f) => Destroy(f);

    /// <summary>
    /// Fires after the timeout, at least 0.01 s, and again every timeout when periodic, without drifting. Its event is
    /// EVENT_GAME_TIMER_EXPIRED, and GetExpiredTimer is nil in it. Among what falls due together it keeps the place it
    /// got when registered, every time (decisions D76).
    /// </summary>
    [Native(Level.Exact, Fact = Probe1 + "; " + Probe2)]
    public @event? TriggerRegisterTimerEvent(trigger? whichTrigger, float timeout, bool periodic)
    {
        if (whichTrigger is null) return null;
        var registered = Register(whichTrigger);
        double period = timeout >= 0.01 ? timeout : 0.01, start = Seconds;
        long place = 0;
        void Queue(long lap)
        {
            var step = Game.Steps(start + lap * period);
            if (lap == 1) place = _game.At(step, Expire);
            else _game.At(step, place, Expire);
            void Expire()
            {
                if (whichTrigger.Destroyed) return;
                if (periodic) Queue(lap + 1);
                Fire(registered, Convert<gameevent>(TimerExpired));
            }
        }
        Queue(1);
        return registered;
    }

    /// <summary>
    /// Fires when the timer expires, before its callback, with GetExpiredTimer the timer; registered twice, twice
    /// (facts, "Errors and callbacks").
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Errors and callbacks: a timer-expiry trigger fires before the timer's own callback (10-01); " + Probe2)]
    public @event? TriggerRegisterTimerExpireEvent(trigger? whichTrigger, timer? t)
    {
        if (whichTrigger is null || t is null) return null;
        var registered = Register(whichTrigger);
        registered.Timer = t;
        (t.ExpireEvents ??= []).Add(registered);
        return registered;
    }

    /// <summary>Never fires: the game's doesn't for a global the script sets.</summary>
    [Native(Level.Exact, Fact = Probe1)]
    public @event? TriggerRegisterVariableEvent(trigger? whichTrigger, string? varName, limitop? opcode, float limitval) =>
        whichTrigger is null ? null : Register(whichTrigger);

    /// <summary>
    /// An integer game state's event fires inside SetIntegerGameState when the value changes to one that passes the
    /// comparison, and NOT_EQUAL's on every change. The time of day's fire inside SetFloatGameState and as the day
    /// passes the limit (GameState.cs). Close: three things the events probe saw the game do in the frames after a set
    /// aren't modelled (decisions D144).
    /// </summary>
    [Native(Level.Close)]
    public @event? TriggerRegisterGameStateEvent(trigger? whichTrigger, gamestate? whichState, limitop? opcode, float limitval)
    {
        if (whichTrigger is null || whichState is null || opcode is null) return null;
        var registered = Register(whichTrigger);
        _stateEvents.Add((whichState, opcode, limitval, registered));
        ScheduleDay();
        return registered;
    }

    /// <summary>The game state of the event that fired the trigger; nil outside one.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public gamestate? GetEventGameState() => Current.State;

    // SetIntegerGameState changed the state: its events whose comparison passes fire, with the comparison less 255 as
    // their event id (probe triggers-3)
    void IntegerStateChanged(igamestate whichIntegerGameState, int value)
    {
        foreach (var (state, op, limit, registered) in _stateEvents.ToArray())
            if (state == whichIntegerGameState && (op.Id == 5 || Compare(op.Id, value, limit)))
                Fire(registered, Convert<gameevent>(op.Id - 255), state);
    }

    @event Register(trigger t)
    {
        var registered = Make(new @event { Trigger = t });
        t.Events.Add(registered);
        return registered;
    }

    // An event: counted; then, when the trigger is enabled and its conditions pass, its actions run (probe triggers-2)
    void Fire(@event registered, eventid id, gamestate? state = null, player? player = null, playerstate? playerState = null, widget? widget = null)
    {
        if (registered.Trigger is not { Destroyed: false } t) return;
        t.EvalCount++;
        if (!t.Enabled) return;
        var firing = new Firing(t, id, state, player, playerState, widget);
        if (!In(firing, () => Passes(t))) return;
        t.ExecCount++;
        Execute(t, firing);
    }

    bool Passes(trigger t)
    {
        var passes = true;
        foreach (var condition in t.Conditions.ToArray()) passes &= Holds(condition.Condition, alone: true);
        return passes;
    }

    // Probes triggers-2 and triggers-3 (decisions D73, D114)
    static bool Holds(boolexpr? e, bool alone = false) => e switch
    {
        null => false,
        { Junction: Junction.Not } => e.A is not null && !Holds(e.A),
        { Junction: Junction.And } => Holds(e.A) && Holds(e.B),
        { Junction: Junction.Or } => Holds(e.A) || Holds(e.B),
        { Place: null } => alone,
        { Place.Code: { } code } => code(),
        _ => false,
    };

    // A destroyed Condition or Filter frees its place and stays the place's: where it is held, or given later, it runs
    // whatever the place holds, nothing until a new one takes it (decisions D115). A destroyed And, Or or Not still runs
    // where it was added.
    void Destroy(boolexpr? e)
    {
        if (e is null) return;
        // shortcut: a destroyed Condition or Filter stays in the game for good, as Lua may still give it to a native (a
        // filter destroyed per call by blizzard.j's matching functions keeps about 80 bytes each); let it go once Lua
        // has collected its userdata, with wave 10's collector-driven id reuse (docs/tasks/later.md).
        if (e.Place is { } place)
        {
            if (place.Code is null) return;
            Release(place.Code);
            place.Code = null;
            _places.Enqueue(place);
            return;
        }
        _game.Remove(e.Key);
        // Condition(nil) and Filter(nil) are one handle in the game (decisions D114)
        if (e != _noCondition && e != _noFilter) return;
        if (_noCondition is not null) _game.Remove(_noCondition.Key);
        if (_noFilter is not null) _game.Remove(_noFilter.Key);
    }

    void Execute(trigger t, Firing firing) =>
        RunThread(firing, t, [.. t.Actions.Select(action => (Func<IScriptThread?>)(() => Start(action.Code)))]);

    static IScriptThread? Start(Action? code)
    {
        if (code?.Target is IScriptFunction function) return function.Start();
        code?.Invoke();
        return null;
    }

    // A thread: its steps in turn, each until it ends or waits; a wait goes on at the step WaitEnd gives. Interrupted, it
    // stops after the step it is in, or at the end of the wait it is in or starts next (probe triggers-4; decisions D77)
    void RunThread(Firing firing, trigger? owner, Func<IScriptThread?>[] steps)
    {
        if (owner is not null) owner.Running++;
        int next = 0, interrupts = owner?.Interrupts ?? 0;
        IScriptThread? waiting = null;
        void Go()
        {
            var outer = _firing;
            _firing = firing;
            try
            {
                while (true)
                {
                    _sleep = 0;
                    if (owner is not null && owner.Interrupts != interrupts)
                    {
                        waiting?.Dispose();
                        break;
                    }
                    if (waiting is not null)
                    {
                        if (!waiting.Resume()) waiting = null;
                    }
                    else if (next < steps.Length) waiting = steps[next++]();
                    else break;
                    if (waiting is not null)
                    {
                        _game.At(WaitEnd(_sleep), Go);
                        return;
                    }
                }
            }
            finally { _firing = outer; }
            if (owner is not null && --owner.Running == 0)
            {
                foreach (var action in owner.Removed) Release(action.Code);
                owner.Removed.Clear();
            }
        }
        Go();
    }

    // The first tenth of a second of game time after the timeout has passed (decisions D71); one past the clock's range
    // never comes
    long WaitEnd(float timeout)
    {
        var end = _game.Step + Game.Steps(Math.Max(timeout, 0));
        if (end >= Game.Steps(double.PositiveInfinity)) return end;
        var tick = end / Game.StepsPerSecond * 10 + end % Game.StepsPerSecond * 10 / Game.StepsPerSecond;
        while (Game.Steps(tick / 10.0) <= end) tick++;
        return Game.Steps(tick / 10.0);
    }

    // An action's function is let go of once no thread of its trigger can still run it
    void LetGo(trigger t, triggeraction action)
    {
        _game.Remove(action.Key);
        if (t.Running > 0) t.Removed.Add(action);
        else Release(action.Code);
    }

    T In<T>(Firing firing, Func<T> run)
    {
        var outer = _firing;
        _firing = firing;
        try { return run(); }
        finally { _firing = outer; }
    }
}
