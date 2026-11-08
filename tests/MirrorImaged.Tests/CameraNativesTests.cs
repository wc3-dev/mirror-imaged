using MirrorImaged.Data;
using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>
/// The local player's camera and the fog flags, against the probes cameras to cameras-4 and units-camera (docs/facts.md,
/// "Cameras").
/// </summary>
public sealed class CameraNativesTests
{
    readonly Game _game = new(0);
    readonly GameNatives _natives;

    public CameraNativesTests()
    {
        _natives = new GameNatives(_game);
        Bounds(-2000, 2000);
    }

    camerafield Field(int i) => _natives.ConvertCameraField(i)!;

    float Get(int i) => _natives.GetCameraField(Field(i));

    void Set(int i, float value, float duration = 0) => _natives.SetCameraField(Field(i), value, duration);

    void Advance(double seconds) => _game.Advance(Game.Steps(seconds));

    void Bounds(float min, float max) => _natives.SetCameraBounds(min, min, max, max, min, max, max, min);

    (float, float) Target => (_natives.GetCameraTargetPositionX(), _natives.GetCameraTargetPositionY());

    (float, float, float) Eye => (_natives.GetCameraEyePositionX(), _natives.GetCameraEyePositionY(), _natives.GetCameraEyePositionZ());

    [Fact]
    public void The_margins_are_512_left_and_right_and_256_top_and_bottom()
    {
        Assert.Equal([512f, 512f, 256f, 256f, 0f, 0f], new[] { 0, 1, 2, 3, 4, -1 }.Select(_natives.GetCameraMargin));
    }

    // cameras: no bounds until the script sets them, which holds the target at (0, 0)
    [Fact]
    public void The_bounds_start_at_0_and_hold_the_target_there()
    {
        var natives = new GameNatives(_game);

        Assert.Equal([0f, 0f, 0f, 0f], [natives.GetCameraBoundMinX(), natives.GetCameraBoundMinY(), natives.GetCameraBoundMaxX(), natives.GetCameraBoundMaxY()]);
        natives.SetCameraPosition(500, 300);
        Advance(0.5);
        Assert.Equal((0f, 0f), (natives.GetCameraTargetPositionX(), natives.GetCameraTargetPositionY()));
    }

    // cameras: the bounds read the least and most of the four corners, in any order, and aren't cut to the map
    [Theory]
    [InlineData(-1000, -800, 1200, 900, -1000, 900, 1200, -800, -1000, -800, 1200, 900)]
    [InlineData(-500, -600, 700, 800, -900, 400, 300, -1000, -900, -1000, 700, 800)]
    [InlineData(1200, 900, -1000, -800, 1200, -800, -1000, 900, -1000, -800, 1200, 900)]
    [InlineData(-100000, -100000, 100000, 100000, -100000, 100000, 100000, -100000, -100000, -100000, 100000, 100000)]
    public void The_bounds_read_the_least_and_most_corner(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4,
        float minX, float minY, float maxX, float maxY)
    {
        _natives.SetCameraBounds(x1, y1, x2, y2, x3, y3, x4, y4);

        Assert.Equal([minX, minY, maxX, maxY], [_natives.GetCameraBoundMinX(), _natives.GetCameraBoundMinY(), _natives.GetCameraBoundMaxX(), _natives.GetCameraBoundMaxY()]);
    }

    // cameras: the game camera's fields, the angles in radians, a zero angle as -0
    [Fact]
    public void The_fields_start_as_the_game_camera()
    {
        var fields = Enumerable.Range(0, 14).Select(Get).ToList();

        Assert.Equal([2000, 5300, 5.3058, 1.2217, 0, 1.5708, 10, 16, 0, 0, 0, 0, 0, 0], fields.Select(f => Math.Round(f, 4)));
        Assert.All(new[] { 4, 8, 9, 10 }, i => Assert.True(float.IsNegative(fields[i])));
        Assert.All(new[] { 11, 12, 13 }, i => Assert.False(float.IsNegative(fields[i])));
    }

