namespace MirrorImaged.Tests;

/// <summary>blizzard.j's player functions, as World Editor's config and main call them, on the player natives.</summary>
[Trait("Needs", "Game")]
public sealed class PlayerBlizzardTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // config as World Editor writes it for two allied players and a third on its own; main, InitBlizzard's player parts.
    // The players probe (docs/facts.md, "Players and the game") read the neutral victim's alliances and bounty after them;
    // the lobby probe, that slot 2, which no one plays, is on team -1.
    [InstallFact]
    public void World_editors_config_and_blizzard_j_s_player_setup_run_on_the_natives()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config()
              SetPlayers(3)
              SetTeams(3)
              SetGamePlacement(MAP_PLACEMENT_TEAMS_TOGETHER)
              DefineStartLocation(0, -7744.0, -9152.0)
              for i = 0, 2 do
                SetPlayerStartLocation(Player(i), i)
                ForcePlayerStartLocation(Player(i), i)
                SetPlayerColor(Player(i), ConvertPlayerColor(i))
                SetPlayerRacePreference(Player(i), RACE_PREF_HUMAN)
                SetPlayerRaceSkin(Player(i), RACE_PREF_USER_SELECTABLE)
                SetPlayerRaceSelectable(Player(i), false)
                SetPlayerController(Player(i), MAP_CONTROL_USER)
              end
              SetPlayerTeam(Player(0), 0)
              SetPlayerTeam(Player(1), 0)
              SetPlayerAllianceStateAllyBJ(Player(0), Player(1), true)
              SetPlayerAllianceStateAllyBJ(Player(1), Player(0), true)
              SetPlayerAllianceStateVisionBJ(Player(0), Player(1), true)
              SetPlayerTeam(Player(2), 1)
              SetStartLocPrioCount(0, 1)
              SetStartLocPrio(0, 0, 1, MAP_LOC_PRIO_HIGH)
            end
            function main()
              SetPlayerTechResearched(Player(0), FourCC("Rhrt"), 1)
              ConfigureNeutralVictim()
              InitRescuableBehaviorBJ()
              InitSummonableCaps()
              local victim, hostile = Player(bj_PLAYER_NEUTRAL_VICTIM), Player(PLAYER_NEUTRAL_AGGRESSIVE)
              print(IsPlayerAlly(Player(0), Player(1)), GetPlayerAlliance(Player(0), Player(1), ALLIANCE_SHARED_VISION), GetPlayerAlliance(Player(1), Player(0), ALLIANCE_SHARED_VISION), IsPlayerEnemy(Player(0), Player(2)))
              print(IsPlayerAlly(victim, Player(0)), IsPlayerAlly(Player(0), victim), IsPlayerAlly(hostile, victim), IsPlayerAlly(victim, hostile), GetPlayerState(victim, PLAYER_STATE_GIVES_BOUNTY))
              print(GetPlayerTechMaxAllowed(Player(0), FourCC("hrtt")), GetPlayerTechMaxAllowed(Player(1), FourCC("hrtt")), GetPlayerTechMaxAllowed(Player(1), FourCC("uske")))
              local users = GetPlayersByMapControl(MAP_CONTROL_USER)
              print(CountPlayersInForceBJ(users), IsPlayerInForce(Player(2), users), GetPlayerTeam(Player(2)), GetStartLocPrioSlot(0, 0), GetStartLocationX(0))
            end
            """));

        Assert.Null(simulation.Stopped);
        Assert.Equal([
            "0.000 print: true\ttrue\tfalse\ttrue",
            "0.000 print: true\tfalse\ttrue\ttrue\t0",
            "0.000 print: 2147483647\t0\t25",
            "0.000 print: 3\ttrue\t-1\t1\t-7744.0",
        ], simulation.Log);
    }
}
