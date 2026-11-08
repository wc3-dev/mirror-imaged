using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class player
{
    /// <summary>The player's slot, 0 to 27.</summary>
    public int Number { get; internal init; }

    internal string Name = "";
    internal int Color, Controller, SlotState, RacePreference, Race; // the values of common.j's Convert constants
    internal int Team = -1, StartLocation = -1;
    internal bool Selectable = true;
    internal bool Removed; // RemovePlayer has given it its result, and does nothing more to it
    internal float Handicap = 1, HandicapXP = 1, HandicapDamage = 1, HandicapReviveTime = 1;
    internal readonly int[] States = new int[26];
    internal readonly int[] Alliances = new int[28]; // bit n is alliancetype n towards that slot
    internal readonly Dictionary<int, int> TechMax = [], TechLevels = [];
}

// Players as a -loadfile game starts them (the players and lobby probes, docs/facts.md "Players and the game"): slots 0
// to 23 empty, unnamed, with no controller and a random race preference until the lobby seats them (Seat), the four
// neutral ones playing as creeps.
public sealed partial class GameNatives
{
    const int Empty = 0, Playing = 1, Left = 2, User = 0, Computer = 1, Creep = 4, NoController = 5, RandomRace = 32;
    const int GameResult = 0, Gold = 1, Lumber = 2, FoodCapCeiling = 6, GivesBounty = 7, Placed = 9;

    player _local;

    player[] MakePlayers()
    {
        var players = Enumerable.Range(0, 28).Select(number => Make(new player
        {
            Number = number, Name = number < 24 ? "" : $"Player {number + 1}", Color = number, RacePreference = RandomRace,
        })).ToArray();
        foreach (var p in players)
        {
            p.States[GameResult] = 3; // PLAYER_GAME_RESULT_NEUTRAL
            p.States[FoodCapCeiling] = 100;
            p.Alliances[27] = players[27].Alliances[p.Number] = 1; // neutral passive and everyone are passive to each other
            p.Alliances[p.Number] = 255; // types 0 to 7 towards itself
            (p.Controller, p.SlotState) = p.Number < 24 ? (NoController, Empty) : (Creep, Playing);
            if (p.Number >= 24) (p.Team, p.States[GivesBounty]) = (0, 1);
        }
        return players;
    }

    /// <summary>
    /// Seats the players as the lobby does once <c>config</c> has run (the players and lobby probes): people in
    /// <paramref name="users"/>, the first of them the local player, called "Local Player" when alone, and computers in
    /// <paramref name="computers"/>, each slot playing with its controller set, and the map's neutral and rescuable
    /// slots in <paramref name="others"/>, playing with the controller <c>config</c> gave them; the other slots of 0 to
    /// 23 are empty, on team -1, with their controller as <c>config</c> left it. A slot of 0 to 23 <c>config</c> named
    /// keeps its name; else a computer takes its slot's name in <paramref name="names"/> (<c>war3map.w3i</c>'s for the
    /// map's computer slots, "Player N" in another) when the map has custom forces with fixed settings and is
    /// "Computer (Normal)" otherwise, and every other slot is "Player N" (decisions D141, D210). A playing slot whose
    /// race is selectable gets the random preference, unless the map has custom forces with
    /// <paramref name="fixedSettings"/>, which keep every preference; a slot whose preference names a race has it, and
    /// the others get a random race, from a generator of their own seeded by the run's seed, drawn for every slot in
    /// order so that a slot's race and the map's own random numbers don't depend on who sits where. Custom forces with
    /// fixed settings also set <c>MAP_LOCK_ALLIANCE_CHANGES</c> and the flag 2^20, which <c>common.j</c> doesn't name.
    /// </summary>
    /// <exception cref="ArgumentException">As <see cref="CheckSeats"/>.</exception>
    public void Seat(IReadOnlyList<int> users, IReadOnlyList<int> computers, IReadOnlyList<int>? others = null,
        bool customForces = false, bool fixedSettings = false, IReadOnlyDictionary<int, string>? names = null)
    {
        others ??= [];
        CheckSeats(users, [.. computers, .. others]);
        var races = new Game(_game.RunSeed ^ 0x52616365); // 'Race'
        var fixedRaces = customForces && fixedSettings;
        foreach (var p in _players.Take(24))
        {
            var (user, computer) = (users.Contains(p.Number), computers.Contains(p.Number));
            var playing = user || computer || others.Contains(p.Number);
            if (user || computer) p.Controller = user ? User : Computer;
            p.SlotState = playing ? Playing : Empty;
            p.States[Placed] = playing ? 1 : 0;
            if (p.Name == "")
                p.Name = user && users.Count == 1 ? "Local Player"
                    : computer ? fixedRaces ? names?.GetValueOrDefault(p.Number) ?? $"Player {p.Number + 1}" : "Computer (Normal)"
                    : $"Player {p.Number + 1}";
            if (!playing) p.Team = -1;
            if (playing && p.Selectable && !fixedRaces) p.RacePreference = RandomRace;
            var drawn = races.RandomInt(1, 4);
            p.Race = FixedRace(p.RacePreference) ?? drawn;
        }
        _local = _players[users[0]];
        if (fixedRaces) _mapFlags |= 1024 | 1 << 20; // MAP_LOCK_ALLIANCE_CHANGES
    }

