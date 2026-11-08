using System.Text;
using System.Text.RegularExpressions;
using MirrorImaged.Data;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using MirrorImaged.Probes;
using War3Net.Build;

namespace MirrorImaged.GameTests;

/// <summary>
/// A line the game and MirrorImaged give differently: the line of <c>probes/expected/&lt;probe&gt;.txt</c> it is at (or
/// after, for a line only MirrorImaged gives), the game's line and MirrorImaged's, each null where that side gave none;
/// or, with <see cref="Stop"/>, MirrorImaged's run stopped without giving lines.
/// </summary>
public sealed record Difference(int Line, string? Game, string? Mirror, string? Stop = null)
{
    /// <summary>The difference as <c>probes/differences/&lt;probe&gt;.txt</c> records it (decisions D121).</summary>
    public IEnumerable<string> Recorded =>
        Stop is not null ? ["! " + Stop] : new[] { Game is null ? null : "- " + Game, Mirror is null ? null : "+ " + Mirror }.OfType<string>();

    public override string ToString() => Describe("MirrorImaged");

    /// <summary>The difference, with <paramref name="run"/> naming the run compared with the game's lines.</summary>
    public string Describe(string run) =>
        Stop is not null ? $"{run} stopped without lines: {Stop}"
        : Game is null ? $"after line {Line}: the game gave nothing, {run} \"{Mirror}\""
        : $"line {Line}: the game gave \"{Game}\", {run} {(Mirror is null ? "nothing" : $"\"{Mirror}\"")}";
}

/// <summary>
/// Compares a probe's lines with the game's, which <c>probes/expected/&lt;probe&gt;.txt</c> records, and runs a probe in
/// MirrorImaged (decisions D120 to D122).
/// </summary>
public static class ProbeComparison
{
    /// <summary>
    /// The one line of a probe whose game closed before it gave any: in the game, it exited by itself; in MirrorImaged,
    /// the run stopped with a <see cref="GameClosedException"/>.
    /// </summary>
    public const string Closes = "! the game closes";

    /// <summary>The most simulated time a probe runs in MirrorImaged, as the game runner gives a probe two minutes.</summary>
    public static readonly TimeSpan Longest = TimeSpan.FromMinutes(2);

