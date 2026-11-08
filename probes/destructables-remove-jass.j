// RemoveDestructable(GetTriggerDestructable()) in the destructable's own death action with no guard, in the JASS
// machine, as World Editor's "Destructible dies -> Remove (Dying destructible)" writes it: how many times the death
// event fires, for a tree killed and for one removed, and whether the code after the call runs. Each runs in a thread of
// its own (ExecuteFunc), writing a line after; a missing line means the thread stopped there.
globals
    integer drj_count = 0
endglobals

function DrjDies takes nothing returns nothing
    set drj_count = drj_count + 1
    call RemoveDestructable(GetTriggerDestructable())
endfunction

function DrjWatched takes nothing returns destructable
    local trigger t = CreateTrigger()
    local destructable d = CreateDestructable('LTlt', 0, 0, 270, 1, 0)
    call TriggerRegisterDeathEvent(t, d)
    call TriggerAddAction(t, function DrjDies)
    return d
endfunction

function DrjKill takes nothing returns nothing
    set drj_count = 0
    call KillDestructable(DrjWatched())
    call out("killed " + I2S(drj_count))
endfunction

function DrjRemove takes nothing returns nothing
    set drj_count = 0
    call RemoveDestructable(DrjWatched())
    call out("removed " + I2S(drj_count))
endfunction

function DrjReport takes nothing returns nothing
    call out("a second later " + I2S(drj_count))
    call done()
endfunction

function probe takes nothing returns nothing
    call ExecuteFunc("DrjKill")
    call out("after kill " + I2S(drj_count))
    call ExecuteFunc("DrjRemove")
    call out("after remove " + I2S(drj_count))
    call TimerStart(CreateTimer(), 1.0, false, function DrjReport)
endfunction
