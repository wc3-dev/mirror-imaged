namespace MirrorImaged.Natives;

// The game's generator isn't known, so its numbers aren't the game's; a seed gives the same numbers every run. Lua's
// math.random and math.randomseed share it, as the game's do (facts, "Lua runtime").
public sealed partial class GameNatives
{
    /// <summary>A random integer from one bound to the other, both included.</summary>
    [Native(Level.Close)]
    public int GetRandomInt(int lowBound, int highBound) => _game.RandomInt(lowBound, highBound);

    /// <summary>A random number between the bounds.</summary>
    [Native(Level.Close)]
    public float GetRandomReal(float lowBound, float highBound) => _game.RandomReal(lowBound, highBound);

    /// <summary>Starts the random numbers over; the same seed gives the same numbers.</summary>
    [Native(Level.Close)]
    public void SetRandomSeed(int seed) => _game.Seed(seed);
}
