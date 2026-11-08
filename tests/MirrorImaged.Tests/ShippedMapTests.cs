using System.Numerics;
using System.Text;
using MirrorImaged.Data;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using War3Net.Build;
using War3Net.Build.Audio;
using War3Net.Build.Common;
using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.IO.Mpq;
using Xunit.Abstractions;

namespace MirrorImaged.Tests;

/// <summary>The install's maps, and the two kinds of file in them War3Net 6.1.2 refuses (decisions D300 to D302).</summary>
public sealed class ShippedMapTests(ITestOutputHelper output) : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    const PlayerRaceHud Marker = (PlayerRaceHud)0x1F2E3D4C;

    // D300: World Editor 6117 and 6127 (2.0.4's campaign maps) wrote format 39 without the race HUD each player has from 6140
    [Theory]
    [InlineData(6127, false)]
    [InlineData(6140, true)]
    public void A_format_39_info_file_has_a_race_hud_in_each_player_from_world_editor_6140(int editor, bool huds)
    {
        var info = MapFactory.Info(32, 32);
        (info.FormatVersion, info.EditorVersion, info.RaceHud) = (MapInfoFormatVersion.v39, (EditorVersion)editor, PlayerRaceHud.Undead);
        info.Players.Clear();
        info.Players.Add(Player(0, PlayerController.User, PlayerRace.Human, PlayerFlags.FixedStartPosition, "TRIGSTR_007", new Vector2(-512, 256), 2));
        info.Players.Add(Player(3, PlayerController.Computer, PlayerRace.Orc, 0, "", new Vector2(1024, -64), 1));
        var bytes = Written(writer => writer.Write(info));
        if (!huds) bytes = Without(bytes, BitConverter.GetBytes((int)Marker));

        var read = MapArchive.Read(Map(("war3map.w3i", bytes))).Files.Info!;

        Assert.Equal(PlayerRaceHud.Undead, read.RaceHud);
        Assert.Equal([(0, PlayerController.User, PlayerRace.Human, PlayerFlags.FixedStartPosition, "TRIGSTR_007", new Vector2(-512, 256), 2, 2),
            (3, PlayerController.Computer, PlayerRace.Orc, 0, "", new Vector2(1024, -64), 1, 1)],
            read.Players.Select(p => (p.Id, p.Controller, p.Race, p.Flags, p.Name, p.StartPosition, p.AllyLowPriorityFlags.ToInt32(), p.EnemyHighPriorityFlags.ToInt32())));
        Assert.All(read.Players, player => Assert.Equal(huds ? Marker : 0, player.RaceHud));
    }

    // D301: Human09's war3map.w3s names a sound g_snd_H09Arthas16 and repeats it as gg_snd_H09Arthas16
    [Fact]
    public void A_sound_whose_repeated_name_differs_keeps_its_first_name()
    {
        var sounds = new MapSounds(MapSoundsFormatVersion.v3) { Sounds = { Sound("gg_snd_Horn", "Horn", 1), Sound("gg_snd_Bell", "Bell", 0) } };
        var bytes = Written(writer => writer.Write(sounds));
        var first = Encoding.UTF8.GetBytes("gg_snd_Horn\0");
        var at = bytes.AsSpan().IndexOf(first);
        bytes = [.. bytes[..at], .. "g_snd_Horn\0"u8, .. bytes[(at + first.Length)..]];

        var read = MapArchive.Read(Map(("war3map.w3s", bytes))).Files.Sounds!;

        Assert.Equal([("g_snd_Horn", "Sound\\Horn.flac", "Horn", "x", 7), ("gg_snd_Bell", "Sound\\Bell.flac", "Bell", null, 7)],
            read.Sounds.Select(s => (s.Name, s.FilePath, s.SoundName, (string?)s.Unk5, s.Unk11)));
    }

    // A4, A5: every map in the install's Maps and Campaign folders reads, and each runs to its first Missing native or
    // through its first 10 minutes; where they stop is D302's
    [InstallFact]
    [Trait("Needs", "Game")]
    public void Every_shipped_map_reads_and_runs_to_its_first_missing_native()
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var maps = storage.Names
            .Where(name => name.StartsWith("War3.w3mod:Maps/", StringComparison.OrdinalIgnoreCase) || name.StartsWith("War3.w3mod:Campaign/", StringComparison.OrdinalIgnoreCase))
            .Where(name => Path.GetExtension(name).ToLowerInvariant() is ".w3x" or ".w3m" or ".w3n")
            .Order(StringComparer.Ordinal)
            .ToList();
        // The install's campaigns are folders of maps; a campaign archive would need opening
        Assert.DoesNotContain(maps, name => name.EndsWith(".w3n", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(maps);

        var failures = new List<string>();
        var stops = new List<(string Stop, string Map)>();
        var errors = new List<string>();
        foreach (var name in maps)
        {
            var path = Path.Combine(_folder, "map" + Path.GetExtension(name));
            File.WriteAllBytes(path, storage.Read(name) ?? throw new InvalidDataException($"{name} isn't stored locally"));
            try
            {
                if (MapArchive.Read(path).Script is null)
                {
                    stops.Add(("not run: no war3map.lua", name));
                    continue;
                }

                using var simulation = Simulation.Load(path);
                if (simulation.Stopped is null)
                    try { simulation.Advance(TimeSpan.FromMinutes(10)); }
                    catch (LuaException) when (simulation.Stopped is not null) { }
                stops.Add((simulation.Stopped?.InnerException switch
                {
                    null => "ran 10 minutes",
                    MissingNativeException missing => "Missing " + missing.Native,
                    var other => throw new InvalidOperationException("stopped by " + other.Message, other),
                }, name));
                errors.AddRange(simulation.Log.Where(line => line.Split(' ')[1] == "error:").Take(1).Select(line => $"{name}: {line}"));
            }
            catch (Exception e) { failures.Add($"{name}: {e.GetType().Name}: {e.Message}"); }
        }

        foreach (var stop in stops.GroupBy(s => s.Stop).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal))
            output.WriteLine($"{stop.Count(),4} {stop.Key}: {string.Join(", ", stop.Take(3).Select(s => s.Map))}{(stop.Count() > 3 ? ", ..." : "")}");
        foreach (var error in errors) output.WriteLine(error);
        Assert.Empty(failures);
    }

    static PlayerData Player(int id, PlayerController controller, PlayerRace race, PlayerFlags flags, string name, Vector2 start, int priorities) => new()
    {
        Id = id, Controller = controller, Race = race, RaceHud = Marker, Flags = flags, Name = name, StartPosition = start,
        AllyLowPriorityFlags = new Bitmask32(priorities), AllyHighPriorityFlags = new Bitmask32(0),
        EnemyLowPriorityFlags = new Bitmask32(0), EnemyHighPriorityFlags = new Bitmask32(priorities),
    };

    static Sound Sound(string name, string label, int unk4) => new()
    {
        Name = name, FilePath = $"Sound\\{label}.flac", EaxSetting = "DefaultEAXON", SoundName = label, Unk2 = "", Unk4 = unk4,
        Unk5 = unk4 != 0 ? "x" : null!, Unk6 = "", Unk7 = "Hart", FacialAnimationLabel = "", FacialAnimationGroupLabel = "",
        FacialAnimationSetFilepath = "", Unk11 = 7,
    };

    static byte[] Written(Action<BinaryWriter> write)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream)) write(writer);
        return stream.ToArray();
    }

    static byte[] Without(byte[] bytes, byte[] part)
    {
        var kept = new List<byte>();
        for (var at = 0; at < bytes.Length;)
            if (bytes.AsSpan(at).StartsWith(part)) at += part.Length;
            else kept.Add(bytes[at++]);
        return [.. kept];
    }

    // A map of a script and the given files
    string Map(params (string Name, byte[] Bytes)[] files)
    {
        var path = Path.Combine(_folder, Path.GetRandomFileName() + ".w3x");
        var archive = new MpqArchiveBuilder();
        foreach (var (name, bytes) in files) archive.AddFile(MpqFile.New(new MemoryStream(bytes), name));
        archive.AddFile(MpqFile.New(new MemoryStream("function main() end"u8.ToArray()), "war3map.lua"));
        archive.SaveTo(path);
        return path;
    }
}
