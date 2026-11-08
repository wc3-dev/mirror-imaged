-- The kinds of a unit type and the kinds added to a unit (W2-10), on the probe map (custom_v1's data): IsUnitIdType for
-- types 0 to 26 on the units probe's twelve types, a few more and ids that are no unit's; then on a fresh footman for
-- each type: IsUnitType, UnitAddType, IsUnitType, its classification field, UnitRemoveType and IsUnitType again, and
-- every kind after the add and after the removal.
local function tf(v) return v and "t" or "f" end
local function kinds(is)
  local s = {}
  for t = 0, 26 do s[#s + 1] = is(ConvertUnitType(t)) and "1" or "0" end
  return table.concat(s)
end

for _, id in ipairs({ "hfoo", "Hpal", "hbar", "hgry", "hpea", "nshe", "ugho", "hmtm", "otau", "ugol", "ewsp", "hrif", "nmrl", "hkni", "zzzz", "phea" }) do
  out("idtype " .. id .. " " .. kinds(function(t) return IsUnitIdType(FourCC(id), t) end))
end
out("idtype 0 " .. kinds(function(t) return IsUnitIdType(0, t) end) .. " nil " .. tf(IsUnitIdType(FourCC("hfoo"), nil)))

for t = 0, 26 do
  local u = CreateUnit(Player(0), FourCC("hfoo"), (t % 9) * 200 - 900, (t // 9) * 200 - 900, 0)
  local which = ConvertUnitType(t)
  local before = IsUnitType(u, which)
  local added = UnitAddType(u, which)
  local after = IsUnitType(u, which)
  local all = kinds(function(k) return IsUnitType(u, k) end)
  local class = BlzGetUnitIntegerField(u, UNIT_IF_UNIT_CLASSIFICATION)
  local removed = UnitRemoveType(u, which)
  out("addtype " .. t .. " " .. tf(before) .. tf(added) .. tf(after) .. tf(removed) .. tf(IsUnitType(u, which)) .. " " .. all .. " "
    .. kinds(function(k) return IsUnitType(u, k) end) .. " class " .. class)
end

local u = CreateUnit(Player(0), FourCC("hfoo"), 900, 900, 0)
UnitAddType(u, UNIT_TYPE_PEON)
UnitAddType(u, UNIT_TYPE_GIANT)
out("added kept " .. kinds(function(k) return IsUnitType(u, k) end) .. " class " .. BlzGetUnitIntegerField(u, UNIT_IF_UNIT_CLASSIFICATION)
  .. " idtype " .. kinds(function(k) return IsUnitIdType(FourCC("hfoo"), k) end) .. " other " .. kinds(function(k) return IsUnitType(CreateUnit(Player(0), FourCC("hfoo"), 900, 700, 0), k) end))
local b = CreateUnit(Player(0), FourCC("hbar"), 900, -900, 0)
out("barracks " .. tf(UnitRemoveType(b, UNIT_TYPE_STRUCTURE)) .. tf(IsUnitType(b, UNIT_TYPE_STRUCTURE)) .. tf(UnitRemoveType(b, UNIT_TYPE_MECHANICAL))
  .. tf(IsUnitType(b, UNIT_TYPE_MECHANICAL)) .. " nil " .. tf(UnitAddType(nil, UNIT_TYPE_PEON)) .. tf(UnitRemoveType(nil, UNIT_TYPE_PEON))
  .. tf(UnitAddType(b, nil)))
done()
