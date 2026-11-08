-- Timers, second part (W1-10): what a timer reads at another's expiries, with the floats' bits, and the order of
-- timers due at the same moment in more arrangements. "reads <k>": at the k-th to (k+5)-th expiry of a 0.01 s
-- periodic timer started with timers of 2, 3600 and 100000 s, the 2 s timer's elapsed and remaining time and the
-- other two's elapsed time, four floats an expiry. "second <k>": at each expiry of a 1 s periodic timer started with
-- them, the same four. "due at <t>": the timers due at t in the order they ran.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local log = {}
local function mark(name) return function() log[#log + 1] = name end end
local function at(t, f) TimerStart(CreateTimer(), t, false, guard(f)) end

local a5, a4, a3, a2, a1 = CreateTimer(), CreateTimer(), CreateTimer(), CreateTimer(), CreateTimer()
local b5, b4, b3, b2, b1 = CreateTimer(), CreateTimer(), CreateTimer(), CreateTimer(), CreateTimer()
local c3, c2, c1 = CreateTimer(), CreateTimer(), CreateTimer()
local short, hour, long = CreateTimer(), CreateTimer(), CreateTimer()
local function read()
    return bits(TimerGetElapsed(short)) .. ' ' .. bits(TimerGetRemaining(short)) .. ' ' .. bits(TimerGetElapsed(hour))
        .. ' ' .. bits(TimerGetElapsed(long))
end

TimerStart(short, 2.0, false, nil)
TimerStart(hour, 3600.0, false, nil)
TimerStart(long, 100000.0, false, nil)
out('start ' .. read())
local reads, tick = {}, CreateTimer()
TimerStart(tick, 0.01, true, guard(function()
    reads[#reads + 1] = read()
    if #reads % 6 == 0 then out('reads ' .. (#reads - 5) .. ' ' .. table.concat(reads, ' ', #reads - 5, #reads)) end
    if #reads == 96 then PauseTimer(tick) end
end))
local seconds = 0
TimerStart(CreateTimer(), 1.0, true, guard(function()
    seconds = seconds + 1
    if seconds <= 7 then out('second ' .. seconds .. ' ' .. read()) end
end))

-- Due at 2.0, made in the opposite order: a1 started at 0 for 2 s; a2 at 0 every 1 s (its second expiry); a3 at 0
-- for 2 s; a4 at 1.0 for 1 s; a5 at 1.5 for 0.5 s.
TimerStart(a1, 2.0, false, mark('a1'))
local a2runs = 0
TimerStart(a2, 1.0, true, function()
    a2runs = a2runs + 1
    if a2runs == 2 then log[#log + 1] = 'a2' PauseTimer(a2) end
end)
TimerStart(a3, 2.0, false, mark('a3'))
at(1.0, function() TimerStart(a4, 1.0, false, mark('a4')) end)
at(1.5, function() TimerStart(a5, 0.5, false, mark('a5')) end)
at(2.5, function() out('due at 2.0: ' .. table.concat(log, ',')) log = {} end)

-- Due at 4.0: b1 started at 0 for 4 s; b2 at 0 for 4 s, paused and resumed at 1.0; b3 at 0 for 3 s, paused at 1.0
-- and resumed at 2.0; b4 at 0 for 4 s; b5 at 3.0 for 1 s.
TimerStart(b1, 4.0, false, mark('b1'))
TimerStart(b2, 4.0, false, mark('b2'))
TimerStart(b3, 3.0, false, mark('b3'))
TimerStart(b4, 4.0, false, mark('b4'))
at(1.0, function() PauseTimer(b2) ResumeTimer(b2) PauseTimer(b3) end)
at(2.0, function() ResumeTimer(b3) end)
at(3.0, function() TimerStart(b5, 1.0, false, mark('b5')) end)
at(4.5, function() out('due at 4.0: ' .. table.concat(log, ',')) log = {} end)

-- Due at 6.0: c1, c2 and c3 started at 0 for 6 s, then c1 started again at 3.0 for 3 s.
TimerStart(c1, 6.0, false, mark('c1'))
TimerStart(c2, 6.0, false, mark('c2'))
TimerStart(c3, 6.0, false, mark('c3'))
at(3.0, function() TimerStart(c1, 3.0, false, mark('c1')) end)
at(6.5, function() out('due at 6.0: ' .. table.concat(log, ',')) done() end)
