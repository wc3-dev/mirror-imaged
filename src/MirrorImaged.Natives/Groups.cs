namespace MirrorImaged.Natives;

public sealed partial class GameNatives
{
    /// <summary>A new group, as blizzard.j makes its own when it loads; what groups hold comes with the units.</summary>
    [Native(Level.Close)]
    public group? CreateGroup() => Make(new group());
}
