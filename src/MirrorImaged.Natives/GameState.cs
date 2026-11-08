using MirrorImaged.World;

namespace MirrorImaged.Natives;

// The game's speed, pause, difficulty and states, and the time of day as a clock (facts, "Time of day"): an hour every
// 20 game seconds at scale 1, a day of 24 hours, the standard gameplay constants (DayLength 480, DayHours 24), or the
// map's own (decisions D208). The time is worked out from the clock, not added up frame by frame, so it moves only
// where an event's pass sets it to its limit, by under a step's share each time (decisions D144); the game adds each
// frame's share and loses a little (2.5 millionths of an hour in 2 s, 0.00013 in 20, docs/facts.md), which isn't
// modelled.
public sealed partial class GameNatives
{
    double? _dayHours;

    double DayHours => _dayHours ??= Positive(Data.Constant("DayHours", 24), 24);

    double HoursPerSecond => DayHours / Positive(Data.Constant("DayLength", 480), 480);

    // A day of no hours or no length is the standard one (decisions D215, not measured).
    static double Positive(float value, double standard) => float.IsFinite(value) && value > 0 ? value : standard;

    int _speed = 2, _difficulty; // MAP_SPEED_NORMAL, MAP_DIFFICULTY_EASY
    readonly Dictionary<int, int> _integerStates = [];
    double _day; // the time of day at _dayStep, in hours
    long _dayStep, _daySetStep = -1; // _daySetStep: the step of the last SetFloatGameState
    Action? _dayPass; // the one pass of a limit queued
    float _dayScale = 1;
    bool _daySuspended;

    // In hours from 0 below DayHours: running backward past 0 comes round to DayHours (not measured). % on doubles is exact, so a
    // huge scale still gives an hour of the day.
    double Day()
    {
        var hours = (_daySuspended ? _day : _day + (_game.Step - _dayStep) * (double)_dayScale * HoursPerSecond / Game.StepsPerSecond) % DayHours;
        if (hours < 0) hours += DayHours;
        return hours < DayHours ? hours : 0;
    }

    // Moves the time's start to now, before its speed or start changes.
    void DayFromNow()
    {
        _day = Day();
        _dayStep = _game.Step;
    }

    /// <summary>
    /// The time of day, cut toward zero to a float so that it reads under 24, and no more than 24 in a longer day
    /// (facts: DayHours=30); 0 for another state. Close: the game's
    /// own frames make it run a little slow (above), and its time-of-day events read the hour a float step under it at
    /// 12 (facts).
    /// </summary>
    [Native(Level.Close)]
    public float GetFloatGameState(fgamestate? whichFloatGameState) => whichFloatGameState?.Id == 2 ? TowardZero(Math.Min(Day(), 24)) : 0; // GAME_STATE_TIME_OF_DAY

    /// <summary>
    /// Sets the time of day, which reads back at once as set; below 0 or above 23.99 (from 23.995, measured), the
    /// day's hours less 0.01, it is 0 (with DayHours=30 the facts' 29.999 read 24, here 0). A set fires the time's events inside the call (the events probe): GREATER_THAN and GREATER_THAN_OR_EQUAL when it
    /// goes up from the limit or below to the limit or above, LESS_THAN and LESS_THAN_OR_EQUAL when it goes down from
    /// above the limit to the limit or below, EQUAL when it lands on the limit with the day stopped (running, the day
    /// fires it as it moves on, ScheduleDay), and NOT_EQUAL never. Close: the game reads some values back a few float
    /// steps off, at once (0.1, 23.9) or after a frame (7 as 7.00000477; the lobby probe), and the frames after a set
    /// do more than this (decisions D144). Another state isn't kept.
    /// </summary>
    [Native(Level.Close)]
    public void SetFloatGameState(fgamestate? whichFloatGameState, float value)
    {
        if (whichFloatGameState?.Id != 2) return;
        var old = TowardZero(Day());
        var now = value >= 0 && value <= DayHours - 0.01 ? value : 0;
        (_day, _dayStep) = (now, _game.Step);
        var stopped = _daySuspended || _dayScale == 0;
        foreach (var (state, op, limit, registered) in _stateEvents.ToArray())
            if (state == whichFloatGameState && old != now && op.Id switch
                {
                    0 or 1 => old > limit && limit >= now,
                    2 => now == limit && stopped,
                    3 or 4 => old <= limit && limit <= now,
                    _ => false,
                })
                Fire(registered, Convert<gameevent>(3), state); // EVENT_GAME_STATE_LIMIT
        _daySetStep = _game.Step;
        ScheduleDay();
    }

