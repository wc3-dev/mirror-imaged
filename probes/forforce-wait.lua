-- The wait that never ends after a force was looped with ForForce and destroyed (game-facts.md, "Errors and callbacks",
-- 10-12): each step on its own, then together, each followed by a trigger whose action waits 0.5 s, 1.5 s apart, with
-- the handle ids around them; then how far the loss reaches: a wait 1 s after the destroy, and a wait already running
-- when it comes, with a new one beside it.
local function id(h) return h == nil and "nil" or tostring(GetHandleId(h)) end
local results, ids, extra = {}, {}, nil
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
        extra()
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
local function waiter(label, timeout)
    local t = CreateTrigger()
    results[label] = "waiting"
    TriggerAddAction(t, function() TriggerSleepAction(timeout) results[label] = "ended" end)
    TriggerExecute(t)
end
local function lose()
    local f = CreateForce()
    ForceAddPlayer(f, Player(0))
    ForForce(f, function() end)
    DestroyForce(f)
end
local function after(seconds, f) TimerStart(CreateTimer(), seconds, false, guard(f)) end
function extra()
    lose()
    after(1, function() waiter("later", 0.5) end)
    after(3, function() waiter("next", 0.5) end)
    after(5, function() waiter("running", 1.5) end)
    after(5.5, function() lose() waiter("beside", 0.5) end)
    after(8, function() waiter("next 2", 0.5) end)
    after(10, function()
        out("9 ForForce one, destroy, a wait 1 s later: " .. results.later .. "; 10 next: " .. results.next)
        out("11 a wait running across ForForce one, destroy: " .. results.running .. "; a new one beside it: " .. results.beside
            .. "; 12 next: " .. results["next 2"])
        done()
    end)
end
step(1)
