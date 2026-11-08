-- More of a unit's abilities (W2-7), on the probe map (custom_v1's data): a column set through another type's field,
-- string sets out of range, which plain fields take a set, the level count set lower, cooldown percent, the abilities
-- every kind of unit starts with in their order, and an upgrade raising an ability's level.
local function show(v) return v == nil and "nil" or tostring(v) end
local function name(id) return id == 0 and "0" or string.pack(">I4", id) end
local spot = 0
local function make(id, p)
  spot = spot % 225 + 1
  return CreateUnit(p or Player(0), FourCC(id), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function with(id)
  local u = make("hfoo")
  UnitAddAbility(u, FourCC(id))
  return u, BlzGetUnitAbility(u, FourCC(id))
end
local function lv(u, id) return GetUnitAbilityLevel(u, FourCC(id)) end
local function list(u)
  local ids = {}
  for i = 0, 20 do
    local a = BlzGetUnitAbilityByIndex(u, i)
    if a == nil then break end
    ids[#ids + 1] = name(BlzGetAbilityId(a))
  end
  return table.concat(ids, ",")
end

TimerStart(CreateTimer(), 3.0, false, guard(function() done() end))

part(0.15, "lists", function()
  local r = {}
  for _, id in ipairs({ "htow", "hkee", "hhou", "halt", "hlum", "hbla", "hgtw", "hwtw", "harm", "hars", "hgra", "hvlt", "ogre", "obar", "oalt",
    "unpl", "usep", "uaod", "etol", "eaom", "eate", "ngol", "ngme", "ntav", "nmrk", "opeo", "uaco", "ugho", "ewsp", "earc", "Obla", "Ulic",
    "Edem", "Nngs", "hmtm", "hrif", "hsor", "hmpr", "hgyr", "ufro", "owyv", "hdhw", "ebal", "hmtt", "ocat", "hbot", "nzep", "nmrl", "nfrl",
    "ncop", "nshe", "hspt", "uabo", "ushd", "ewsp", "nvil", "hwat", "otot", "uske" }) do
    local u = make(id)
    out(id .. " " .. (u == nil and "nil" or list(u)))
  end
end)
