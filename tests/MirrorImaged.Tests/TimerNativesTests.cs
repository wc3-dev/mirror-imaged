using System.Globalization;
using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>The timer natives, against the facts' "Errors and callbacks" and "Lua runtime" timer measurements.</summary>
public sealed class TimerNativesTests
{
    readonly Game _game = new(0);
    readonly GameNatives _natives;
    readonly List<string> _ran = [];

    public TimerNativesTests() => _natives = new GameNatives(_game);

    double Seconds => _game.Step / (double)Game.StepsPerSecond;

    string Now => Seconds.ToString(CultureInfo.InvariantCulture);

    void Advance(double seconds) => _game.Advance(Game.Steps(seconds));

    timer Start(float timeout, bool periodic, Action? callback)
    {
        var timer = _natives.CreateTimer()!;
        _natives.TimerStart(timer, timeout, periodic, callback);
        return timer;
    }

    [Fact]
    public void Timers_run_in_the_order_they_fall_due_at_their_times()
    {
        Start(2, false, () => _ran.Add($"two {Now}"));
        Start(0.5f, false, () => _ran.Add($"half {Now}"));
        Start(0.75f, true, () => _ran.Add($"every {Now}"));

        Advance(2);

        Assert.Equal(["half 0.5", "every 0.75", "every 1.5", "two 2"], _ran);
    }

    [Fact]
    public void A_periodic_timer_doesnt_drift()
    {
        var times = new List<long>();
        Start(0.03f, true, () => times.Add(_game.Step));

        Advance(3600);

        Assert.Equal(120000, times.Count);
        for (int lap = 1; lap <= times.Count; lap += 997)
            Assert.Equal(Game.Steps(lap * (double)0.03f), times[lap - 1]);
    }

    // facts: a periodic timer of 1/256 s and a one-shot started again every 1/256 s each fire 256 times a game second
    [Fact]
    public void A_periodic_256th_second_timer_and_a_one_shot_restarted_every_256th_run_256_times_a_second()
    {
        int periodic = 0, oneShot = 0;
        Start(1 / 256f, true, () => periodic++);
        var restarted = _natives.CreateTimer();
        void Again()
        {
            oneShot++;
            _natives.TimerStart(restarted, 1 / 256f, false, Again);
        }
        _natives.TimerStart(restarted, 1 / 256f, false, Again);

        Advance(1);

        Assert.Equal(256, periodic);
        Assert.Equal(256, oneShot);
    }

    // facts: a 0 s timer started inside a callback runs in the same frame; six links all read the same elapsed time
    [Fact]
    public void A_0_second_timer_started_in_a_callback_runs_in_the_same_step()
    {
        var clock = Start(100000, false, null);
        var reads = new List<float>();
        void Link(int left)
        {
            reads.Add(_natives.TimerGetElapsed(clock));
            if (left > 0) _natives.TimerStart(_natives.CreateTimer(), 0, false, () => Link(left - 1));
        }
        Start(0.5f, false, () => Link(6));

        Advance(0.5);

        Assert.Equal(Enumerable.Repeat(0.5f, 7), reads);
    }

    // facts: a 10 s timer started again in its own callback keeps time across a lap, with no drift against a 30 s timer
    [Fact]
    public void A_10_second_timer_started_again_in_its_callback_keeps_time()
    {
        var thirty = Start(30, false, null);
        var laps = _natives.CreateTimer();
        void Lap()
        {
            _ran.Add(_natives.TimerGetElapsed(thirty).ToString(CultureInfo.InvariantCulture));
            _natives.TimerStart(laps, 10, false, Lap);
        }
        _natives.TimerStart(laps, 10, false, Lap);

        Advance(30);

        Assert.Equal(["10", "20", "30"], _ran);
    }

    // facts: elapsed of a timer started for 604800 s steps by 0.0625 s, of one for 10 s is exact
    [Fact]
    public void Elapsed_is_the_timeout_less_the_remaining_time_in_single_floats()
    {
        var week = Start(604800, false, null);
        var ten = Start(10, false, null);
        var reads = new List<float>();
        for (int i = 0; i < 100; i++)
        {
            Advance(0.01);
            reads.Add(_natives.TimerGetElapsed(week));
            Assert.Equal((float)Seconds, _natives.TimerGetElapsed(ten));
        }

        Assert.All(reads, read => Assert.Equal(0, read % 0.0625f));
        Assert.InRange(reads.Zip(reads.Skip(1)).Count(pair => pair.First == pair.Second), 50, 99);
    }

    [Fact]
    public void Remaining_and_timeout_read_the_timer()
    {
        var timer = Start(2, false, null);
        Advance(0.5);

        Assert.Equal(1.5f, _natives.TimerGetRemaining(timer));
        Assert.Equal(0.5f, _natives.TimerGetElapsed(timer));
        Assert.Equal(2f, _natives.TimerGetTimeout(timer));

        Advance(2);
        Assert.Equal(0f, _natives.TimerGetRemaining(timer));
        Assert.Equal(2f, _natives.TimerGetElapsed(timer));
    }

    [Fact]
    public void A_paused_timer_keeps_its_remaining_time_and_runs_it_out_when_resumed()
    {
        var timer = Start(1, true, () => _ran.Add($"{Now}"));
        Advance(0.25);
        _natives.PauseTimer(timer);
        Advance(5);
        Assert.Empty(_ran);
        Assert.Equal(0.75f, _natives.TimerGetRemaining(timer));

        _natives.ResumeTimer(timer);
        Advance(2);

        Assert.Equal(["6", "7"], _ran);
    }

    [Fact]
    public void Starting_a_timer_again_starts_it_over()
    {
        var timer = Start(1, false, () => _ran.Add($"old {Now}"));
        Advance(0.5);
        _natives.TimerStart(timer, 1, false, () => _ran.Add($"new {Now}"));

        Advance(2);

        Assert.Equal(["new 1.5"], _ran);
    }

    [Fact]
    public void A_timer_lets_go_of_its_callback_when_started_again_or_destroyed()
    {
        var first = new Callback();
        var second = new Callback();
        var timer = Start(1, false, first.Run);
        _natives.TimerStart(timer, 1, false, second.Run);
        Assert.True(first.Disposed);

        _natives.DestroyTimer(timer);
        Advance(2);

        Assert.True(second.Disposed);
        Assert.False(second.Ran);
        Assert.Null(_game.Handle(timer.Id));
    }

    sealed class Callback : IDisposable
    {
        public bool Ran, Disposed;
        public void Run() => Ran = true;
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void The_expired_timer_is_the_one_whose_callback_runs()
    {
        timer? expired = null;
        var timer = Start(1, false, () => expired = _natives.GetExpiredTimer());

        Assert.Null(_natives.GetExpiredTimer());
        Advance(1);

        Assert.Same(timer, expired);
        Assert.Null(_natives.GetExpiredTimer());
    }

    [Fact]
    public void A_periodic_0_second_timer_runs_once_a_step()
    {
        var runs = 0;
        Start(0, true, () => runs++);

        _game.Advance(9);

        Assert.Equal(10, runs);
    }

    [Fact]
    public void A_null_timer_does_nothing()
    {
        _natives.TimerStart(null, 1, false, () => _ran.Add("ran"));
        _natives.PauseTimer(null);
        _natives.DestroyTimer(null);
        Advance(2);

        Assert.Empty(_ran);
        Assert.Equal(0f, _natives.TimerGetElapsed(null));
    }
}