    // cameras-2: each field's limits, read at once
    [Theory]
    [InlineData(0, new[] { 0, 0.3f, 0.5f, 1, 100, 100000 }, new[] { 0.5f, 0.5f, 0.5f, 1, 100, 10000 })]
    [InlineData(1, new[] { 0, 50, 99.9f, 100, 101, 100000 }, new[] { 100f, 100, 100, 100, 101, 100000 })]
    [InlineData(6, new[] { -100000f, 100000, 0, -50 }, new[] { -4096f, 4096, 0, -50 })]
    [InlineData(7, new[] { 0, 5, 15.9f, 16, 17, 100, 100000 }, new[] { 16f, 16, 16, 16, 17, 100, 9500 })]
    [InlineData(11, new[] { -100f, 500 }, new[] { 0f, 500 })]
    [InlineData(12, new[] { -2f, 2 }, new[] { 0f, 2 })]
    [InlineData(13, new[] { 0.5f, -1, 2 }, new[] { 0.5f, -1, 2 })]
    public void A_field_is_held_in_its_limits(int field, float[] set, float[] read)
    {
        Assert.Equal(read, set.Select(value =>
        {
            Set(field, value);
            return Get(field);
        }));
    }

    // cameras and cameras-2: an angle in degrees comes round to a turn; whole turns read 2π above 0 and -0 below; the
    // field of view comes round, then is held from 5 to 120 degrees
    [Theory]
    [InlineData(2, 0, -0.0)]
    [InlineData(2, 0.0001, 0.0)]
    [InlineData(2, 359.9999, 6.2832)]
    [InlineData(2, 360, 6.2832)]
    [InlineData(2, 720, 6.2832)]
    [InlineData(2, 1080, 6.2832)]
    [InlineData(2, -360, -0.0)]
    [InlineData(2, -720, -0.0)]
    [InlineData(2, 400, 0.6981)]
    [InlineData(4, -30, 5.7596)]
    [InlineData(4, 361, 0.0175)]
    [InlineData(5, 450, 1.5708)]
    [InlineData(9, -6, 6.1785)]
    [InlineData(3, 0, 0.0873)]
    [InlineData(3, 19.9, 0.3473)]
    [InlineData(3, 119, 2.0769)]
    [InlineData(3, 121, 2.0944)]
    [InlineData(3, 200, 2.0944)]
    [InlineData(3, 361, 0.0873)]
    public void An_angle_comes_round_to_a_turn(int field, float degrees, double radians)
    {
        Set(field, degrees);

        Assert.Equal(radians, Math.Round(Get(field), 4));
        Assert.Equal(double.IsNegative(radians), float.IsNegative(Get(field)));
    }

    // cameras-2: a field set over a time moves there in a straight line from where it is, from the next step
    [Fact]
    public void A_field_set_over_a_time_moves_in_a_straight_line()
    {
        Set(0, 3000, 2);
        Assert.Equal(2000f, Get(0));
        Advance(0.5);
        Assert.Equal(2250f, Get(0), 0.01f);
        Advance(0.5);
        Set(0, 1000, 2);
        Advance(1);
        Assert.Equal(1750f, Get(0), 0.01f);
        Advance(1.5);
        Assert.Equal(1000f, Get(0));
    }

    // cameras-4: an angle over a time goes the shorter way round
    [Fact]
    public void An_angle_over_a_time_goes_the_shorter_way_round()
    {
        Set(5, 350);
        Set(5, 10, 1);
        Advance(0.25);
        Assert.Equal(6.1959, Math.Round(Get(5), 4));
        Advance(0.5);
        Assert.Equal(0.0873, Math.Round(Get(5), 4));
        Advance(0.25);
        Assert.Equal(0.1745, Math.Round(Get(5), 4));
    }

