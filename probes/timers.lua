-- Timers (decisions D45, W1-8): the order of timers due at the same moment, which MirrorImaged runs in the order their
-- expiries were queued, and a 3600 s timer's TimerGetElapsed and TimerGetRemaining read every 0.01 s for a second.
-- The timers are made in another order than they are started, so the order they run in tells which counts.
local log = {}
local function mark(name) return function() log[#log + 1] = name end end
local late, restarted, periodic, resumed, second, first = CreateTimer(), CreateTimer(), CreateTimer(), CreateTimer(), CreateTimer(), CreateTimer()

-- Each is due at 1.0, queued at 0 (first, second, resumed's start), 0.25 (resumed), 0.5 (late, restarted) and 0.75
-- (periodic's fourth expiry).
TimerStart(first, 1.0, false, mark('first'))
local runs = 0
TimerStart(periodic, 0.25, true, function()
    runs = runs + 1
    if runs == 4 then log[#log + 1] = 'periodic' end
end)
TimerStart(second, 1.0, false, mark('second'))
TimerStart(resumed, 1.0, false, mark('resumed'))
TimerStart(restarted, 2.0, false, mark('restarted early'))
TimerStart(CreateTimer(), 0.25, false, function()
    PauseTimer(resumed)
    ResumeTimer(resumed)
end)
TimerStart(CreateTimer(), 0.5, false, function()
    TimerStart(late, 0.5, false, mark('late'))
    TimerStart(restarted, 0.5, false, mark('restarted'))
end)
TimerStart(CreateTimer(), 1.5, false, guard(function()
    PauseTimer(periodic)
    out('due at 1.0: ' .. table.concat(log, ','))

    local long, reads = CreateTimer(), {}
    TimerStart(long, 3600.0, false, nil)
    TimerStart(CreateTimer(), 0.01, true, guard(function()
        reads[#reads + 1] = string.format('%.5f', TimerGetElapsed(long))
        if #reads % 10 == 0 then
            out('3600 s timer elapsed ' .. table.concat(reads, ' ', #reads - 9, #reads))
        end
        if #reads == 100 then
            out('3600 s timer remaining ' .. string.format('%.5f', TimerGetRemaining(long)))
            done()
        end
    end))
end))
