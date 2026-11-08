namespace MirrorImaged.Tests;

/// <summary>
/// Regions and their enter and leave events against the game's lines of the probe units-regions (docs/facts.md,
/// "Regions"; probes/expected/units-regions.txt) and the facts' "Units".
/// </summary>
public sealed class RegionTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            {{GroupTests.Helpers}}
            local function s(v) return v and "1" or "0" end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: GroupTests.Standard());
        simulation.Advance(TimeSpan.FromSeconds(1));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // probe units-regions and game-facts: whole 32 cells from 0, a rect covering the cells its corners are in, so a max
    // edge on the grid takes the cell past it; clearing as adding; a swapped rect's corners in order; nil reads false
    [Fact]
    public void A_region_is_whole_32_cells()
    {
        Assert.Equal(["1100101", "1010", "001011", "00110", "100011", "111", "1000", "110", "0"], Run("""
            local r = CreateRegion()
            RegionAddCell(r, 10, 10)
            RegionAddCell(r, -10, -10)
            print(s(IsPointInRegion(r, 0, 0)) .. s(IsPointInRegion(r, 31.9, 31.9)) .. s(IsPointInRegion(r, 32, 0)) .. s(IsPointInRegion(r, -0.1, 0))
              .. s(IsPointInRegion(r, -32, -32)) .. s(IsPointInRegion(r, -32.1, -1)) .. s(IsPointInRegion(r, -0.1, -0.1)))
            local q = CreateRegion()
            RegionAddRect(q, Rect(100.5, 100.5, 300.5, 300.5))
            print(s(IsPointInRegion(q, 96, 96)) .. s(IsPointInRegion(q, 95.9, 200)) .. s(IsPointInRegion(q, 319.9, 319.9)) .. s(IsPointInRegion(q, 320, 200)))
            RegionClearRect(q, Rect(150, 150, 200, 200))
            print(s(IsPointInRegion(q, 140, 140)) .. s(IsPointInRegion(q, 128, 128)) .. s(IsPointInRegion(q, 127.9, 127.9))
              .. s(IsPointInRegion(q, 223.9, 223.9)) .. s(IsPointInRegion(q, 224, 224)) .. s(IsPointInRegion(q, 192, 100)))
            RegionClearCell(q, 300, 300)
            RegionAddCell(q, 160, 160)
            print(s(IsPointInRegion(q, 300, 300)) .. s(IsPointInRegion(q, 288, 288)) .. s(IsPointInRegion(q, 287.9, 287.9))
              .. s(IsPointInRegion(q, 160, 160)) .. s(IsPointInRegion(q, 140, 140)))
            local e = CreateRegion()
            RegionAddRect(e, Rect(0, 0, 256, 256))
            RegionClearRect(e, Rect(0, 0, 64, 64))
            print(s(IsPointInRegion(e, 287.9, 10)) .. s(IsPointInRegion(e, 288, 10)) .. s(IsPointInRegion(e, 70, 70))
              .. s(IsPointInRegion(e, 95.9, 10)) .. s(IsPointInRegion(e, 96, 10)) .. s(IsPointInRegion(e, 10, 96)))
            local w = CreateRegion()
            RegionAddRect(w, Rect(300, 300, 100, 100))
            print(s(IsPointInRegion(w, 200, 200)) .. s(IsPointInRegion(w, 96, 96)) .. s(IsPointInRegion(w, 300, 300)))
            local loc = Location(10, 10)
            print(s(IsLocationInRegion(r, loc)) .. s(IsLocationInRegion(r, nil)) .. s(IsLocationInRegion(nil, loc)) .. s(IsPointInRegion(nil, 0, 0)))
            RegionAddCellAtLoc(q, Location(-500, -500))
            RegionClearCellAtLoc(q, Location(110, 110))
            print(s(IsPointInRegion(q, -500, -500)) .. s(IsPointInRegion(q, -512, -512)) .. s(IsPointInRegion(q, 110, 110)))
            RemoveRegion(r)
            print(s(IsPointInRegion(r, 0, 0)))
            """));
    }

    // probe units-regions: a unit is in a region by its place against the cells as they are now, dead or removed in
    // this step too
    [Fact]
    public void A_unit_is_in_a_region_by_its_place()
    {
        Assert.Equal(["00100", "01", "1", "1"], Run("""
            local q, e, big = CreateRegion(), CreateRegion(), CreateRegion()
            RegionAddRect(q, Rect(100.5, 100.5, 300.5, 300.5))
            RegionAddRect(e, Rect(0, 0, 256, 256))
            RegionClearRect(e, Rect(0, 0, 64, 64))
            RegionAddRect(big, GetWorldBounds())
            local u = make(1, "hfoo", 10, 10)
            print(s(IsUnitInRegion(q, u)) .. s(IsUnitInRegion(e, u)) .. s(IsUnitInRegion(big, u)) .. s(IsUnitInRegion(nil, u)) .. s(IsUnitInRegion(e, nil)))
            RegionClearCell(e, 10, 10)
            RegionAddCell(q, 10, 10)
            print(s(IsUnitInRegion(e, u)) .. s(IsUnitInRegion(q, u)))
            KillUnit(u)
            print(s(IsUnitInRegion(q, u)))
            RemoveUnit(u)
            print(s(IsUnitInRegion(q, u)))
            """));
    }

    const string Watch = """
        local function watch(name, r, enter, filter)
          local t = CreateTrigger()
          if enter then TriggerRegisterEnterRegion(t, r, filter) else TriggerRegisterLeaveRegion(t, r, filter) end
          TriggerAddAction(t, function()
            local u = GetTriggerUnit()
            print((enter and "enter " or "leave ") .. name .. " " .. L(u) .. " at " .. math.floor(GetUnitX(u)) .. "," .. math.floor(GetUnitY(u))
              .. " " .. L(GetEnteringUnit()) .. "/" .. L(GetLeavingUnit()) .. "/" .. L(GetFilterUnit()) .. " event " .. GetHandleId(GetTriggerEventId())
              .. " region " .. tostring(GetTriggeringRegion() == r) .. " player " .. (GetTriggerPlayer() and GetPlayerId(GetTriggerPlayer()) or "nil")
              .. " type " .. (GetUnitTypeId(u) ~= 0 and "1" or "0"))
          end)
          return t
        end
        local A, B = CreateRegion(), CreateRegion()
        RegionAddRect(A, Rect(1000, 1000, 1200, 1200))
        RegionAddRect(B, Rect(1100, 1000, 1300, 1200))
        watch("B", B, true)
        watch("B", B, false)
        watch("A", A, true)
        watch("A", A, false)
        """;

    // probe units-regions: made in a region, moved in and out by SetUnitX and SetUnitY, made and removed: nothing fires
    // inside the calls; each registration fires its units after the code, at the place it took among what falls due in
    // the step when its first unit came (A's enter before the 0 s timer, B's after it), each unit read where it is then;
    // the responses
    [Fact]
    public void Each_registration_fires_its_units_after_the_code_at_its_place_in_the_step()
    {
        Assert.Equal([
            "made", "x", "y", "out", "removed",
            "enter A 2 at 1050,1050 2/nil/nil event 5 region true player nil type 1",
            "enter A 3 at 1500,1100 3/nil/nil event 5 region true player nil type 1",
            "enter A 4 at 1150,1100 4/nil/nil event 5 region true player nil type 1",
            "timer",
            "enter B 3 at 1500,1100 3/nil/nil event 5 region true player nil type 1",
            "enter B 4 at 1150,1100 4/nil/nil event 5 region true player nil type 1",
            "leave B 3 at 1500,1100 nil/3/nil event 6 region true player nil type 1",
            "leave A 3 at 1500,1100 nil/3/nil event 6 region true player nil type 1",
        ], Run(Watch + """
            local u = make(2, "hfoo", 1050, 1050)
            print("made")
            TimerStart(CreateTimer(), 0, false, function() print("timer") end)
            local v = make(3, "hfoo", 500, 500)
            SetUnitX(v, 1150)
            print("x")
            SetUnitY(v, 1100)
            print("y")
            SetUnitX(v, 1500)
            print("out")
            local w = make(4, "hfoo", 1150, 1100)
            RemoveUnit(w)
            print("removed")
            """));
    }

    // probe units-regions: SetUnitPosition fires, a dead unit moved fires, a hidden one too; moving within fires nothing,
    // nor does a death or a removal; a leave registration's units fire before an enter one's when its first came first
    [Fact]
    public void A_move_fires_for_dead_and_hidden_units_and_a_death_or_removal_fires_nothing()
    {
        Assert.Equal([
            "within", "position out", "position in", "killed", "dead out", "dead in", "hidden in", "removed inside",
            "leave A 2 at 1050,1050 nil/2/nil event 6 region true player nil type 1",
            "leave A 2 at 1050,1050 nil/2/nil event 6 region true player nil type 1",
            "enter A 2 at 1050,1050 2/nil/nil event 5 region true player nil type 1",
            "enter A 2 at 1050,1050 2/nil/nil event 5 region true player nil type 1",
            "enter A 3 at 1150,1100 3/nil/nil event 5 region true player nil type 1",
            "enter B 3 at 1150,1100 3/nil/nil event 5 region true player nil type 1",
        ], Run(Watch + """
            local u, v = make(2, "hfoo", 1050, 1050), make(3, "hfoo", 1500, 1100)
            TimerStart(CreateTimer(), 0.01, false, function()
              SetUnitX(u, 1060) print("within")
              SetUnitPosition(u, 1050, 700) print("position out")
              SetUnitPosition(u, 1050, 1050) print("position in")
              KillUnit(u) print("killed")
              SetUnitX(u, 500) print("dead out")
              SetUnitX(u, 1050) print("dead in")
              ShowUnit(v, false)
              SetUnitX(v, 1150) print("hidden in")
              ShowUnit(v, true)
              RemoveUnit(v) print("removed inside")
            end)
            """).Skip(1));
    }

    // probe units-regions: a cell cleared or added under a unit standing still fires nothing, then or when it moves
    // within; regions read places, not who came in
    [Fact]
    public void Cells_changed_under_a_unit_fire_nothing()
    {
        Assert.Equal([
            "enter A 5 at 1050,1150 5/nil/nil event 5 region true player nil type 1",
            "0", "moved in cleared", "1", "moved in added", "moved out of added", "x out",
            "leave A 6 at 700,1150 nil/6/nil event 6 region true player nil type 1",
            "leave A 5 at 1500,1150 nil/5/nil event 6 region true player nil type 1",
        ], Run(Watch + """
            local x, y = make(5, "hfoo", 1050, 1150), make(6, "hfoo", 500, 1150)
            TimerStart(CreateTimer(), 0.01, false, function()
              RegionClearRect(A, Rect(1000, 1100, 1090, 1200))
              print(s(IsUnitInRegion(A, x)))
              SetUnitX(x, 1040) print("moved in cleared")
              RegionAddRect(A, Rect(400, 1100, 600, 1200))
              print(s(IsUnitInRegion(A, y)))
              SetUnitX(y, 510) print("moved in added")
              SetUnitX(y, 700) print("moved out of added")
              RegionAddRect(A, Rect(1000, 1100, 1090, 1200))
              SetUnitX(x, 1500) print("x out")
            end)
            """));
    }

    // probe units-regions: the filter runs inside the call that moved the unit (CreateUnit, before the probe set the
    // unit's data, so GetUnitUserData read 0), with GetFilterUnit the unit and no event; each registration of a trigger
    // fires; a removed region's registrations fire nothing
    [Fact]
    public void The_filter_runs_inside_the_move_and_a_removed_region_fires_nothing()
    {
        Assert.Equal([
            "enter C 7 at -1100,1100 7/nil/nil event 5 region true player nil type 1",
            "enter C 8 at -1100,900 8/nil/nil event 5 region true player nil type 1",
            "enter C 7 at -1100,1100 7/nil/nil event 5 region true player nil type 1",
            "enter C 8 at -1100,900 8/nil/nil event 5 region true player nil type 1",
            "0/nil/nil/0,0/nil/nil/0",
            "enter A 9 at 1150,1100 9/nil/nil event 5 region true player nil type 1",
        ], Run(Watch + """
            local C = CreateRegion()
            RegionAddRect(C, Rect(-1200, 1000, -1000, 1200))
            local calls = {}
            local t = watch("C", C, true, Filter(function()
              calls[#calls + 1] = L(GetFilterUnit()) .. "/" .. L(GetTriggerUnit()) .. "/" .. L(GetEnteringUnit()) .. "/" .. GetHandleId(GetTriggerEventId())
              return GetUnitUserData(GetFilterUnit()) ~= 7
            end))
            TriggerRegisterEnterRegion(t, C, nil)
            local z1, z2 = make(7, "hfoo", -1100, 1100), make(8, "hfoo", -1100, 1150)
            local gone = make(9, "hfoo", 0, 0)
            RemoveRegion(B)
            SetUnitY(z2, 900)
            TimerStart(CreateTimer(), 0.01, false, function()
              print(table.concat(calls, ","))
              SetUnitX(gone, 1150)
              SetUnitY(gone, 1100)
            end)
            """));
    }

    // A trigger destroyed before its units come fires nothing (not measured)
    [Fact]
    public void A_trigger_destroyed_before_its_units_come_fires_nothing()
    {
        Assert.DoesNotContain(Run(Watch + """
            local d = watch("D", A, true)
            make(1, "hfoo", 1050, 1050)
            DestroyTrigger(d)
            """), line => line.StartsWith("enter D"));
    }
}
