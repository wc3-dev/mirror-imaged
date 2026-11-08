namespace MirrorImaged.Natives;

public partial class player
{
    /// <summary>The player's slot, 0 to 27.</summary>
    public int Number { get; internal init; }

    internal string Name = "";
    internal int Color, Controller, SlotState, RacePreference, Race; // the values of common.j's Convert constants
    internal int Team = -1, StartLocation = -1;
    internal bool Selectable = true;
    internal float Handicap = 1, HandicapXP = 1, HandicapDamage = 1, HandicapReviveTime = 1;
    internal readonly int[] States = new int[26];
    internal readonly int[] Alliances = new int[28]; // bit n is alliancetype n towards that slot
    internal readonly Dictionary<int, int> TechMax = [], TechLevels = [];
}

// Players as a one-player -loadfile game starts them (the players probe, docs/facts.md "Players and the game"): slots 0
// to 23 empty with no controller until the lobby seats them (Seat), the four neutral ones playing as creeps.
public sealed partial class GameNatives
{
    const int Empty = 0, Playing = 1, Left = 2, User = 0, Computer = 1, Creep = 4, NoController = 5;
    const int GameResult = 0, Gold = 1, Lumber = 2, FoodCapCeiling = 6, GivesBounty = 7, Placed = 9;

    player _local;

