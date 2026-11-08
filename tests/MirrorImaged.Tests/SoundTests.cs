using System.Buffers.Binary;
using System.Globalization;
using MirrorImaged.Data;
using MirrorImaged.Natives;
using MirrorImaged.World;
using War3Net.Build;
using War3Net.IO.Mpq;

namespace MirrorImaged.Tests;

/// <summary>
/// Sound files' lengths, sounds and their play state, against the probes sounds and sounds-2 (docs/facts.md, "Sounds")
/// and the facts' "Presentation" and "Files" sound bullets (decisions D520 to D527). The sound files are made here.
/// </summary>
public sealed class SoundTests : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
    readonly Game _game = new(0);

    public void Dispose() => Directory.Delete(_folder, true);

    // A PCM wav of 16-bit mono samples, with a chunk of odd size before its data
    internal static byte[] Wave(int rate, int samples)
    {
        var bytes = new byte[44 + 10 + samples * 2];
        "RIFF"u8.CopyTo(bytes);
        "WAVE"u8.CopyTo(bytes.AsSpan(8));
        "fmt "u8.CopyTo(bytes.AsSpan(12));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24), rate);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28), rate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(34), 16);
        "junk"u8.CopyTo(bytes.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(40), 1);
        "data"u8.CopyTo(bytes.AsSpan(46));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(50), samples * 2);
        return bytes;
    }

    // fLaC and its STREAMINFO block
    static byte[] Flac(int rate, long samples)
    {
        var bytes = new byte[42];
        "fLaC"u8.CopyTo(bytes);
        bytes[7] = 34;
        BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(18), (ulong)rate << 44 | 1UL << 41 | 15UL << 36 | (ulong)samples);
        return bytes;
    }

    // Ogg pages: the Vorbis identification header's, a page between and the last, whose granule position is the samples
    static byte[] Vorbis(int rate, long samples)
    {
        static byte[] Page(long granule, ReadOnlySpan<byte> packet)
        {
            var page = new byte[28 + packet.Length];
            "OggS"u8.CopyTo(page);
            BinaryPrimitives.WriteInt64LittleEndian(page.AsSpan(6), granule);
            page[26] = 1;
            page[27] = (byte)packet.Length;
            packet.CopyTo(page.AsSpan(28));
            return page;
        }
        var identification = new byte[30];
        "\u0001vorbis"u8.CopyTo(identification);
        identification[11] = 1;
        BinaryPrimitives.WriteInt32LittleEndian(identification.AsSpan(12), rate);
        return [.. Page(0, identification), .. Page(samples / 2, new byte[50]), .. Page(samples, new byte[50])];
    }

    // MPEG-1 layer III frames of 128 kbit/s at 44100 Hz, every other one padded, after an ID3v2 tag if asked
    static byte[] Mp3(int frames, bool tag)
    {
        var bytes = new List<byte>();
        if (tag) bytes.AddRange([.. "ID3"u8, 3, 0, 0, 0, 0, 0, 20, .. new byte[20]]);
        for (var i = 0; i < frames; i++)
        {
            var padding = i % 2;
            bytes.AddRange([0xFF, 0xFB, (byte)(0x90 | padding << 1), 0x64]);
            bytes.AddRange(new byte[417 + padding - 4]);
        }
        return [.. bytes];
    }

    [Fact]
    public void A_sound_files_length_is_read_from_its_headers_in_whole_milliseconds_cut_toward_zero()
    {
        Assert.Equal(1000, SoundFiles.Duration(Wave(22050, 22050)));
        Assert.Equal(1288, SoundFiles.Duration(Wave(22050, 28401)));
        Assert.Equal(1287, SoundFiles.Duration(Flac(44100, 56800)));
        Assert.Equal(107050, SoundFiles.Duration(Flac(48000, 5138400)));
        Assert.Equal(1288, SoundFiles.Duration(Vorbis(22050, 28401)));
        Assert.Equal(287, SoundFiles.Duration(Mp3(10, false)));
        Assert.Equal(287, SoundFiles.Duration(Mp3(10, true)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a sound at all")]
    [InlineData("RIFF")]
    [InlineData("RIFF\0\0\0\0WAVEfmt ")]
    [InlineData("fLaC")]
    [InlineData("OggS")]
    [InlineData("ID3")]
    [InlineData("ID3\u0003\0\0\0\0\0")]
    [InlineData("ID3\u0003\0\0\0\0\u007f")]
    public void A_file_of_no_format_known_or_cut_short_has_no_length(string text) =>
        Assert.Null(SoundFiles.Duration(System.Text.Encoding.Latin1.GetBytes(text)));

    static byte[] Chunk(string id, uint size, int bytes = 0) => [.. System.Text.Encoding.ASCII.GetBytes(id), .. BitConverter.GetBytes(size), .. new byte[bytes]];

    static byte[] Riff(params byte[][] chunks) => [.. "RIFF\0\0\0\0WAVE"u8, .. chunks.SelectMany(chunk => chunk)];

    // A hostile file neither hangs nor throws: a chunk said to run past the file (0xFFFFFFF8 once made the walk stand
    // still), a rate of 0, a granule position too large to make milliseconds of or below 0, a cut header
    [Fact]
    public void A_hostile_file_has_no_length()
    {
        byte[][] files =
        [
            Riff(Chunk("junk", 0xFFFFFFF8)),
            Riff(Chunk("junk", 0xFFFFFFF7)),
            Riff(Chunk("junk", 0xFFFFFFF8), Chunk("data", 100, 100)),
            Riff(Chunk("fmt ", 16, 16), Chunk("data", 100, 100)),
            Riff(Chunk("junk", 0), Chunk("junk", 0)),
            Vorbis(22050, long.MaxValue),
            Vorbis(22050, -1),
            Flac(44100, 100)[..20],
            Mp3(3, true)[..12],
        ];

        Assert.All(files, file => Assert.Null(SoundFiles.Duration(file)));
        Assert.Equal(1000, SoundFiles.Duration(Riff(Chunk("junk", 0), Chunk("fmt ", 16, 16)[..8], [.. Wave(22050, 22050)[20..36]], Chunk("data", 44100))));
    }

    // The install as the storage names its files: a classic file under the language's folder, its Definitive Edition
    // and HD files of other lengths, a classic one with no language, and one only of the Definitive Edition and HD
    static StandardData Install()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["War3.w3mod:_Locales/enUS.w3mod:Units/Human/HeroPaladin/HeroPaladinWhat1.ogg"] = Vorbis(22050, 28401),
            ["War3.w3mod:_DE.w3mod:_Locales/enUS.w3mod:Units/Human/HeroPaladin/HeroPaladinWhat1.flac"] = Flac(48000, 85824),
            ["War3.w3mod:_HD.w3mod:_Locales/enUS.w3mod:Units/Human/HeroPaladin/HeroPaladinWhat1.ogg"] = Vorbis(48000, 85824),
            ["War3.w3mod:Sound/Interface/QuestNew.ogg"] = Vorbis(44100, 165375),
            ["War3.w3mod:_DE.w3mod:Abilities/Spells/Human/HeroicSlash/HeroicSlash0.ogg"] = Vorbis(48000, 61968),
            ["War3.w3mod:_HD.w3mod:Abilities/Spells/Human/HeroicSlash/HeroicSlash0.ogg"] = Vorbis(48000, 96000),
            ["War3.w3mod:Sound/Music/mp3Music/Tension.mp3"] = Mp3(748, false),
        };
        var data = new StandardData { Storage = files.GetValueOrDefault };
        data.Sounds["QuestNew"] = "Sound/Interface/QuestNew.flac";
        data.Sounds["HeroPaladinWhat"] = "HeroPaladinWhat1,HeroPaladinWhat2";
        data.Sounds["RoosterSound"] = "Sound/Interface/QuestNew.flac,Sound/Time/DaybreakRooster.flac";
        return data;
    }

    // A map whose script is main's body, with the files given
    string Map(string main, params (string Name, byte[] Bytes)[] files)
    {
        var path = Path.Combine(_folder, Path.GetRandomFileName() + ".w3x");
        var info = MapFactory.Info(32, 32);
        var builder = new MapBuilder(new Map { Info = info, Environment = MapFactory.Environment(info), Script = $"function config() end\nfunction main()\n{main}\nend\n" });
        foreach (var (name, bytes) in files) builder.AddFiles(MpqFile.New(new MemoryStream(bytes), name));
        builder.Build(path);
        return path;
    }

    // probe sounds: the classic file whatever the extension asked for (1288, where the Definitive Edition's and HD's
    // are 1788), in any case and with either slash; the Definitive Edition's when there is no classic one (1291); an
    // mp3 by its frames and one more (Tension's 748, the game 19565)
    [Theory]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat1.wav", 1288)]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat1.flac", 1288)]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat1.ogg", 1288)]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat1.mp3", 1288)]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat1", 1288)]
    [InlineData("units/human/heropaladin/heropaladinwhat1.wav", 1288)]
    [InlineData(@"Sound\Interface\QuestNew.wav", 3750)]
    [InlineData(@"Abilities\Spells\Human\HeroicSlash\HeroicSlash0.ogg", 1291)]
    [InlineData(@"Sound\Music\mp3Music\Tension.mp3", 19565)]
    [InlineData(@"Sound\NoSuchFile.wav", 0)]
    [InlineData("", 0)]
    [InlineData("QuestNew", 0)]
    public void A_sound_files_length_is_the_installs_classic_files(string path, int length) =>
        Assert.Equal(length, new GameData(Install()).SoundLength(path));

    // facts ("Files"): a map's own Sound\ProbeAska.wav read 1000 ms, and the same packed as Sound\ProbeÅska.wav 0
    [Fact]
    public void A_maps_own_file_comes_first_but_not_under_a_name_with_a_letter_outside_ascii()
    {
        var data = new GameData(Install(), MapArchive.Read(Map("", (@"Sound\Interface\QuestNew.wav", Wave(22050, 22050)), (@"Sound\ProbeÅska.wav", Wave(22050, 22050)))));

        Assert.Equal(1000, data.SoundLength(@"Sound\Interface\QuestNew.wav"));
        Assert.Equal(1000, data.SoundLength("sound/interface/questnew.flac"));
        Assert.Equal(0, data.SoundLength(@"Sound\ProbeÅska.wav"));
    }

    GameNatives Natives() => new(_game) { Data = new GameData(Install()) };

    double Seconds => _game.Step / (double)Game.StepsPerSecond;

    void Advance(double seconds) => _game.Advance(Game.Steps(Seconds + seconds) - _game.Step);

    const string Paladin = @"Units\Human\HeroPaladin\HeroPaladinWhat1.wav";

    static sound Make(GameNatives natives, bool looping = false) => natives.CreateSound(Paladin, looping, false, false, 10, 10, "DefaultEAXON")!;

    // probe sounds: a sound takes the next id; its length is its file's, which SetSoundDuration and
    // SetSoundParamsFromLabel don't change; a label's sound its label's file's (QuestNew 3750, RoosterSound 2935), and
    // one of a label or file the game doesn't have 0; GetSoundIsLoading is always false
    [Fact]
    public void A_sounds_length_is_its_files_whatever_its_set_to()
    {
        var natives = Natives();
        var before = natives.CreateTimer()!;
        var plain = Make(natives);
        var after = natives.CreateTimer()!;
        var set = Make(natives);
        natives.SetSoundDuration(set, 3000);
        var negative = Make(natives);
        natives.SetSoundDuration(negative, -5);
        var parameters = Make(natives);
        natives.SetSoundParamsFromLabel(parameters, "QuestNew");

        Assert.Equal([1, 1], new[] { plain.Id - before.Id, after.Id - plain.Id });
        Assert.All(new[] { plain, set, negative, parameters }, s => Assert.Equal(1288, natives.GetSoundDuration(s)));
        Assert.Equal(1288, natives.GetSoundDuration(natives.CreateSoundFilenameWithLabel(Paladin, false, false, false, 10, 10, "QuestNew")));
        Assert.Equal(3750, natives.GetSoundDuration(natives.CreateSoundFromLabel("QuestNew", false, false, false, 10000, 10000)));
        Assert.Equal(3750, natives.GetSoundDuration(natives.CreateMIDISound("RoosterSound", 20, 20)));
        Assert.Equal(0, natives.GetSoundDuration(natives.CreateSound(@"Sound\NoSuchFile.wav", false, false, false, 10, 10, "DefaultEAXON")));
        Assert.NotNull(natives.CreateSoundFromLabel("NoSuchLabel", false, false, false, 10000, 10000));
        Assert.Equal(0, natives.GetSoundDuration(natives.CreateSoundFromLabel("NoSuchLabel", false, false, false, 10000, 10000)));
        Assert.Equal(0, natives.GetSoundDuration(natives.CreateMIDISound("NoSuchLabel", 20, 20)));
        Assert.Equal(0, natives.GetSoundDuration(null));
        Assert.Equal(1288, natives.GetSoundFileDuration(Paladin));
        Assert.Equal(0, natives.GetSoundFileDuration(null));
        natives.StartSound(plain);
        Assert.False(natives.GetSoundIsLoading(plain));
        Assert.False(natives.GetSoundIsLoading(null));
    }

    // probe sounds-2: a unit's label names its files without their folder, and the game read one at random (1184 or
    // 1288 for HeroPaladinWhat); here they aren't found
    [Fact]
    public void A_label_whose_files_have_no_folder_has_no_length()
    {
        var natives = Natives();

        Assert.Equal(0, natives.GetSoundDuration(natives.CreateSoundFromLabel("HeroPaladinWhat", false, false, false, 10000, 10000)));
    }

    // probe sounds: "" until set; a set gives true and reads back as given; nil reads "" and its sets give false
    [Fact]
    public void Dialogue_keys_read_back_and_the_facial_animation_sets_give_true()
    {
        var natives = Natives();
        var s = Make(natives);

        Assert.Equal("", natives.GetDialogueSpeakerNameKey(s));
        Assert.Equal("", natives.GetDialogueTextKey(s));
        Assert.True(natives.SetDialogueSpeakerNameKey(s, "TRIGSTR_70"));
        Assert.True(natives.SetDialogueTextKey(s, "Some text"));
        Assert.Equal("TRIGSTR_70", natives.GetDialogueSpeakerNameKey(s));
        Assert.Equal("Some text", natives.GetDialogueTextKey(s));
        Assert.Equal("", natives.GetDialogueSpeakerNameKey(null));
        Assert.False(natives.SetDialogueSpeakerNameKey(null, "x"));
        Assert.False(natives.SetDialogueTextKey(null, "x"));
        Assert.True(natives.SetSoundFacialAnimationLabel(s, "x"));
        Assert.True(natives.SetSoundFacialAnimationGroupLabel(s, "Map-Arthas"));
        Assert.True(natives.SetSoundFacialAnimationSetFilepath(s, "Sound/Dialogue/FaceAnimation/Human01/FacialAnimation/Arthas.animset"));
        Assert.False(natives.SetSoundFacialAnimationLabel(null, "x"));
        Assert.False(natives.SetSoundFacialAnimationGroupLabel(null, "x"));
        Assert.False(natives.SetSoundFacialAnimationSetFilepath(null, "x"));
    }

    // probes sounds and sounds-2: playing from StartSound, in its instant too, for its length; a looping one on; one of
    // no file never; stopped at once, with a fade too; a second start, in the same instant or later, leaves its end;
    // started again after it ended, it plays again; SetSoundDuration doesn't change how long it plays
    [Fact]
    public void A_sound_reads_playing_from_its_start_for_its_length()
    {
        var natives = Natives();
        var plain = Make(natives);
        var loop = Make(natives, looping: true);
        var missing = natives.CreateSound(@"Sound\NoSuchFile.wav", false, false, false, 10, 10, "DefaultEAXON")!;
        var twice = Make(natives);
        var again = Make(natives);
        var set = Make(natives);
        natives.SetSoundDuration(set, 3000);
        var stopped = Make(natives);
        var faded = Make(natives);
        Assert.False(natives.GetSoundIsPlaying(plain));
        foreach (var s in new[] { plain, loop, missing, twice, again, set, stopped, faded }) natives.StartSound(s);
        natives.StartSound(twice);
        natives.StopSound(stopped, false, false);
        natives.StopSound(faded, false, true);

        Assert.Equal([true, true, false, true, true, true, false, false], new[] { plain, loop, missing, twice, again, set, stopped, faded }.Select(natives.GetSoundIsPlaying));
        Advance(0.6);
        natives.StartSound(again);
        Advance(0.6);
        Assert.Equal([true, true, true, true], new[] { plain, twice, again, set }.Select(natives.GetSoundIsPlaying));
        Advance(0.1);
        Assert.Equal([false, true, false, false, false], new[] { plain, loop, twice, again, set }.Select(natives.GetSoundIsPlaying));
        natives.StartSound(plain);
        Advance(1);
        Assert.True(natives.GetSoundIsPlaying(plain));
        Assert.False(natives.GetSoundIsPlaying(null));
    }

    // probe sounds-2: a play position set while it plays ends it that much sooner (1.25 s before the others in the
    // game), and before StartSound does nothing; pitch 2 plays it in half its length, 0.5 in twice
    [Fact]
    public void A_play_position_while_playing_and_a_pitch_change_how_long_it_plays()
    {
        var natives = Natives();
        var position = Make(natives);
        var first = Make(natives);
        var fast = Make(natives);
        var slow = Make(natives);
        natives.SetSoundPlayPosition(first, 1000);
        natives.SetSoundPitch(fast, 2);
        natives.SetSoundPitch(slow, 0.5f);
        foreach (var s in new[] { position, first, fast, slow }) natives.StartSound(s);
        natives.SetSoundPlayPosition(position, 1000);

        Advance(0.28);
        Assert.Equal([true, true, true, true], new[] { position, first, fast, slow }.Select(natives.GetSoundIsPlaying));
        Advance(0.02);
        Assert.False(natives.GetSoundIsPlaying(position));
        Advance(0.35);
        Assert.False(natives.GetSoundIsPlaying(fast));
        Advance(1);
        Assert.Equal([false, true], new[] { first, slow }.Select(natives.GetSoundIsPlaying));
        Advance(1.3);
        Assert.False(natives.GetSoundIsPlaying(slow));
    }

    // probe sounds: KillSoundWhenDone on a sound playing makes it read not playing at once, and its length reads on;
    // on one not started it is destroyed, reading 0; StopSound with killWhenDone keeps its length
    [Fact]
    public void Kill_when_done_stops_a_sound_playing_and_destroys_one_not_started()
    {
        var natives = Natives();
        var killed = Make(natives);
        var idle = Make(natives);
        var stopped = Make(natives);
        natives.StartSound(killed);
        natives.StartSound(stopped);
        natives.KillSoundWhenDone(killed);
        natives.KillSoundWhenDone(idle);
        natives.StopSound(stopped, true, false);

        Assert.Equal([false, false, false], new[] { killed, idle, stopped }.Select(natives.GetSoundIsPlaying));
        Advance(4.5);
        Assert.Equal(1288, natives.GetSoundDuration(killed));
        Assert.Equal(1288, natives.GetSoundDuration(stopped));
        Assert.Null(_game.Handle(idle.Key));
    }

    // probe sounds-2, from 6.5 s: a sound of 1288 ms waited 1.5 s, again once ended 0.2, with 0.5 s left 1.0, with
    // SetSoundDuration 3000 1.5, one never started 0.2 (and 0.2 with offset 2), nil 0.1, QuestNew with offset -1 0.2.
    // In a Lua map's trigger action, its sound file the map's own.
    [Fact]
    public void Trigger_wait_for_sound_waits_its_time_left_and_ends_on_a_tenth()
    {
        using var simulation = Simulation.Load(Map($$"""
            local paladin = '{{Paladin.Replace(@"\", @"\\")}}'
            local function make() return CreateSound(paladin, false, false, false, 10, 10, 'DefaultEAXON') end
            TimerStart(CreateTimer(), 6.5, false, function()
                local trigger = CreateTrigger()
                TriggerAddAction(trigger, function()
                    local clock = CreateTimer()
                    TimerStart(clock, 1000.0, false, nil)
                    local last = 0
                    local function waited(name)
                        print(string.format('wait %s %.1f', name, TimerGetElapsed(clock) - last))
                        last = TimerGetElapsed(clock)
                    end
                    local s = make()
                    StartSound(s)
                    TriggerWaitForSound(s, 0)
                    waited('0')
                    TriggerWaitForSound(s, 0)
                    waited('ended')
                    s = make()
                    StartSound(s)
                    TriggerWaitForSound(s, 0.5)
                    waited('0.5')
                    s = make()
                    SetSoundDuration(s, 3000)
                    StartSound(s)
                    TriggerWaitForSound(s, 0)
                    waited('set 3000')
                    TriggerWaitForSound(make(), 0)
                    waited('idle')
                    TriggerWaitForSound(make(), 2)
                    waited('idle 2')
                    TriggerWaitForSound(nil, 0)
                    waited('nil')
                    s = CreateSound('Sound\\Interface\\QuestNew.wav', false, false, false, 10, 10, 'DefaultEAXON')
                    StartSound(s)
                    TriggerWaitForSound(s, -1)
                    waited('label -1')
                end)
                TriggerExecute(trigger)
            end)
            """, (Paladin, Wave(22050, 28401)), (@"Sound\Interface\QuestNew.wav", Wave(22050, 82688))), "");
        simulation.Advance(TimeSpan.FromSeconds(15));

        Assert.Equal(["wait 0 1.5", "wait ended 0.2", "wait 0.5 1.0", "wait set 3000 1.5", "wait idle 0.2", "wait idle 2 0.2", "wait nil 0.1", "wait label -1 0.2"],
            simulation.Log.Select(line => line.Split(" print: ")[1]));
    }

    // A main as World Editor writes it for a map with sounds and music (the shipped campaign maps' InitSounds), and a
    // trigger's sound and music calls: none stops the run or raises
    [Fact]
    public void A_maps_sound_and_music_setup_runs_through()
    {
        using var simulation = Simulation.Load(Map("""
            NewSoundEnvironment("Default")
            SetMapMusic("Music", true, 0)
            local s = CreateSound("Units\\Creeps\\Bandit\\BanditYesAttack2.flac", false, true, true, 10, 10, "DefaultEAXON")
            SetSoundParamsFromLabel(s, "BanditYesAttack")
            SetSoundFacialAnimationLabel(s, "BanditYesAttack2")
            SetSoundFacialAnimationGroupLabel(s, "Map-Bandit")
            SetSoundFacialAnimationSetFilepath(s, "Sound/Dialogue/FaceAnimation/Human01/FacialAnimation/Bandit.animset")
            SetDialogueSpeakerNameKey(s, "TRIGSTR_70")
            SetDialogueTextKey(s, "TRIGSTR_71")
            SetSoundDuration(s, 1216)
            SetSoundChannel(s, 0)
            SetSoundVolume(s, 127)
            SetSoundPitch(s, 1.0)
            SetSoundDistances(s, 0.0, 10000.0)
            SetSoundDistanceCutoff(s, 3000.0)
            SetSoundConeAngles(s, 0.0, 0.0, 0)
            SetSoundConeOrientation(s, 0.0, 0.0, 0.0)
            SetSoundPosition(s, 0.0, 0.0, 0.0)
            SetSoundVelocity(s, 0.0, 0.0, 0.0)
            AttachSoundToUnit(s, nil)
            RegisterStackedSound(s, true, 128.0, 128.0)
            UnregisterStackedSound(s, true, 128.0, 128.0)
            StartSoundEx(s, true)
            SetSoundPlayPosition(s, 100)
            StopSound(s, false, true)
            ClearMapMusic()
            PlayMusic("Sound\\Music\\mp3Music\\Human1.mp3;Sound\\Music\\mp3Music\\Human2.mp3")
            PlayMusicEx("Comradeship", 1000, 500)
            SetMusicVolume(100)
            SetMusicPlayPosition(5000)
            StopMusic(false)
            ResumeMusic()
            PlayThematicMusic("Comradeship")
            PlayThematicMusicEx("Comradeship", 1000)
            SetThematicMusicVolume(100)
            SetThematicMusicPlayPosition(1000)
            BlzSetThematicMusicAbsoluteVolume(100)
            BlzPauseThematicMusicOnFocusLost(true)
            EndThematicMusic()
            VolumeGroupSetVolume(SOUND_VOLUMEGROUP_MUSIC, 0.55)
            VolumeGroupReset()
            print(GetSoundDuration(s), GetSoundFileDuration("Units\\Creeps\\Bandit\\BanditYesAttack2.wav"))
            KillSoundWhenDone(s)
            """, (@"Units\Creeps\Bandit\BanditYesAttack2.ogg", Wave(22050, 26813))), "");

        Assert.Null(simulation.Stopped);
        Assert.Equal(["0.000 print: 1216\t1216"], simulation.Log);
    }
}

