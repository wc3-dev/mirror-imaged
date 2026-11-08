using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class timer
{
    internal float Timeout;
    internal bool Periodic;
    internal Action? Callback;
    internal bool Running, Paused;
    internal double Start;  // seconds: the expiries are at Start plus whole timeouts, so a timer doesn't drift
    internal long Laps;
    internal float Remaining;  // while paused
    internal int Generation;  // moved on to drop the expiry already queued
}

// Timers expire on the clock's steps (decisions D45): a timer started for 0 s in a callback runs in the same step, as
// the facts measure ("Errors and callbacks"). Measured and matched: a periodic 1/256 s timer, and a one-shot started
// again every 1/256 s, both run 256 times a second; a 10 s timer started again in its own callback keeps time.
// Not matched: the game ran timers due at 0.5, 2 and 3.25 s 0.0001 s early; here they run on time. Not measured: the
// order of timers due in one step, a timeout below a step, pausing and resuming.
public sealed partial class GameNatives
{
    timer? _expired;

    double Seconds => _game.Step / (double)Game.StepsPerSecond;

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
        _game.Remove(whichTimer.Key);
    }

    /// <summary>
    /// Starts the timer over. A timeout below 0 counts as 0; a periodic timer runs at most once a step, so a periodic
    /// 0 s timer runs every step. A callback given with no timer is let go of.
    /// </summary>
    [Native(Level.Close)]
    public void TimerStart(timer? whichTimer, float timeout, bool periodic, Action? handlerFunc)
    {
        if (whichTimer is null)
        {
            Release(handlerFunc);
            return;
        }
        if (whichTimer.Callback != handlerFunc) Release(whichTimer.Callback);
        whichTimer.Timeout = timeout;
        whichTimer.Periodic = periodic;
        whichTimer.Callback = handlerFunc;
        Run(whichTimer, Seconds);
    }

    /// <summary>
    /// The timeout less the remaining time, both cut toward zero to single floats, as the facts infer from the game's
    /// steps: 0.0625 s steps at 604800 s, exact at 10 s. The timeout once a one-shot has expired; 0 before a start.
    /// </summary>
    [Native(Level.Close)]
    public float TimerGetElapsed(timer? whichTimer) =>
        whichTimer is null ? 0 : TowardZero((double)whichTimer.Timeout - TimerGetRemaining(whichTimer));

    /// <summary>The time to the next expiry, cut toward zero to a single float; frozen while paused, 0 once a one-shot has expired.</summary>
    [Native(Level.Close)]
    public float TimerGetRemaining(timer? whichTimer) =>
        whichTimer is null ? 0
        : whichTimer.Running ? TowardZero(Math.Max(0, Expiry(whichTimer) - Seconds))
        : whichTimer.Remaining;

    [Native(Level.Close)]
    public float TimerGetTimeout(timer? whichTimer) => whichTimer?.Timeout ?? 0;

    [Native(Level.Close)]
    public void PauseTimer(timer? whichTimer)
    {
        if (whichTimer is not { Running: true }) return;
        whichTimer.Remaining = TimerGetRemaining(whichTimer);
        whichTimer.Running = false;
        whichTimer.Paused = true;
        whichTimer.Generation++;
    }

    /// <summary>
    /// Runs a paused timer on for its remaining time, then a periodic one for whole timeouts again. One paused in the
    /// step it was due, before it ran, runs as soon as it is resumed.
    /// </summary>
    [Native(Level.Close)]
    public void ResumeTimer(timer? whichTimer)
    {
        if (whichTimer is not { Paused: true }) return;
        Run(whichTimer, Seconds + whichTimer.Remaining - Period(whichTimer));
    }

    /// <summary>The timer whose callback is running; null outside one (the game may crash there, a claim).</summary>
    [Native(Level.Close)]
    public timer? GetExpiredTimer() => _expired;

    static double Period(timer t) => Math.Max(t.Timeout, 0);

    static double Expiry(timer t) => t.Start + (t.Laps + 1) * Period(t);

    void Run(timer t, double start)
    {
        t.Start = start;
        t.Laps = 0;
        t.Running = true;
        t.Paused = false;
        Queue(t);
    }

    void Queue(timer t)
    {
        var generation = ++t.Generation;
        var due = Game.Steps(Expiry(t));
        _game.At(t.Laps > 0 ? Math.Max(due, _game.Step + 1) : due, () =>
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
        try
        {
            // its expiry events fire first (facts, "Errors and callbacks")
            foreach (var registered in t.ExpireEvents?.ToArray() ?? []) Fire(registered, Convert<gameevent>(TimerExpired));
            t.Callback?.Invoke();
        }
        finally { _expired = outer; }
    }

    // To a single float, cut toward zero as the game's arithmetic cuts (D27); no NaN, as the game makes none.
    static float TowardZero(double x)
    {
        if (double.IsNaN(x)) return 0;
        var f = (float)x;
        return Math.Abs((double)f) <= Math.Abs(x) ? f : f > 0 ? MathF.BitDecrement(f) : MathF.BitIncrement(f);
    }

    // A callback a native lets go of is disposed when it can be, as a Lua function is (MirrorImaged's LuaCode).
    static void Release(Delegate? callback) => (callback?.Target as IDisposable)?.Dispose();
}
