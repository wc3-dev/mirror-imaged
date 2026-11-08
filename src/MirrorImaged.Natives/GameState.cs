using MirrorImaged.World;

namespace MirrorImaged.Natives;

// The game's speed, pause, difficulty and states, and the time of day as a clock (facts, "Time of day"): an hour every
// 20 game seconds at scale 1, a day of 24 hours, the standard gameplay constants (DayLength 480, DayHours 24; a map's
// own come with the game data). The time is worked out from the clock, so it doesn't drift; the game adds each frame's
// share and loses a little (2.5 millionths of an hour in 2 s, 0.00013 in 20, docs/facts.md), which isn't modelled.
public sealed partial class GameNatives
{
    const double HoursPerSecond = 24.0 / 480;

    int _speed = 2, _difficulty; // MAP_SPEED_NORMAL, MAP_DIFFICULTY_EASY
    readonly Dictionary<int, int> _integerStates = [];
    double _day; // the time of day at _dayStep, in hours
    long _dayStep;
    float _dayScale = 1;
    bool _daySuspended;

    // In hours from 0 below 24: running backward past 0 comes round to 24 (not measured). % on doubles is exact, so a
    // huge scale still gives an hour of the day.
    double Day()
    {
        var hours = (_daySuspended ? _day : _day + (_game.Step - _dayStep) * (double)_dayScale * HoursPerSecond / Game.StepsPerSecond) % 24;
        if (hours < 0) hours += 24;
        return hours < 24 ? hours : 0;
    }

    // Moves the time's start to now, before its speed or start changes.
    void DayFromNow()
    {
        _day = Day();
        _dayStep = _game.Step;
    }

    /// <summary>
    /// The time of day, cut toward zero to a float so that it reads under 24; 0 for another state. Close: the game's
    /// own frames make it run a little slow (above), and its time-of-day events read the hour a float step under it at
    /// 12 (facts).
    /// </summary>
    [Native(Level.Close)]
    public float GetFloatGameState(fgamestate? whichFloatGameState) => whichFloatGameState?.Id == 2 ? TowardZero(Day()) : 0; // GAME_STATE_TIME_OF_DAY

    /// <summary>
    /// Sets the time of day, which reads back at once as set; below 0 or above 23.99 (from 23.995, measured) it is 0.
    /// Close: the game reads 0.1 back a float step under it (docs/facts.md), and a 7 set on a Moonwell check map read
    /// 6.999999 later on (facts, 11-05), against the probe's 7 read at once (W1-9). Another state isn't kept.
    /// </summary>
    [Native(Level.Close)]
    public void SetFloatGameState(fgamestate? whichFloatGameState, float value)
    {
        if (whichFloatGameState?.Id != 2) return;
        _day = value is >= 0 and <= 23.99f ? value : 0;
        _dayStep = _game.Step;
    }

    /// <summary>Sets the time's speed; an infinite or NaN scale is refused and the scale stays (not measured).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Time of day: scale 2 runs twice as fast, 0 stops, -1 runs backward, and it reads what was set (11-02)")]
    public void SetTimeOfDayScale(float r)
    {
        if (!float.IsFinite(r)) return;
        DayFromNow();
        _dayScale = r;
    }

    [Native(Level.Exact, Fact = "game-facts.md, Time of day: GetTimeOfDayScale reads 1, then what was set (11-02)")]
    public float GetTimeOfDayScale() => _dayScale;

    /// <summary>Stops the time of day, or runs it again at its scale; suspends aren't counted.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Time of day: suspending stops it, a time or scale set meanwhile holds, one resume runs it (11-02)")]
    public void SuspendTimeOfDay(bool b)
    {
        DayFromNow();
        _daySuspended = b;
    }

    [Native(Level.Close)]
    public int GetIntegerGameState(igamestate? whichIntegerGameState) =>
        whichIntegerGameState is null ? 0 : _integerStates.GetValueOrDefault(whichIntegerGameState.Id);

    [Native(Level.Close)]
    public void SetIntegerGameState(igamestate? whichIntegerGameState, int value)
    {
        if (whichIntegerGameState is not null) _integerStates[whichIntegerGameState.Id] = value;
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
}