    // In place of the game's blizzard.j, which the comparison runs without (decisions D120, D150): what blizzard.j does
    // that the probe maps and the probes read, as blizzard.j does it. Its handle globals, the slot functions config
    // calls, InitBlizzard's ConfigureNeutralVictim and bj_FORCE_ALL_PLAYERS, GetPlayersAll and PolledWait. InitBlizzard's
    // sounds, rects and units wait for their natives (later waves), so the handles they make are missing here.
    const string Frame = """
        bj_volumeGroupsTimer = CreateTimer()
        bj_queuedExecTimeoutTimer = CreateTimer()
        bj_suspendDecayFleshGroup = CreateGroup()
        bj_suspendDecayBoneGroup = CreateGroup()
        bj_delayedSuspendDecayTimer = CreateTimer()
        bj_lastCreatedGroup = CreateGroup()
        bj_lastStartedTimer = CreateTimer()

        local used = {}

        function SetPlayerSlotAvailable(p, control)
            used[GetPlayerId(p)] = true
        end

        local function TeamInitPlayerSlots(count)
            SetTeams(count)
            local team = 0
            for i = 0, GetBJMaxPlayers() - 1 do
                if used[i] then
                    SetPlayerTeam(Player(i), team)
                    team = (team + 1) % count
                end
            end
        end

        function InitGenericPlayerSlots()
            local type = GetGameTypeSelected()
            if type == GAME_TYPE_MELEE or type == GAME_TYPE_FFA then TeamInitPlayerSlots(GetBJMaxPlayers())
            elseif type == GAME_TYPE_ONE_ON_ONE then SetTeams(2) SetPlayers(2) TeamInitPlayerSlots(2)
            elseif type == GAME_TYPE_TWO_TEAM_PLAY then TeamInitPlayerSlots(2)
            elseif type == GAME_TYPE_THREE_TEAM_PLAY then TeamInitPlayerSlots(3)
            elseif type == GAME_TYPE_FOUR_TEAM_PLAY then TeamInitPlayerSlots(4)
            end
        end

        function InitBlizzard()
            local victim = Player(GetBJPlayerNeutralVictim())
            for i = 0, GetBJMaxPlayers() - 1 do
                SetPlayerAlliance(victim, Player(i), ALLIANCE_PASSIVE, true)
                SetPlayerAlliance(Player(i), victim, ALLIANCE_PASSIVE, false)
            end
            SetPlayerAlliance(victim, Player(PLAYER_NEUTRAL_AGGRESSIVE), ALLIANCE_PASSIVE, true)
            SetPlayerAlliance(Player(PLAYER_NEUTRAL_AGGRESSIVE), victim, ALLIANCE_PASSIVE, true)
            SetPlayerState(victim, PLAYER_STATE_GIVES_BOUNTY, 0)
            bj_FORCE_ALL_PLAYERS = CreateForce()
            ForceEnumPlayers(bj_FORCE_ALL_PLAYERS, nil)
        end

        function GetPlayersAll()
            return bj_FORCE_ALL_PLAYERS
        end

        function PolledWait(duration)
            if duration > 0 then
                local t = CreateTimer()
                TimerStart(t, duration, false, nil)
                while true do
                    local remaining = TimerGetRemaining(t)
                    if remaining <= 0 then break end
                    TriggerSleepAction(remaining > 2.0 and 0.1 * remaining or 0.1)
                end
                DestroyTimer(t)
            end
        end
        """;

    static string Probes => Path.Combine(AppContext.BaseDirectory, "probes");

    /// <summary>Every probe in <c>probes/</c>, Lua and JASS, by name.</summary>
    public static IEnumerable<string> Names =>
        Directory.GetFiles(Probes, "*.lua").Concat(Directory.GetFiles(Probes, "*.j")).Select(file => Path.GetFileNameWithoutExtension(file)).Order(StringComparer.Ordinal);

    /// <summary>Whether a probe is JASS, <c>probes/&lt;name&gt;.j</c>, which the game runs in its JASS machine (decisions D130).</summary>
    public static bool IsJass(string name) => File.Exists(Path.Combine(Probes, name + ".j"));

    /// <summary>A probe's script, Lua or JASS.</summary>
    public static string Source(string name) => IsJass(name) ? ProbeMap.JassSource(name) : ProbeMap.Source(name);

    /// <summary>Whether a probe runs on the map with a computer and named slots (decisions D140).</summary>
    public static bool WithComputer(string name) => name is "events" or "lobby" or "data-names";

    /// <summary>
    /// Whether a probe reads the game's data: W2-1's and W2-2's probes, run in MirrorImaged with the install's standard
    /// data, so only where the game is installed (decisions D213).
    /// </summary>
    public static bool ReadsData(string name) => name.StartsWith("data", StringComparison.Ordinal) || name.StartsWith("units", StringComparison.Ordinal);

    /// <summary>How a probe's map is changed, if it is: the data probes' info, strings and object data (decisions D213).</summary>
    public static Action<Map>? Change(string name) => name switch
    {
        "data" or "data-objects" or "data-tags" => DataProbeMaps.Data,
        "data-sets-melee" => DataProbeMaps.Melee,
        "data-sets-tft" => DataProbeMaps.FrozenThroneMelee,
        "data-sets-fk" => DataProbeMaps.ForsakenKingdom,
        "data-names" => DataProbeMaps.Names,
        _ => null,
    };

    /// <summary>The game's lines of a probe, <c>probes/expected/&lt;probe&gt;.txt</c>.</summary>
    public static string Expected(string name) => Path.Combine(Probes, "expected", name + ".txt");