    // review: a quarter turn at a time over a time goes a quarter of the way round each time, set or adjusted, through
    // any number of turns
    [Fact]
    public void Quarter_turns_over_a_time_keep_going_the_same_way()
    {
        for (var turn = 1; turn <= 12; turn++)
        {
            Set(5, 90 + 90 * turn, 1);
            Advance(0.5);
            Assert.Equal(Turn(45 + 90 * turn), Degrees(Get(5)), 0.01);
            Advance(0.5);
        }
        for (var turn = 1; turn <= 12; turn++)
        {
            _natives.AdjustCameraField(Field(5), 90, 1);
            Advance(0.5);
            Assert.Equal(Turn(45 + 90 * turn), Degrees(Get(5)), 0.01);
            Advance(0.5);
        }

        static double Turn(double degrees) => (degrees % 360 + 360) % 360;
        static double Degrees(float radians) => radians * 180 / Math.PI % 360;
    }

    // cameras-4: the rotate mode takes the target round the point, the view turning with it while the rotation field
    // reads as it was, without the z offset until the game camera is put back
    [Fact]
    public void The_rotate_mode_takes_the_target_round_the_point()
    {
        _natives.SetCameraRotateMode(500, 0, MathF.PI / 2, 2);
        Advance(1);
        Assert.Equal((146.4466f, -353.5534f), Target, Near);
        Advance(1.5);

        Assert.Equal((500f, -500f), Target, Near);
        Assert.Equal((1.5708, 0f), (Math.Round(Get(5), 4), _natives.GetCameraTargetPositionZ()));
        Assert.Equal((1618.3857f, -500f, 1658.0752f), Eye, Within(0.01f));
        _natives.ResetToGameCamera(0);
        Advance(0.5);
        Assert.Equal((500f, -500f, 10f), (_natives.GetCameraTargetPositionX(), _natives.GetCameraTargetPositionY(), _natives.GetCameraTargetPositionZ()), Within(0.01f));
    }

    // units-camera-2: in the rotate mode a position set or StopCamera stops the sweep and the mode holds the target; a
    // pan ends it
    [Fact]
    public void The_rotate_mode_holds_the_target_until_a_pan()
    {
        _natives.SetCameraRotateMode(500, 0, MathF.PI / 2, 1);
        Advance(0.5);
        _natives.SetCameraPosition(-300, 200);
        var stopped = Target;
        Advance(1);
        Assert.Equal(stopped, Target);
        Assert.Equal(0f, _natives.GetCameraTargetPositionZ());

        _natives.StopCamera();
        _natives.SetCameraPosition(-300, 200);
        Advance(0.5);
        Assert.Equal(stopped, Target);

        _natives.PanCameraToTimed(300, 300, 0);
        Advance(0.5);
        Assert.Equal((300f, 300f, 10f), (_natives.GetCameraTargetPositionX(), _natives.GetCameraTargetPositionY(), _natives.GetCameraTargetPositionZ()));
    }

    // units-camera: the target follows a unit at the offsets from the next step, stays where it was once the unit is
    // removed, and a pan or a position ends the follow
    [Fact]
    public void The_target_follows_a_unit_until_a_pan()
    {
        var natives = new GameNatives(_game) { Data = new GameData(new StandardData().Add(ObjectKind.Unit, "hfoo", new Dictionary<string, string> { ["uhpm"] = "420" })) };
        natives.SetCameraBounds(-2000, -2000, 2000, 2000, -2000, 2000, 2000, -2000);
        var footman = natives.CreateUnit(natives.Player(0), 0x68666F6F, 200, 100, 90);
        (float, float) Target() => (natives.GetCameraTargetPositionX(), natives.GetCameraTargetPositionY());

        natives.SetCameraTargetController(footman, 100, 50, false);
        Assert.Equal((0f, 0f), Target());
        _game.Advance(1);
        Assert.Equal((300f, 150f), Target());
        natives.SetUnitX(footman, 600);
        Advance(0.5);
        Assert.Equal((700f, 150f), Target());

        natives.RemoveUnit(footman);
        Advance(0.5);
        Assert.Equal((700f, 150f), Target());
        natives.PanCameraToTimed(0, 0, 0);
        Advance(0.5);
        Assert.Equal((0f, 0f), Target());

        // units-camera-2: a unit controller in the rotate mode is dropped
        natives.SetCameraRotateMode(500, 0, MathF.PI / 2, 1);
        Advance(0.5);
        natives.SetCameraTargetController(footman, 0, 0, false);
        Advance(1);
        Assert.Equal((500f, -500f), Target(), Near);
    }

