-- The fog flags InitBlizzard reads, and the camera natives the first three probes left: the rotate mode, a setup
-- applied with a z as it pans, a rotation over a time across a whole turn, and the quick position and the noise, which
-- shouldn't change what reads back. Each step runs after the wait before it.
local function F(x) return string.format("%.4f", x) end
local function target() return F(GetCameraTargetPositionX()) .. " " .. F(GetCameraTargetPositionY()) .. " " .. F(GetCameraTargetPositionZ()) end
local function eye() return F(GetCameraEyePositionX()) .. " " .. F(GetCameraEyePositionY()) .. " " .. F(GetCameraEyePositionZ()) end
local function field(i) return F(GetCameraField(ConvertCameraField(i))) end

local steps, a = {}, nil
local function step(wait, f) steps[#steps + 1] = {wait, f} end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function() steps[i][2]() run(i + 1) end))
end

out("fog " .. tostring(IsFogEnabled()) .. " mask " .. tostring(IsFogMaskEnabled()))
FogEnable(false)
FogMaskEnable(false)
out("fog set " .. tostring(IsFogEnabled()) .. " mask " .. tostring(IsFogMaskEnabled()))
FogEnable(true)
FogMaskEnable(true)
out("fog again " .. tostring(IsFogEnabled()) .. " mask " .. tostring(IsFogMaskEnabled()))

SetCameraBounds(-2000, -2000, 2000, 2000, -2000, 2000, 2000, -2000)
step(0.5, function()
  SetCameraRotateMode(500, 0, math.pi / 2, 2)
  out("rotate now " .. target() .. " rotation " .. field(5))
end)
step(0, function() out("rotate 0s " .. target() .. " rotation " .. field(5)) end)
step(1, function() out("rotate 1s " .. target() .. " rotation " .. field(5) .. " eye " .. eye()) end)
step(1.5, function()
  out("rotate 2.5s " .. target() .. " rotation " .. field(5) .. " eye " .. eye())
  SetCameraPosition(0, 0)
  ResetToGameCamera(0)
end)
step(0.5, function()
  out("reset after rotate " .. target() .. " rotation " .. field(5))
  a = CreateCameraSetup()
  CameraSetupSetDestPosition(a, 800, 400, 0)
  CameraSetupApplyWithZ(a, 150)
  out("apply with z now " .. target() .. " zoffset " .. field(6))
end)
step(0.5, function() out("apply with z 0.5s " .. target() .. " zoffset " .. field(6)) end)
step(1, function()
  out("apply with z 1.5s " .. target() .. " zoffset " .. field(6))
  SetCameraPosition(0, 0)
end)
step(0.5, function()
  CameraSetupApplyForceDurationWithZ(a, 250, 1)
  out("apply force with z now " .. target() .. " zoffset " .. field(6))
end)
step(0.5, function() out("apply force with z 0.5s " .. target() .. " zoffset " .. field(6)) end)
step(1, function()
  out("apply force with z 1.5s " .. target() .. " zoffset " .. field(6))
  SetCameraQuickPosition(1000, 1000)
  CameraSetSourceNoise(50, 50)
  CameraSetTargetNoise(50, 50)
end)
step(0.5, function()
  out("noise " .. target() .. " eye " .. eye())
  CameraSetSourceNoise(0, 0)
  CameraSetTargetNoise(0, 0)
  SetCameraField(CAMERA_FIELD_ROTATION, 350, 0)
  SetCameraField(CAMERA_FIELD_ROTATION, 10, 1)
end)
step(0.5, function() out("rotation 350 to 10 0.5s " .. field(5)) end)
step(1, function() out("rotation 350 to 10 1.5s " .. field(5)) end)
run(1)