    /// <summary>Refuses a lineup with no user, a slot outside 0 to 23, or a slot given twice.</summary>
    /// <param name="users">The slots people play.</param>
    /// <param name="others">The other slots that play.</param>
    /// <exception cref="ArgumentException">The lineup can't be seated; the message says why.</exception>
    public static void CheckSeats(IReadOnlyList<int> users, IReadOnlyList<int> others)
    {
        if (users.Count == 0) throw new ArgumentException("A run needs a person playing, and no user slot is given");
        var seated = users.Concat(others).ToList();
        foreach (var slot in seated)
            if ((uint)slot >= 24) throw new ArgumentException($"Slot {slot} can't be played: people and computers play slots 0 to 23");
        if (seated.GroupBy(slot => slot).FirstOrDefault(slot => slot.Count() > 1) is { } twice)
            throw new ArgumentException($"Slot {twice.Key} is given twice");
    }

    // RACE_PREF_HUMAN, ORC, NIGHTELF, UNDEAD and DEMON as RACE_*; null for random or none.
    static int? FixedRace(int preference) =>
        (preference & -preference) switch { 1 => 1, 2 => 2, 4 => 4, 8 => 3, 16 => 5, _ => null };

    /// <summary>The player in slot <paramref name="number"/>, 0 to 27; null for any other number, not measured.</summary>
    [Native(Level.Close)]
    public player? Player(int number) => (uint)number < _players.Length ? _players[number] : null;

    /// <summary>The player the run is played as: slot 0 until <see cref="Seat"/> says otherwise.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, C# through CSharp.lua: in single player, GetLocalPlayer inside Local.For is slot 0 (10-19)")]
    public player? GetLocalPlayer() => _local;

    /// <summary>The player's slot; 0 for null, not measured.</summary>
    [Native(Level.Close)]
    public int GetPlayerId(player? whichPlayer) => whichPlayer?.Number ?? 0;

    // The slots common.j and blizzard.j name: users and computers 0 to 23, then neutral aggressive, victim, extra and
    // passive. Only neutral passive's is measured.
    [Native(Level.Close)]
    public int GetBJMaxPlayers() => 24;

    [Native(Level.Close)]
    public int GetPlayerNeutralAggressive() => 24;

    [Native(Level.Close)]
    public int GetBJPlayerNeutralVictim() => 25;

    [Native(Level.Close)]
    public int GetBJPlayerNeutralExtra() => 26;

    [Native(Level.Exact, Fact = "game-facts.md, Orders and events: a new item belongs to neutral passive (slot 27) (10-21)")]
    public int GetPlayerNeutralPassive() => 27;

    [Native(Level.Close)]
    public int GetBJMaxPlayerSlots() => _players.Length;

    /// <summary>Text for <paramref name="toPlayer"/>, which goes in the run's log: <c>2.000 player 0: text</c>.</summary>
    [Native(Level.Recorded, Reason = "Shows text on the player's screen; the run's log records it with the player")]
    public void DisplayTextToPlayer(player? toPlayer, float x, float y, string? message) => Show(toPlayer, message);

    /// <summary>As <see cref="DisplayTextToPlayer"/>; the duration isn't kept.</summary>
    [Native(Level.Recorded, Reason = "Shows text on the player's screen for a time; the run's log records it with the player")]
    public void DisplayTimedTextToPlayer(player? toPlayer, float x, float y, float duration, string? message) => Show(toPlayer, message);

    void Show(player? toPlayer, string? message)
    {
        if (toPlayer is not null) _game.Write($"player {toPlayer.Number}", message ?? "");
    }

