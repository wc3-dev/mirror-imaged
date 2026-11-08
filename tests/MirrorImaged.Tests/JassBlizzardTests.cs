using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>
/// The installed game's blizzard.j as a Lua map runs it, in the game's own Lua, and as a JASS map runs it, in the game's
/// JASS machine (decisions D131).
/// </summary>
[Trait("Needs", "Game")]
public sealed class JassBlizzardTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // probe blizzard-numbers (ModuloInteger floors, as Lua's //) and jass-numbers (its body cuts toward zero, as JASS's /);
    // a real global given a whole number is a float in the game's Lua (probe blizzard-numbers)
    [InstallFact]
    public void ModuloInteger_floors_in_a_Lua_map_and_cuts_toward_zero_in_a_JASS_map()
    {
        using var lua = Simulation.Load(_maps.Build("""
            function config() end
            function main()
              print(ModuloInteger(7, -3), ModuloInteger(-7, 3), ModuloInteger(-7, -3), ModuloInteger(7, 3))
              print(math.type(bj_MELEE_MINE_SEARCH_RADIUS), bj_MELEE_MINE_SEARCH_RADIUS, math.type(bj_cineFadeContinueRed))
            end
            """));
        using var jass = Simulation.Load(_maps.Build("""
            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                call BJDebugMsg(I2S(ModuloInteger(7, -3)) + " " + I2S(ModuloInteger(-7, 3)) + " " + I2S(ModuloInteger(-7, -3)) + " " + I2S(ModuloInteger(7, 3)) + " " + R2S(bj_MELEE_MINE_SEARCH_RADIUS))
            endfunction
            """, ScriptLanguage.Jass));

        Assert.Equal(["0.000 print: -5\t2\t-4\t1", "0.000 print: float\t2000.0\tfloat"], lua.Log);
        Assert.Equal(Enumerable.Range(0, 24).Select(player => $"0.000 player {player}: 1 2 -4 1 2000.000"), jass.Log);
    }

    // World Editor's JASS: blizzard.j's helpers, a trigger and a timer, and a wait
    [InstallFact]
    public void A_jass_map_calls_blizzard_j_s_functions()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            globals
                trigger gg_trg_Later = null
            endglobals

            function Trig_Later_Actions takes nothing returns nothing
                call PolledWait(1.00)
                call DisplayTextToPlayer(Player(0), 0, 0, "later " + R2S(TimerGetElapsed(bj_lastStartedTimer)))
            endfunction

            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                set gg_trg_Later = CreateTrigger()
                call TriggerAddAction(gg_trg_Later, function Trig_Later_Actions)
                call CreateTimerBJ(false, 5)
                call ConditionalTriggerExecute(gg_trg_Later)
            endfunction
            """, ScriptLanguage.Jass));

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(["1.100 player 0: later 1.100"], simulation.Log);
    }
}
