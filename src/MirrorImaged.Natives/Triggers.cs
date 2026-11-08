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

public partial class boolexpr
{
    internal Func<bool>? Code;  // a Condition's or a Filter's
    internal Junction Junction;
    internal boolexpr? A, B;
    internal bool Destroyed;
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
    sealed record Firing(trigger? Trigger, eventid Event, gamestate? State = null);

    readonly List<(gamestate State, limitop Op, float Limit, @event Event)> _stateEvents = [];
    Firing? _firing, _outside;
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
        _game.Remove(whichTrigger.Key);
    }

    /// <summary>
    /// Whether a run of its actions hasn't ended, waiting or not, also one interrupted that hasn't reached its end yet;
    /// its conditions in TriggerEvaluate are no run.
    /// </summary>
    [Native(Level.Exact, Fact = Probe4)]
    public bool BlzTriggerIsRunning(trigger? whichTrigger) => whichTrigger?.Running > 0;

    /// <summary>
    /// Stops its runs: the action running goes on to its end, but no other action runs, and a run that waits never goes
    /// on after its wait.
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
    /// A nil condition makes the trigger's conditions fail. Close: in the game a destroyed condition's place ran a
    /// condition made after it (decisions D73).
    /// </summary>
    [Native(Level.Close)]
    public triggercondition? TriggerAddCondition(trigger? whichTrigger, boolexpr? condition)
    {
        if (whichTrigger is null) return null;
        var added = Make(new triggercondition { Condition = condition });
        whichTrigger.Conditions.Add(added);
        return added;
    }

    /// <summary>Close as TriggerAddCondition (decisions D73).</summary>
    [Native(Level.Close)]
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
    /// condition that raises, or waits, fails. Counts nothing; a disabled trigger's conditions run too. Close as
    /// TriggerAddCondition (decisions D73).
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
    /// ends or waits. A name that isn't a function raises in that thread (not measured).
    /// </summary>
    [Native(Level.Exact, Fact = Probe2)]
    public void ExecuteFunc(string? funcName)
    {
        if (funcName is not null && Threads is { } threads) RunThread(Current, null, [() => threads.Start(funcName)]);
    }

    /// <summary>
    /// Waits in a trigger's actions or ExecuteFunc's function until the first tenth of a second of game time after the
    /// timeout (decisions D71). A wait in a timer's callback or a condition never ends; in the main thread, or past a C
    /// function, it raises.
    /// </summary>
    [Native(Level.Close)]
    public void TriggerSleepAction(float timeout)
    {
        _sleep = timeout;
        if (Threads is null) throw new InvalidOperationException("attempt to yield from outside a coroutine");
        Threads.Yield();
    }

    /// <summary>The trigger whose conditions or actions run; nil outside one, and ExecuteFunc keeps its caller's.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public trigger? GetTriggeringTrigger() => Current.Trigger;

    /// <summary>
    /// The event that fired the trigger, kept through TriggerExecute and ExecuteFunc: EVENT_GAME_TIMER_EXPIRED for timer
    /// events, EVENT_GAME_STATE_LIMIT for a float game state, the comparison less 255 for an integer one, and a handle
    /// with id 0 outside an event. Lua's == compares it with a constant of another type (decisions D72), but not with
    /// another handle of the same id, as the game's does.
    /// </summary>
    [Native(Level.Close)]
    public eventid? GetTriggerEventId() => Current.Event;

    /// <summary>
    /// A new handle each call, even for the same function. Condition(nil) gives one handle every time, which passes as a
    /// trigger's condition and is false in an And, Or or Not; in the game it equals Filter(nil) too (decisions D73).
    /// </summary>
    [Native(Level.Close)]
    public conditionfunc? Condition(Func<bool>? func) =>
        func is null ? _noCondition ??= Make(new conditionfunc()) : Make(new conditionfunc { Code = func });

    /// <summary>As Condition.</summary>
    [Native(Level.Close)]
    public filterfunc? Filter(Func<bool>? func) =>
        func is null ? _noFilter ??= Make(new filterfunc()) : Make(new filterfunc { Code = func });

    /// <summary>
    /// The second operand runs only when the first passes; a nil operand is false. Not matched: And(x, Condition(nil))
    /// is x in the game, false here.
    /// </summary>
    [Native(Level.Close)]
    public boolexpr? And(boolexpr? operandA, boolexpr? operandB) => Make(new boolexpr { Junction = Junction.And, A = operandA, B = operandB });

    /// <summary>The second operand runs only when the first fails; a nil operand is false.</summary>
    [Native(Level.Exact, Fact = Probe3)]
    public boolexpr? Or(boolexpr? operandA, boolexpr? operandB) => Make(new boolexpr { Junction = Junction.Or, A = operandA, B = operandB });

    /// <summary>Not(nil) is false.</summary>
    [Native(Level.Exact, Fact = Probe3)]
    public boolexpr? Not(boolexpr? operand) => Make(new boolexpr { Junction = Junction.Not, A = operand });

    /// <summary>A destroyed And, Or or Not still runs where it was added.</summary>
    [Native(Level.Exact, Fact = Probe2)]
    public void DestroyBoolExpr(boolexpr? e) => Destroy(e);

    /// <summary>
    /// A destroyed condition no longer runs, and fails where it was added. Close: in the game a condition made after it
    /// ran in its place (decisions D73).
    /// </summary>
    [Native(Level.Close)]
    public void DestroyCondition(conditionfunc? c) => Destroy(c);

    /// <summary>As DestroyCondition.</summary>
    [Native(Level.Exact, Fact = Probe2)]
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
    /// comparison, and NOT_EQUAL's on every change. The time of day's comes with its clock, in W1-9 (decisions D76).
    /// </summary>
    [Native(Level.Close)]
    public @event? TriggerRegisterGameStateEvent(trigger? whichTrigger, gamestate? whichState, limitop? opcode, float limitval)
    {
        if (whichTrigger is null || whichState is null || opcode is null) return null;
        var registered = Register(whichTrigger);
        _stateEvents.Add((whichState, opcode, limitval, registered));
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
            if (state == whichIntegerGameState && op.Id switch
                {
                    0 => value < limit,
                    1 => value <= limit,
                    2 => value == limit,
                    3 => value >= limit,
                    4 => value > limit,
                    5 => true,
                    _ => false,
                })
                Fire(registered, Convert<gameevent>(op.Id - 255), state);
    }

    @event Register(trigger t)
    {
        var registered = Make(new @event { Trigger = t });
        t.Events.Add(registered);
        return registered;
    }

    // An event: counted; then, when the trigger is enabled and its conditions pass, its actions run (probe triggers-2)
    void Fire(@event registered, eventid id, gamestate? state = null)
    {
        if (registered.Trigger is not { Destroyed: false } t) return;
        t.EvalCount++;
        if (!t.Enabled) return;
        var firing = new Firing(t, id, state);
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

    // Probes triggers-2 and triggers-3 (decisions D73)
    static bool Holds(boolexpr? e, bool alone = false) => e switch
    {
        null => false,
        { Junction: Junction.Not } => e.A is not null && !Holds(e.A),
        { Junction: Junction.And } => Holds(e.A) && Holds(e.B),
        { Junction: Junction.Or } => Holds(e.A) || Holds(e.B),
        { Destroyed: true } => false,
        { Code: null } => alone,
        _ => e.Code(),
    };

    void Destroy(boolexpr? e)
    {
        if (e is null) return;
        e.Destroyed = true;
        Release(e.Code);
        _game.Remove(e.Key);
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
    // stops after the step it is in, or at its wait's end (probe triggers-4)
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