    /// <summary>
    /// "" for slots 0 to 23 until the lobby seats them, then "Player N" for slot N - 1, "Local Player" for the one person
    /// of a single-player game, and for a computer its name in <c>war3map.w3i</c> with fixed player settings and
    /// "Computer (Normal)" without, as the players, lobby and data probes read them; a name <c>config</c> set is kept (not
    /// measured). Close: several people's names, their accounts', aren't known.
    /// </summary>
    [Native(Level.Close)]
    public string? GetPlayerName(player? whichPlayer) => whichPlayer?.Name;

    /// <summary>Not measured: the game crashes lobby creation when it is called in <c>config</c> (facts), which this doesn't.</summary>
    [Native(Level.Close)]
    public void SetPlayerName(player? whichPlayer, string? name)
    {
        if (whichPlayer is not null) whichPlayer.Name = name ?? "";
    }

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: GetPlayerColor compares equal to ConvertPlayerColor(n) (10-20)")]
    public playercolor? GetPlayerColor(player? whichPlayer) => whichPlayer is null ? null : Convert<playercolor>(whichPlayer.Color);

    /// <summary>Close until units: the game leaves the player's units in their old colour (facts).</summary>
    [Native(Level.Close)]
    public void SetPlayerColor(player? whichPlayer, playercolor? color)
    {
        if (whichPlayer is not null && color is not null) whichPlayer.Color = color.Id;
    }

    /// <summary>The race the lobby gave the player (<see cref="Seat"/>).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: GetPlayerRace compares equal to RACE_* (10-20)")]
    public race? GetPlayerRace(player? whichPlayer) => whichPlayer is null ? null : Convert<race>(whichPlayer.Race);

    [Native(Level.Close)]
    public void SetPlayerRacePreference(player? whichPlayer, racepreference? whichRacePreference)
    {
        if (whichPlayer is not null && whichRacePreference is not null) whichPlayer.RacePreference = whichRacePreference.Id;
    }

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: a selectable race reads RACE_PREF_RANDOM set (10-12)")]
    public bool IsPlayerRacePrefSet(player? whichPlayer, racepreference? pref) =>
        whichPlayer is not null && pref is not null && (whichPlayer.RacePreference & pref.Id) != 0;

    [Native(Level.Close)]
    public void SetPlayerRaceSelectable(player? whichPlayer, bool value)
    {
        if (whichPlayer is not null) whichPlayer.Selectable = value;
    }

    /// <summary>Whether the player's race is selectable; true until set (the players probe).</summary>
    [Native(Level.Close)]
    public bool GetPlayerSelectable(player? whichPlayer) => whichPlayer?.Selectable ?? false;

    [Native(Level.Recorded, Reason = "Picks the look of the player's race; no native reads it back")]
    public void SetPlayerRaceSkin(player? whichPlayer, racepreference? whichRacePreference) { }

    [Native(Level.Exact, Fact = "game-facts.md, Input and sync: in single player player 1 is a user (10-20); docs/facts.md, Players and the game")]
    public mapcontrol? GetPlayerController(player? whichPlayer) => whichPlayer is null ? null : Convert<mapcontrol>(whichPlayer.Controller);

    /// <summary>The controller a script reads; who plays is the lobby's (<see cref="Seat"/>), which sets it again for the seated slots.</summary>
    [Native(Level.Close)]
    public void SetPlayerController(player? whichPlayer, mapcontrol? controlType)
    {
        if (whichPlayer is not null && controlType is not null) whichPlayer.Controller = controlType.Id;
    }

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: RemovePlayer and CustomDefeatBJ leave the slot Left, victory Playing (11-06); docs/facts.md, Players and the game")]
    public playerslotstate? GetPlayerSlotState(player? whichPlayer) => whichPlayer is null ? null : Convert<playerslotstate>(whichPlayer.SlotState);

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: an unused slot's team is -1 (10-20); docs/facts.md, Players and the game")]
    public int GetPlayerTeam(player? whichPlayer) => whichPlayer?.Team ?? 0;

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: SetPlayerTeam changes the team number only (11-06)")]
    public void SetPlayerTeam(player? whichPlayer, int whichTeam)
    {
        if (whichPlayer is not null) whichPlayer.Team = whichTeam;
    }

    /// <summary>The start location set; -1 until then. The lobby doesn't place players (not measured).</summary>
    [Native(Level.Close)]
    public int GetPlayerStartLocation(player? whichPlayer) => whichPlayer?.StartLocation ?? -1;

    [Native(Level.Close)]
    public void SetPlayerStartLocation(player? whichPlayer, int startLocIndex)
    {
        if (whichPlayer is not null) whichPlayer.StartLocation = startLocIndex;
    }

