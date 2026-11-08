-- Game time against real time: each game second for 20, a line with the count, a timer's elapsed game time and the real
-- seconds since the start (os.time), so a run shows whether the game ran at full speed.
local clock = CreateTimer()
TimerStart(clock, 100000.0, false, function() end)
local start = os.time()
local count = 0
TimerStart(CreateTimer(), 1.0, true, guard(function()
    count = count + 1
    out(string.format("%d %.2f %d", count, TimerGetElapsed(clock), os.time() - start))
    if count == 20 then
        done()
    end
end))
