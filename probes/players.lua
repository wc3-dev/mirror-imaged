-- Players, forces and the game as a one-player -loadfile game starts them: each slot's name, state, controller, race,
-- team, start location and colour; alliances; player states, handicaps and tech; forces; the game's settings; and the
-- time of day as it is set and read back.
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

local slots = { "PLAYER_SLOT_STATE_EMPTY", "PLAYER_SLOT_STATE_PLAYING", "PLAYER_SLOT_STATE_LEFT" }
local controls = { "MAP_CONTROL_USER", "MAP_CONTROL_COMPUTER", "MAP_CONTROL_RESCUABLE", "MAP_CONTROL_NEUTRAL", "MAP_CONTROL_CREEP", "MAP_CONTROL_NONE" }
local races = { "RACE_HUMAN", "RACE_ORC", "RACE_UNDEAD", "RACE_NIGHTELF", "RACE_DEMON", "RACE_OTHER" }

for i = 0, 27 do
    local p = Player(i)
    out(string.format("slot %d %s|%s %s %s team %d start %d colour %d observer %s selectable %s",
        i, GetPlayerName(p), named(GetPlayerSlotState(p), slots), named(GetPlayerController(p), controls),
        named(GetPlayerRace(p), races), GetPlayerTeam(p), GetPlayerStartLocation(p), GetHandleId(GetPlayerColor(p)),
        tostring(IsPlayerObserver(p)), tostring(GetPlayerSelectable(p))))
end

-- Alliances as a mask, bit n for ALLIANCE type n, then IsPlayerAlly and IsPlayerEnemy.
local function alliance(a, b)
    local mask = 0
    for n = 0, 9 do
        if GetPlayerAlliance(Player(a), Player(b), ConvertAllianceType(n)) then mask = mask | (1 << n) end
    end
    return string.format("%d>%d %d %s %s", a, b, mask, tostring(IsPlayerAlly(Player(a), Player(b))), tostring(IsPlayerEnemy(Player(a), Player(b))))
