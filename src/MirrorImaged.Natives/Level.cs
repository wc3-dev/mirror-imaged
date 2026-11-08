namespace MirrorImaged.Natives;

/// <summary>How far a native can be trusted to do what the game does.</summary>
public enum Level
{
    /// <summary>Matches the game on every case a probe measured; <see cref="NativeAttribute.Fact"/> names the probe or fact.</summary>
    Exact,

    /// <summary>A documented model of the game, not fully measured; its docs say where it may differ.</summary>
    Close,

    /// <summary>
    /// Changes no game state that maps can read back, beyond what its getters read; the call is logged.
    /// <see cref="NativeAttribute.Reason"/> says why it is not simulated.
    /// </summary>
    Recorded,

    /// <summary>Not simulated: calling it stops the run with a <see cref="MissingNativeException"/>.</summary>
    Missing,
}