    // units-camera-2: StopCamera and ResetToGameCamera drop a pan's height from the next step; a follow keeps it
    [Fact]
    public void A_pans_height_ends_on_a_stop_or_a_reset()
    {
        float Z() => _natives.GetCameraTargetPositionZ();

        _natives.PanCameraToTimedWithZ(1000, 0, 410, 2);
        Advance(1);
        _natives.StopCamera();
        Assert.Equal(210f, Z(), 1f);
        _game.Advance(1);
        Assert.Equal(10f, Z());

        _natives.PanCameraToTimedWithZ(500, 500, 300, 0.5f);
        Advance(1);
        _natives.ResetToGameCamera(0);
        Advance(0.5);
        Assert.Equal(10f, Z());
    }

    // units-camera-2: a blocker stops a pan where it enters it, a position set inside it lands past it, and off it lets
    // the target in
    [Fact]
    public void A_camera_blocker_keeps_the_target_out()
    {
        var blocker = _natives.Rect(200, -200, 600, 200);
        _natives.AddCameraBlocker(blocker);

        _natives.SetCameraPosition(400, 0);
        Advance(0.5);
        Assert.Equal((600f, 0f), Target);
        _natives.SetCameraPosition(0, 0);
        Advance(0.5);
        _natives.PanCameraToTimed(1000, 0, 1);
        Advance(1.5);
        Assert.Equal((200f, 0f), Target);

        _natives.EnableCameraBlocker(blocker, false);
        _natives.SetCameraPosition(400, 0);
        Advance(0.5);
        Assert.Equal((400f, 0f), Target);
    }

    // units-camera-2: the focal distance and depth of field scale are the depth of field fields
    [Fact]
    public void The_depth_of_field_natives_set_their_fields()
    {
        _natives.CameraSetFocalDistance(500);
        _natives.CameraSetDepthOfFieldScale(2);

        Assert.Equal((500f, 2f), (Get(11), Get(12)));
    }

    // cameras, cameras-2: AdjustCameraField adds to the field, and ResetToGameCamera puts back all but ZABSOLUTE
    [Fact]
    public void Adjusting_adds_and_resetting_puts_the_game_camera_back()
    {
        _natives.AdjustCameraField(Field(0), 100, 0);
        _natives.AdjustCameraField(Field(5), 30, 0);
        Assert.Equal((2100f, 2.0944), (Get(0), Math.Round(Get(5), 4)));

        Set(13, 2);
        Set(3, 90);
        _natives.ResetToGameCamera(2);
        Assert.Equal(2100f, Get(0));
        Advance(1);
        Assert.Equal(2050f, Get(0), 0.01f);
        Advance(1);
        Assert.Equal([2000, 5300, 1.2217, 1.5708, 2], new[] { 0, 1, 3, 5, 13 }.Select(i => Math.Round(Get(i), 4)));
    }