/// <summary>The installed game's sound files and labels, against the probes sounds and sounds-2 (decisions D520, D523).</summary>
[Trait("Needs", "Game")]
public sealed class InstalledSoundTests : IDisposable
{
    static readonly GameData Data = new(StandardData.Installed(GameInstall.Require(), null));

    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // blizzard.j's sounds: InitBlizzard's (InitBlizzardGlobals' quest and dialog sounds, made here as it makes them, as
    // the rest of it needs IsFogEnabled; InitDNCSounds' dawn and dusk), a map's ambient sounds and music as World
    // Editor's main sets them (the clock's hour 0 plays the night's), and the BJ functions over the sound natives
    [InstallFact]
    public void Blizzard_j_s_sound_and_music_functions_run_on_the_natives()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
              NewSoundEnvironment("Default")
              SetAmbientDaySound("LordaeronSummerDay")
              SetAmbientNightSound("LordaeronSummerNight")
              SetMapMusic("Music", true, 0)
              local function label(name) return CreateSoundFromLabel(name, false, false, false, 10000, 10000) end
              bj_rescueSound = label("Rescue")
              bj_questDiscoveredSound = label("QuestNew")
              bj_questUpdatedSound = label("QuestUpdate")
              bj_questCompletedSound = label("QuestCompleted")
              bj_questFailedSound = label("QuestFailed")
              bj_questHintSound = label("Hint")
              bj_questSecretSound = label("SecretFound")
              bj_questItemAcquiredSound = label("ItemReward")
              bj_questWarningSound = label("Warning")
              bj_victoryDialogSound = label("QuestCompleted")
              bj_defeatDialogSound = label("QuestFailed")
              InitDNCSounds()
              print(GetSoundDurationBJ(bj_questDiscoveredSound), GetSoundDurationBJ(bj_dawnSound), GetSoundDurationBJ(nil), GetSoundFileDurationBJ("Sound\\Interface\\QuestNew.wav"))
              print(GetSoundIsPlaying(bj_dayAmbientSound), GetSoundIsPlaying(bj_nightAmbientSound), GetSoundDuration(bj_dayAmbientSound))
              PlaySound("Sound\\Interface\\QuestNew.wav")
              PlaySoundBJ(bj_questHintSound)
              print(GetSoundIsPlaying(bj_lastPlayedSound))
              SetSoundVolumeBJ(bj_questHintSound, 50)
              SetSoundOffsetBJ(1, bj_questHintSound)
              StopSoundBJ(bj_questHintSound, true)
              SetSpeechVolumeGroupsBJ()
              SetCineModeVolumeGroupsBJ()
              VolumeGroupResetBJ()
              PlayThematicMusicBJ("Comradeship")
              EndThematicMusicBJ()
              SetMapMusicIndexedBJ("music", 0)
              PlayMusicBJ("Sound\\Music\\mp3Music\\Human1.mp3")
              SetMusicVolumeBJ(50)
              StopMusicBJ(false)
              ClearMapMusicBJ()
            end
            """));

        Assert.Null(simulation.Stopped);
        Assert.Equal(["0.000 print: 3.75\t2.935\t5.0\t3.75", "0.000 print: false\ttrue\t117210", "0.000 print: true"], simulation.Log);
    }

    // The game's GetSoundFileDuration (probes sounds and sounds-2); for the mp3s that differ, the game's after the line
    // (decisions D520: the frames and one more, 27 of 61)
    [InstallTheory]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat1.wav", 1288)]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat1", 1288)]
    [InlineData("units/human/heropaladin/heropaladinwhat1.mp3", 1288)]
    [InlineData(@"Units\Human\HeroPaladin\HeroPaladinWhat2.flac", 1184)]
    [InlineData(@"Sound\Music\mp3Music\Combat1.ogg", 107050)]
    [InlineData(@"Sound\Interface\MouseClick1.flac", 239)]
    [InlineData(@"Sound\Interface\QuestNew.wav", 3750)]
    [InlineData(@"Abilities\Spells\Human\HeroicSlash\HeroicSlash0.ogg", 1291)]
    [InlineData(@"Sound\NoSuchFile.wav", 0)]
    [InlineData("Comradeship", 0)]
    [InlineData(@"Sound\Dialogue\HumanCampaign\Human01\H01Arthas02.flac", 4493)]
    [InlineData(@"Sound\Music\mp3Music\PH1.flac", 281600)]
    [InlineData(@"Sound\Music\mp3Music\DarkAgents.mp3", 64966)] // the game 64940, the frames the file holds
    [InlineData(@"Sound\Dialogue\HumanCampaign\Human01\H01VillagerF27.flac", 9456)] // the game 9430, the same
    [InlineData(@"Sound\Music\mp3Music\Human1.mp3", 273057)] // the game 272848, 8 frames fewer
    [InlineData(@"Sound\Music\mp3Music\Orc1.mp3", 312581)] // the game 311745, 32 frames fewer
    public void A_sound_files_length_is_the_classic_files(string path, int length) =>
        Assert.Equal(length, Data.SoundLength(path));
    // probe sounds: QuestNew 3750, RoosterSound 2935, LordaeronSummerDay (an ambient label) 117210
    [InstallTheory]
    [InlineData("QuestNew", 3750)]
    [InlineData("RoosterSound", 2935)]
    [InlineData("LordaeronSummerDay", 117210)]
    public void A_labels_sound_is_its_files(string label, int length) =>
        Assert.Equal(length, Data.SoundLength(Data.SoundLabelFile(label)!));
}