end
local pairs_ = { { 0, 0 }, { 0, 1 }, { 1, 0 }, { 1, 2 }, { 0, 24 }, { 24, 0 }, { 0, 25 }, { 25, 0 }, { 0, 26 }, { 0, 27 }, { 27, 0 }, { 24, 25 }, { 25, 24 }, { 24, 27 }, { 27, 24 }, { 24, 24 }, { 27, 27 } }
local line = {}
for _, pair in ipairs(pairs_) do line[#line + 1] = alliance(pair[1], pair[2]) end
out("alliances " .. table.concat(line, ", ", 1, 9))
out("alliances " .. table.concat(line, ", ", 10))

-- Player states 0 to 16 and 25 for players 0, 1, 24, 25 and 27.
for _, i in ipairs({ 0, 1, 24, 25, 27 }) do
    local values = {}
    for _, s in ipairs({ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 25 }) do
        values[#values + 1] = GetPlayerState(Player(i), ConvertPlayerState(s))
    end
    out("states " .. i .. " " .. table.concat(values, " "))
end
local p0 = Player(0)
SetPlayerState(p0, PLAYER_STATE_RESOURCE_GOLD, -5)
local gold = GetPlayerState(p0, PLAYER_STATE_RESOURCE_GOLD)
SetPlayerState(p0, PLAYER_STATE_RESOURCE_GOLD, 2000000)
local lots = GetPlayerState(p0, PLAYER_STATE_RESOURCE_GOLD)
SetPlayerState(p0, PLAYER_STATE_RESOURCE_FOOD_CAP, 150)
local cap = GetPlayerState(p0, PLAYER_STATE_RESOURCE_FOOD_CAP)
SetPlayerState(p0, PLAYER_STATE_RESOURCE_GOLD, 0)
out(string.format("gold -5 reads %d, 2000000 reads %d; food cap 150 reads %d; gathered %d",
    gold, lots, cap, GetPlayerState(p0, PLAYER_STATE_GOLD_GATHERED)))
out(string.format("handicaps %s %s %s %s", GetPlayerHandicap(p0), GetPlayerHandicapXP(p0), GetPlayerHandicapDamage(p0), GetPlayerHandicapReviveTime(p0)))

-- Tech: a unit type, an upgrade with 3 levels, an unknown id.
local hfoo, rhme, zzzz = FourCC("hfoo"), FourCC("Rhme"), FourCC("zzzz")
out(string.format("max allowed hfoo %d Rhme %d zzzz %d; count Rhme %d hfoo %d; researched Rhme %s",
    GetPlayerTechMaxAllowed(p0, hfoo), GetPlayerTechMaxAllowed(p0, rhme), GetPlayerTechMaxAllowed(p0, zzzz),
    GetPlayerTechCount(p0, rhme, true), GetPlayerTechCount(p0, hfoo, true), tostring(GetPlayerTechResearched(p0, rhme, true))))
local steps = {}
SetPlayerTechResearched(p0, rhme, 2) steps[#steps + 1] = GetPlayerTechCount(p0, rhme, true)
SetPlayerTechResearched(p0, rhme, 1) steps[#steps + 1] = GetPlayerTechCount(p0, rhme, true)
AddPlayerTechResearched(p0, rhme, 5) steps[#steps + 1] = GetPlayerTechCount(p0, rhme, true)
BlzDecPlayerTechResearched(p0, rhme, 1) steps[#steps + 1] = GetPlayerTechCount(p0, rhme, true)
SetPlayerTechResearched(p0, zzzz, 4) steps[#steps + 1] = GetPlayerTechCount(p0, zzzz, true)
SetPlayerTechMaxAllowed(p0, hfoo, -5) steps[#steps + 1] = GetPlayerTechMaxAllowed(p0, hfoo)
out("tech set 2, set 1, add 5, dec 1 Rhme; set 4 zzzz; max -5 hfoo: " .. table.concat(steps, " ") .. " " .. tostring(GetPlayerTechResearched(p0, rhme, true)))

-- Forces: what the enumerations put in a force, whether they clear it first, and ForForce's order.
local function members(f)
    local list = {}
    for i = 0, 27 do
        if IsPlayerInForce(Player(i), f) then list[#list + 1] = i end
    end
    return table.concat(list, " ")
end
local f = CreateForce()
ForceAddPlayer(f, Player(5))
ForceEnumPlayers(f, nil)
local all = members(f)
ForceEnumAllies(f, p0, nil)
local allies = members(f)
ForceEnumEnemies(f, p0, nil)
local enemies = members(f)
ForceEnumPlayersCounted(f, nil, 1)
local counted = members(f)
out("enum players: " .. all .. "; allies of 0: " .. allies .. "; enemies of 0: " .. enemies .. "; counted 1: " .. counted)
ForceClear(f)
ForceAddPlayer(f, Player(3))
ForceAddPlayer(f, Player(1))
ForceAddPlayer(f, Player(2))
local order = {}
ForForce(f, function() order[#order + 1] = GetPlayerId(GetEnumPlayer()) end)
out("ForForce over 3, 1, 2 added: " .. table.concat(order, " ") .. "; GetEnumPlayer after: " .. tostring(GetEnumPlayer()))
local fall = GetPlayersAll()
out("GetPlayersAll: " .. members(fall))

-- The game's settings.
out(string.format("speed %s difficulty %s placement %s type %s players %d teams %d version %s cheats %s %s",
    named(GetGameSpeed(), { "MAP_SPEED_SLOWEST", "MAP_SPEED_SLOW", "MAP_SPEED_NORMAL", "MAP_SPEED_FAST", "MAP_SPEED_FASTEST" }),
    named(GetGameDifficulty(), { "MAP_DIFFICULTY_EASY", "MAP_DIFFICULTY_NORMAL", "MAP_DIFFICULTY_HARD", "MAP_DIFFICULTY_INSANE" }),
    named(GetGamePlacement(), { "MAP_PLACEMENT_RANDOM", "MAP_PLACEMENT_FIXED", "MAP_PLACEMENT_USE_MAP_SETTINGS", "MAP_PLACEMENT_TEAMS_TOGETHER" }),
    named(GetGameTypeSelected(), { "GAME_TYPE_MELEE", "GAME_TYPE_FFA", "GAME_TYPE_USE_MAP_SETTINGS", "GAME_TYPE_BLIZ", "GAME_TYPE_ONE_ON_ONE", "GAME_TYPE_TWO_TEAM_PLAY", "GAME_TYPE_THREE_TEAM_PLAY", "GAME_TYPE_FOUR_TEAM_PLAY" }),
    GetPlayers(), GetTeams(), tostring(VersionGet() == VERSION_FROZEN_THRONE), tostring(IsNoVictoryCheat()), tostring(IsNoDefeatCheat())))
local flags = {}
for n = 0, 20 do
    if IsMapFlagSet(ConvertMapFlag(1 << n)) then flags[#flags + 1] = 1 << n end
end
out(string.format("map flags set: %s; game states %d %d; start 0 at %s %s, start 5 at %s %s; race pref human %s random %s",
    table.concat(flags, " "), GetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION), GetIntegerGameState(GAME_STATE_DISCONNECTED),
    GetStartLocationX(0), GetStartLocationY(0), GetStartLocationX(5), GetStartLocationY(5),
    tostring(IsPlayerRacePrefSet(p0, RACE_PREF_HUMAN)), tostring(IsPlayerRacePrefSet(p0, RACE_PREF_RANDOM))))
SetGameSpeed(MAP_SPEED_FASTEST)
local fastest = named(GetGameSpeed(), { "MAP_SPEED_SLOWEST", "MAP_SPEED_SLOW", "MAP_SPEED_NORMAL", "MAP_SPEED_FAST", "MAP_SPEED_FASTEST" })
SetGameSpeed(MAP_SPEED_SLOWEST)
local slowest = named(GetGameSpeed(), { "MAP_SPEED_SLOWEST", "MAP_SPEED_SLOW", "MAP_SPEED_NORMAL", "MAP_SPEED_FAST", "MAP_SPEED_FASTEST" })
SetGameSpeed(MAP_SPEED_NORMAL)
out("speed set fastest reads " .. fastest .. ", slowest reads " .. slowest)

-- The time of day: its start and scale, then values set and read back at once, with their float bits.
local tod = GAME_STATE_TIME_OF_DAY
out(string.format("time of day %s %s scale %s", GetFloatGameState(tod), bits(GetFloatGameState(tod)), GetTimeOfDayScale()))
local reads = {}
for _, x in ipairs({ 7, 12, 18, 6, 23.9, 23.95, 23.98, 23.99, 23.995, 23.999, 0.5, 0.1 }) do
    SetFloatGameState(tod, x)
    local r = GetFloatGameState(tod)
    reads[#reads + 1] = string.format("%s>%.9g %s", x, r, bits(r))
end
out("set " .. table.concat(reads, ", ", 1, 6))
out("set " .. table.concat(reads, ", ", 7))
SetFloatGameState(tod, 23.9)
TimerStart(CreateTimer(), 2.0, false, guard(function()
    local r = GetFloatGameState(tod)
    out(string.format("23.9 then 2 s: %.9g %s", r, bits(r)))
    SetFloatGameState(tod, 0)
    TimerStart(CreateTimer(), 2.0, false, guard(function()
        local a = GetFloatGameState(tod)
        TimerStart(CreateTimer(), 18.0, false, guard(function()
            local b = GetFloatGameState(tod)
            out(string.format("0 then 2 s: %.9g %s; then 20 s: %.9g %s", a, bits(a), b, bits(b)))
            done()
        end))
    end))
end))
