namespace MirrorImaged.Natives;

public partial class sound
{
    internal string? File;  // as given, or its label's first
    internal bool Looping;
    internal float Pitch = 1;
    internal double End;  // seconds: it reads playing until then; 0 before its first start
    internal string SpeakerKey = "", TextKey = "";
}

// Sounds as the probes sounds and sounds-2 measure them (docs/facts.md, "Sounds"): nothing is played. A sound's length is
// its file's, read from the map or the install (GameData.SoundLength), and it reads playing from StartSound for that
// long. The game reads a sound playing longer, as its start waits a varying while (a 1.288 s sound until 2 to 5 s after
// StartSound), so GetSoundIsPlaying is Close (decisions D520 to D524).
public sealed partial class GameNatives
{
    const string Sounds = "facts.md, Sounds: probes sounds and sounds-2 (11-08)";
    const string NotPlayed = "nothing is played; no native reads it back (probes sounds and sounds-2)";

    /// <summary>A sound of the file, which takes the next handle id. Nothing is played (decisions D521).</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public sound? CreateSound(string? fileName, bool looping, bool is3D, bool stopwhenoutofrange, int fadeInRate, int fadeOutRate, string? eaxSetting) =>
        Make(new sound { File = fileName, Looping = looping });

    /// <summary>As CreateSound: its length is the file's, not the label's.</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public sound? CreateSoundFilenameWithLabel(string? fileName, bool looping, bool is3D, bool stopwhenoutofrange, int fadeInRate, int fadeOutRate, string? SLKEntryName) =>
        CreateSound(fileName, looping, is3D, stopwhenoutofrange, fadeInRate, fadeOutRate, null);

    /// <summary>
    /// A sound of the label's first file in the game's sound tables; a label they don't have still makes a sound, of
    /// length 0. Close: the game takes one of a label's files at random (decisions D523).
    /// </summary>
    [Native(Level.Close)]
    public sound? CreateSoundFromLabel(string? soundLabel, bool looping, bool is3D, bool stopwhenoutofrange, int fadeInRate, int fadeOutRate) =>
        CreateSound(soundLabel is null ? null : Data.SoundLabelFile(soundLabel), looping, is3D, stopwhenoutofrange, fadeInRate, fadeOutRate, null);

    /// <summary>As CreateSoundFromLabel (the ambient sounds' labels, <c>LordaeronSummerDay</c>).</summary>
    [Native(Level.Close)]
    public sound? CreateMIDISound(string? soundLabel, int fadeInRate, int fadeOutRate) =>
        CreateSoundFromLabel(soundLabel, false, false, false, fadeInRate, fadeOutRate);

    /// <summary>The length in milliseconds of the file the sound plays (GetSoundFileDuration's); 0 for none.</summary>
    [Native(Level.Close)]
    public int GetSoundDuration(sound? soundHandle) => soundHandle?.File is { } file ? GetSoundFileDuration(file) : 0;

    /// <summary>
    /// A sound file's length in milliseconds (GameData.SoundLength), the install's classic file first: Exact on every ogg
    /// measured; an mp3 is its frames and one more, which the game reads for 29 of 61, and up to 31 frames (0.8 s) more
    /// than the game's (decisions D520).
    /// </summary>
    [Native(Level.Close)]
    public int GetSoundFileDuration(string? musicFileName) => musicFileName is null ? 0 : Data.SoundLength(musicFileName);

    /// <summary>
    /// From StartSound until the sound's length has passed, divided by its pitch, or until it is stopped; a looping one
    /// until stopped, and one of length 0 never. Close: the game's start waits a varying while, 1 to 4 s in the probes (D522).
    /// </summary>
    [Native(Level.Close)]
    public bool GetSoundIsPlaying(sound? soundHandle) => soundHandle?.End > Seconds;

    /// <summary>False: every read of the probes was false, also in the instant of StartSound.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public bool GetSoundIsLoading(sound? soundHandle) => false;

    /// <summary>Plays it unless it is playing: a second start leaves it to end when the first would (D522).</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public void StartSound(sound? soundHandle)
    {
        if (soundHandle is null || GetSoundIsPlaying(soundHandle) || GetSoundDuration(soundHandle) == 0) return;
        soundHandle.End = soundHandle.Looping ? double.PositiveInfinity : Seconds + Played(soundHandle, 0);
    }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void StartSoundEx(sound? soundHandle, bool fadeIn) => StartSound(soundHandle);

    /// <summary>Stops it at once, a fade too; it isn't destroyed, killWhenDone or not (its length still reads).</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public void StopSound(sound? soundHandle, bool killWhenDone, bool fadeOut)
    {
        if (soundHandle is not null) soundHandle.End = 0;
    }