    // cameras-2, cameras-3: a position set reads in the next step, held in the bounds' box whatever the rotation
    [Fact]
    public void A_position_set_reads_from_the_next_step_held_in_the_bounds()
    {
        _natives.SetCameraPosition(500, 300);
        Assert.Equal((0f, 0f), Target);
        _game.Advance(1);
        Assert.Equal((500f, 300f), Target);

        Bounds(-200, 200);
        Set(5, 120);
        _natives.SetCameraPosition(1000, 1000);
        Advance(0.5);
        Assert.Equal((200f, 200f), Target);
        _natives.SetCameraBounds(0, -1000, 0, 1000, -1000, 0, 1000, 0);
        _natives.SetCameraPosition(900, 900);
        Advance(0.5);
        Assert.Equal((900f, 900f), Target);
    }

    // cameras-2, cameras-3: a timed pan moves in a straight line, PanCameraTo at 3000 a second, and StopCamera stops it
    [Fact]
    public void Pans_move_in_a_straight_line()
    {
        _natives.PanCameraToTimed(-600, 400, 2);
        Advance(1);
        Assert.Equal((-300f, 200f), Target, Near);
        Advance(1.5);
        Assert.Equal((-600f, 400f), Target);

        _natives.PanCameraTo(1400, 400);
        Advance(0.5);
        Assert.Equal((900f, 400f), Target, Near);
        _natives.StopCamera();
        Advance(1);
        Assert.Equal((900f, 400f), Target, Near);
    }

    static readonly IEqualityComparer<(float, float)> Near =
        EqualityComparer<(float, float)>.Create((a, b) => Math.Abs(a.Item1 - b.Item1) < 5 && Math.Abs(a.Item2 - b.Item2) < 5);

    // cameras-2, cameras-4: a pan with a z takes the target's height there with it, and the z offset field stays; a
    // position set puts it back; a pan that leaves the target where it is keeps its height (cameras)
    [Fact]
    public void A_pan_with_a_z_sets_the_targets_height()
    {
        Assert.Equal(10f, _natives.GetCameraTargetPositionZ());
        _natives.PanCameraToWithZ(0, 0, 200);
        Advance(1);
        Assert.Equal(10f, _natives.GetCameraTargetPositionZ());
        _natives.PanCameraToTimedWithZ(1000, 0, 210, 2);
        Advance(1);
        Assert.Equal(110f, _natives.GetCameraTargetPositionZ(), 0.5f);
        Advance(1.5);
        Assert.Equal((210f, 10f), (_natives.GetCameraTargetPositionZ(), Get(6)));

        _natives.SetCameraPosition(0, 0);
        Advance(0.5);
        Assert.Equal(10f, _natives.GetCameraTargetPositionZ());
    }

    // cameras, cameras-3: the eye is the distance back from the target along the angle of attack and the rotation, as
    // the step before saw them
    [Fact]
    public void The_eye_is_behind_the_target()
    {
        Assert.Equal((0f, -1118.3857f, 1668.0752f), Eye, Within(0.01f));

        Set(5, 45);
        Assert.Equal((0f, -1118.3857f, 1668.0752f), Eye, Within(0.01f));
        _game.Advance(1);
        Assert.Equal((-790.8182f, -790.8182f, 1668.0752f), Eye, Within(0.01f));
    }

    static IEqualityComparer<(float, float, float)> Within(float margin) => EqualityComparer<(float, float, float)>.Create((a, b) =>
        Math.Abs(a.Item1 - b.Item1) < margin && Math.Abs(a.Item2 - b.Item2) < margin && Math.Abs(a.Item3 - b.Item3) < margin);

