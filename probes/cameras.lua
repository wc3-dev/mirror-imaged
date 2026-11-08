-- The local player's camera: the margins, bounds, fields, target and eye at the start; what the setters read back, at
-- once and later; camera setups' defaults and what they read back; applying one. Each step runs after the wait before it.
local function F(x) return string.format("%.4f", x) end
local function fields(get)
  local t = {}
  for i = 0, 13 do t[#t + 1] = F(get(ConvertCameraField(i))) end
  return table.concat(t, " ")
end
local function bounds() return F(GetCameraBoundMinX()) .. " " .. F(GetCameraBoundMinY()) .. " " .. F(GetCameraBoundMaxX()) .. " " .. F(GetCameraBoundMaxY()) end
local function target() return F(GetCameraTargetPositionX()) .. " " .. F(GetCameraTargetPositionY()) .. " " .. F(GetCameraTargetPositionZ()) end
local function eye() return F(GetCameraEyePositionX()) .. " " .. F(GetCameraEyePositionY()) .. " " .. F(GetCameraEyePositionZ()) end
local function field(i) return F(GetCameraField(ConvertCameraField(i))) end

local steps, applied = {}, nil
local function step(wait, f) steps[#steps + 1] = {wait, f} end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function() steps[i][2]() run(i + 1) end))
end

out("margins " .. F(GetCameraMargin(CAMERA_MARGIN_LEFT)) .. " " .. F(GetCameraMargin(CAMERA_MARGIN_RIGHT)) .. " " .. F(GetCameraMargin(CAMERA_MARGIN_TOP))
  .. " " .. F(GetCameraMargin(CAMERA_MARGIN_BOTTOM)) .. " out of range " .. F(GetCameraMargin(4)) .. " " .. F(GetCameraMargin(-1)))
local x1, y1, x2, y2 = GetCameraBoundMinX(), GetCameraBoundMinY(), GetCameraBoundMaxX(), GetCameraBoundMaxY()
out("bounds start " .. bounds())
out("fields start " .. fields(GetCameraField))
out("target start " .. target() .. " eye " .. eye())
local controlled = {}
for i = 0, 13 do controlled[#controlled + 1] = tostring(GetCameraFieldControlledByInput(ConvertCameraField(i))) end
out("controlled " .. table.concat(controlled, " "))
out("type " .. BlzCameraGetCameraType() .. " lock " .. tostring(BlzGetCameraAllowsHotkeyTargetLock()))

local s = CreateCameraSetup()
out("setup fields " .. fields(function(f) return CameraSetupGetField(s, f) end))
out("setup dest " .. F(CameraSetupGetDestPositionX(s)) .. " " .. F(CameraSetupGetDestPositionY(s)) .. " label [" .. tostring(BlzCameraSetupGetLabel(s))
  .. "] type " .. BlzCameraSetupGetCameraType(s))
local values = {1650, 5000, 304, 70, 10, 90, 50, 16, 5, 6, 7, 100, 2, 1}
for i = 0, 13 do CameraSetupSetField(s, ConvertCameraField(i), values[i + 1], 0) end
out("setup set " .. fields(function(f) return CameraSetupGetField(s, f) end))
local odd = {-5, -1, 400, 200, -30, 450, -50, -16, 365, -6, 720, -100, -2, 2}
for i = 0, 13 do CameraSetupSetField(s, ConvertCameraField(i), odd[i + 1], 0) end
out("setup odd " .. fields(function(f) return CameraSetupGetField(s, f) end))
CameraSetupSetDestPosition(s, 123.5, -456.25, 3)
BlzCameraSetupSetLabel(s, "label")
BlzCameraSetupSetCameraType(s, 2)
out("setup dest " .. F(CameraSetupGetDestPositionX(s)) .. " " .. F(CameraSetupGetDestPositionY(s)) .. " label [" .. tostring(BlzCameraSetupGetLabel(s))
  .. "] type " .. BlzCameraSetupGetCameraType(s))

SetCameraBounds(-1000, -800, 1200, 900, -1000, 900, 1200, -800)
out("bounds rect " .. bounds())
SetCameraBounds(-500, -600, 700, 800, -900, 400, 300, -1000)
out("bounds quad " .. bounds())
SetCameraBounds(1200, 900, -1000, -800, 1200, -800, -1000, 900)
out("bounds swapped " .. bounds())
SetCameraBounds(-100000, -100000, 100000, 100000, -100000, 100000, 100000, -100000)
out("bounds huge " .. bounds())
SetCameraBounds(x1, y1, x2, y2, x1, y2, x2, y1)
out("bounds again " .. bounds())

SetCameraPosition(500, 300)
out("position now " .. target())
step(0, function() out("position 0s " .. target()) end)
step(0.1, function() out("position 0.1s " .. target()) end)
step(0.4, function()
  out("position 0.5s " .. target() .. " eye " .. eye())
  SetCameraField(CAMERA_FIELD_TARGET_DISTANCE, 1000, 0)
  SetCameraField(CAMERA_FIELD_ANGLE_OF_ATTACK, 300, 0)
  out("field now " .. field(0) .. " " .. field(2))
end)
step(0, function() out("field 0s " .. field(0) .. " " .. field(2)) end)
step(0.5, function()
  out("field 0.5s " .. field(0) .. " " .. field(2) .. " eye " .. eye())
  for i = 0, 13 do SetCameraField(ConvertCameraField(i), values[i + 1], 0) end
end)
step(0.5, function()
  out("fields set " .. fields(GetCameraField))
  out("fields set target " .. target() .. " eye " .. eye())
  for i = 0, 13 do SetCameraField(ConvertCameraField(i), odd[i + 1], 0) end
end)
step(0.5, function()
  out("fields odd " .. fields(GetCameraField))
  ResetToGameCamera(0)
end)
step(0.5, function()
  out("reset " .. fields(GetCameraField))
  out("reset target " .. target() .. " eye " .. eye())
  AdjustCameraField(CAMERA_FIELD_TARGET_DISTANCE, 100, 0)
  AdjustCameraField(CAMERA_FIELD_ROTATION, 30, 0)
end)
step(0.5, function()
  out("adjusted " .. field(0) .. " " .. field(5))
  SetCameraField(CAMERA_FIELD_TARGET_DISTANCE, 3000, 2)
end)
step(1, function() out("timed field 1s " .. field(0)) end)
step(1.5, function()
  out("timed field 2.5s " .. field(0))
  PanCameraToTimed(-600, 400, 2)
end)
step(1, function() out("pan 1s " .. target()) end)
step(1.5, function()
  out("pan 2.5s " .. target())
  PanCameraTo(100, 100)
end)
step(0.5, function() out("panto 0.5s " .. target()) end)
step(2, function()
  out("panto 2.5s " .. target())
  PanCameraToWithZ(0, 0, 200)
end)
step(1, function()
  out("panwithz " .. target() .. " zoffset " .. field(6))
  SetCameraBounds(-200, -200, 200, 200, -200, 200, 200, -200)
  SetCameraPosition(1000, 1000)
end)
step(0.5, function()
  out("clamped " .. target())
  SetCameraBounds(x1, y1, x2, y2, x1, y2, x2, y1)
  local a = CreateCameraSetup()
  for i, v in pairs({[0] = 1650, [2] = 330, [3] = 60, [5] = 60, [6] = 0}) do CameraSetupSetField(a, ConvertCameraField(i), v, 0) end
  CameraSetupSetDestPosition(a, 300, -200, 0)
  CameraSetupApply(a, true, false)
  applied = a
end)
step(0.5, function()
  out("applied " .. fields(GetCameraField))
  out("applied target " .. target())
  SetCameraPosition(0, 0)
end)
step(0.5, function()
  CameraSetupApply(applied, false, false)
end)
step(0.5, function()
  out("applied without pan " .. target())
  CameraSetupApplyWithZ(applied, 150)
end)
step(0.5, function()
  out("applied with z " .. target() .. " zoffset " .. field(6))
  BlzCameraSetCameraType(1)
  BlzSetCameraAllowsHotkeyTargetLock(false)
  SetCameraFieldControlledByInput(CAMERA_FIELD_ROTATION, false)
  out("set type " .. BlzCameraGetCameraType() .. " lock " .. tostring(BlzGetCameraAllowsHotkeyTargetLock())
    .. " controlled " .. tostring(GetCameraFieldControlledByInput(CAMERA_FIELD_ROTATION)))
end)
run(1)
