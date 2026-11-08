-- What Preload, Preloader and the game cache natives do, for MirrorImaged's model of them (W1-5). It writes under
-- CustomMapData\MirrorImaged\files-probe, where the files it wrote are to be looked at, and reads files back through
-- an ability's tooltip: each piece is a Preload text that sets it when Preloader runs the file. Its lines as the game
-- gave them are in expected/files.txt.
local dir = "MirrorImaged\\files-probe\\"
local carrier = FourCC("AHbz")

local function write(path, ...)
    PreloadGenClear()
    PreloadGenStart()
    for _, text in ipairs({...}) do Preload(text) end
    PreloadGenEnd(path)
end

local function piece(text)
    return "\")\ncall BlzSetAbilityTooltip('AHbz', \"" .. text .. "\", 0)\n//"
end

local function read(path)
    BlzSetAbilityTooltip(carrier, "-", 0)
    Preloader(path)
    return BlzGetAbilityTooltip(carrier, 0)
end

local function yes(b) return b and "y" or "n" end

-- A path read once keeps its first content (facts); is the map's own write an exception?
write(dir .. "own.pld", piece("first"))
out("own write, read: " .. read(dir .. "own.pld"))
write(dir .. "own.pld", piece("second"))
out("own write again, read: " .. read(dir .. "own.pld"))
out("the path in capitals, read: " .. read(string.upper(dir) .. "OWN.PLD"))

-- What a file holds: Preload before PreloadGenStart, without PreloadGenClear, after PreloadGenEnd.
PreloadGenClear()
Preload("before start")
PreloadGenStart()
Preload("after start")
PreloadGenEnd(dir .. "order.pld")
PreloadGenStart()
Preload("second start")
PreloadGenEnd(dir .. "noclear.pld")
PreloadGenEnd(dir .. "again.pld")
Preload("after end")
PreloadGenEnd(dir .. "afterend.pld")
PreloadGenClear()
PreloadGenEnd(dir .. "cleared.pld")
out("buffer files written")

-- Extensions and paths, read back.
for i, name in ipairs({"ext.txt", "ext.pld", "ext.dat", "ext.json", "ext", "ext.ini", "ext.log", "ext.w3v", "sub\\..\\dots.pld"}) do
    write(dir .. name, piece("p" .. i))
    out(name .. ": " .. read(dir .. name))
end
write("MirrorImaged/files-probe/slash.pld", piece("slash"))
out("forward slashes: " .. read("MirrorImaged/files-probe/slash.pld"))
for _, n in ipairs({150, 200, 201, 202, 230}) do
    local path = dir .. string.rep("a", n - #dir - 4) .. ".pld"
    write(path, piece("len" .. n))
    out("relative path of " .. #path .. ": " .. read(path))
end

-- Game caches.
local c1 = InitGameCache("files-probe.w3v")
StoreInteger(c1, "m", "k", 5)
local c2 = InitGameCache("files-probe.w3v")
out("same name: " .. GetStoredInteger(c2, "m", "k") .. ", same handle " .. yes(c1 == c2))
out("other name: " .. GetStoredInteger(InitGameCache("files-probe-other.w3v"), "m", "k"))
local s = GetStoredString(c1, "m", "none")
out("missing: " .. GetStoredInteger(c1, "m", "none") .. " " .. GetStoredReal(c1, "m", "none") .. " "
    .. yes(GetStoredBoolean(c1, "m", "none")) .. " " .. type(s) .. " '" .. tostring(s) .. "'")
StoreReal(c1, "m", "k", 1.5)
out("integer and real on one key: " .. GetStoredInteger(c1, "m", "k") .. " " .. GetStoredReal(c1, "m", "k") .. ", have "
    .. yes(HaveStoredInteger(c1, "m", "k")) .. yes(HaveStoredReal(c1, "m", "k")) .. yes(HaveStoredBoolean(c1, "m", "k"))
    .. yes(HaveStoredString(c1, "m", "k")))
FlushStoredInteger(c1, "m", "k")
out("integer flushed, have " .. yes(HaveStoredInteger(c1, "m", "k")) .. yes(HaveStoredReal(c1, "m", "k")))
out("StoreString gives " .. yes(StoreString(c1, "m", "s", "text")) .. ", of an empty string " .. yes(StoreString(c1, "m", "e", ""))
    .. ", have it " .. yes(HaveStoredString(c1, "m", "e")) .. " '" .. tostring(GetStoredString(c1, "m", "e")) .. "'")
StoreBoolean(c1, "m", "b", false)
StoreInteger(c1, "m", "zero", 0)
out("false and 0 stored, have " .. yes(HaveStoredBoolean(c1, "m", "b")) .. yes(HaveStoredInteger(c1, "m", "zero")))
StoreInteger(c1, "Case", "Key", 3)
out("keys in other cases: " .. GetStoredInteger(c1, "case", "key") .. " " .. GetStoredInteger(c1, "CASE", "KEY"))
FlushStoredMission(c1, "m")
out("mission flushed, have " .. yes(HaveStoredReal(c1, "m", "k")) .. yes(HaveStoredString(c1, "m", "s"))
    .. yes(HaveStoredBoolean(c1, "m", "b")) .. ", another mission " .. GetStoredInteger(c1, "Case", "Key"))
FlushGameCache(c1)
out("cache flushed: " .. GetStoredInteger(c1, "Case", "Key") .. ", by name " .. GetStoredInteger(InitGameCache("files-probe.w3v"), "Case", "Key"))
StoreInteger(c1, "m", "after", 9)
out("stored after the flush: " .. GetStoredInteger(c1, "m", "after") .. ", by name "
    .. GetStoredInteger(InitGameCache("files-probe.w3v"), "m", "after"))
SyncStoredInteger(c1, "m", "after")
out("synced: " .. GetStoredInteger(c1, "m", "after"))

-- SaveGameCache and ReloadGameCachesFromDisk were measured once and left out here (docs/facts.md, "Files and game
-- cache"): SaveGameCache rewrites the account's Campaigns.w3v, which a probe mustn't touch.

-- A game cache as the carrier.
write(dir .. "cache.pld", "\")\ncall StoreInteger(InitGameCache(\"files-probe.w3v\"), \"pre\", \"k\", 42)\n//")
Preloader(dir .. "cache.pld")
out("stored by Preloader: " .. GetStoredInteger(InitGameCache("files-probe.w3v"), "pre", "k"))

-- The number PreloadEnd is written with, 2.5 s after PreloadGenStart.
PreloadGenClear()
PreloadGenStart()
TimerStart(CreateTimer(), 2.5, false, guard(function()
    Preload("late")
    PreloadGenEnd(dir .. "late.pld")
    out("late written")
    done()
end))
