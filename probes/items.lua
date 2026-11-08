-- Items (W2-3), on the probe map (custom_v1's data): making and removing, the handles an item takes, its values,
-- flags, fields and names, its life and death, its place, and a hero's inventory: slots, giving, taking, using,
-- powerups, stacking, the item events and their responses, and what carried items give a hero. Each hero is made on
-- a spot of its own (a grid 200 apart), as the game moves a unit made on another.
local function show(v) return v == nil and "nil" or tostring(v) end
local function kind(h) return h == nil and "nil" or tostring(h):match("^[%a_]+") end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local function id(i)
  local n = GetItemTypeId(i)
  return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
end
local spot = 0
local function make(uid)
  spot = spot % 225 + 1
  return CreateUnit(Player(0), FourCC(uid), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function slots(u)
  local s = {}
  for i = 0, 5 do s[#s + 1] = id(UnitItemInSlot(u, i)) end
  return table.concat(s, ",") .. " count " .. UnitInventoryCount(u)
end

TimerStart(CreateTimer(), 7.0, false, guard(function() done() end))

part(0.05, "create", function()
  local before = CreateTimer()
  local i = CreateItem(FourCC("phea"), 100.5, -200.25)
  local after = CreateTimer()
  out("ids " .. (GetHandleId(i) - GetHandleId(before)) .. " " .. (GetHandleId(after) - GetHandleId(i)) .. " " .. kind(i))
  out("phea at " .. GetItemX(i) .. " " .. GetItemY(i) .. " " .. GetWidgetX(i) .. " " .. GetWidgetY(i) .. " life " .. GetWidgetLife(i)
    .. " player " .. pid(GetItemPlayer(i)) .. " data " .. GetItemUserData(i) .. " name " .. show(GetItemName(i)))
  out("phea flags visible " .. tostring(IsItemVisible(i)) .. " owned " .. tostring(IsItemOwned(i)) .. " invulnerable "
    .. tostring(IsItemInvulnerable(i)) .. " powerup " .. tostring(IsItemPowerup(i)) .. " sellable " .. tostring(IsItemSellable(i))
    .. " pawnable " .. tostring(IsItemPawnable(i)) .. " bag " .. tostring(IsItemInBag(i)) .. " equipped " .. tostring(IsItemEquipped(i)))
  for _, n in ipairs({ "phea", "ratc", "rag1", "tdex", "stwp", "wneg", "ankh", "gold", "rej1", "rej3", "ckng", "pghe", "texp", "manh" }) do
    local it = CreateItem(FourCC(n), 0, 0)
    out(n .. " charges " .. GetItemCharges(it) .. " level " .. GetItemLevel(it) .. " type " .. GetHandleId(GetItemType(it)) .. " powerup "
      .. tostring(IsItemIdPowerup(FourCC(n))) .. " sellable " .. tostring(IsItemIdSellable(FourCC(n))) .. " pawnable "
      .. tostring(IsItemIdPawnable(FourCC(n))) .. " life " .. GetWidgetLife(it))
    RemoveItem(it)
  end
  out("make hfoo " .. kind(CreateItem(FourCC("hfoo"), 0, 0)) .. " 0 " .. kind(CreateItem(0, 0, 0)) .. " zzzz " .. kind(CreateItem(FourCC("zzzz"), 0, 0)))
  local s = BlzCreateItemWithSkin(FourCC("phea"), 0, 0, FourCC("ratc"))
  out("skin " .. id(s) .. " " .. tostring(BlzGetItemSkin(s) == FourCC("ratc")) .. " " .. show(GetItemName(s)) .. " " .. GetItemCharges(s))
  out("tips [" .. show(BlzGetItemTooltip(i)) .. "] [" .. show(BlzGetItemDescription(i)) .. "]")
  out("ext [" .. show(BlzGetItemExtendedTooltip(i)):sub(1, 200) .. "]")
  out("icon [" .. show(BlzGetItemIconPath(i)) .. "]")
  BlzSetItemName(i, "Probe Potion")
  BlzSetItemTooltip(i, "Probe Tip")
  out("renamed " .. show(GetItemName(i)) .. " " .. show(BlzGetItemTooltip(i)) .. " " .. show(GetObjectName(FourCC("phea"))))
  RemoveItem(i)
  out("removed " .. GetItemTypeId(i) .. " " .. GetItemX(i) .. " " .. GetWidgetLife(i) .. " " .. GetItemCharges(i) .. " " .. pid(GetItemPlayer(i)))
end)

part(0.1, "values", function()
  local i = CreateItem(FourCC("wneg"), 0, 0)
  SetItemCharges(i, 7) out("charges 7 " .. GetItemCharges(i))
  SetItemCharges(i, -2) out("charges -2 " .. GetItemCharges(i))
  SetItemUserData(i, 42) out("data " .. GetItemUserData(i))
  SetItemPlayer(i, Player(2), true) out("player " .. pid(GetItemPlayer(i)) .. " owned " .. tostring(IsItemOwned(i)))
  SetItemInvulnerable(i, true) out("invulnerable " .. tostring(IsItemInvulnerable(i)))
  SetItemVisible(i, false) out("hidden " .. tostring(IsItemVisible(i)))
  SetItemVisible(i, true)
  SetItemPawnable(i, false) out("pawnable " .. tostring(IsItemPawnable(i)) .. " " .. tostring(IsItemIdPawnable(FourCC("wneg"))))
  SetItemDropOnDeath(i, true)
  SetItemDroppable(i, false)
  SetItemDropID(i, FourCC("hfoo"))
  out("bools " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_DROPPED_WHEN_CARRIER_DIES)) .. " " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_CAN_BE_DROPPED))
    .. " " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_CAN_BE_SOLD_TO_MERCHANTS)) .. " " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_PERISHABLE))
    .. " " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_INCLUDE_AS_RANDOM_CHOICE)) .. " " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_USE_AUTOMATICALLY_WHEN_ACQUIRED))
    .. " " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_ACTIVELY_USED)))
  out("ints " .. BlzGetItemIntegerField(i, ITEM_IF_LEVEL) .. " " .. BlzGetItemIntegerField(i, ITEM_IF_NUMBER_OF_CHARGES) .. " "
    .. BlzGetItemIntegerField(i, ITEM_IF_COOLDOWN_GROUP) .. " " .. BlzGetItemIntegerField(i, ITEM_IF_MAX_HIT_POINTS) .. " "
    .. BlzGetItemIntegerField(i, ITEM_IF_HIT_POINTS) .. " " .. BlzGetItemIntegerField(i, ITEM_IF_PRIORITY) .. " "
    .. BlzGetItemIntegerField(i, ITEM_IF_ARMOR_TYPE) .. " " .. BlzGetItemIntegerField(i, ITEM_IF_TINTING_COLOR_RED) .. " "
    .. BlzGetItemIntegerField(i, ITEM_IF_TINTING_COLOR_ALPHA))
  out("real " .. BlzGetItemRealField(i, ITEM_RF_SCALING_VALUE) .. " model [" .. show(BlzGetItemStringField(i, ITEM_SF_MODEL_USED)) .. "]")
  out("set level " .. tostring(BlzSetItemIntegerField(i, ITEM_IF_LEVEL, 9)) .. " " .. GetItemLevel(i) .. " " .. BlzGetItemIntegerField(i, ITEM_IF_LEVEL))
  out("set charges " .. tostring(BlzSetItemIntegerField(i, ITEM_IF_NUMBER_OF_CHARGES, 4)) .. " " .. GetItemCharges(i))
  out("set hp " .. tostring(BlzSetItemIntegerField(i, ITEM_IF_HIT_POINTS, 30)) .. " " .. GetWidgetLife(i) .. " "
    .. tostring(BlzSetItemIntegerField(i, ITEM_IF_MAX_HIT_POINTS, 50)) .. " " .. BlzGetItemIntegerField(i, ITEM_IF_MAX_HIT_POINTS) .. " " .. GetWidgetLife(i))
  out("set bool " .. tostring(BlzSetItemBooleanField(i, ITEM_BF_PERISHABLE, false)) .. " " .. tostring(BlzGetItemBooleanField(i, ITEM_BF_PERISHABLE))
    .. " " .. tostring(BlzSetItemBooleanField(i, ITEM_BF_USE_AUTOMATICALLY_WHEN_ACQUIRED, true)) .. " " .. tostring(IsItemPowerup(i)))
  out("set real " .. tostring(BlzSetItemRealField(i, ITEM_RF_SCALING_VALUE, 2)) .. " " .. BlzGetItemRealField(i, ITEM_RF_SCALING_VALUE))
  SetItemPosition(i, 300.5, 400.25) out("position " .. GetItemX(i) .. " " .. GetItemY(i))
  SetItemPosition(i, 5000, -5000) out("far " .. GetItemX(i) .. " " .. GetItemY(i))
