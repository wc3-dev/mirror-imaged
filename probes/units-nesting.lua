-- How deep the callbacks a native starts nest in a Lua map (W2-4's review, decisions D250): ForForce, ForGroup and a unit
-- enumeration's filter each calling themselves again, and ExecuteFunc's function, up to 300 levels, each in a timer of
-- its own. For each: the deepest level reached and how many levels went on after their call.
local f = CreateForce()
ForceAddPlayer(f, Player(0))
local g = CreateGroup()
local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
GroupAddUnit(g, u)
local deepest, returned
local function nest(label, call)
  deepest, returned = 0, 0
  local function level(n)
    if n > deepest then deepest = n end
    if n < 300 then call(function() level(n + 1) return true end) end
    returned = returned + 1
  end
  level(1)
  out(label .. " deepest " .. deepest .. " returned " .. returned)
end
local n = 0
function NestExecute()
  n = n + 1
  if n > deepest then deepest = n end
  if n < 300 then ExecuteFunc("NestExecute") end
  returned = returned + 1
  n = n - 1
end
local tests = {
  function() nest("forforce", function(cb) ForForce(f, cb) end) end,
  function() nest("forgroup", function(cb) ForGroup(g, cb) end) end,
  function() nest("filter", function(cb) GroupEnumUnitsInRange(CreateGroup(), 0, 0, 100, Filter(cb)) end) end,
  function()
    deepest, returned = 0, 0
    ExecuteFunc("NestExecute")
    out("executefunc deepest " .. deepest .. " returned " .. returned)
  end,
}
for i, test in ipairs(tests) do TimerStart(CreateTimer(), i * 0.1, false, guard(test)) end
TimerStart(CreateTimer(), 1, false, guard(function() done() end))
