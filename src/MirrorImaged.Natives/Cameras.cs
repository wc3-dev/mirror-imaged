using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class camerasetup
{
    // CAMERA_FIELD_TARGET_DISTANCE (0) to CAMERA_FIELD_ZABSOLUTE (13) as set, angles in degrees, each with its time
    internal readonly float[] Fields = [3000, 5400, 304, 70, 0, 90, 0, 16, 0, 0, 0, 0, 0, 0];
    internal readonly float[] Durations = new float[14];
    internal float X, Y, Duration;
    internal string? Label = "";
}

// The local player's camera (probes cameras, cameras-2 and cameras-3; decisions D500 to D505): its fields, kept as
// set (angles in degrees) and read at once, its target and the bounds that hold it, and its eye, which the game works
// out as it draws, so the target and the eye read as the step before left them. A change over a time moves in a
// straight line from where the value is. The view itself isn't simulated. Every player's camera is the local
// player's (multiplayer: wave 6).
public sealed partial class GameNatives
{
    const string Cameras = "docs/facts.md, Cameras: probes cameras, cameras-2 and cameras-3 (11-08)";
    const int CameraFields = 14;
    const float PanSpeed = 3000; // PanCameraTo's, and a setup's pan that isn't timed, a second (cameras-3)

    // The game camera's fields, which ResetToGameCamera puts back (cameras)
    static readonly float[] GameCamera = [2000, 5300, 304, 70, 0, 90, 10, 16, 0, 0, 0, 0, 0, 0];

    readonly Motion[] _camera = [.. GameCamera.Select((value, field) => new Motion(Limit(field, value)))];
    Motion _cameraX, _cameraY;
    Motion? _panZ; // a pan's height, in place of the z offset, until the target moves again (cameras-2, cameras-4)

    // SetCameraRotateMode's turn about a point (cameras-4): the target goes round it by the sweep over the time, the
    // view turning with it while the rotation field reads as it was, and without the z offset, until the game camera is
    // put back; the angles are in radians.
    (float X, float Y, double Radius, double Start, Motion Swept)? _orbit;
    readonly float[] _bounds = new float[8];
    readonly bool[] _byInput = [.. Enumerable.Repeat(true, CameraFields)];
    int _cameraType;
    bool _hotkeyLock = true;

    // A value that is Before until step Start, then goes from From to To by step End in a straight line. Set again in
    // the same step, Before keeps what the step before saw.
    readonly record struct Motion(float Before, float From, float To, long Start, long End)
    {
        public Motion(float value) : this(value, value, value, 0, 0) { }

        public Motion Toward(float to, long now, long steps) => new(At(now - 1), At(now), to, now, now + steps);

        public float At(long step) => step < Start ? Before : step >= End ? To : (float)(From + (To - From) * (double)(step - Start) / (End - Start));
    }

    // A field as the camera keeps it (cameras-2): angles come round to a turn, the field of view then held from 5 to
    // 120 degrees, and the lengths held in limits.
    static float Limit(int field, float value) => field switch
    {
        0 => Math.Clamp(value, 0.5f, 10000),
        1 => Math.Max(value, 100),
        2 or 4 or 5 or 8 or 9 or 10 => Turn(value),
        3 => Math.Clamp(Turn(value), 5, 120),
        6 => Math.Clamp(value, -4096, 4096),
        7 => Math.Clamp(value, 16, 9500),
        11 or 12 => Math.Max(value, 0),
        _ => value,
    };

    // Degrees into a turn: a whole number of turns is 360 above 0, and -0 at 0 and below, as the game reads them.
    static float Turn(float degrees)
    {
        var rest = degrees % 360;
        return rest < 0 ? rest + 360 : rest == 0 ? degrees > 0 ? 360 : -0f : rest;
    }

    static bool IsAngle(int field) => field is 2 or 3 or 4 or 5 or 8 or 9 or 10;

    static bool Turns(int field) => field is 2 or 4 or 5 or 8 or 9 or 10;

    static double Radians(float degrees) => degrees * Math.PI / 180;

    static long CameraSteps(float seconds) => Math.Max(0, Game.Steps(seconds));

    // Sets a field, over a time in a straight line; an angle that turns goes the shorter way round (cameras-4).
    void SetCameraField(int field, float value, float duration)
    {
        var (to, from, steps) = (Limit(field, value), _camera[field].At(_game.Step), CameraSteps(duration));
        if (Turns(field) && steps > 0) to = from + ((to - from + 540) % 360 - 180);
        _camera[field] = _camera[field].Toward(to, _game.Step, steps);
    }