end)

part(0.15, "life", function()
  local i = CreateItem(FourCC("phea"), 200, 200)
  local t = CreateTrigger()
  TriggerRegisterDeathEvent(t, i)
  TriggerAddAction(t, function()
    out("death " .. GetHandleId(GetTriggerEventId()) .. " " .. tostring(GetTriggerWidget() == i) .. " " .. kind(GetTriggerWidget())
      .. " life " .. GetWidgetLife(i) .. " type " .. id(i) .. " unit " .. show(GetTriggerUnit()))
  end)
  SetWidgetLife(i, 30.5) out("life 30.5 " .. GetWidgetLife(i))
  SetWidgetLife(i, 500) out("life 500 " .. GetWidgetLife(i))
  SetWidgetLife(i, 0.3) out("life 0.3 " .. GetWidgetLife(i) .. " " .. id(i))
  SetWidgetLife(i, 0) out("life 0 " .. GetWidgetLife(i) .. " " .. id(i))
  TimerStart(CreateTimer(), 0, false, function() out("later " .. id(i) .. " " .. GetWidgetLife(i)) end)
  local v = CreateItem(FourCC("phea"), 200, 200)
  SetItemInvulnerable(v, true)
  SetWidgetLife(v, 0) out("invulnerable life 0 " .. GetWidgetLife(v) .. " " .. id(v))
end)

