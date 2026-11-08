namespace MirrorImaged.Natives;

/// <summary>Thrown by a native whose level is Missing: MirrorImaged does not simulate it yet.</summary>
public sealed class MissingNativeException(string native)
    : Exception($"The native {native} is Missing: MirrorImaged does not simulate it yet.")
{
    /// <summary>The native's name, as <c>common.j</c> or <c>common.ai</c> declares it.</summary>
    public string Native { get; } = native;
}
