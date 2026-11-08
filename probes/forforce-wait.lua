-- The wait that never ends after a force was looped with ForForce and destroyed (game-facts.md, "Errors and callbacks",
-- 10-12): each step on its own, then together, each followed by a trigger whose action waits 0.5 s, 1.5 s apart, with
-- the handle ids around them.
local function id(h) return h == nil and "nil" or tostring(GetHandleId(h)) end
local results, ids = {}, {}
local steps = {
    { "base", function() end },
    { "create, destroy", function() DestroyForce(CreateForce()) end },
    { "ForForce empty", function() ForForce(CreateForce(), function() end) end },
    { "ForForce one", function() local f = CreateForce() ForceAddPlayer(f, Player(0)) ForForce(f, function() end) end },
    { "ForForce empty, destroy", function() local f = CreateForce() ForForce(f, function() end) DestroyForce(f) end },
    { "ForForce one, destroy", function() local f = CreateForce() ids.force = id(f) ForceAddPlayer(f, Player(0)) ForForce(f, function() end) DestroyForce(f) end },
    { "next", function() end },
    { "polled", function() end },
}
local function step(n)
    if n > #steps then
        for i, s in ipairs(steps) do out(i .. " " .. s[1] .. ": " .. tostring(results[i])) end
        out("ids: force " .. tostring(ids.force) .. ", then trigger " .. tostring(ids.trigger) .. ", timer " .. tostring(ids.timer))
        done()
        return
    end
    steps[n][2]()
    local t = CreateTrigger()
    if n == 6 then ids.trigger = id(t) end
    results[n] = "waiting"
    TriggerAddAction(t, function()
        if n == 8 then PolledWait(0.5) else TriggerSleepAction(0.5) end
        results[n] = "ended"
    end)
    TriggerExecute(t)
    local timer = CreateTimer()
    if n == 6 then ids.timer = id(timer) end
    TimerStart(timer, 1.5, false, guard(function() step(n + 1) end))
end
step(1)