    /// <summary>A sound playing stops reading playing at once and keeps its length; one not playing is destroyed (D522).</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public void KillSoundWhenDone(sound? soundHandle)
    {
        if (soundHandle is null) return;
        if (GetSoundIsPlaying(soundHandle)) soundHandle.End = 0;
        else _game.Remove(soundHandle.Key);
    }

    /// <summary>The sound's length doesn't change (GetSoundDuration still reads its file's), nor how long it plays.</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundDuration(sound? soundHandle, int duration) { }

    /// <summary>On a sound playing, it plays on from there; before StartSound it does nothing.</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundPlayPosition(sound? soundHandle, int millisecs)
    {
        if (soundHandle is not null && GetSoundIsPlaying(soundHandle) && !soundHandle.Looping) soundHandle.End = Seconds + Played(soundHandle, millisecs);
    }

    /// <summary>The next start plays it faster or slower: pitch 2 for half its length.</summary>
    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundPitch(sound? soundHandle, float pitch)
    {
        if (soundHandle is not null) soundHandle.Pitch = pitch;
    }

    // How long it plays from a place in it; a pitch of 0 or below (not measured) plays it at its own speed
    double Played(sound s, int from) => Math.Max(GetSoundDuration(s) - from, 0) / 1000.0 / (s.Pitch > 0 ? s.Pitch : 1);

    /// <summary>
    /// Waits in a trigger's actions until the sound has <paramref name="offset"/> seconds left, then 0.15 s more, ending
    /// on a tenth as TriggerSleepAction's waits do: 0.2 s for a sound not playing or a negative offset, 0.1 s for nil.
    /// Close: this matches 38 of the 44 waits measured (D524).
    /// </summary>
    [Native(Level.Close)]
    public void TriggerWaitForSound(sound? s, float offset) =>
        TriggerSleepAction(s is null ? 0 : 0.15f + (float)(GetSoundIsPlaying(s) && offset >= 0 ? Math.Max(s.End - Seconds - offset, 0) : 0));

    /// <summary>"" until set.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public string? GetDialogueSpeakerNameKey(sound? soundHandle) => soundHandle?.SpeakerKey ?? "";

    /// <summary>"" until set.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public string? GetDialogueTextKey(sound? soundHandle) => soundHandle?.TextKey ?? "";

    /// <summary>Kept as given and true; false for nil.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public bool SetDialogueSpeakerNameKey(sound? soundHandle, string? speakerName)
    {
        if (soundHandle is null) return false;
        soundHandle.SpeakerKey = speakerName ?? "";
        return true;
    }

    /// <summary>Kept as given and true; false for nil.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public bool SetDialogueTextKey(sound? soundHandle, string? dialogueText)
    {
        if (soundHandle is null) return false;
        soundHandle.TextKey = dialogueText ?? "";
        return true;
    }

    /// <summary>True; false for nil. Nothing reads it back.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public bool SetSoundFacialAnimationLabel(sound? soundHandle, string? animationLabel) => soundHandle is not null;

    /// <summary>True; false for nil. Nothing reads it back.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public bool SetSoundFacialAnimationGroupLabel(sound? soundHandle, string? groupLabel) => soundHandle is not null;

    /// <summary>True; false for nil. Nothing reads it back.</summary>
    [Native(Level.Exact, Fact = Sounds)]
    public bool SetSoundFacialAnimationSetFilepath(sound? soundHandle, string? animationSetFilepath) => soundHandle is not null;

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundParamsFromLabel(sound? soundHandle, string? soundLabel) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundChannel(sound? soundHandle, int channel) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundVolume(sound? soundHandle, int volume) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundDistances(sound? soundHandle, float minDist, float maxDist) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundDistanceCutoff(sound? soundHandle, float cutoff) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundConeAngles(sound? soundHandle, float inside, float outside, int outsideVolume) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundConeOrientation(sound? soundHandle, float x, float y, float z) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundPosition(sound? soundHandle, float x, float y, float z) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void SetSoundVelocity(sound? soundHandle, float x, float y, float z) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void AttachSoundToUnit(sound? soundHandle, unit? whichUnit) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void RegisterStackedSound(sound? soundHandle, bool byPosition, float rectwidth, float rectheight) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void UnregisterStackedSound(sound? soundHandle, bool byPosition, float rectwidth, float rectheight) { }

    [Native(Level.Recorded, Reason = NotPlayed)]
    public void NewSoundEnvironment(string? environmentName) { }
}