    // cameras: a setup's defaults, and what it reads back, in degrees as set: lengths below 0 are 0
    [Fact]
    public void A_setup_reads_back_what_was_set()
    {
        var setup = _natives.CreateCameraSetup();
        float[] Fields() => [.. Enumerable.Range(0, 14).Select(i => _natives.CameraSetupGetField(setup, Field(i)))];

        Assert.Equal([3000, 5400, 304, 70, 0, 90, 0, 16, 0, 0, 0, 0, 0, 0], Fields());
        Assert.Equal((0f, 0f, ""), (_natives.CameraSetupGetDestPositionX(setup), _natives.CameraSetupGetDestPositionY(setup), _natives.BlzCameraSetupGetLabel(setup)));

        float[] odd = [-5, -1, 400, 200, -30, 450, -50, -16, 365, -6, 720, -100, -2, 2];
        for (var i = 0; i < 14; i++) _natives.CameraSetupSetField(setup, Field(i), odd[i], 1);
        _natives.CameraSetupSetDestPosition(setup, 123.5f, -456.25f, 3);
        _natives.BlzCameraSetupSetLabel(setup, "label");

        Assert.Equal([0, 0, 400, 200, -30, 450, -50, 0, 365, -6, 720, 0, 0, 2], Fields());
        Assert.Equal((123.5f, -456.25f, "label"), (_natives.CameraSetupGetDestPositionX(setup), _natives.CameraSetupGetDestPositionY(setup), _natives.BlzCameraSetupGetLabel(setup)));
    }

    // cameras-2, cameras-3: a setup applied sets every field over its own time; a pan goes to its place at 3000 a second,
    // timed over its place's time, or not at all
    [Fact]
    public void A_setup_applied_sets_its_fields_and_pans()
    {
        var setup = _natives.CreateCameraSetup()!;
        _natives.CameraSetupSetField(setup, Field(0), 3000, 2);
        _natives.CameraSetupSetDestPosition(setup, -800, 600, 2);

        _natives.CameraSetupApply(setup, false, false);
        Assert.Equal(2000f, Get(0));
        Advance(1);
        Assert.Equal(2500f, Get(0), 2f);
        Assert.Equal((0f, (0f, 0f)), (_natives.GetCameraTargetPositionZ(), Target));

        Set(0, 2000);
        _natives.CameraSetupApply(setup, true, true);
        Advance(1);
        Assert.Equal(2500f, Get(0), 2f);
        Assert.Equal((-400f, 300f), Target, Near);

        _natives.SetCameraPosition(0, 0);
        Advance(1);
        _natives.CameraSetupApply(setup, true, false);
        Advance(0.2);
        Assert.Equal((-480f, 360f), Target, Near);

        _natives.SetCameraPosition(0, 0);
        Set(0, 2000);
        Advance(1);
        _natives.CameraSetupApplyForceDuration(setup, true, 4);
        Advance(1);
        Assert.Equal(2250f, Get(0), 2f);
        Assert.Equal((-200f, 150f), Target, Near);
    }

    // cameras: which fields input controls, the camera type and the hotkey lock read back what was set
    [Fact]
    public void Flags_read_back_what_was_set()
    {
        Assert.Equal((true, 0, true), (_natives.GetCameraFieldControlledByInput(Field(5)), _natives.BlzCameraGetCameraType(), _natives.BlzGetCameraAllowsHotkeyTargetLock()));

        _natives.SetCameraFieldControlledByInput(Field(5), false);
        _natives.BlzCameraSetCameraType(1);
        _natives.BlzSetCameraAllowsHotkeyTargetLock(false);

        Assert.Equal((false, 1, false), (_natives.GetCameraFieldControlledByInput(Field(5)), _natives.BlzCameraGetCameraType(), _natives.BlzGetCameraAllowsHotkeyTargetLock()));
        Assert.True(_natives.GetCameraFieldControlledByInput(Field(0)));
    }

    // cameras-4: the fog and its mask start on, and read what was set
    [Fact]
    public void The_fog_flags_start_on_and_read_back_what_was_set()
    {
        Assert.Equal((true, true), (_natives.IsFogEnabled(), _natives.IsFogMaskEnabled()));

        _natives.FogEnable(false);
        _natives.FogMaskEnable(false);

        Assert.Equal((false, false), (_natives.IsFogEnabled(), _natives.IsFogMaskEnabled()));
    }
}