    float Field(int field, long step) => Turns(field) ? Turn(_camera[field].At(step)) : _camera[field].At(step);

    // The least or most x (first 0) or y (first 1) of the bounds' four corners
    float Bound(int first, bool most)
    {
        float[] corners = [_bounds[first], _bounds[first + 2], _bounds[first + 4], _bounds[first + 6]];
        return most ? corners.Max() : corners.Min();
    }

    float TargetX => Math.Clamp(_orbit is { } o ? (float)(o.X + o.Radius * Math.Cos(o.Start + o.Swept.At(_game.Step - 1))) : _cameraX.At(_game.Step - 1),
        Bound(0, false), Bound(0, true));

    float TargetY => Math.Clamp(_orbit is { } o ? (float)(o.Y + o.Radius * Math.Sin(o.Start + o.Swept.At(_game.Step - 1))) : _cameraY.At(_game.Step - 1),
        Bound(1, false), Bound(1, true));

    float TargetZ => (Data.Terrain?.Height(TargetX, TargetY) ?? 0) + (_orbit is null ? ZOffset : 0);

    float ZOffset => _panZ?.At(_game.Step - 1) ?? _camera[6].At(_game.Step - 1);

    // Ends a turn about a point, the target staying where the turn has it.
    void Settle()
    {
        if (_orbit is not null) (_cameraX, _cameraY, _orbit) = (new Motion(TargetX), new Motion(TargetY), null);
    }

    // Moves the target to (x, y), held in the bounds, over the steps given; 0 lands in the next step. A pan's z moves
    // with it. A move that leaves the target where it is keeps its height (cameras: PanCameraToWithZ inside bounds of 0),
    // and the rotate mode holds the target (cameras-4: a position set in it didn't move it).
    void MoveCamera(float x, float y, long steps, float? z = null)
    {
        if (_orbit is not null) return;
        var (toX, toY) = (Math.Clamp(x, Bound(0, false), Bound(0, true)), Math.Clamp(y, Bound(1, false), Bound(1, true)));
        if ((toX, toY) != (TargetX, TargetY)) _panZ = z is { } height ? new Motion(ZOffset).Toward(height, _game.Step, steps) : null;
        _cameraX = _cameraX.Toward(toX, _game.Step, steps);
        _cameraY = _cameraY.Toward(toY, _game.Step, steps);
    }

    long PanSteps(float x, float y) =>
        Game.Steps(Math.Sqrt(Math.Pow(Math.Clamp(x, Bound(0, false), Bound(0, true)) - TargetX, 2) + Math.Pow(Math.Clamp(y, Bound(1, false), Bound(1, true)) - TargetY, 2)) / PanSpeed);

    // The eye: the distance back from the target along the angle of attack and the rotation, as the step before saw them
    (float X, float Y, float Z) Eye()
    {
        var step = _game.Step - 1;
        var (distance, turned) = (_camera[0].At(step), _orbit is { } o ? o.Swept.At(step) * 180 / Math.PI : 0);
        var ((attackSin, attackCos), (rotationSin, rotationCos)) = (double.SinCosPi(Field(2, step) / 180.0), double.SinCosPi((Field(5, step) + turned) / 180.0));
        return ((float)(TargetX - distance * attackCos * rotationCos), (float)(TargetY - distance * attackCos * rotationSin), (float)(TargetZ - distance * attackSin));
    }

    // Sets every field of the setup over its own time or the one given, and pans to its place if asked: over its
    // place's time when timed, else at PanSpeed (cameras-2, cameras-3), with a z if given.
    void Apply(camerasetup? setup, bool pan, bool timed, float? duration = null, float? z = null)
    {
        if (setup is null) return;
        for (var field = 0; field < CameraFields; field++) SetCameraField(field, setup.Fields[field], duration ?? setup.Durations[field]);
        if (pan) MoveCamera(setup.X, setup.Y, duration is { } d ? CameraSteps(d) : timed ? CameraSteps(setup.Duration) : PanSteps(setup.X, setup.Y), z);
    }

    [Native(Level.Exact, Fact = Cameras)]
    public float GetCameraMargin(int whichMargin) => whichMargin switch
    {
        0 or 1 => 512, // CAMERA_MARGIN_LEFT, CAMERA_MARGIN_RIGHT
        2 or 3 => 256, // CAMERA_MARGIN_TOP, CAMERA_MARGIN_BOTTOM
        _ => 0,
    };

