namespace MirrorImaged.Natives;

/// <summary>Marks a method as a native of the game's <c>common.j</c> or <c>common.ai</c>, at its trust level.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NativeAttribute(Level level) : Attribute
{
    public Level Level { get; } = level;

    /// <summary>For a Recorded native, why it is not simulated.</summary>
    public string? Reason { get; set; }

    /// <summary>For an Exact native, the probe or the game fact that measures it.</summary>
    public string? Fact { get; set; }
}
