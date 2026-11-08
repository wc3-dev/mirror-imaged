namespace MirrorImaged.Tests;

/// <summary>Triggers and waits through the installed game's blizzard.j: PolledWait and World Editor's map initialisation.</summary>
[Trait("Needs", "Game")]
public sealed class TriggerBlizzardTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string main) => Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n"));

    // facts, "Errors and callbacks": xpcall and pcall keep working across PolledWait, and an error after it reaches the
    // handler. PolledWait polls with TriggerSleepAction(0.1) until its timer has run out: PolledWait(1) took 1.1 s here
    // and in the game twice (probe triggers-2; 1.2 s in probe triggers), PolledWait(0.5) 0.5 s here and 0.6 s there
    [InstallFact]
    public void Pcall_and_xpcall_keep_working_across_PolledWait()
    {
        using var simulation = Load("""
            local t = CreateTrigger()
            TriggerAddAction(t, function()
              print(pcall(function() PolledWait(1) error("after") end))
              print(xpcall(function() PolledWait(0.5) error("again") end, function(e) return "handled " .. e end))
            end)
            TriggerExecute(t)
            """);

        simulation.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(["1.100 print: false\twar3map.lua:5: after", "1.600 print: false\thandled war3map.lua:6: again"], simulation.Log);
    }

    // facts, "Initialisation": World Editor's main runs the map-initialisation triggers through ConditionalTriggerExecute
    // before the game has started; a timer event made with blizzard.j's helper fires once it has
    [InstallFact]
    public void World_editors_initialisation_triggers_run_inside_main()
    {
        using var simulation = Load("""
            local init, started = CreateTrigger(), CreateTrigger()
            TriggerAddCondition(init, Condition(function() return true end))
            TriggerAddAction(init, function() print("init", GetTriggeringTrigger() == init) end)
            TriggerRegisterTimerEventSingle(started, 0.00)
            TriggerAddAction(started, function() print("started") end)
            ConditionalTriggerExecute(init)
            print("end of main")
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.000 print: init\ttrue", "0.000 print: end of main", "0.010 print: started"], simulation.Log);
    }
}