    // Schedules the next moment the running day passes a limit of the time's events, which fires them with the time
    // at the limit (the events probe): forward its EQUAL, GREATER_THAN_OR_EQUAL and GREATER_THAN events, backward its
    // EQUAL, LESS_THAN_OR_EQUAL and LESS_THAN ones. The day passes a limit it is at a day later, unless it was just set
    // there in this step: then it passes it at the next step. Only the nearest pass is queued, so a map setting the
    // time every frame doesn't fill the queue.
    void ScheduleDay()
    {
        if (_dayPass is not null) _game.Cancel(_dayPass);
        _dayPass = null;
        if (_daySuspended || _dayScale == 0) return;
        var (now, forward) = (Day(), _dayScale > 0);
        double? nearest = null;
        float limit = 0;
        foreach (var (state, _, l, _) in _stateEvents)
        {
            if (state is not fgamestate { Id: 2 } || l < 0 || l >= DayHours) continue;
            var hours = forward ? l - now : now - l;
            if (hours < 0 || hours == 0 && _game.Step != _daySetStep) hours += DayHours;
            if (hours < nearest || nearest is null) (nearest, limit) = (hours, l);
        }
        if (nearest is not { } distance) return;
        var steps = Math.Max(1, Math.Ceiling(distance / (Math.Abs(_dayScale) * HoursPerSecond) * Game.StepsPerSecond));
        if (steps >= 1L << 60) return; // past the clock's range: never
        _dayPass = () =>
        {
            _dayPass = null;
            (_day, _dayStep) = (limit, _game.Step);
            foreach (var (state, op, l, registered) in _stateEvents.ToArray())
                if (state is fgamestate { Id: 2 } && l == limit && (op.Id == 2 || (forward ? op.Id is 3 or 4 : op.Id is 0 or 1)))
                    Fire(registered, Convert<gameevent>(3), state);
            ScheduleDay();
        };
        _game.At(_game.Step + (long)steps, _dayPass);
    }

    /// <summary>Sets the time's speed; an infinite or NaN scale is refused and the scale stays (not measured).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Time of day: scale 2 runs twice as fast, 0 stops, -1 runs backward, and it reads what was set (11-02)")]
    public void SetTimeOfDayScale(float r)
    {
        if (!float.IsFinite(r)) return;
        DayFromNow();
        _dayScale = r;
        ScheduleDay();
    }

    [Native(Level.Exact, Fact = "game-facts.md, Time of day: GetTimeOfDayScale reads 1, then what was set (11-02)")]
    public float GetTimeOfDayScale() => _dayScale;

    /// <summary>Stops the time of day, or runs it again at its scale; suspends aren't counted.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Time of day: suspending stops it, a time or scale set meanwhile holds, one resume runs it (11-02)")]
    public void SuspendTimeOfDay(bool b)
    {
        DayFromNow();
        _daySuspended = b;
        ScheduleDay();
    }

    /// <summary>0 at the start.</summary>
    [Native(Level.Exact, Fact = Probe3)]
    public int GetIntegerGameState(igamestate? whichIntegerGameState) =>
        whichIntegerGameState is null ? 0 : _integerStates.GetValueOrDefault(whichIntegerGameState.Id);

    /// <summary>
    /// Kept and read back; a change fires the state's events (TriggerRegisterGameStateEvent). What the game does with
    /// divine intervention or the disconnected state isn't simulated.
    /// </summary>
    [Native(Level.Exact, Fact = Probe3)]
    public void SetIntegerGameState(igamestate? whichIntegerGameState, int value)
    {
        if (whichIntegerGameState is null || GetIntegerGameState(whichIntegerGameState) == value) return;
        _integerStates[whichIntegerGameState.Id] = value;
        IntegerStateChanged(whichIntegerGameState, value);
    }

    /// <summary>
    /// The speed, normal at the start (the players probe). Game time is the simulation's whatever the speed: it
    /// changes only how fast real time would pass.
    /// </summary>
    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game: normal at the start of a -loadfile game")]
    public gamespeed? GetGameSpeed() => Convert<gamespeed>(_speed);

    /// <summary>Fastest is refused and the speed stays (the players probe); the others aren't all measured.</summary>
    [Native(Level.Close)]
    public void SetGameSpeed(gamespeed? whichspeed)
    {
        if (whichspeed is { Id: not 4 }) _speed = whichspeed.Id; // MAP_SPEED_FASTEST
    }

    /// <summary>
    /// Stops the game's time, timers and the time of day with it, once what is due in this step has run, until the map
    /// goes on (not measured).
    /// </summary>
    [Native(Level.Close)]
    public void PauseGame(bool flag) => _game.Paused = flag;

    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game: easy in a -loadfile game")]
    public gamedifficulty? GetGameDifficulty() => Convert<gamedifficulty>(_difficulty);

    [Native(Level.Close)]
    public void SetGameDifficulty(gamedifficulty? whichdifficulty)
    {
        if (whichdifficulty is not null) _difficulty = whichdifficulty.Id;
    }

    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game: VERSION_FROZEN_THRONE")]
    public version? VersionGet() => Convert<version>(1);

    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game: false in a -loadfile game")]
    public bool IsNoVictoryCheat() => false;

    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game: false in a -loadfile game")]
    public bool IsNoDefeatCheat() => false;

    /// <summary>
    /// Ends the game: the rest of the step still runs, the code after the call and a 0 s timer started before it among
    /// it, then the clock stops for good; no end-level event fires. The score screen isn't simulated: a -loadfile game
    /// skips it (facts, "Version and launching").
    /// </summary>
    [Native(Level.Exact, Fact = Events)]
    public void EndGame(bool doScoreScreen) => _game.Ended = true;
}
