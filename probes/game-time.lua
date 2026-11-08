-- Game time against real time: each game second for 20, a line with the count, a timer's elapsed game time and the real
-- seconds since the start (os.time), so a window in the background shows whether the game slows down or stops. The lines
-- so far are also written to game-time-progress.pld each second, for a run that never gets to the end.
local clock = CreateTimer()
TimerStart(clock, 100000.0, false, function() end)
local start = os.time()
local count = 0
local progress = {}
TimerStart(CreateTimer(), 1.0, true, function()
    count = count + 1
    local line = string.format("%d %.2f %d", count, TimerGetElapsed(clock), os.time() - start)
    out(line)
    progress[#progress + 1] = line
    PreloadGenClear()
    PreloadGenStart()
    for _, each in ipairs(progress) do
        Preload("\")\n" .. each .. "\n//")
    end
    PreloadGenEnd("MirrorImaged\\game-time-progress.pld")
    if count == 20 then
        done()
    end
end)
