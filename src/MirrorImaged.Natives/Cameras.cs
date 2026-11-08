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

// The local player's camera (probes cameras to cameras-4, units-camera and units-camera-2; decisions D500 to D505):
// its fields, kept as set (angles in degrees) and read at once, its target and the bounds that hold it, and its eye,
// which the game works out as it draws, so the target and the eye read as the step before left them. A change over a
// time moves in a straight line from where the value is. The view itself isn't simulated. Every player's camera is
// the local player's (multiplayer: wave 6).
public sealed partial class GameNatives
{
    const string Cameras = "docs/facts.md, Cameras: probes cameras, cameras-2 and cameras-3 (11-08)";
    const string Cameras5 = "docs/facts.md, Cameras: probe units-camera-2 (11-08)";
    const int CameraFields = 14;
    const float PanSpeed = 3000; // PanCameraTo's, and a setup's pan that isn't timed, a second (cameras-3)

    // The game camera's fields, which ResetToGameCamera puts back (cameras)
    static readonly float[] GameCamera = [2000, 5300, 304, 70, 0, 90, 10, 16, 0, 0, 0, 0, 0, 0];

    readonly Motion[] _camera = [.. GameCamera.Select((value, field) => new Motion(Limit(field, value)))];
    Motion _cameraX, _cameraY;
    // A pan's height, in place of the z offset, until the target moves again or, from the step after _panZUntil,
    // StopCamera or ResetToGameCamera drops it; following a unit keeps it (cameras-2, cameras-4, units-camera-2)
    Motion? _panZ;
    long _panZUntil = long.MaxValue;

    // SetCameraRotateMode's turn about a point (cameras-4, units-camera-2): the target goes round it by the sweep over the
    // time, the view turning with it while the rotation field reads as it was, and without the z offset, until a pan or
    // ResetToGameCamera ends it; the angles are in radians.
    (float X, float Y, double Radius, double Start, Motion Swept)? _orbit;

    // A unit the target follows at offsets from the step after Start (units-camera)
    (unit Unit, float X, float Y, long Start)? _follow;

