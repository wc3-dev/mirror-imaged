-- The map's own data as the game reads it, through natives MirrorImaged has (W2-1). The map (DataProbeMaps.Data) names
-- slot 0 "TRIGSTR_001" and adds a computer slot 2 named "TRIGSTR_002" in war3map.w3i, which config seats;
-- war3map.wts has strings 0 to 5; war3map.w3q gives Rhme 5 levels and makes R000 from Rhme. data-objects reads the
-- rest of the map's object data.
local function show(value)
    return value == nil and "nil" or tostring(value)
end

for i = 0, 3 do
    local p = Player(i)
    out("slot " .. i .. " " .. show(GetPlayerName(p)) .. " controller " .. GetHandleId(GetPlayerController(p))
        .. " state " .. GetHandleId(GetPlayerSlotState(p)))
end

local p = Player(0)
out("max Rhme " .. GetPlayerTechMaxAllowed(p, FourCC("Rhme")) .. " R000 " .. GetPlayerTechMaxAllowed(p, FourCC("R000"))
    .. " Rhar " .. GetPlayerTechMaxAllowed(p, FourCC("Rhar")))
AddPlayerTechResearched(p, FourCC("Rhme"), 9)
AddPlayerTechResearched(p, FourCC("R000"), 9)
AddPlayerTechResearched(p, FourCC("Rhar"), 9)
out("added Rhme " .. GetPlayerTechCount(p, FourCC("Rhme"), true) .. " R000 " .. GetPlayerTechCount(p, FourCC("R000"), true)
    .. " Rhar " .. GetPlayerTechCount(p, FourCC("Rhar"), true))

for _, key in ipairs({ "TRIGSTR_001", "TRIGSTR_1", "TRIGSTR_0001", "TRIGSTR_004", "TRIGSTR_999", "trigstr_001",
    "TRIGSTR_001 ", "TRIGSTR_", "TRIGSTR_abc", "TRIGSTR_000", "TRIGSTR_-1", "PLAYER", "player", "GAMEOVER_CONTINUE",
    "KEY_HOTKEYS", "AGILITY", "ERROR_ID_INVALIDPARAMS", "MONTH_01", "WESTRING_APPNAME", "NO_SUCH_KEY_ZZ", "" }) do
    out("string [" .. key .. "] [" .. show(GetLocalizedString(key)) .. "]")
end
for _, key in ipairs({ "GAMEOVER_CONTINUE", "GAMEOVER_QUIT_GAME", "GAMEOVER_OK", "KEY_DELETE", "MinimapTerrain",
    "gameover_continue", "PLAYER", "ACCEPT", "KEY_HOTKEYS", "NO_SUCH_KEY_ZZ", "TRIGSTR_001", "" }) do
    out("hotkey [" .. key .. "] " .. GetLocalizedHotkey(key))
end

-- A TRIGSTR_ given to other natives.
SetPlayerName(Player(5), "TRIGSTR_002")
out("named " .. show(GetPlayerName(Player(5))))
out("length " .. StringLength("TRIGSTR_001") .. " " .. StringLength("TRIGSTR_") .. " case " .. StringCase("TRIGSTR_001", false))
for _, key in ipairs({ "TRIGSTR_", "TRIGSTR_abc", "TRIGSTR_000", "TRIGSTR_-1" }) do
    out("case [" .. key .. "] [" .. StringCase(key, false) .. "]")
end
out("hash " .. StringHash("TRIGSTR_001") .. " " .. StringHash("Probe Alpha") .. " " .. StringHash("TRIGSTR_001x"))
out("number " .. S2I("TRIGSTR_005") .. " " .. R2S(S2R("TRIGSTR_005")))
out("sub [" .. SubString("TRIGSTR_001", 0, 5) .. "]")
local h = InitHashtable()
SaveStr(h, 0, 0, "TRIGSTR_001")
out("saved [" .. show(LoadStr(h, 0, 0)) .. "]")
done()