    /// <summary>Where MirrorImaged's known differences from a probe's expected lines are, <c>probes/differences/&lt;probe&gt;.txt</c>.</summary>
    public static string Differences(string name) => Path.Combine(Probes, "differences", name + ".txt");

    /// <summary>
    /// Runs a probe's map in MirrorImaged, through the seam that runs without blizzard.j, for at most
    /// <see cref="Longest"/>; returns its lines, <see cref="Closes"/> alone if the run closed the game, or why it gave
    /// none.
    /// </summary>
    public static (IReadOnlyList<string>? Lines, string? Stop) Mirror(string name)
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
        try
        {
            var map = Path.Combine(folder, name + ".w3x");
            if (IsJass(name)) ProbeMap.BuildJass(name, ProbeMap.JassSource(name), map);
            else ProbeMap.Build(name, ProbeMap.Source(name), map, WithComputer(name), Change(name));
            var data = ReadsData(name) ? StandardData.Installed(GameInstall.Require(), MapArchive.Read(map).Files.Info) : null;
            using var simulation = Simulation.Load(map, Frame, data: data);
            var file = $@"{ProbeMap.Folder}\{name}.pld";
            for (var time = TimeSpan.Zero; time < Longest && simulation is { Stopped: null, Ended: false } && !simulation.Documents.CustomMapData.ContainsKey(file); time += TimeSpan.FromSeconds(1))
            {
                try { simulation.Advance(TimeSpan.FromSeconds(1)); }
                catch (LuaException) when (simulation.Stopped is not null) { }
            }

            // The probe ends with EndGame, so a run stopped after its lines is as the game's.
            if (simulation.Documents.CustomMapData.TryGetValue(file, out var bytes) && Preload.Lines(Encoding.UTF8.GetString(bytes)) is { } lines)
                return (lines, null);
            if (simulation.Stopped is { InnerException: GameClosedException })
                return ([Closes], null);
            if (simulation.Stopped is not null)
                return (null, simulation.Stopped.Message.Split('\n')[0]);
            var error = simulation.Log.LastOrDefault(entry => entry.Contains(" error: "));
            var why = simulation.Ended ? "the game ended without lines" : $"no lines in {Longest.TotalSeconds} simulated seconds";
            return (null, $"{why}{(error is null ? "" : $"; the last error: {error}")}");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>
    /// How a run's lines differ from an expected file (decisions D71, D124): a line starting <c># </c> is a note; in any
    /// other, each <c>{t}</c> stands for a number and each <c>{w}</c> for a word (ASCII letters, digits and underscores),
    /// the rest exact. A line starting <c>~ </c> moves from run to run, and the run gives it once, anywhere, as its time
    /// decides its place; every other line comes in order. The lines that differ are paired in order where both sides
    /// give some between the same common lines.
    /// </summary>
    /// <exception cref="InvalidDataException">A line starting <c>~ </c> has no <c>{t}</c> or <c>{w}</c>, or nothing else.</exception>
    public static List<Difference> Compare(string expected, IReadOnlyList<string> lines)
    {
        var file = File.ReadAllLines(expected).Select((line, i) => Expect(expected, line, i + 1)).OfType<ExpectedLine>().ToList();
        var moving = file.Where(e => e.Moves).ToList();
        var game = file.Where(e => !e.Moves).ToList();

        var mirror = new List<string>();
        foreach (var line in lines)
        {
            var match = moving.FindIndex(e => e.Matches(line));
            if (match < 0) mirror.Add(line);
            else moving.RemoveAt(match);
        }

        // The longest common run of lines, from each pair of places to the ends.
        var common = new int[game.Count + 1, mirror.Count + 1];
        for (var i = game.Count - 1; i >= 0; i--)
            for (var j = mirror.Count - 1; j >= 0; j--)
                common[i, j] = game[i].Matches(mirror[j]) ? common[i + 1, j + 1] + 1 : Math.Max(common[i + 1, j], common[i, j + 1]);

        var differences = new List<Difference>();
        var (gameOnly, mirrorOnly, after) = (new List<ExpectedLine>(), new List<string>(), 0);
        void Pair()
        {
            var last = gameOnly.Count > 0 ? gameOnly[^1].Number : after;
            for (var k = 0; k < Math.Max(gameOnly.Count, mirrorOnly.Count); k++)
                differences.Add(new(k < gameOnly.Count ? gameOnly[k].Number : last, gameOnly.ElementAtOrDefault(k)?.Line, mirrorOnly.ElementAtOrDefault(k)));
            gameOnly.Clear();
            mirrorOnly.Clear();
        }

        var (a, b) = (0, 0);
        while (a < game.Count || b < mirror.Count)
        {
            if (a < game.Count && b < mirror.Count && game[a].Matches(mirror[b]))
            {
                Pair();
                after = game[a].Number;
                (a, b) = (a + 1, b + 1);
            }
            else if (b == mirror.Count || a < game.Count && common[a + 1, b] >= common[a, b + 1])
                gameOnly.Add(game[a++]);
            else
                mirrorOnly.Add(mirror[b++]);
        }

        Pair();
        differences.AddRange(moving.Select(e => new Difference(e.Number, e.Line, null)));
        return differences;
    }

    // An expected file's line: as written, its number, whether it moves (~), and a pattern when it has a {t} or {w}.
    sealed record ExpectedLine(string Line, int Number, bool Moves, Regex? Pattern)
    {
        public bool Matches(string line) => Pattern?.IsMatch(line) ?? (Moves ? Line[2..] : Line) == line;
    }

    static ExpectedLine? Expect(string file, string line, int number)
    {
        if (line.StartsWith("# "))
            return null;
        var moves = line.StartsWith("~ ");
        var text = moves ? line[2..] : line;
        var tokens = Regex.IsMatch(text, @"\{[tw]}");
        if (moves && (!tokens || Regex.Replace(text, @"\{[tw]}", "").Trim().Length == 0))
            throw new InvalidDataException(
                $"{file}:{number}: a line starting \"~ \" moves, and needs a {{t}} or {{w}} for what varies and some text that doesn't");
        return new(line, number, moves, tokens
            ? new Regex("^" + Regex.Escape(text).Replace(@"\{t}", @"-?[0-9]+(\.[0-9]+)?").Replace(@"\{w}", "[A-Za-z0-9_]+") + "$")
            : null);
    }

    /// <summary>Runs a probe in MirrorImaged and fails unless it differs from the game's lines as recorded.</summary>
    public static void AssertMirror(string name)
    {
        Assert.True(File.Exists(Expected(name)), $"{name} has no probes/expected/{name}.txt: run it in the game first");
        var (lines, stop) = Mirror(name);
        AssertRecorded(name, lines is null ? [new Difference(0, null, null, stop)] : Compare(Expected(name), lines));
    }

    /// <summary>
    /// Fails unless <paramref name="differences"/> are what <c>probes/differences/&lt;probe&gt;.txt</c> records, in
    /// order (decisions D121): none when there is no such file. The message names each difference and gives the lines
    /// the file would hold.
    /// </summary>
    public static void AssertRecorded(string name, IReadOnlyList<Difference> differences)
    {
        var file = Differences(name);
        var recorded = File.Exists(file) ? File.ReadAllLines(file).Where(line => line.Length > 0 && !line.StartsWith("# ")).ToList() : [];
        var actual = differences.SelectMany(d => d.Recorded).ToList();
        Assert.True(recorded.SequenceEqual(actual), $"""
            {name}: MirrorImaged's lines differ from the game's (probes/expected/{name}.txt) otherwise than probes/differences/{name}.txt records.
            {(differences.Count == 0 ? "They no longer differ." : string.Join('\n', differences))}
            The file's lines but its notes would be:
            {string.Join('\n', actual)}
            """);
    }
}
