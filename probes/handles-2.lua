-- Handles, second part: whether a destroyed handle's id comes back later or after Lua collects it, what takes a
-- destroyed condition's place, == between handle types, the handles' metatables, and whether a handle Lua collected is
-- the same userdata when it comes back. Each part runs in its own timer callback.
local function kind(h) return h == nil and "nil" or (tostring(h):match("^[%a_]+")) end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function try(f, ...)
  local ok, v = pcall(f, ...)
  return ok and ("ok " .. (type(v) == "userdata" and kind(v) or tostring(v))) or ("error " .. tostring(v))
end
local function collect(weak)
  local spins = 0
  while next(weak) ~= nil and spins < 3000000 do
    local junk = {spins}
    spins = spins + 1
  end
  return tostring(next(weak) == nil)
end
local log = {}
local function cond(name, value) return Condition(function() log[#log + 1] = name return value end) end
local function flush(label) out(label .. " " .. table.concat(log, ",")) log = {} end

TimerStart(CreateTimer(), 6.0, false, guard(done))

-- ids freed: three timers destroyed and held, three destroyed and collected; new timers made 1 s later, then again
-- after the held ones are let go of and collected. The new timers' ids that were the held or collected ones are given
-- in their order as h<n> or c<n> (the n-th held or collected one's), then the count of other ids, as where another id
-- freed with them comes varies from run to run.
local heldIds, collectedIds, held = {}, {}, {}
local function named(n)
  for i = 1, 3 do
    if heldIds[i] == n then return "h" .. i end
    if collectedIds[i] == n then return "c" .. i end
  end
end
local function fresh(count)
  local names, others = {}, 0
  for i = 1, count do
    local name = named(GetHandleId(CreateTimer()))
    if name then names[#names + 1] = name else others = others + 1 end
  end
  return (#names > 0 and table.concat(names, " ") .. ", " or "") .. others .. " other"
end
part(0.1, "freed", function()
  local weak = setmetatable({}, {__mode = "k"})
  for i = 1, 3 do
    held[i] = CreateTimer()
    heldIds[i] = GetHandleId(held[i])
    DestroyTimer(held[i])
  end
  for i = 1, 3 do
    local t = CreateTimer()
    collectedIds[i] = GetHandleId(t)
    weak[t] = true
    DestroyTimer(t)
  end
  out("collected " .. collect(weak))
  out("at once " .. fresh(2))
end)
part(1.1, "a second later", function() out("new " .. fresh(4)) end)
part(1.2, "held let go", function()
  local weak = setmetatable({}, {__mode = "k"})
  for i = 1, 3 do weak[held[i]] = true end
  held = nil
  out("collected " .. collect(weak))
  out("at once " .. fresh(2))
end)
part(2.2, "a second after", function() out("new " .. fresh(4)) end)

part(3.0, "places", function()
  local function trig(...)
    local t = CreateTrigger()
    for _, c in ipairs({...}) do TriggerAddCondition(t, c) end
    return t
  end
  local dc = cond("d1", true)
  local t = trig(dc)
  DestroyCondition(dc)
  out("before " .. tostring(TriggerEvaluate(t)))
  local nc = cond("n1", false)
  out("after " .. tostring(TriggerEvaluate(t)))
  flush("ran")
  dc = cond("d2", true)
  t = trig(dc)
  DestroyCondition(dc)
  local nf = Filter(function() log[#log + 1] = "filter2" return false end)
  out("filter " .. tostring(TriggerEvaluate(t)))
  flush("ran")
  local df = Filter(function() log[#log + 1] = "d3" return true end)
  t = trig(df)
  DestroyFilter(df)
  nc = cond("n3", false)
  out("filter then condition " .. tostring(TriggerEvaluate(t)))
  flush("ran")
  local a, b = cond("a4", true), cond("b4", true)
  t = trig(a, b)
  DestroyCondition(a)
  DestroyCondition(b)
  local x, y = cond("x4", true), cond("y4", true)
  out("two " .. tostring(TriggerEvaluate(t)))
  flush("ran")
  dc = cond("d5", true)
  t = trig(dc)
  DestroyCondition(dc)
  local tm, tr = CreateTimer(), CreateTrigger()
  nc = cond("n5", true)
  out("after other handles " .. tostring(TriggerEvaluate(t)))
  flush("ran")
  dc = cond("d6", true)
  local e = Not(dc)
  t = trig(e)
  DestroyCondition(dc)
  nc = cond("n6", true)
  out("in a not " .. tostring(TriggerEvaluate(t)))
  flush("ran")
  local de = And(cond("a7", true), cond("b7", true))
  t = trig(de)
  DestroyBoolExpr(de)
  local ne = Or(cond("x7", true), cond("y7", true))
  out("and then or " .. tostring(TriggerEvaluate(t)))
  flush("ran")
end)

part(3.1, "equality", function()
  local t, u, g = CreateTimer(), CreateTimer(), CreateGroup()
  local n = GetHandleId(t)
  out("agents " .. try(function() return t == u end) .. ", " .. try(function() return t == g end) .. ", " .. try(function() return t == Player(0) end))
  out("agent first " .. try(function() return t == ConvertPlayerColor(n) end) .. ", " .. try(function() return Player(0) == RACE_HUMAN end))
  out("handle first " .. try(function() return ConvertPlayerColor(n) == t end) .. ", " .. try(function() return RACE_HUMAN == Player(0) end) .. ", " .. try(function() return ConvertRace(GetHandleId(Player(0))) == Player(0) end))
  out("handles " .. try(function() return ConvertRace(1) == ConvertPlayerColor(1) end) .. ", " .. try(function() return GetTriggerEventId() == ConvertPlayerColor(0) end)
    .. ", " .. try(function() return ConvertRace(1) == ConvertRace(2) end))
  out("not handles " .. try(function() return t == {} end) .. ", " .. try(function() return t == nil end) .. ", " .. try(function() return t ~= u end))
  local mt = getmetatable(t)
  out("metatable " .. type(mt))
  if type(mt) == "table" then
    local keys = {}
    for k, v in pairs(mt) do keys[#keys + 1] = tostring(k) .. "=" .. type(v) end
    table.sort(keys)
    out("timer fields " .. table.concat(keys, " ") .. " name " .. tostring(mt.__name))
    out("shared " .. tostring(mt == getmetatable(u)) .. " " .. tostring(mt == getmetatable(g)) .. " " .. tostring(mt.__eq == getmetatable(g).__eq)
      .. " " .. tostring(mt.__eq == getmetatable(RACE_HUMAN).__eq) .. " " .. tostring(getmetatable(RACE_HUMAN).__eq == getmetatable(PLAYER_COLOR_RED).__eq))
    keys = {}
    for k, v in pairs(getmetatable(RACE_HUMAN)) do keys[#keys + 1] = tostring(k) .. "=" .. type(v) end
    table.sort(keys)
    out("race fields " .. table.concat(keys, " ") .. " name " .. tostring(getmetatable(RACE_HUMAN).__name))
    out("eq called " .. try(mt.__eq, t, 5) .. ", " .. try(mt.__eq, 5, t) .. ", " .. try(mt.__eq, t, t) .. ", " .. try(getmetatable(RACE_HUMAN).__eq, RACE_HUMAN, 5)
      .. ", " .. try(getmetatable(RACE_HUMAN).__eq, RACE_HUMAN, t))
  end
end)

part(3.2, "same userdata", function()
  local t = CreateTimer()
  TimerStart(t, 0.01, false, function()
    local ok, err = pcall(function()
      out("expired " .. tostring(rawequal(GetExpiredTimer(), t)))
      local ht = InitHashtable()
      local h = CreateTimer()
      SaveTimerHandle(ht, 0, 0, h)
      local before = tostring(h)
      local weak = setmetatable({}, {__mode = "v"})
      weak[1] = h
      h = nil
      out("collected " .. collect(weak))
      out("loaded again " .. tostring(tostring(LoadTimerHandle(ht, 0, 0)) == before) .. " " .. tostring(rawequal(LoadTimerHandle(ht, 0, 0), LoadTimerHandle(ht, 0, 0))))
    end)
    if not ok then out("same userdata error " .. tostring(err)) end
  end)
end)