part(0.2, "inventory", function()
  local h = make("Hpal")
  local f = make("hfoo")
  out("size " .. UnitInventorySize(h) .. " " .. UnitInventorySize(f) .. " " .. slots(h))
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_PICKUP_ITEM)
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_DROP_ITEM)
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_USE_ITEM)
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_STACK_ITEM)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_PICKUP_ITEM, nil)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_DROP_ITEM, nil)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_USE_ITEM, nil)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_STACK_ITEM, nil)
  TriggerAddAction(t, function()
    local m = GetManipulatedItem()
    out("event " .. GetHandleId(GetTriggerEventId()) .. " " .. tostring(GetManipulatingUnit() == h) .. " " .. tostring(GetTriggerUnit() == h)
      .. " " .. pid(GetTriggerPlayer()) .. " item " .. id(m) .. " has " .. tostring(UnitHasItem(h, m)) .. " charges " .. GetItemCharges(m)
      .. " slots " .. slots(h) .. " stack " .. id(BlzGetStackingItemSource()) .. " " .. id(BlzGetStackingItemTarget()) .. " "
      .. BlzGetStackingItemTargetPreviousCharges())
  end)
  local r = UnitAddItemById(h, FourCC("ratc"))
  out("added " .. id(r) .. " " .. slots(h) .. " owned " .. tostring(IsItemOwned(r)) .. " player " .. pid(GetItemPlayer(r)) .. " visible "
    .. tostring(IsItemVisible(r)) .. " at " .. GetItemX(r) .. " " .. GetItemY(r) .. " hero " .. GetUnitX(h) .. " " .. GetUnitY(h))
  out("to slot 3 " .. tostring(UnitAddItemToSlotById(h, FourCC("rag1"), 3)) .. " " .. slots(h))
  out("to slot 3 again " .. tostring(UnitAddItemToSlotById(h, FourCC("rag1"), 3)) .. " " .. slots(h))
  out("slots -1 6 " .. kind(UnitItemInSlot(h, -1)) .. " " .. kind(UnitItemInSlot(h, 6)))
  local p1 = UnitAddItemById(h, FourCC("phea"))
  local p2 = UnitAddItemById(h, FourCC("phea"))
  out("two potions " .. slots(h) .. " " .. GetItemCharges(p1) .. " " .. id(p2) .. " " .. GetItemCharges(p2))
  local ground = CreateItem(FourCC("phea"), 600, 600)
  out("ground potion " .. tostring(UnitAddItem(h, ground)) .. " " .. slots(h) .. " " .. id(ground) .. " " .. GetItemCharges(p1))
  local tome = UnitAddItemById(h, FourCC("tdex"))
  out("tome " .. id(tome) .. " " .. slots(h) .. " agi " .. GetHeroAgi(h, false) .. " " .. GetHeroAgi(h, true))
  TimerStart(CreateTimer(), 0, false, function() out("tome later " .. id(tome) .. " " .. slots(h) .. " agi " .. GetHeroAgi(h, false)) end)
  out("attrs " .. GetHeroStr(h, true) .. " " .. GetHeroAgi(h, false) .. " " .. GetHeroAgi(h, true) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_WITH_BONUS)
    .. " armour " .. BlzGetUnitRealField(h, UNIT_RF_DEFENSE))
  local slot3 = UnitRemoveItemFromSlot(h, 3)
  out("from slot 3 " .. id(slot3) .. " " .. slots(h) .. " at " .. GetItemX(slot3) .. " " .. GetItemY(slot3) .. " visible " .. tostring(IsItemVisible(slot3))
    .. " owned " .. tostring(IsItemOwned(slot3)) .. " agi " .. GetHeroAgi(h, true))
  out("from slot 3 again " .. kind(UnitRemoveItemFromSlot(h, 3)))
  SetWidgetLife(h, 100)
  out("use " .. tostring(UnitUseItem(h, p1)) .. " life " .. GetWidgetLife(h) .. " charges " .. GetItemCharges(p1) .. " " .. slots(h))
  TimerStart(CreateTimer(), 0.5, false, function() out("used later " .. id(p1) .. " life " .. GetWidgetLife(h) .. " " .. slots(h)) end)
