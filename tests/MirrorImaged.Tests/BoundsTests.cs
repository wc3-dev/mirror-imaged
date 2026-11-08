namespace MirrorImaged.Tests;

/// <summary>
/// Rects, locations, start locations and the map's bounds against the game's lines of the probe units-bounds
/// (docs/facts.md, "Rects and locations"; probes/expected/units-bounds.txt), on the tests' map of 32 by 32 tiles, the
/// world -2048 to 2048 each way, as the probe map's.
/// </summary>
public sealed class BoundsTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f(v) return string.format("%.3f", v) end
            local function box(r) return f(GetRectMinX(r)) .. " " .. f(GetRectMinY(r)) .. " " .. f(GetRectMaxX(r)) .. " " .. f(GetRectMaxY(r)) .. " c " .. f(GetRectCenterX(r)) .. " " .. f(GetRectCenterY(r)) end
            local function place(u) return u == nil and "nil" or f(GetUnitX(u)) .. " " .. f(GetUnitY(u)) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: GroupTests.Standard());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // probe units-bounds: corners kept in order; Rect, SetRect and RectFromLoc hold each corner from the world's west
    // (south) edge to 32 short of its east (north) one, MoveRectTo and GetWorldBounds don't; a removed rect and nil read 0
    [Fact]
    public void A_rect_keeps_its_corners_in_order_held_in_the_world()
    {
        Assert.Equal([
            "10.000 20.000 30.000 40.000 c 20.000 30.000",
            "10.000 20.000 30.000 40.000 c 20.000 30.000",
            "0.100 0.200 0.200 0.700 c 0.150 0.450",
            "-2048.000 -1.000 2016.000 2016.000 c -16.000 1007.500",
            "-9.900 -1010.300 10.100 -990.300 c 0.100 -1000.300",
            "490.000 490.000 510.000 510.000 c 500.000 500.000",
            "5.000 6.000 7.000 8.000 c 6.000 7.000",
            "1.000 2.000 3.000 4.000 c 2.000 3.000",
            "-3.000 -4.000 -1.000 -2.000 c -2.000 -3.000",
            "0.000 0.000 0.000 0.000 c 0.000 0.000",
            "0.000 0.000 0.000 0.000 c 0.000 0.000",
            "2016.000 2016.000 2016.000 2016.000 c 2016.000 2016.000\t-2048.000 -2048.000 -2048.000 -2040.000 c -2048.000 -2044.000",
            "4950.000 4950.000 5050.000 5050.000 c 5000.000 5000.000",
            "-2048.000 0.000 2016.000 2016.000 c -16.000 1008.000\t5000.000",
            "-2048.000 -2048.000 2048.000 2048.000 c 0.000 0.000\tfalse\t1",
            "-1948.000 -1948.000 2148.000 2148.000 c 100.000 100.000",
            "-2048.000 -2048.000 2016.000 2016.000 c -16.000 -16.000",
        ], Run("""
            print(box(Rect(10, 20, 30, 40)))
            print(box(Rect(30, 40, 10, 20)))
            print(box(Rect(0.1, 0.2, 0.2, 0.7)))
            print(box(Rect(-3e38, -1, 3e38, 1e38)))
            local r = Rect(10, 20, 30, 40)
            MoveRectTo(r, 0.1, -1000.3)
            print(box(r))
            MoveRectToLoc(r, Location(500, 500))
            print(box(r))
            SetRect(r, 7, 8, 5, 6)
            print(box(r))
            SetRectFromLoc(r, Location(1, 2), Location(3, 4))
            print(box(r))
            print(box(RectFromLoc(Location(-1, -2), Location(-3, -4))))
            RemoveRect(r)
            print(box(r))
            print(box(nil))
            print(box(Rect(2020, 2020, 2040, 2040)), box(Rect(-2060, -2060, -2050, -2040)))
            local m = Rect(0, 0, 100, 100)
            MoveRectTo(m, 5000, 5000)
            print(box(m))
            print(box(RectFromLoc(Location(-5000, 0), Location(5000, 3000))), f(GetLocationX(Location(5000, -5000))))
            local w1, w2 = GetWorldBounds(), GetWorldBounds()
            print(box(w1), w1 == w2, GetHandleId(w2) - GetHandleId(w1))
            MoveRectTo(w1, 100, 100)
            print(box(w1))
            SetRect(w1, -2048, -2048, 2048, 2048)
            print(box(w1))
            """));
    }

    // probe units-bounds: locations anywhere; a removed one reads 0; GetUnitLoc and GetStartLocationLoc give a new one
    // each call, an undefined start location's (0, 0); DefineStartLocationLoc of nil defines nothing
    [Fact]
    public void Locations_are_places_anywhere_and_new_each_call()
    {
        Assert.Equal([
            "1.500 -2.500 0.000", "-7.000 8.000", "0.000 0.000", "0.000 0.000",
            "0.000 0.000\tfalse\tfalse\t0.000", "123.500 -45.250 123.500\t0.000\tfalse",
            "5000.000 -5000.000\tfalse\ttrue",
        ], Run("""
            local l = Location(1.5, -2.5)
            print(f(GetLocationX(l)) .. " " .. f(GetLocationY(l)) .. " " .. f(GetLocationZ(l)))
            MoveLocation(l, -7, 8)
            print(f(GetLocationX(l)) .. " " .. f(GetLocationY(l)))
            RemoveLocation(l)
            print(f(GetLocationX(l)) .. " " .. f(GetLocationY(l)))
            print(f(GetLocationX(nil)) .. " " .. f(GetLocationZ(nil)))
            local s0 = GetStartLocationLoc(0)
            print(f(GetLocationX(s0)) .. " " .. f(GetLocationY(s0)), s0 == GetStartLocationLoc(0), GetStartLocationLoc(5) == nil, f(GetLocationX(GetStartLocationLoc(5))))
            DefineStartLocationLoc(3, Location(123.5, -45.25))
            DefineStartLocationLoc(4, nil)
            print(f(GetStartLocationX(3)) .. " " .. f(GetStartLocationY(3)) .. " " .. f(GetLocationX(GetStartLocationLoc(3))), f(GetStartLocationX(4)), GetStartLocationLoc(4) == nil)
            local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            SetUnitX(u, 5000)
            SetUnitY(u, -5000)
            local ul = GetUnitLoc(u)
            print(f(GetLocationX(ul)) .. " " .. f(GetLocationY(ul)), ul == GetUnitLoc(u), GetUnitLoc(nil) == nil)
            """));
    }

    // probe units-bounds: a unit made or put off the world is held in it as a rect's corner is, exactly so for a flier;
    // SetUnitX and SetUnitY aren't held; nil places nothing
    [Fact]
    public void A_unit_made_or_put_off_the_world_is_held_in_it()
    {
        Assert.Equal([
            "2016.000 -2048.000, -2048.000 2016.000, 2016.000 2016.000, -2048.000 -2048.000, 1500.000 0.000, 0.000 1500.000, -1500.000 -1700.000, 2016.000 0.000, 0.000 -2040.000, 2016.000 100.000",
            "2016.000 -2048.000, -2048.000 2016.000, 2016.000 2016.000, -2048.000 -2048.000, 1500.000 0.000, 0.000 1500.000, -1500.000 -1700.000, 2016.000 0.000, 0.000 -2040.000, 2016.000 100.000",
            "2016.000 -2048.000\tnil\t-1000.500 800.250", "-300.500 400.250", "-300.500 400.250", "5000.000 -5000.000",
        ], Run("""
            local asked = { { 5000, -5000 }, { -5000, 5000 }, { 5000, 5000 }, { -5000, -5000 }, { 1500, 0 }, { 0, 1500 }, { -1500, -1700 },
              { 2040, 0 }, { 0, -2040 }, { 2100, 100 } }
            local made, moved = {}, {}
            local u = CreateUnit(Player(0), FourCC("hgry"), 0, 0, 0)
            for _, p in ipairs(asked) do
              local v = CreateUnit(Player(0), FourCC("hgry"), p[1], p[2], 0)
              made[#made + 1] = place(v)
              RemoveUnit(v)
              SetUnitPosition(u, p[1], p[2])
              moved[#moved + 1] = place(u)
            end
            print(table.concat(made, ", "))
            print(table.concat(moved, ", "))
            print(place(CreateUnitAtLoc(Player(0), FourCC("hgry"), Location(5000, -5000), 0)), place(CreateUnitAtLoc(Player(0), FourCC("hgry"), nil, 0)),
              place(CreateUnitAtLoc(Player(0), FourCC("hgry"), Location(-1000.5, 800.25), 90)))
            SetUnitPositionLoc(u, Location(-300.5, 400.25))
            print(place(u))
            SetUnitPositionLoc(u, nil)
            print(place(u))
            SetUnitX(u, 5000)
            SetUnitY(u, -5000)
            print(place(u))
            """));
    }
}