    /// <summary>The four corners, held as given; the bounds start at 0 until the script sets them.</summary>
    [Native(Level.Exact, Fact = Cameras)]
    public void SetCameraBounds(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) =>
        (_bounds[0], _bounds[1], _bounds[2], _bounds[3], _bounds[4], _bounds[5], _bounds[6], _bounds[7]) = (x1, y1, x2, y2, x3, y3, x4, y4);

    /// <summary>The least x of the four corners.</summary>
    [Native(Level.Exact, Fact = Cameras)]
    public float GetCameraBoundMinX() => Bound(0, false);

    [Native(Level.Exact, Fact = Cameras)]
    public float GetCameraBoundMinY() => Bound(1, false);

    [Native(Level.Exact, Fact = Cameras)]
    public float GetCameraBoundMaxX() => Bound(0, true);

    [Native(Level.Exact, Fact = Cameras)]
    public float GetCameraBoundMaxY() => Bound(1, true);

    /// <summary>
    /// The field, read at once, the angles in radians. Close: the game reads 0.0004 off at times, and while a change
    /// over a time goes on its value is a frame or so ahead.
    /// </summary>
    [Native(Level.Close)]
    public float GetCameraField(camerafield? whichField) =>
        whichField is { Id: >= 0 and < CameraFields } f ? IsAngle(f.Id) ? (float)Radians(Field(f.Id, _game.Step)) : Field(f.Id, _game.Step) : 0;

    /// <summary>
    /// Sets the field in degrees or game units, held in its limits; over a time it moves there in a straight line.
    /// Close: the game's change over a time is a frame or so ahead of this one.
    /// </summary>
    [Native(Level.Close)]
    public void SetCameraField(camerafield? whichField, float value, float duration)
    {
        if (whichField is { Id: >= 0 and < CameraFields }) SetCameraField(whichField.Id, value, duration);
    }

    [Native(Level.Close)]
    public void AdjustCameraField(camerafield? whichField, float offset, float duration)
    {
        if (whichField is { Id: >= 0 and < CameraFields }) SetCameraField(whichField.Id, Field(whichField.Id, _game.Step) + offset, duration);
    }

    /// <summary>Puts the game camera's fields back but CAMERA_FIELD_ZABSOLUTE; the target stays, and a turn about a point ends.</summary>
    [Native(Level.Close)]
    public void ResetToGameCamera(float duration)
    {
        Settle();
        for (var field = 0; field < CameraFields - 1; field++) SetCameraField(field, GameCamera[field], duration);
    }

    /// <summary>
    /// Moves the target there, held in the bounds' box, read from the next step. Close: the game's lands after a frame,
    /// and at the game's start 0.2 to 0.25 s later (cameras-3); its bounds' hold isn't always the box (cameras).
    /// </summary>
    [Native(Level.Close)]
    public void SetCameraPosition(float x, float y) => MoveCamera(x, y, 0);

    /// <summary>A pan at 3000 a second, as the game's, measured to a frame or so (cameras-3).</summary>
    [Native(Level.Close)]
    public void PanCameraTo(float x, float y) => MoveCamera(x, y, PanSteps(x, y));

    [Native(Level.Close)]
    public void PanCameraToTimed(float x, float y, float duration) => MoveCamera(x, y, CameraSteps(duration));

    /// <summary>The target's height becomes the z given, the z offset field stays (cameras-2).</summary>
    [Native(Level.Close)]
    public void PanCameraToWithZ(float x, float y, float zOffsetDest) => MoveCamera(x, y, PanSteps(x, y), zOffsetDest);

    [Native(Level.Close)]
    public void PanCameraToTimedWithZ(float x, float y, float zOffsetDest, float duration) => MoveCamera(x, y, CameraSteps(duration), zOffsetDest);

    /// <summary>Stops a pan where it is (cameras-2); a field's change goes on, and a turn about a point stops (not measured).</summary>
    [Native(Level.Close)]
    public void StopCamera()
    {
        Settle();
        (_cameraX, _cameraY) = (_cameraX.Toward(_cameraX.At(_game.Step - 1), _game.Step, 0), _cameraY.Toward(_cameraY.At(_game.Step - 1), _game.Step, 0));
    }