end)

part(1.0, "full", function()
  local h = make("Hpal")
  for _, n in ipairs({ "ratc", "rag1", "ckng", "modt", "ankh", "stwp" }) do UnitAddItemById(h, FourCC(n)) end
  out("full " .. slots(h) .. " life " .. GetWidgetLife(h) .. " " .. BlzGetUnitMaxHP(h) .. " mana " .. BlzGetUnitMaxMana(h) .. " attrs "
    .. GetHeroStr(h, true) .. " " .. GetHeroAgi(h, true) .. " " .. GetHeroInt(h, true))
  local x = UnitAddItemById(h, FourCC("phea"))
  out("seventh " .. kind(x) .. " " .. slots(h))
  local near = 0
  local r = Rect(GetUnitX(h) - 400, GetUnitY(h) - 400, GetUnitX(h) + 400, GetUnitY(h) + 400)
  EnumItemsInRect(r, nil, function()
    local e = GetEnumItem()
    near = near + 1
    out("near " .. id(e) .. " at " .. (GetItemX(e) - GetUnitX(h)) .. " " .. (GetItemY(e) - GetUnitY(h)))
  end)
  out("near count " .. near)
  local g = CreateItem(FourCC("phea"), GetUnitX(h) + 50, GetUnitY(h))
  out("full add " .. tostring(UnitAddItem(h, g)) .. " " .. tostring(IsItemOwned(g)))
  out("to slot 6 " .. tostring(UnitAddItemToSlotById(h, FourCC("phea"), 6)) .. " -1 " .. tostring(UnitAddItemToSlotById(h, FourCC("phea"), -1)))
  local s = UnitItemInSlot(h, 0)
  out("drop slot 0 to 4 " .. tostring(UnitDropItemSlot(h, s, 4)) .. " " .. slots(h))
  out("drop point " .. tostring(UnitDropItemPoint(h, UnitItemInSlot(h, 1), GetUnitX(h) + 100, GetUnitY(h))) .. " " .. slots(h))
  TimerStart(CreateTimer(), 2.0, false, function() out("drop point later " .. slots(h)) end)
  local other = make("Hpal")
  out("drop target " .. tostring(UnitDropItemTarget(h, UnitItemInSlot(h, 2), other)) .. " " .. slots(h) .. " " .. slots(other))
  out("give " .. tostring(UnitAddItem(other, UnitItemInSlot(h, 3))) .. " " .. slots(h) .. " " .. slots(other))
  UnitRemoveItem(h, UnitItemInSlot(h, 5))
  out("removed " .. slots(h) .. " attrs " .. GetHeroStr(h, true) .. " " .. GetHeroAgi(h, true) .. " " .. GetHeroInt(h, true) .. " life " .. GetWidgetLife(h)
    .. " " .. BlzGetUnitMaxHP(h))
  KillUnit(h)
  out("dead hero " .. slots(h))
end)

part(1.5, "enum", function()
  local r = Rect(-1900, 1500, -1500, 1900)
  local made = {}
  for k, n in ipairs({ "phea", "ratc", "rag1", "wneg" }) do made[k] = CreateItem(FourCC(n), -1800 + k * 60, 1700 - k * 40) end
  SetItemVisible(made[2], false)
  local f = Filter(function() return GetItemTypeId(GetFilterItem()) ~= FourCC("wneg") end)
  local seen = {}
  EnumItemsInRect(r, f, function() seen[#seen + 1] = id(GetEnumItem()) .. "/" .. id(GetFilterItem()) end)
  out("enum " .. table.concat(seen, " ") .. " outside " .. show(GetEnumItem()))
  RemoveItem(made[1])
  seen = {}
  EnumItemsInRect(r, nil, function() seen[#seen + 1] = id(GetEnumItem()) end)
  out("enum removed " .. table.concat(seen, " "))
end)
