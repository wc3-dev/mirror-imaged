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

part(0.2, "upgrades", function()
  local p = Player(0)
  local n = make("unec")
  local f = make("hfoo")
  UnitAddAbility(f, FourCC("Arai"))
  local o = make("unec", Player(1))
  SetPlayerTechResearched(p, FourCC("Rusm"), 1)
  local m = make("unec")
  local g = make("hfoo")
  out("researched " .. lv(n, "Arai") .. " new " .. lv(m, "Arai") .. " added before " .. lv(f, "Arai") .. " added after " .. show(UnitAddAbility(g, FourCC("Arai"))) .. " " .. lv(g, "Arai")
    .. " other " .. lv(o, "Arai"))
  out("set " .. SetUnitAbilityLevel(n, FourCC("Arai"), 1) .. " " .. lv(n, "Arai") .. " inc " .. IncUnitAbilityLevel(m, FourCC("Arai")) .. " " .. lv(m, "Arai")
    .. " dec " .. DecUnitAbilityLevel(g, FourCC("Arai")) .. " " .. lv(g, "Arai"))
  SetPlayerTechResearched(p, FourCC("Rusm"), 0)
  out("unresearched " .. lv(n, "Arai") .. " " .. lv(m, "Arai") .. " " .. lv(f, "Arai") .. " " .. lv(g, "Arai") .. " new " .. lv(make("unec"), "Arai"))
  local k = make("unec")
  SetPlayerTechResearched(p, FourCC("Rusm"), 2)
  out("level 2 " .. lv(k, "Arai") .. " " .. lv(n, "Arai") .. " new " .. lv(make("unec"), "Arai"))
  SetPlayerTechResearched(p, FourCC("Rusm"), 1)
  out("back to 1 " .. lv(k, "Arai") .. " " .. lv(n, "Arai"))
  SetUnitOwner(k, Player(1), true)
  out("given away " .. lv(k, "Arai"))
  SetUnitOwner(o, p, true)
  out("given here " .. lv(o, "Arai"))
  local q = make("unec", Player(2))
  AddPlayerTechResearched(Player(2), FourCC("Rusm"), 1)
  out("added " .. lv(q, "Arai") .. " " .. GetPlayerTechCount(Player(2), FourCC("Rusm"), true))
  BlzDecPlayerTechResearched(Player(2), FourCC("Rusm"), 1)
  out("decreased " .. lv(q, "Arai"))
  local h = make("unec")
  UnitRemoveAbility(h, FourCC("Arai"))
  UnitAddAbility(h, FourCC("Arai"))
  out("re-added " .. lv(h, "Arai"))
end)
