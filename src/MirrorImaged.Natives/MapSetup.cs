namespace MirrorImaged.Natives;

// What config() tells the lobby, kept for the getters. What only the lobby reads (the map's name and description, the
// enemy start priorities) is Recorded: no native reads it back (decisions D98).
public sealed partial class GameNatives
{
    int _playerCount, _teamCount, _placement = 2, _gameType = 1, _mapFlags; // MAP_PLACEMENT_USE_MAP_SETTINGS, GAME_TYPE_MELEE
    readonly Dictionary<int, (float X, float Y)> _startLocations = [];
    readonly Dictionary<(int Location, int Slot), (int Other, int Priority)> _startPriorities = [];

    [Native(Level.Recorded, Reason = "Names the map in the lobby and on the score screen; no native reads it back")]
    public void SetMapName(string? name) { }

    [Native(Level.Recorded, Reason = "Describes the map in the lobby; no native reads it back")]
    public void SetMapDescription(string? description) { }

    [Native(Level.Close)]
    public void SetPlayers(int playercount) => _playerCount = playercount;

    [Native(Level.Close)]
    public int GetPlayers() => _playerCount;

    [Native(Level.Close)]
    public void SetTeams(int teamcount) => _teamCount = teamcount;

    [Native(Level.Close)]
    public int GetTeams() => _teamCount;

    [Native(Level.Close)]
    public void SetGamePlacement(placement? whichPlacementType)
    {
        if (whichPlacementType is not null) _placement = whichPlacementType.Id;
    }

    /// <summary>The placement set; the map's settings until then.</summary>
    [Native(Level.Close)]
    public placement? GetGamePlacement() => Convert<placement>(_placement);

    /// <summary>
    /// Melee, as a <c>-loadfile</c> game of a map without custom forces is (the players probe); the map's settings for
    /// one with custom forces (<see cref="Seat"/>, not measured).
    /// </summary>
    [Native(Level.Close)]
    public gametype? GetGameTypeSelected() => Convert<gametype>(_gameType);

    [Native(Level.Close)]
    public void DefineStartLocation(int whichStartLoc, float x, float y) => _startLocations[whichStartLoc] = (x, y);

    /// <summary>The start location's x; 0 for one not defined (the players probe).</summary>
    [Native(Level.Close)]
    public float GetStartLocationX(int whichStartLocation) => _startLocations.GetValueOrDefault(whichStartLocation).X;

    [Native(Level.Close)]
    public float GetStartLocationY(int whichStartLocation) => _startLocations.GetValueOrDefault(whichStartLocation).Y;

    /// <summary>
    /// How many priorities the start location has, which the lobby's placement reads. Close rather than Recorded:
    /// whether GetStartLocPrio and GetStartLocPrioSlot read the slots past it differently isn't measured; here they read
    /// what was set, whatever the count.
    /// </summary>
    [Native(Level.Close)]
    public void SetStartLocPrioCount(int whichStartLoc, int prioSlotCount) { }

    [Native(Level.Close)]
    public void SetStartLocPrio(int whichStartLoc, int prioSlotIndex, int otherStartLocIndex, startlocprio? priority) =>
        _startPriorities[(whichStartLoc, prioSlotIndex)] = (otherStartLocIndex, priority?.Id ?? 0);

    /// <summary>The other start location of the priority; 0 for one not set (not measured).</summary>
    [Native(Level.Close)]
    public int GetStartLocPrioSlot(int whichStartLoc, int prioSlotIndex) => _startPriorities.GetValueOrDefault((whichStartLoc, prioSlotIndex)).Other;

    /// <summary>The priority set; MAP_LOC_PRIO_LOW for one not set (not measured).</summary>
    [Native(Level.Close)]
    public startlocprio? GetStartLocPrio(int whichStartLoc, int prioSlotIndex) =>
        Convert<startlocprio>(_startPriorities.GetValueOrDefault((whichStartLoc, prioSlotIndex)).Priority);

    [Native(Level.Recorded, Reason = "Only the lobby's placement of players reads it; no native reads it back")]
    public void SetEnemyStartLocPrioCount(int whichStartLoc, int prioSlotCount) { }

    [Native(Level.Recorded, Reason = "Only the lobby's placement of players reads it; no native reads it back")]
    public void SetEnemyStartLocPrio(int whichStartLoc, int prioSlotIndex, int otherStartLocIndex, startlocprio? priority) { }

    /// <summary>Close: only MAP_LOCK_ALLIANCE_CHANGES set false is measured, and it reads back false (facts).</summary>
    [Native(Level.Close)]
    public void SetMapFlag(mapflag? whichMapFlag, bool value)
    {
        if (whichMapFlag is not null) _mapFlags = value ? _mapFlags | whichMapFlag.Id : _mapFlags & ~whichMapFlag.Id;
    }

    /// <summary>A flag set, or the lobby's: none, but MAP_LOCK_ALLIANCE_CHANGES for custom forces with fixed player settings.</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: custom forces and fixed player settings start with MAP_LOCK_ALLIANCE_CHANGES set (11-06); docs/facts.md, Players and the game: no flag without them")]
    public bool IsMapFlagSet(mapflag? whichMapFlag) => whichMapFlag is not null && (_mapFlags & whichMapFlag.Id) != 0;
}
