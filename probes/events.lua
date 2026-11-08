-- Player and game events on the probe map with a computer (ProbeMap's withComputer): the ForceEnum filters and
-- GetFilterPlayer, alliance change events, player state events, which time-of-day sets fire each comparison,
-- RemovePlayer's events, the time of day passing an hour, and what TriggerRegisterGameEvent's events fire. EndGame's
-- end-level event and what runs after it go to events-end.pld, events-after.pld and events-timer.pld.
local function id(h) return h == nil and "nil" or tostring(GetHandleId(h)) end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return string.format("%.4f", TimerGetElapsed(clock)) end
local function write(file, text)
    PreloadGenClear()
    PreloadGenStart()
    Preload("\")\n" .. text .. "\n//")
    PreloadGenEnd(file)
end
local ops = { LT = LESS_THAN, LE = LESS_THAN_OR_EQUAL, EQ = EQUAL, GE = GREATER_THAN_OR_EQUAL, GT = GREATER_THAN, NE = NOT_EQUAL }
local names = { "LT", "LE", "EQ", "GE", "GT", "NE" }
local log = {}
-- At most 8 entries, then how many more, to keep a line short.
local function flush()
    local s = table.concat(log, ",", 1, math.min(#log, 8)) .. (#log > 8 and (",+" .. (#log - 8)) or "")
    log = {}
    return s
end

-- TriggerRegisterGameEvent for every gameevent, counted until the end.
local gameEvents, fired = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 256, 257, 258, 259, 310 }, {}
for _, n in ipairs(gameEvents) do
    local t = CreateTrigger()
    TriggerRegisterGameEvent(t, ConvertGameEvent(n))
    TriggerAddAction(t, function()
        fired[n] = (fired[n] or 0) + 1
        if n == 1 then write("MirrorImaged\\events-end.pld", "end level fired, event " .. id(GetTriggerEventId())) end
        if n == 0 then fired.winner = pid(GetWinningPlayer()) end
    end)
end

-- Filters: which players each enumeration's filter is called for, in order, and what the force holds after.
local function members(f)
    local list = {}
    for i = 0, 27 do
        if IsPlayerInForce(Player(i), f) then list[#list + 1] = i end
    end
    return table.concat(list, " ")
end
local f = CreateForce()
local function filter(result)
    return Filter(function()
        log[#log + 1] = pid(GetFilterPlayer()) .. (GetEnumPlayer() and "e" or "") .. (GetTriggerPlayer() and "t" or "")
        if type(result) == "function" then return result() end
        return result
    end)
end
local function enum(label, call)
    ForceClear(f)
    ForceAddPlayer(f, Player(5))
    call()
    return label .. " " .. flush() .. ">" .. members(f)
end
out("filter " .. enum("true", function() ForceEnumPlayers(f, filter(true)) end)
    .. "; " .. enum("false", function() ForceEnumPlayers(f, filter(false)) end)
    .. "; " .. enum("is 1", function() ForceEnumPlayers(f, filter(function() return GetFilterPlayer() == Player(1) end)) end)
    .. "; after " .. pid(GetFilterPlayer()))
out("filter " .. enum("counted 1", function() ForceEnumPlayersCounted(f, filter(true), 1) end)
    .. "; " .. enum("counted 1 not 0", function() ForceEnumPlayersCounted(f, filter(function() return GetFilterPlayer() ~= Player(0) end), 1) end)
    .. "; " .. enum("allies of 0", function() ForceEnumAllies(f, Player(0), filter(true)) end)
    .. "; " .. enum("enemies of 0", function() ForceEnumEnemies(f, Player(0), filter(true)) end))
local results = {}
for _, r in ipairs({ { "1", 1 }, { "0", 0 }, { "x", "x" }, { "nil", nil }, { "table", {} } }) do
    results[#results + 1] = enum(r[1], function() ForceEnumPlayers(f, filter(r[2])) end)
end
out("filter results " .. table.concat(results, "; "))
local destroyed = Filter(function() log[#log + 1] = "d" return true end)
DestroyFilter(destroyed)
out("filter " .. enum("condition", function() ForceEnumPlayers(f, Condition(function() log[#log + 1] = "c" return true end)) end)
    .. "; " .. enum("and", function() ForceEnumPlayers(f, And(filter(true), filter(false))) end)
    .. "; " .. enum("destroyed", function() ForceEnumPlayers(f, destroyed) end)
    .. "; " .. enum("Filter(nil)", function() ForceEnumPlayers(f, Filter(nil)) end)
    .. "; " .. enum("raises", function() ForceEnumPlayers(f, Filter(function() log[#log + 1] = "r" error("raised") end)) end))

-- Alliance changes: which registrations fire, with the event id and trigger player.
local function on(name, register)
    local t = CreateTrigger()
    register(t)
    TriggerAddAction(t, function()
        log[#log + 1] = name .. ":" .. id(GetTriggerEventId()) .. ":" .. pid(GetTriggerPlayer()) .. ":"
            .. tostring(GetPlayerAlliance(Player(0), Player(1), ALLIANCE_PASSIVE))
    end)
    return t
end
on("p0", function(t) TriggerRegisterPlayerAllianceChange(t, Player(0), ALLIANCE_PASSIVE) end)
on("p1", function(t) TriggerRegisterPlayerAllianceChange(t, Player(1), ALLIANCE_PASSIVE) end)
on("v0", function(t) TriggerRegisterPlayerAllianceChange(t, Player(0), ALLIANCE_SHARED_VISION) end)
local changed0 = on("c0", function(t) TriggerRegisterPlayerEvent(t, Player(0), EVENT_PLAYER_ALLIANCE_CHANGED) end)
local changed1 = on("c1", function(t) TriggerRegisterPlayerEvent(t, Player(1), EVENT_PLAYER_ALLIANCE_CHANGED) end)
local function set(label, a, b, type, value)
    SetPlayerAlliance(Player(a), Player(b), type, value)
    return label .. " " .. flush()
end
out("alliance " .. set("0>1 passive on", 0, 1, ALLIANCE_PASSIVE, true) .. "; " .. set("again", 0, 1, ALLIANCE_PASSIVE, true))
out("alliance " .. set("0>1 passive off", 0, 1, ALLIANCE_PASSIVE, false) .. "; " .. set("again", 0, 1, ALLIANCE_PASSIVE, false))
out("alliance " .. set("1>0 passive on", 1, 0, ALLIANCE_PASSIVE, true) .. "; " .. set("off", 1, 0, ALLIANCE_PASSIVE, false))
out("alliance " .. set("0>1 vision on", 0, 1, ALLIANCE_SHARED_VISION, true) .. "; " .. set("off", 0, 1, ALLIANCE_SHARED_VISION, false))
out("alliance " .. set("0>0 passive off", 0, 0, ALLIANCE_PASSIVE, false) .. " reads " .. tostring(GetPlayerAlliance(Player(0), Player(0), ALLIANCE_PASSIVE))
    .. "; " .. set("on", 0, 0, ALLIANCE_PASSIVE, true))
local types = {}
for n = 0, 9 do
    SetPlayerAlliance(Player(0), Player(1), ConvertAllianceType(n), true)
    local a = flush():gsub("[pv]0:[^,]*,?", ""):gsub(":[^,]*", "")
    SetPlayerAlliance(Player(0), Player(1), ConvertAllianceType(n), false)
    local b = flush():gsub("[pv]0:[^,]*,?", ""):gsub(":[^,]*", "")
    types[#types + 1] = n .. " " .. a .. "/" .. b
end
out("alliance changed by type " .. table.concat(types, "; "))
DisableTrigger(changed0)
DisableTrigger(changed1)

-- Player states: which gold sets fire each comparison, as the integer game states' probe (triggers-3).
local limit = CreateTrigger()
local limitFired = 0
TriggerRegisterPlayerEvent(limit, Player(0), EVENT_PLAYER_STATE_LIMIT)
TriggerAddAction(limit, function() limitFired = limitFired + 1 end)
local other = CreateTrigger()
TriggerRegisterPlayerStateEvent(other, Player(1), PLAYER_STATE_RESOURCE_GOLD, GREATER_THAN_OR_EQUAL, 0)
TriggerAddAction(other, function() log[#log + 1] = "other" end)
local gold = PLAYER_STATE_RESOURCE_GOLD
for _, name in ipairs(names) do
    local t = CreateTrigger()
    TriggerRegisterPlayerStateEvent(t, Player(0), gold, ops[name], 2)
    TriggerAddAction(t, function()
        log[#log + 1] = GetPlayerState(Player(0), gold) .. ":" .. id(GetTriggerEventId())
    end)
    for _, value in ipairs({ 5, 0, 1, 2, 2, 3, 2, 1, 0, 4, -1, 0 }) do SetPlayerState(Player(0), gold, value) end
    DisableTrigger(t)
    out("gold " .. name .. " " .. flush())
end
local inside = CreateTrigger()
TriggerRegisterPlayerStateEvent(inside, Player(0), PLAYER_STATE_RESOURCE_LUMBER, GREATER_THAN, 10.5)
TriggerAddAction(inside, function()
    local s = GetEventPlayerState()
    log[#log + 1] = table.concat({ GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER), pid(GetTriggerPlayer()),
        tostring(s == PLAYER_STATE_RESOURCE_LUMBER), (tostring(s):match("^%a+")), tostring(GetTriggerEventId() == EVENT_PLAYER_STATE_LIMIT) }, " ")
end)
SetPlayerState(Player(0), gold, 50)
for _, value in ipairs({ 10, 11, 12, 10 }) do SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER, value) end
out("lumber over 10.5 " .. flush() .. "; state limit event fired " .. limitFired .. "; outside " .. id(GetEventPlayerState()))

-- The time of day, suspended: which sets fire each comparison with 12, and what the time reads inside.
local tod = GAME_STATE_TIME_OF_DAY
SuspendTimeOfDay(true)
local at = 0
local sets = { 20, 10, 11, 12, 12, 13, 12, 11, 10, 14, 11.5, 12.5, 11.5 }
for _, name in ipairs(names) do
    SetFloatGameState(tod, 0)
    local t = CreateTrigger()
    TriggerRegisterGameStateEvent(t, tod, ops[name], 12)
    TriggerAddAction(t, function() log[#log + 1] = at .. ":" .. string.format("%.9g", GetFloatGameState(tod)) end)
    for i, value in ipairs(sets) do
        at = i
        SetFloatGameState(tod, value)
    end
    DisableTrigger(t)
    out("time " .. name .. " " .. flush())
end
local t = CreateTrigger()
TriggerRegisterGameStateEvent(t, tod, EQUAL, 6)
TriggerAddAction(t, function()
    log[#log + 1] = table.concat({ id(GetTriggerEventId()), tostring(GetEventGameState() == GAME_STATE_TIME_OF_DAY), pid(GetTriggerPlayer()) }, " ")
end)
SetFloatGameState(tod, 6)
DisableTrigger(t)
out("time 6 inside: " .. flush())

-- RemovePlayer: the events of players 0 and 1, with the trigger player, its slot state and game result inside.
local slots = { [0] = "empty", "playing", "left" }
local function slot(p) return p == nil and "nil" or slots[GetHandleId(GetPlayerSlotState(p))] end
for _, p in ipairs({ 0, 1 }) do
    for _, e in ipairs({ "VICTORY", "DEFEAT", "LEAVE", "END_CINEMATIC" }) do
        local t = CreateTrigger()
        TriggerRegisterPlayerEvent(t, Player(p), _G["EVENT_PLAYER_" .. e])
        TriggerAddAction(t, function()
            log[#log + 1] = p .. e:sub(1, 3) .. ":" .. id(GetTriggerEventId()) .. ":" .. pid(GetTriggerPlayer()) .. ":"
                .. slot(Player(p)) .. ":" .. GetPlayerState(Player(p), PLAYER_STATE_GAME_RESULT)
        end)
    end
end
local function remove(p, result, label)
    RemovePlayer(Player(p), result)
    return label .. " " .. flush() .. " then " .. slot(Player(p)) .. " " .. GetPlayerState(Player(p), PLAYER_STATE_GAME_RESULT)
end
out("remove 1 is " .. slot(Player(1)) .. "; " .. remove(1, PLAYER_GAME_RESULT_VICTORY, "victory") .. "; " .. remove(1, PLAYER_GAME_RESULT_VICTORY, "again"))
out("remove " .. remove(1, PLAYER_GAME_RESULT_TIE, "1 tie") .. "; " .. remove(1, PLAYER_GAME_RESULT_DEFEAT, "defeat")
    .. "; " .. remove(1, PLAYER_GAME_RESULT_NEUTRAL, "neutral") .. "; game victory " .. tostring(fired[0]) .. " " .. tostring(fired.winner))

-- The time of day passing 12 as it runs: forward from 11.9, backward from 12.1, then set to exactly 12 and to 13 with
-- the day stopped, each watched for 2 s.
for _, name in ipairs(names) do
    local t = CreateTrigger()
    TriggerRegisterGameStateEvent(t, tod, ops[name], 12)
    TriggerAddAction(t, function()
        local r = GetFloatGameState(tod)
        log[#log + 1] = string.format("%s@%s:%.9g", name, now(), r)
    end)
end
local function watch(label, start, after)
    local before = now()
    start()
    local inside = flush()
    TimerStart(CreateTimer(), 2.5, false, guard(function()
        out(label .. " at " .. before .. ": inside " .. inside .. "; then " .. flush())
        after()
    end))
end
local function finish()
    local counts = {}
    for _, n in ipairs(gameEvents) do
        if fired[n] then counts[#counts + 1] = n .. ":" .. fired[n] end
    end
    log = {}
    RemovePlayer(Player(0), PLAYER_GAME_RESULT_VICTORY)
    local victory = flush()
    TimerStart(CreateTimer(), 1, false, guard(function()
        out("0 victory: inside " .. victory .. "; 1 s later " .. flush() .. "; slot " .. slot(Player(0)) .. " "
            .. GetPlayerState(Player(0), PLAYER_STATE_GAME_RESULT) .. "; game victory " .. tostring(fired[0]) .. " " .. tostring(fired.winner))
        out("game events fired " .. table.concat(counts, " "))
        TimerStart(CreateTimer(), 0, false, function() write("MirrorImaged\\events-timer.pld", "a 0 s timer started before EndGame ran") end)
        done()
        write("MirrorImaged\\events-after.pld", "the code after EndGame ran")
    end))
end
watch("forward from 11.9", function()
    SetFloatGameState(tod, 11.9)
    SuspendTimeOfDay(false)
end, function()
    watch("backward from 12.1", function()
        SetTimeOfDayScale(-1)
        SetFloatGameState(tod, 12.1)
    end, function()
        watch("stopped at 12", function()
            SuspendTimeOfDay(true)
            SetTimeOfDayScale(1)
            SetFloatGameState(tod, 12)
        end, function()
            watch("stopped, 12 to 13", function()
                SetFloatGameState(tod, 13)
            end, function()
                watch("running, set 12", function()
                    SuspendTimeOfDay(false)
                    SetFloatGameState(tod, 12)
                end, finish)
            end)
        end)
    end)
end)
