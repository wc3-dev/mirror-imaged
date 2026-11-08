-- The lobby on the probe map with a computer (ProbeMap's withComputer): what config read before it set anything, each
-- slot as the game starts it, the computer's and the empty named slot's alliances, states and handicaps, and the time
-- of day read back at once and later.
local function named(h, names)
    for _, name in ipairs(names) do
        if h == _G[name] then return name end
    end
    return h == nil and "nil" or ("other " .. GetHandleId(h))
end

local function bits(x)
    local b = string.pack("<f", x)
    return string.format("%02x%02x%02x%02x", b:byte(4), b:byte(3), b:byte(2), b:byte(1))
end

out("config ran " .. (ProbeConfig and #ProbeConfig or 0) .. " times")
for n, seen in ipairs(ProbeConfig or {}) do
    for part in seen:gmatch("[^;]+") do out("config " .. n .. ":" .. part) end
end

local slots = { "PLAYER_SLOT_STATE_EMPTY", "PLAYER_SLOT_STATE_PLAYING", "PLAYER_SLOT_STATE_LEFT" }
local controls = { "MAP_CONTROL_USER", "MAP_CONTROL_COMPUTER", "MAP_CONTROL_RESCUABLE", "MAP_CONTROL_NEUTRAL", "MAP_CONTROL_CREEP", "MAP_CONTROL_NONE" }
local races = { "RACE_HUMAN", "RACE_ORC", "RACE_UNDEAD", "RACE_NIGHTELF", "RACE_DEMON", "RACE_OTHER" }

for _, i in ipairs({ 0, 1, 2, 24, 27 }) do
    local p = Player(i)
    out(string.format("slot %d %s|%s %s %s team %d start %d colour %d selectable %s random %s human %s",
        i, GetPlayerName(p), named(GetPlayerSlotState(p), slots), named(GetPlayerController(p), controls),
        named(GetPlayerRace(p), races), GetPlayerTeam(p), GetPlayerStartLocation(p), GetHandleId(GetPlayerColor(p)),
        tostring(GetPlayerSelectable(p)), tostring(IsPlayerRacePrefSet(p, RACE_PREF_RANDOM)), tostring(IsPlayerRacePrefSet(p, RACE_PREF_HUMAN))))
end

-- Alliances as a mask, bit n for ALLIANCE type n.
local function alliance(a, b)
    local mask = 0
    for n = 0, 9 do
        if GetPlayerAlliance(Player(a), Player(b), ConvertAllianceType(n)) then mask = mask | (1 << n) end
    end
    return string.format("%d>%d %d", a, b, mask)
end
local line = {}
for _, pair in ipairs({ { 0, 1 }, { 1, 0 }, { 0, 2 }, { 2, 0 }, { 1, 2 }, { 2, 1 }, { 1, 1 }, { 2, 2 }, { 1, 24 }, { 24, 1 }, { 1, 25 }, { 25, 1 }, { 1, 26 }, { 1, 27 }, { 27, 1 }, { 2, 27 }, { 27, 2 }, { 1, 3 } }) do
    line[#line + 1] = alliance(pair[1], pair[2])
end
out("alliances " .. table.concat(line, ", "))

for _, i in ipairs({ 0, 1, 2 }) do
    local values = {}
    for _, s in ipairs({ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 25 }) do
        values[#values + 1] = GetPlayerState(Player(i), ConvertPlayerState(s))
    end
    local p = Player(i)
    out(string.format("states %d %s; handicaps %s %s %s %s", i, table.concat(values, " "),
        GetPlayerHandicap(p), GetPlayerHandicapXP(p), GetPlayerHandicapDamage(p), GetPlayerHandicapReviveTime(p)))
end

local function members(f)
    local list = {}
    for i = 0, 27 do
        if IsPlayerInForce(Player(i), f) then list[#list + 1] = i end
    end
    return table.concat(list, " ")
end
local f = CreateForce()
ForceEnumPlayers(f, nil)
local all = members(f)
ForceEnumAllies(f, Player(1), nil)
local allies = members(f)
ForceEnumEnemies(f, Player(1), nil)
out("enum players: " .. all .. "; allies of 1: " .. allies .. "; enemies of 1: " .. members(f) .. "; all: " .. members(GetPlayersAll()))

local flags = {}
for n = 0, 20 do
    if IsMapFlagSet(ConvertMapFlag(1 << n)) then flags[#flags + 1] = 1 << n end
end
out(string.format("type %s placement %s players %d teams %d map flags %s",
    named(GetGameTypeSelected(), { "GAME_TYPE_MELEE", "GAME_TYPE_FFA", "GAME_TYPE_USE_MAP_SETTINGS", "GAME_TYPE_BLIZ", "GAME_TYPE_ONE_ON_ONE", "GAME_TYPE_TWO_TEAM_PLAY", "GAME_TYPE_THREE_TEAM_PLAY", "GAME_TYPE_FOUR_TEAM_PLAY" }),
    named(GetGamePlacement(), { "MAP_PLACEMENT_RANDOM", "MAP_PLACEMENT_FIXED", "MAP_PLACEMENT_USE_MAP_SETTINGS", "MAP_PLACEMENT_TEAMS_TOGETHER" }),
    GetPlayers(), GetTeams(), table.concat(flags, " ")))

-- The time of day read back at once and later: suspended (each set read at once, then after 0 s, 0.1 s and 1 s), then
-- running at scale 1 (at once and in a 0 s timer).
local tod = GAME_STATE_TIME_OF_DAY
local values = { 7, 12, 18, 6, 0.1, 23.9, 7.5 }
local function read() local r = GetFloatGameState(tod) return string.format("%.9g %s", r, bits(r)) end
SuspendTimeOfDay(true)
local function suspended(n)
    if n > #values then
        SuspendTimeOfDay(false)
        SetFloatGameState(tod, 7)
        local now = read()
        TimerStart(CreateTimer(), 0, false, guard(function()
            out("running 7: at once " .. now .. "; 0 s later " .. read())
            done()
        end))
        return
    end
    SetFloatGameState(tod, values[n])
    local reads = { read() }
    TimerStart(CreateTimer(), 0, false, guard(function()
        reads[#reads + 1] = read()
        TimerStart(CreateTimer(), 0.1, false, guard(function()
            reads[#reads + 1] = read()
            TimerStart(CreateTimer(), 0.9, false, guard(function()
                reads[#reads + 1] = read()
                out("suspended " .. values[n] .. ": " .. table.concat(reads, ", "))
                suspended(n + 1)
            end))
        end))
    end))
end
suspended(1)