    /// <summary>
    /// The target goes round the point by the sweep, counterclockwise, over the time, the view turning with it while
    /// the rotation field reads as it was, without the z offset; it holds the target, positions and pans dropped, until
    /// ResetToGameCamera (cameras-4). Close: the game's turn is a frame or so ahead; StopCamera ending it is the model's.
    /// </summary>
    [Native(Level.Close)]
    public void SetCameraRotateMode(float x, float y, float radiansToSweep, float duration)
    {
        Settle();
        var (dx, dy) = (TargetX - x, TargetY - y);
        _orbit = (x, y, Math.Sqrt(dx * dx + dy * dy), Math.Atan2(dy, dx), new Motion(0).Toward(radiansToSweep, _game.Step, CameraSteps(duration)));
    }

    /// <summary>The target's x as the step before left it.</summary>
    [Native(Level.Close)]
    public float GetCameraTargetPositionX() => TargetX;

    [Native(Level.Close)]
    public float GetCameraTargetPositionY() => TargetY;

    /// <summary>
    /// The ground's height there and the z offset, or a pan's z. Close: measured on flat ground; on a hill, inferred.
    /// </summary>
    [Native(Level.Close)]
    public float GetCameraTargetPositionZ() => TargetZ;

    /// <summary>
    /// The eye, the distance back from the target along the angle of attack and the rotation as the step before saw
    /// them. Close: the game's sums are a float step or so off.
    /// </summary>
    [Native(Level.Close)]
    public float GetCameraEyePositionX() => Eye().X;

    [Native(Level.Close)]
    public float GetCameraEyePositionY() => Eye().Y;

    [Native(Level.Close)]
    public float GetCameraEyePositionZ() => Eye().Z;

    [Native(Level.Exact, Fact = Cameras)]
    public camerasetup? CreateCameraSetup() => Make(new camerasetup());

    /// <summary>Kept as given, in degrees for an angle, a length below 0 as 0; its time is the field's when the setup is applied.</summary>
    [Native(Level.Exact, Fact = Cameras)]
    public void CameraSetupSetField(camerasetup? whichSetup, camerafield? whichField, float value, float duration)
    {
        if (whichSetup is null || whichField is not { Id: >= 0 and < CameraFields } f) return;
        whichSetup.Fields[f.Id] = f.Id is 0 or 1 or 7 or 11 or 12 ? Math.Max(value, 0) : value;
        whichSetup.Durations[f.Id] = duration;
    }

    [Native(Level.Exact, Fact = Cameras)]
    public float CameraSetupGetField(camerasetup? whichSetup, camerafield? whichField) =>
        whichSetup is not null && whichField is { Id: >= 0 and < CameraFields } f ? whichSetup.Fields[f.Id] : 0;

    [Native(Level.Exact, Fact = Cameras)]
    public void CameraSetupSetDestPosition(camerasetup? whichSetup, float x, float y, float duration)
    {
        if (whichSetup is not null) (whichSetup.X, whichSetup.Y, whichSetup.Duration) = (x, y, duration);
    }

    [Native(Level.Exact, Fact = Cameras)]
    public float CameraSetupGetDestPositionX(camerasetup? whichSetup) => whichSetup?.X ?? 0;

    [Native(Level.Exact, Fact = Cameras)]
    public float CameraSetupGetDestPositionY(camerasetup? whichSetup) => whichSetup?.Y ?? 0;

    [Native(Level.Exact, Fact = Cameras)]
    public void BlzCameraSetupSetLabel(camerasetup? whichSetup, string? label)
    {
        if (whichSetup is not null) whichSetup.Label = label;
    }

    [Native(Level.Exact, Fact = Cameras)]
    public string? BlzCameraSetupGetLabel(camerasetup? whichSetup) => whichSetup?.Label;

    /// <summary>0. Close: the game gives a number of its own each run, whatever was set (cameras).</summary>
    [Native(Level.Close)]
    public int BlzCameraSetupGetCameraType(camerasetup? whichSetup) => 0;

    [Native(Level.Recorded, Reason = "The game's getter doesn't read it back (cameras); what it changes in the view isn't simulated")]
    public void BlzCameraSetupSetCameraType(camerasetup? whichSetup, int cameraType) { }

    /// <summary>
    /// Sets every field over its own time from the setup and pans to its place if asked: over the place's time when
    /// timed, else at 3000 a second.
    /// </summary>
    [Native(Level.Close)]
    public void CameraSetupApply(camerasetup? whichSetup, bool doPan, bool panTimed) => Apply(whichSetup, doPan, panTimed);