    player[] MakePlayers()
    {
        var players = Enumerable.Range(0, 28).Select(number => Make(new player { Number = number, Name = $"Player {number + 1}", Color = number })).ToArray();
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
    /// Seats the players as the lobby does once <c>config</c> has run: people in <paramref name="users"/>, the first of
    /// them the local player, called "Local Player" when alone, and computers in <paramref name="computers"/>, each
    /// slot playing with its controller set; the other slots of 0 to 23 are empty. A playing slot whose race is
    /// selectable gets the random preference; it and every slot without a fixed race get a race from the run's random
    /// numbers. With <paramref name="customForces"/> the game type is the map's settings, and with
    /// <paramref name="fixedSettings"/> too, alliance changes are locked (facts, "Version and launching").
    /// </summary>
    /// <exception cref="ArgumentException">No user, a slot outside 0 to 23, or a slot given twice.</exception>
    public void Seat(IReadOnlyList<int> users, IReadOnlyList<int> computers, bool customForces = false, bool fixedSettings = false)
    {
        var seated = users.Concat(computers).ToList();
        if (users.Count == 0 || seated.Any(slot => (uint)slot >= 24) || seated.Distinct().Count() != seated.Count)
            throw new ArgumentException("A run seats at least one user, and users and computers in slots 0 to 23, each slot once");
        foreach (var p in _players.Take(24))
        {
            var playing = seated.Contains(p.Number);
            if (playing) p.Controller = users.Contains(p.Number) ? User : Computer;
            p.SlotState = playing ? Playing : Empty;
            p.States[Placed] = playing ? 1 : 0;
            if (playing && p.Selectable) p.RacePreference = 32; // RACE_PREF_RANDOM
            p.Race = playing && FixedRace(p.RacePreference) is { } race ? race : _game.RandomInt(1, 4);
        }
        _local = _players[users[0]];
        if (users.Count == 1) _local.Name = "Local Player";
        _gameType = customForces ? 4 : 1; // GAME_TYPE_USE_MAP_SETTINGS, GAME_TYPE_MELEE
        if (customForces && fixedSettings) _mapFlags |= 1024; // MAP_LOCK_ALLIANCE_CHANGES
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

    /// <summary>"Player N" for slot N - 1, and "Local Player" for the one person of a single-player game.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: player 1 is Local Player (10-20); docs/facts.md, Players and the game: the others Player N")]
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

    /// <summary>The skin (look) of the player's race, which the script can't read back.</summary>
    [Native(Level.Close)]
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

    /// <summary>One way: the other player's alliance back is left as it was. Close until triggers: the game fires the alliance event inside the call (facts).</summary>
    [Native(Level.Close)]
    public void SetPlayerAlliance(player? sourcePlayer, player? otherPlayer, alliancetype? whichAllianceSetting, bool value)
    {
        if (sourcePlayer is null || otherPlayer is null || whichAllianceSetting is not { Id: >= 0 and < 32 }) return;
        var bit = 1 << whichAllianceSetting.Id;
        sourcePlayer.Alliances[otherPlayer.Number] = value ? sourcePlayer.Alliances[otherPlayer.Number] | bit : sourcePlayer.Alliances[otherPlayer.Number] & ~bit;
    }

    /// <summary>Whether the player is passive to the other.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: IsPlayerAlly is true with ALLIANCE_PASSIVE alone, false with ALLIANCE_SHARED_VISION alone (10-20)")]
    public bool IsPlayerAlly(player? whichPlayer, player? otherPlayer) => GetPlayerAlliance(whichPlayer, otherPlayer, Convert<alliancetype>(0));

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: IsPlayerEnemy is IsPlayerAlly's opposite (10-20)")]
    public bool IsPlayerEnemy(player? whichPlayer, player? otherPlayer) =>
        whichPlayer is not null && otherPlayer is not null && !IsPlayerAlly(whichPlayer, otherPlayer);

    /// <summary>
    /// The state as the game starts it (the players probe: game result 3, food cap ceiling 100, placed 1 for a seated
    /// slot, bounty from neutral hostile and passive) or as set. Gold and lumber kept by units and buildings come with them.
    /// </summary>
    [Native(Level.Close)]
    public int GetPlayerState(player? whichPlayer, playerstate? whichPlayerState) =>
        whichPlayer is not null && whichPlayerState is { Id: >= 0 and < 26 } ? whichPlayer.States[whichPlayerState.Id] : 0;

    /// <summary>
    /// Gold below 0 is 0 (the players probe), and lumber too (not measured); the rest is kept as set. Close until
    /// triggers: the game fires the player state event on every change that meets it (facts).
    /// </summary>
    [Native(Level.Close)]
    public void SetPlayerState(player? whichPlayer, playerstate? whichPlayerState, int value)
    {
        if (whichPlayer is null || whichPlayerState is not { Id: >= 0 and < 26 }) return;
        whichPlayer.States[whichPlayerState.Id] = whichPlayerState.Id is Gold or Lumber ? Math.Max(value, 0) : value;
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
    /// The limit set, 2147483647 for none or a negative one. Close until the game's data: an upgrade's limit is its
    /// level count (3 for Iron Forged Swords, facts), here 2147483647 too.
    /// </summary>
    [Native(Level.Close)]
    public int GetPlayerTechMaxAllowed(player? whichPlayer, int techid) =>
        whichPlayer is not null && whichPlayer.TechMax.TryGetValue(techid, out var max) && max >= 0 ? max : int.MaxValue;

    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: a limit of 0 and of 2147483647 read back (10-20); docs/facts.md, Players and the game: a negative one reads 2147483647")]
    public void SetPlayerTechMaxAllowed(player? whichPlayer, int techid, int maximum)
    {
        if (whichPlayer is not null) whichPlayer.TechMax[techid] = maximum;
    }

    /// <summary>The level of an upgrade; a unit type's count of units comes with the units.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: SetPlayerTechResearched sets the level GetPlayerTechCount reads (10-20)")]
    public int GetPlayerTechCount(player? whichPlayer, int techid, bool specificonly) => whichPlayer?.TechLevels.GetValueOrDefault(techid) ?? 0;

    [Native(Level.Close)]
    public bool GetPlayerTechResearched(player? whichPlayer, int techid, bool specificonly) => GetPlayerTechCount(whichPlayer, techid, specificonly) > 0;

    /// <summary>Sets the level, also lower (the players probe).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: SetPlayerTechResearched sets the level GetPlayerTechCount reads (10-20); docs/facts.md, Players and the game: also lower")]
    public void SetPlayerTechResearched(player? whichPlayer, int techid, int setToLevel)
    {
        if (whichPlayer is not null) whichPlayer.TechLevels[techid] = setToLevel;
    }

    /// <summary>Close until the game's data: the game stops an upgrade at its level count (the players probe).</summary>
    [Native(Level.Close)]
    public void AddPlayerTechResearched(player? whichPlayer, int techid, int levels) =>
        SetPlayerTechResearched(whichPlayer, techid, GetPlayerTechCount(whichPlayer, techid, true) + levels);

    [Native(Level.Close)]
    public void BlzDecPlayerTechResearched(player? whichPlayer, int techid, int levels) => AddPlayerTechResearched(whichPlayer, techid, -levels);

    /// <summary>
    /// Takes the player out of the game: defeat leaves its slot Left, any other result keeps it Playing; its alliances
    /// stay. Close until triggers: the game fires the victory or defeat event inside the call for a computer (facts).
    /// </summary>
    [Native(Level.Close)]
    public void RemovePlayer(player? whichPlayer, playergameresult? gameResult)
    {
        if (whichPlayer is not null && gameResult?.Id == 1) whichPlayer.SlotState = Left; // PLAYER_GAME_RESULT_DEFEAT
    }
}