    // AddCameraBlocker's rects, and those EnableCameraBlocker turned off (units-camera-2)
    readonly List<rect> _blockers = [];
    readonly HashSet<rect> _blockersOff = [];
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
        if (Turns(field) && steps > 0) to = from + (((to - from) % 360 + 540) % 360 - 180); // a half turn goes clockwise
        _camera[field] = _camera[field].Toward(to, _game.Step, steps);
    }

    float Field(int field, long step) => Turns(field) ? Turn(_camera[field].At(step)) : _camera[field].At(step);

    // The least or most x (first 0) or y (first 1) of the bounds' four corners
    float Bound(int first, bool most)
    {
        float[] corners = [_bounds[first], _bounds[first + 2], _bounds[first + 4], _bounds[first + 6]];
        return most ? corners.Max() : corners.Min();
    }

    float TargetX => Math.Clamp(
        _orbit is { } o ? (float)(o.X + o.Radius * Math.Cos(o.Start + o.Swept.At(_game.Step - 1)))
        : _follow is { } f && _game.Step > f.Start ? f.Unit.X + f.X
        : _cameraX.At(_game.Step - 1),
        Bound(0, false), Bound(0, true));

    float TargetY => Math.Clamp(
        _orbit is { } o ? (float)(o.Y + o.Radius * Math.Sin(o.Start + o.Swept.At(_game.Step - 1)))
        : _follow is { } f && _game.Step > f.Start ? f.Unit.Y + f.Y
        : _cameraY.At(_game.Step - 1),
        Bound(1, false), Bound(1, true));

    float TargetZ => Height(TargetX, TargetY) + (_orbit is null ? ZOffset : 0);

    float ZOffset => _panZ is { } z && _game.Step - 1 < _panZUntil ? z.At(_game.Step - 1) : _camera[6].At(_game.Step - 1);

    // Ends a turn about a point or a follow, the target staying where it reads.
    void Settle()
    {
        if (_orbit is not null || _follow is not null) (_cameraX, _cameraY, _orbit, _follow) = (new Motion(TargetX), new Motion(TargetY), null, null);
    }

    // Stops the rotate mode's sweep where it is; the mode goes on (units-camera-2).
    void StopSweep()
    {
        if (_orbit is { } o) _orbit = o with { Swept = o.Swept.Toward(o.Swept.At(_game.Step - 1), _game.Step, 0) };
    }

    // Moves the target to (x, y), held in the bounds and kept out of the camera blockers, over the steps given; 0 lands
    // in the next step. A pan's z moves with it. A move that leaves the target where it is keeps its height (cameras:
    // PanCameraToWithZ inside bounds of 0). It ends the rotate mode and a follow.
    void MoveCamera(float x, float y, long steps, float? z = null)
    {
        Settle();
        var (fromX, fromY) = (TargetX, TargetY);
        var (toX, toY) = Blocked(fromX, fromY, Math.Clamp(x, Bound(0, false), Bound(0, true)), Math.Clamp(y, Bound(1, false), Bound(1, true)), steps > 0);
        if ((toX, toY) != (fromX, fromY)) (_panZ, _panZUntil) = (z is { } height ? new Motion(ZOffset).Toward(height, _game.Step, steps) : null, long.MaxValue);
        _cameraX = _cameraX.Toward(toX, _game.Step, steps);
        _cameraY = _cameraY.Toward(toY, _game.Step, steps);
    }

    // Where a move from (fromX, fromY) to (toX, toY) ends for the blockers on (units-camera-2): a pan stops where its
    // path enters one, a position set inside one lands past it along the move. Each blocker in the order added.
    (float X, float Y) Blocked(float fromX, float fromY, float toX, float toY, bool pan)
    {
        if ((toX, toY) == (fromX, fromY)) return (toX, toY);
        foreach (var r in _blockers)
        {
            if (_blockersOff.Contains(r)) continue;
            var (dx, dy) = ((double)toX - fromX, (double)toY - fromY);
            var (enter, leave) = (double.NegativeInfinity, double.PositiveInfinity);
            foreach (var (from, d, min, max) in new[] { (fromX, dx, r.MinX, r.MaxX), (fromY, dy, r.MinY, r.MaxY) })
            {
                if (d == 0)
                {
                    if (from < min || from > max) (enter, leave) = (1, 0);
                    continue;
                }
                var (a, b) = ((min - from) / d, (max - from) / d);
                (enter, leave) = (Math.Max(enter, Math.Min(a, b)), Math.Min(leave, Math.Max(a, b)));
            }
            if (enter > leave) continue;
            if (pan && enter is >= 0 and <= 1) (toX, toY) = ((float)(fromX + dx * enter), (float)(fromY + dy * enter));
            else if (!pan && enter <= 1 && leave >= 1) (toX, toY) = ((float)(fromX + dx * leave), (float)(fromY + dy * leave));
        }
        return (toX, toY);
    }

    long PanSteps(float x, float y)
    {
        var (dx, dy) = (Math.Clamp(x, Bound(0, false), Bound(0, true)) - TargetX, Math.Clamp(y, Bound(1, false), Bound(1, true)) - TargetY);
        return Game.Steps(Math.Sqrt((double)dx * dx + (double)dy * dy) / PanSpeed);
    }

    // The eye: the distance back from the target along the angle of attack and the rotation, as the step before saw
    // them, in floats as the game's: 5 of the 6 eyes the probes read match to the last digit, the sixth by its cos(300).
    (float X, float Y, float Z) Eye()
    {
        var step = _game.Step - 1;
        var distance = _camera[0].At(step);
        var (attack, rotation) = ((float)Radians(Field(2, step)), (float)(Radians(Field(5, step)) + (_orbit is { } o ? o.Swept.At(step) : 0)));
        var (attackCos, attackSin, rotationCos, rotationSin) = ((float)Math.Cos(attack), (float)Math.Sin(attack), (float)Math.Cos(rotation), (float)Math.Sin(rotation));
        return (TargetX - distance * (attackCos * rotationCos), TargetY - distance * (attackCos * rotationSin), TargetZ - distance * attackSin);
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

    /// <summary>
    /// Puts the game camera's fields back but CAMERA_FIELD_ZABSOLUTE; the target stays, and the rotate mode, a follow and a
    /// pan's height end (units-camera, units-camera-2).
    /// </summary>
    [Native(Level.Close)]
    public void ResetToGameCamera(float duration)
    {
        Settle();
        _panZUntil = Math.Min(_panZUntil, _game.Step);
        for (var field = 0; field < CameraFields - 1; field++) SetCameraField(field, GameCamera[field], duration);
    }

    /// <summary>
    /// Moves the target there, held in the bounds' box, read from the next step; in the rotate mode it only stops the
    /// sweep (units-camera-2). Close: the game's lands after a frame, and at the game's start 0.2 to 0.25 s later
    /// (cameras-3); its bounds' hold isn't always the box (cameras).
    /// </summary>
    [Native(Level.Close)]
    public void SetCameraPosition(float x, float y)
    {
        if (_orbit is not null) StopSweep();
        else MoveCamera(x, y, 0);
    }

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

    /// <summary>
    /// Stops a pan where it is (cameras-2), its height going back to the z offset from the next step, and the rotate
    /// mode's sweep, the mode going on (units-camera-2); a field's change goes on (not measured).
    /// </summary>
    [Native(Level.Close)]
    public void StopCamera()
    {
        _panZUntil = Math.Min(_panZUntil, _game.Step);
        if (_orbit is not null) StopSweep();
        else (_cameraX, _cameraY) = (_cameraX.Toward(_cameraX.At(_game.Step - 1), _game.Step, 0), _cameraY.Toward(_cameraY.At(_game.Step - 1), _game.Step, 0));
    }

    /// <summary>
    /// The target goes round the point by the sweep, counterclockwise, over the time, the view turning with it while
    /// the rotation field reads as it was, without the z offset (cameras-4). The mode holds the target until a pan, a
    /// setup's pan or ResetToGameCamera: a position set or StopCamera only stops the sweep, and a unit controller is
    /// dropped (units-camera-2). Close: the game's turn is a frame or so ahead; a new rotate mode in one starting from
    /// where the target is is the model's.
    /// </summary>
    [Native(Level.Close)]
    public void SetCameraRotateMode(float x, float y, float radiansToSweep, float duration)
    {
        Settle();
        var (dx, dy) = (TargetX - x, TargetY - y);
        _orbit = (x, y, Math.Sqrt(dx * dx + dy * dy), Math.Atan2(dy, dx), new Motion(0).Toward(radiansToSweep, _game.Step, CameraSteps(duration)));
    }

    /// <summary>
    /// The target follows the unit at the offsets from the next step, and stays where it was once it is removed, until a
    /// position, a pan or ResetToGameCamera (units-camera), keeping a pan's height (units-camera-2); in the rotate mode
    /// it is dropped (units-camera-2), and a nil unit ends a follow (the model's). Close: the game follows a move a frame
    /// later. Neither inheritOrientation nor the unit's facing changes the rotation field (units-camera); what they turn
    /// in the view isn't simulated.
    /// </summary>
    [Native(Level.Close)]
    public void SetCameraTargetController(unit? whichUnit, float xoffset, float yoffset, bool inheritOrientation) => Follow(whichUnit, xoffset, yoffset);

    /// <summary>As SetCameraTargetController with these offsets: the target went to the unit at them (units-camera).</summary>
    [Native(Level.Close)]
    public void SetCameraOrientController(unit? whichUnit, float xoffset, float yoffset) => Follow(whichUnit, xoffset, yoffset);

    void Follow(unit? whichUnit, float x, float y)
    {
        if (_orbit is not null) return;
        Settle();
        if (whichUnit is not null) _follow = (whichUnit, x, y, _game.Step);
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

    /// <summary>
    /// Shakes the view. Close: the game's target and eye read shaken, here as they would be still: at a magnitude of 50,
    /// in three runs, the target read 0.1 to 2.8 off and the eye 0.3 to 5.6 (cameras-4).
    /// </summary>
    [Native(Level.Close)]
    public void CameraSetSourceNoise(float mag, float velocity) { }

    /// <summary>As CameraSetSourceNoise (not measured apart).</summary>
    [Native(Level.Close)]
    public void CameraSetSourceNoiseEx(float mag, float velocity, bool vertOnly) { }

    /// <summary>As CameraSetSourceNoise.</summary>
    [Native(Level.Close)]
    public void CameraSetTargetNoise(float mag, float velocity) { }

    /// <summary>As CameraSetSourceNoise (not measured apart).</summary>
    [Native(Level.Close)]
    public void CameraSetTargetNoiseEx(float mag, float velocity, bool vertOnly) { }

    [Native(Level.Recorded, Reason = "Smooths the view's moves: at a factor of 1 a pan's target read as without it (units-camera-2)")]
    public void CameraSetSmoothingFactor(float factor) { }

    /// <summary>Sets CAMERA_FIELD_DEPTH_OF_FIELD_DISTANCE at once; below 0 it is held at 0 as the field is (inferred).</summary>
    [Native(Level.Exact, Fact = Cameras5)]
    public void CameraSetFocalDistance(float distance) => SetCameraField(11, distance, 0);

    /// <summary>Sets CAMERA_FIELD_DEPTH_OF_FIELD_SCALE at once; below 0 it is held at 0 as the field is (inferred).</summary>
    [Native(Level.Exact, Fact = Cameras5)]
    public void CameraSetDepthOfFieldScale(float scale) => SetCameraField(12, scale, 0);

    /// <summary>
    /// Close: while a cinematic camera plays, the game's target and eye read the model's camera (CameraZoomOut: the
    /// target about 110 to 120 up, the eye 200 to 250 from it), here the game camera's; the fields read as they were and
    /// ResetToGameCamera ends it, in both (units-camera-2). Reading the models' cameras is later.md's.
    /// </summary>
    [Native(Level.Close)]
    public void SetCinematicCamera(string? cameraModelFile) { }

    /// <summary>
    /// The rect keeps the target out: a pan stops where its path enters it, a position set inside it lands past it along
    /// the move (units-camera-2). Close: the two cases measured, with the rect as it is when the target moves.
    /// </summary>
    [Native(Level.Close)]
    public void AddCameraBlocker(rect? where)
    {
        if (where is not null && !_blockers.Contains(where)) _blockers.Add(where);
    }

    /// <summary>Turns an added blocker off and on again; off, the target goes in (units-camera-2).</summary>
    [Native(Level.Close)]
    public void EnableCameraBlocker(rect? r, bool flag)
    {
        if (r is null) return;
        if (flag) _blockersOff.Remove(r);
        else _blockersOff.Add(r);
    }
}