    /// <summary>As <see cref="SetPlayerStartLocation"/>; the lobby's placement, which it forces, isn't simulated.</summary>
    [Native(Level.Close)]
    public void ForcePlayerStartLocation(player? whichPlayer, int startLocIndex) => SetPlayerStartLocation(whichPlayer, startLocIndex);

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: alliances go one way (10-20); docs/facts.md, Players and the game: the starting alliances")]
    public bool GetPlayerAlliance(player? sourcePlayer, player? otherPlayer, alliancetype? whichAllianceSetting) =>
        sourcePlayer is not null && otherPlayer is not null && whichAllianceSetting is { Id: >= 0 and < 32 }
        && (sourcePlayer.Alliances[otherPlayer.Number] & (1 << whichAllianceSetting.Id)) != 0;

    /// <summary>
    /// One way: the other player's alliance back is left as it was. A change fires the source player's alliance events
    /// inside the call (<see cref="AllianceChanged"/>); a player's alliance with itself doesn't change (measured for
    /// passive).
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: alliances go one way (10-20); " + Events)]
    public void SetPlayerAlliance(player? sourcePlayer, player? otherPlayer, alliancetype? whichAllianceSetting, bool value)
    {
        if (sourcePlayer is null || otherPlayer is null || sourcePlayer == otherPlayer || whichAllianceSetting is not { Id: >= 0 and < 32 }) return;
        var (was, bit) = (sourcePlayer.Alliances[otherPlayer.Number], 1 << whichAllianceSetting.Id);
        var now = value ? was | bit : was & ~bit;
        if (now == was) return;
        sourcePlayer.Alliances[otherPlayer.Number] = now;
        AllianceChanged(sourcePlayer, whichAllianceSetting.Id);
    }

    /// <summary>Whether the player is passive to the other.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: IsPlayerAlly is true with ALLIANCE_PASSIVE alone, false with ALLIANCE_SHARED_VISION alone (10-20)")]
    public bool IsPlayerAlly(player? whichPlayer, player? otherPlayer) => GetPlayerAlliance(whichPlayer, otherPlayer, Convert<alliancetype>(0));

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: IsPlayerEnemy is IsPlayerAlly's opposite (10-20)")]
    public bool IsPlayerEnemy(player? whichPlayer, player? otherPlayer) =>
        whichPlayer is not null && otherPlayer is not null && !IsPlayerAlly(whichPlayer, otherPlayer);

    /// <summary>
    /// The state as the game starts it (the players probe: game result 3, food cap ceiling 100, placed 1 for a seated
    /// slot, bounty from neutral hostile and passive) or as set. The food that units and buildings use and give comes
    /// with them (W2-2).
    /// </summary>
    [Native(Level.Close)]
    public int GetPlayerState(player? whichPlayer, playerstate? whichPlayerState) =>
        whichPlayer is not null && whichPlayerState is { Id: >= 0 and < 26 } ? whichPlayer.States[whichPlayerState.Id] : 0;

    /// <summary>
    /// Gold below 0 is 0 (the players and events probes), and lumber too (inferred); the rest is kept as set. A change
    /// fires the player's state events whose comparison the new value meets, inside the call
    /// (<see cref="PlayerStateChanged"/>).
    /// </summary>
    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game: gold below 0 reads 0; " + Events)]
    public void SetPlayerState(player? whichPlayer, playerstate? whichPlayerState, int value)
    {
        if (whichPlayer is null || whichPlayerState is not { Id: >= 0 and < 26 }) return;
        if (whichPlayerState.Id is Gold or Lumber) value = Math.Max(value, 0);
        if (whichPlayer.States[whichPlayerState.Id] == value) return;
        whichPlayer.States[whichPlayerState.Id] = value;
        PlayerStateChanged(whichPlayer, whichPlayerState, value);
    }

    [Native(Level.Close)]
    public bool IsPlayerObserver(player? whichPlayer) => whichPlayer?.States[11] is > 0; // PLAYER_STATE_OBSERVER

    [Native(Level.Close)] public float GetPlayerHandicap(player? whichPlayer) => whichPlayer?.Handicap ?? 0;
    [Native(Level.Close)] public float GetPlayerHandicapXP(player? whichPlayer) => whichPlayer?.HandicapXP ?? 0;
    [Native(Level.Close)] public float GetPlayerHandicapDamage(player? whichPlayer) => whichPlayer?.HandicapDamage ?? 0;
    [Native(Level.Close)] public float GetPlayerHandicapReviveTime(player? whichPlayer) => whichPlayer?.HandicapReviveTime ?? 0;

    [Native(Level.Close)]
    public void SetPlayerHandicap(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.Handicap = handicap;
    }

