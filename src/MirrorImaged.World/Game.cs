using System.Globalization;

namespace MirrorImaged.World;

/// <summary>
/// The simulated game: a clock moving in fixed steps, a seeded random generator, the handles by id, and the run's
/// log. The same seed and the same calls give the same game.
/// </summary>
public sealed class Game
{
    /// <summary>The clock's steps in a second (decisions D45).</summary>
    public const int StepsPerSecond = 1024;

    readonly PriorityQueue<Action, (long Step, long Order)> _due = new();
    readonly Dictionary<int, object> _handles = [];
    readonly List<string> _log = [];
    long _scheduled;
    int _nextId = 0x100000;
    ulong _random;

    public Game(int seed) => Seed(seed);

    /// <summary>The clock: the steps since the run started.</summary>
    public long Step { get; private set; }

    /// <summary>What the run showed, a line each: the time in seconds, where it came from, and the text.</summary>
    public IReadOnlyList<string> Log => _log;

    /// <summary>The whole number of steps nearest to <paramref name="seconds"/>.</summary>
    public static long Steps(double seconds) => (long)Math.Round(seconds * StepsPerSecond);

    /// <summary>Adds a line to the log at the clock's time: <c>1.500 print: hello</c>.</summary>
    public void Write(string source, string text) =>
        _log.Add($"{(Step / (double)StepsPerSecond).ToString("0.000", CultureInfo.InvariantCulture)} {source}: {text}");

    /// <summary>
    /// Runs <paramref name="action"/> when the clock reaches <paramref name="step"/>, or in this step if that has
    /// passed, after everything already due then.
    /// </summary>
    public void At(long step, Action action) => _due.Enqueue(action, (Math.Max(step, Step), _scheduled++));

    /// <summary>Moves the clock <paramref name="steps"/> on, running what falls due with the clock at its step.</summary>
    public void Advance(long steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        var end = Step + steps;
        while (_due.TryPeek(out _, out var due) && due.Step <= end)
        {
            Step = due.Step;
            _due.Dequeue()();
        }
        Step = end;
    }

    /// <summary>Gives <paramref name="handle"/> the next id; ids count up from 0x100000 and are never given twice.</summary>
    public int Add(object handle)
    {
        _handles.Add(_nextId, handle);
        return _nextId++;
    }

    /// <summary>The handle with <paramref name="id"/>, or null once it is removed.</summary>
    public object? Handle(int id) => _handles.GetValueOrDefault(id);

    public void Remove(int id) => _handles.Remove(id);

    /// <summary>Starts the random numbers over from <paramref name="seed"/>.</summary>
    public void Seed(int seed) => _random = (uint)seed;

    /// <summary>A random integer from <paramref name="low"/> to <paramref name="high"/>, both included, in either order.</summary>
    public int RandomInt(int low, int high)
    {
        if (low > high) (low, high) = (high, low);
        return (int)(low + (long)(((Next() >> 32) * (ulong)((long)high - low + 1)) >> 32));
    }

    /// <summary>A random number between <paramref name="low"/> and <paramref name="high"/>; from 0 to 1, never 1.</summary>
    public float RandomReal(float low, float high) => low + (high - low) * ((Next() >> 40) / 16777216f);

    // SplitMix64: small, and the same on every platform and .NET version.
    ulong Next()
    {
        var z = _random += 0x9E3779B97F4A7C15;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }
}