    /// <summary>Pans as CameraSetupApply's that isn't timed; the z isn't read back (cameras-2, cameras-4).</summary>
    [Native(Level.Close)]
    public void CameraSetupApplyWithZ(camerasetup? whichSetup, float zDestOffset) => Apply(whichSetup, true, false);

    /// <summary>The fields and the pan over the time given.</summary>
    [Native(Level.Close)]
    public void CameraSetupApplyForceDuration(camerasetup? whichSetup, bool doPan, float forceDuration) => Apply(whichSetup, doPan, true, forceDuration);

    /// <summary>Pans over the time given, the target's height going to the z with it (cameras-4); over 0 s the z isn't read back (cameras-2).</summary>
    [Native(Level.Close)]
    public void CameraSetupApplyForceDurationWithZ(camerasetup? whichSetup, float zDestOffset, float forceDuration) =>
        Apply(whichSetup, true, true, forceDuration, forceDuration > 0 ? zDestOffset : null);

    /// <summary>As CameraSetupApplyForceDuration; the easing isn't modelled.</summary>
    [Native(Level.Close)]
    public void BlzCameraSetupApplyForceDurationSmooth(camerasetup? whichSetup, bool doPan, float forcedDuration, float easeInDuration, float easeOutDuration, float smoothFactor) =>
        Apply(whichSetup, doPan, true, forcedDuration);

    [Native(Level.Exact, Fact = Cameras)]
    public bool GetCameraFieldControlledByInput(camerafield? whichField) => whichField is { Id: >= 0 and < CameraFields } f && _byInput[f.Id];

    [Native(Level.Exact, Fact = Cameras)]
    public void SetCameraFieldControlledByInput(camerafield? whichField, bool controlled)
    {
        if (whichField is { Id: >= 0 and < CameraFields }) _byInput[whichField.Id] = controlled;
    }

    [Native(Level.Exact, Fact = Cameras)]
    public int BlzCameraGetCameraType() => _cameraType;

    [Native(Level.Exact, Fact = Cameras)]
    public void BlzCameraSetCameraType(int cameraType) => _cameraType = cameraType;

    [Native(Level.Exact, Fact = Cameras)]
    public bool BlzGetCameraAllowsHotkeyTargetLock() => _hotkeyLock;

    [Native(Level.Exact, Fact = Cameras)]
    public void BlzSetCameraAllowsHotkeyTargetLock(bool allows) => _hotkeyLock = allows;

    [Native(Level.Recorded, Reason = "Where the space key takes the camera; no native reads it back")]
    public void SetCameraQuickPosition(float x, float y) { }

    [Native(Level.Recorded, Reason = "Shakes the view: in the game the target and the eye read shaken (cameras-4), here as they would be still")]
    public void CameraSetSourceNoise(float mag, float velocity) { }

    [Native(Level.Recorded, Reason = "Shakes the view: in the game the target and the eye read shaken (cameras-4), here as they would be still")]
    public void CameraSetSourceNoiseEx(float mag, float velocity, bool vertOnly) { }

    [Native(Level.Recorded, Reason = "Shakes the view: in the game the target and the eye read shaken (cameras-4), here as they would be still")]
    public void CameraSetTargetNoise(float mag, float velocity) { }

    [Native(Level.Recorded, Reason = "Shakes the view: in the game the target and the eye read shaken (cameras-4), here as they would be still")]
    public void CameraSetTargetNoiseEx(float mag, float velocity, bool vertOnly) { }

    [Native(Level.Recorded, Reason = "Smooths the view's moves; no native reads it back")]
    public void CameraSetSmoothingFactor(float factor) { }

    [Native(Level.Recorded, Reason = "The view's depth of field; no native reads it back")]
    public void CameraSetFocalDistance(float distance) { }

    [Native(Level.Recorded, Reason = "The view's depth of field; no native reads it back")]
    public void CameraSetDepthOfFieldScale(float scale) { }

    [Native(Level.Recorded, Reason = "Plays a camera model in the view; no native reads it back")]
    public void SetCinematicCamera(string? cameraModelFile) { }

    [Native(Level.Recorded, Reason = "Stops the view at a rect's edge; no native reads it back")]
    public void AddCameraBlocker(rect? where) { }

    [Native(Level.Recorded, Reason = "Stops the view at a rect's edge; no native reads it back")]
    public void EnableCameraBlocker(rect? r, bool flag) { }
}
