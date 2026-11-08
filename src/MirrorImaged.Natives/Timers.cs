using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class timer
{
    internal float Timeout;
    internal bool Periodic;
    internal Action? Callback;
    internal bool Running;
    internal long Origin, Laps, Due;  // a run's expiries are at Origin plus whole timeouts, so it doesn't drift
    internal float Remaining;  // while not running
    internal int Generation;  // moved on to drop the expiry already queued
}

// Timers expire on the clock's steps (decisions D45), each step's in the order they were started: a timer started for
// 0 s in a callback runs in the same step, as the facts measure ("Errors and callbacks"). Measured and matched: a
// periodic 1/256 s timer, and a one-shot started again every 1/256 s, both run 256 times a second; a 10 s timer
// started again in its own callback keeps time. Not matched: the game ran timers due at 0.5, 2 and 3.25 s 0.0001 s
// early; here they run on time. Not measured: the order of timers due together, a timeout below a step, pausing.
public sealed partial class GameNatives
{
    timer? _expired;

    [Native(Level.Close)]
    public timer? CreateTimer() => Make(new timer());

    /// <summary>Stops the timer and lets go of its callback; Lua's handle then reads as null to the natives.</summary>
    [Native(Level.Close)]
    public void DestroyTimer(timer? whichTimer)
    {
        if (whichTimer is null) return;
        PauseTimer(whichTimer);
        Release(whichTimer.Callback);
        whichTimer.Callback = null;
        _game.Remove(whichTimer.Id);
    }

    /// <summary>
    /// Starts the timer over. A timeout below 0 counts as 0; a periodic timer runs at most once a step, so a periodic
    /// 0 s timer runs every step.
    /// </summary>
    [Native(Level.Close)]
    public void TimerStart(timer? whichTimer, float timeout, bool periodic, Action? handlerFunc)
    {
        if (whichTimer is null) return;
        if (whichTimer.Callback != handlerFunc) Release(whichTimer.Callback);
        whichTimer.Timeout = timeout;
        whichTimer.Periodic = periodic;
        whichTimer.Callback = handlerFunc;
        Run(whichTimer, _game.Step);
    }

    /// <summary>
    /// The timeout minus the remaining time, in single floats, as the facts infer from the game's steps: 0.0625 s
    /// steps at 604800 s, exact at 10 s. The timeout once a one-shot has expired; 0 before a start.
    /// </summary>
    [Native(Level.Close)]
    public float TimerGetElapsed(timer? whichTimer) => whichTimer is null ? 0 : whichTimer.Timeout - TimerGetRemaining(whichTimer);

    /// <summary>The time to the next expiry; frozen while paused, 0 once a one-shot has expired.</summary>
    [Native(Level.Close)]
    public float TimerGetRemaining(timer? whichTimer) =>
        whichTimer is null ? 0
        : whichTimer.Running ? (float)((whichTimer.Due - _game.Step) / (double)Game.StepsPerSecond)
        : whichTimer.Remaining;

    [Native(Level.Close)]
    public float TimerGetTimeout(timer? whichTimer) => whichTimer?.Timeout ?? 0;

    [Native(Level.Close)]
    public void PauseTimer(timer? whichTimer)
    {
        if (whichTimer is not { Running: true }) return;
        whichTimer.Remaining = TimerGetRemaining(whichTimer);
        whichTimer.Running = false;
        whichTimer.Generation++;
    }

    /// <summary>Runs a paused timer on for its remaining time, then a periodic one for whole timeouts again.</summary>
    [Native(Level.Close)]
    public void ResumeTimer(timer? whichTimer)
    {
        if (whichTimer is not { Running: false, Remaining: > 0 }) return;
        Run(whichTimer, _game.Step + Game.Steps(whichTimer.Remaining) - Game.Steps(whichTimer.Timeout));
    }

    /// <summary>The timer whose callback is running; null outside one (the game may crash there, a claim).</summary>
    [Native(Level.Close)]
    public timer? GetExpiredTimer() => _expired;

    void Run(timer t, long origin)
    {
        t.Origin = origin;
        t.Laps = 0;
        t.Running = true;
        Queue(t);
    }

    void Queue(timer t)
    {
        var generation = ++t.Generation;
        t.Due = t.Origin + Game.Steps((t.Laps + 1) * (double)Math.Max(t.Timeout, 0));
        if (t.Laps > 0) t.Due = Math.Max(t.Due, _game.Step + 1);
        _game.At(t.Due, () =>
        {
            if (t.Generation == generation) Expire(t);
        });
    }

    void Expire(timer t)
    {
        if (t.Periodic)
        {
            t.Laps++;
            Queue(t);
        }
        else
        {
            t.Running = false;
            t.Remaining = 0;
        }
        var outer = _expired;
        _expired = t;
        try { t.Callback?.Invoke(); }
        finally { _expired = outer; }
    }

    // A callback a native lets go of is disposed when it can be, as a Lua function is (MirrorImaged's LuaCode).
    static void Release(Action? callback) => (callback?.Target as IDisposable)?.Dispose();
}