    [Native(Level.Close)]
    public void SetPlayerHandicapXP(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.HandicapXP = handicap;
    }

    [Native(Level.Close)]
    public void SetPlayerHandicapDamage(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.HandicapDamage = handicap;
    }

    [Native(Level.Close)]
    public void SetPlayerHandicapReviveTime(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.HandicapReviveTime = handicap;
    }

    /// <summary>
    /// The limit set; for none or a negative one, an upgrade's level count, with the map's change (a new upgrade's is
    /// its standard base's), and 2147483647 for anything else.
    /// </summary>
    [Native(Level.Exact, Fact = "docs/facts.md, Players and the game (Rhme 3, hfoo and zzzz 2147483647, -5 set) and The game's data (Rhme changed to 5, R000 made from it 3)")]
    public int GetPlayerTechMaxAllowed(player? whichPlayer, int techid) =>
        whichPlayer is not null && whichPlayer.TechMax.TryGetValue(techid, out var max) && max >= 0 ? max : Levels(techid) ?? int.MaxValue;

    // An upgrade's level count; null for anything else.
    int? Levels(int techid) => Data.Object(ObjectKind.Upgrade, techid)?.Levels;

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: a limit of 0 and of 2147483647 read back (10-20); docs/facts.md, Players and the game: a negative one reads 2147483647")]
    public void SetPlayerTechMaxAllowed(player? whichPlayer, int techid, int maximum)
    {
        if (whichPlayer is not null) whichPlayer.TechMax[techid] = maximum;
    }

    /// <summary>
    /// The level set, as the facts and the players probe read it. Close: a unit type's count of the player's units
    /// comes with the units (W2-2), and <paramref name="specificonly"/>, which then counts only that type and not the
    /// ones it upgrades to, is ignored.
    /// </summary>
    [Native(Level.Close)]
    public int GetPlayerTechCount(player? whichPlayer, int techid, bool specificonly) => whichPlayer?.TechLevels.GetValueOrDefault(techid) ?? 0;

    [Native(Level.Close)]
    public bool GetPlayerTechResearched(player? whichPlayer, int techid, bool specificonly) => GetPlayerTechCount(whichPlayer, techid, specificonly) > 0;

    /// <summary>
    /// Sets the level, also lower (the players probe), and not below 0 (not measured). Close: setting an upgrade above
    /// its level count isn't measured.
    /// </summary>
    [Native(Level.Close)]
    public void SetPlayerTechResearched(player? whichPlayer, int techid, int setToLevel)
    {
        if (whichPlayer is not null) whichPlayer.TechLevels[techid] = Math.Max(setToLevel, 0);
    }

    /// <summary>
    /// Adds levels, stopping an upgrade at its level count (the players and data probes) and anything else at
    /// 2147483647. Close: a count that already passes the level count isn't measured, nor a limit set below it.
    /// </summary>
    [Native(Level.Close)]
    public void AddPlayerTechResearched(player? whichPlayer, int techid, int levels) =>
        SetPlayerTechResearched(whichPlayer, techid, (int)Math.Min((long)GetPlayerTechCount(whichPlayer, techid, true) + levels, Levels(techid) ?? int.MaxValue));

    [Native(Level.Close)]
    public void BlzDecPlayerTechResearched(player? whichPlayer, int techid, int levels) => AddPlayerTechResearched(whichPlayer, techid, -levels);

    /// <summary>
    /// Takes the player out of the game, once: its <c>PLAYER_STATE_GAME_RESULT</c> becomes the result, victory keeps its
    /// slot Playing and any other result leaves it Left (tie not measured), its alliances stay, and a player that isn't a
    /// user fires its <c>EVENT_PLAYER_VICTORY</c> or <c>EVENT_PLAYER_DEFEAT</c> inside the call, after the change; a
    /// user fires neither, a neutral result nothing. A player already taken out is left as it is.
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: RemovePlayer, CustomVictoryBJ and CustomDefeatBJ (11-06); " + Events)]
    public void RemovePlayer(player? whichPlayer, playergameresult? gameResult)
    {
        if (whichPlayer is not { Removed: false } || gameResult is null) return;
        whichPlayer.Removed = true;
        whichPlayer.States[GameResult] = gameResult.Id;
        if (gameResult.Id != 0) whichPlayer.SlotState = Left; // all but PLAYER_GAME_RESULT_VICTORY
        if (whichPlayer.Controller != User && gameResult.Id is 0 or 1) FirePlayerEvent(whichPlayer, gameResult.Id == 0 ? PlayerVictory : PlayerDefeat, whichPlayer);
    }
}
