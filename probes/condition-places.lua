-- A destroyed condition's place taken twice (decisions D115, W1-10): c1 destroyed and taken by c2, c2 destroyed and
-- taken by c3, then the stale c1 destroyed; and whether And, Or, Not or Condition(nil) take a freed place. Each line
-- gives what each trigger's evaluation answered and which functions ran ("-" for none).
local log = {}
local function cond(name)
    return Condition(function() log[#log + 1] = name return true end)
end
local function trig(c)
    local t = CreateTrigger()
    TriggerAddCondition(t, c)
    return t
end
local function eval(label, ...)
    local answers = {}
    for _, t in ipairs({...}) do
        answers[#answers + 1] = tostring(TriggerEvaluate(t))
        answers[#answers + 1] = #log > 0 and table.concat(log, ",") or "-"
        log = {}
    end
    out(label .. " " .. table.concat(answers, " "))
end

local c1 = cond("c1")
local t1 = trig(c1)
DestroyCondition(c1)
local c2 = cond("c2")
local t2 = trig(c2)
eval("c2 took c1's place", t1, t2)
DestroyCondition(c2)
eval("c2 destroyed", t1, t2)
local c3 = cond("c3")
local t3 = trig(c3)
eval("c3 took it", t1, t2, t3)
DestroyCondition(c1)
eval("stale c1 destroyed", t1, t2, t3)
local c4 = cond("c4")
eval("c4 made", t1, t2, t3)
DestroyCondition(c2)
eval("stale c2 destroyed", t1, t2, t3)
local c5 = cond("c5")
eval("c5 made", t1, t2, t3)

local d = cond("d")
local td = trig(d)
DestroyCondition(d)
local made = {And(nil, nil), Or(nil, nil), Not(nil), Condition(nil), Filter(nil)}
eval("boolexprs and nil made", td)
local e = cond("e")
eval("e made", td)
done()
