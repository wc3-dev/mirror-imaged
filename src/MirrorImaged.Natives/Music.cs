namespace MirrorImaged.Natives;

// Music and the volume groups: nothing is played, and no native reads any of it back, so they keep nothing (decisions
// D525). GetSoundFileDuration reads a music file's length (Sounds.cs).
public sealed partial class GameNatives
{
    const string NotHeard = "nothing is played; no native reads music or a volume back";

    /// <summary>
    /// A file, or a list of files split by <c>;</c>, to play once, then the map's music. Where the game starts in a list
    /// isn't steady (facts, "Presentation": the first file, then the second and third when played again); nothing here
    /// reads it.
    /// </summary>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayMusic(string? musicName) { }

    /// <summary>As PlayMusic, from a place and with a fade in.</summary>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayMusicEx(string? musicName, int frommsecs, int fadeinmsecs) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void StopMusic(bool fadeOut) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void ResumeMusic() { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetMapMusic(string? musicName, bool random, int index) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void ClearMapMusic() { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetMusicVolume(int volume) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetMusicPlayPosition(int millisecs) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayThematicMusic(string? musicFileName) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayThematicMusicEx(string? musicFileName, int frommsecs) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void EndThematicMusic() { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetThematicMusicVolume(int volume) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetThematicMusicPlayPosition(int millisecs) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void BlzSetThematicMusicAbsoluteVolume(int volume) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void BlzPauseThematicMusicOnFocusLost(bool pause) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void VolumeGroupSetVolume(volumegroup? vgroup, float scale) { }

    [Native(Level.Recorded, Reason = NotHeard)]
    public void VolumeGroupReset() { }
}
