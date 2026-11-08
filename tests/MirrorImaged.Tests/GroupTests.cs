using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// Groups and the unit enumerations against the game's lines of the probes units-groups and units-order
/// (docs/facts.md, "Groups"; probes/expected), on maps with the data the test gives: the units' collision sizes.
/// </summary>
public sealed class GroupTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    internal static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, collision) in new[] { ("hfoo", "31"), ("Hpal", "32"), ("hpea", "16"), ("hgry", "8"), ("hkni", "32") })
            data.Add(ObjectKind.Unit, id, new Dictionary<string, string>
            {
                ["uhpm"] = "420", ["ucol"] = collision, ["umvt"] = id == "hgry" ? "fly" : "foot", ["udtm"] = "3.04", ["udea"] = "3",
            });
        data.Constants["DecayTime"] = "2";
        data.Constants["BoneDecayTime"] = "88";
        return data;
    }

    // Lua helpers: L labels a unit by its user data, make makes one labelled, at lists a group by index, each by ForGroup
    internal const string Helpers = """
        local function L(u) return u == nil and "nil" or tostring(GetUnitUserData(u)) end
        local function make(n, id, x, y, p)
          local u = CreateUnit(Player(p or 0), FourCC(id), x, y, 0)
          SetUnitUserData(u, n)
          return u
        end
        local function at(g)
          local t = {}
          for i = 0, BlzGroupGetSize(g) - 1 do t[#t + 1] = L(BlzGroupUnitAt(g, i)) end
          return table.concat(t, ",")
        end
        local function each(g)
          local t = {}
          ForGroup(g, function() t[#t + 1] = L(GetEnumUnit()) end)
          return table.concat(t, ",")
        end
        """;

    List<string> Run(string main, int seconds = 1)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            {{Helpers}}
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // probe units-groups: adding, taking out, the size, the index, nil and no group; a group keeps its own order (here
    // the order made), not the order added
    [Fact]
    public void A_group_holds_each_unit_once_in_an_order_of_its_own()
    {
        Assert.Equal([
            "true\ttrue\ttrue\tfalse\tfalse\tfalse",
            "3\t1,3,4\t1,3,4\t1\tnil\tnil\t0\tnil",
            "true\tfalse\tfalse\tfalse",
            "true\tfalse\tfalse\t3,4",
            "1,3,4,5,6",
            "2\t1,2,3,4,5,6\t3,4,5,6",
            "2\t1,3,4,6",
            "0\t0",
            "0\tnil",
            "0\tnil\tfalse\tfalse",
        ], Run("""
            local u = {}
            for n = 1, 7 do u[n] = make(n, "hfoo", n * 150, 0) end
            local g = CreateGroup()
            print(GroupAddUnit(g, u[3]), GroupAddUnit(g, u[1]), GroupAddUnit(g, u[4]), GroupAddUnit(g, u[1]), GroupAddUnit(g, nil), GroupAddUnit(nil, u[1]))
            print(BlzGroupGetSize(g), at(g), each(g), L(FirstOfGroup(g)), L(BlzGroupUnitAt(g, -1)), L(BlzGroupUnitAt(g, 3)), BlzGroupGetSize(nil), L(FirstOfGroup(nil)))
            print(IsUnitInGroup(u[1], g), IsUnitInGroup(u[2], g), IsUnitInGroup(nil, g), IsUnitInGroup(u[1], nil))
            print(GroupRemoveUnit(g, u[1]), GroupRemoveUnit(g, u[1]), GroupRemoveUnit(g, nil), at(g))
            for n = 6, 1, -1 do GroupAddUnit(g, u[n]) end
            GroupRemoveUnit(g, u[2])
            print(at(g))
            local h = CreateGroup()
            for n = 3, 6 do GroupAddUnit(h, u[n]) end
            GroupClear(g)
            for n = 1, 4 do GroupAddUnit(g, u[n]) end
            print(BlzGroupAddGroupFast(h, g), at(g), at(h))
            GroupClear(h)
            for _, n in ipairs({ 2, 7, 5 }) do GroupAddUnit(h, u[n]) end
            print(BlzGroupRemoveGroupFast(h, g), at(g))
            print(BlzGroupAddGroupFast(nil, g), BlzGroupAddGroupFast(h, nil))
            GroupClear(g)
            print(BlzGroupGetSize(g), L(FirstOfGroup(g)))
            GroupAddUnit(g, u[1])
            DestroyGroup(g)
            print(BlzGroupGetSize(g), L(FirstOfGroup(g)), GroupAddUnit(g, u[2]), IsUnitInGroup(u[1], g))
            """));
    }

    // probe units-groups: ForGroup visits the units the group held as it started, one taken out meanwhile too, not one
    // added; GetEnumUnit is the outer unit again after a nested ForGroup, and nil outside
    [Fact]
    public void ForGroup_visits_the_units_the_group_held_as_it_started()
    {
        Assert.Equal(["1,2,3,4\t1,2,4,5", "1,2,4,5\t0", "1/1,in6,in7,2/2,3/3\tnil"], Run("""
            local u = {}
            for n = 1, 7 do u[n] = make(n, "hfoo", n * 150, 0) end
            local g = CreateGroup()
            for n = 1, 4 do GroupAddUnit(g, u[n]) end
            local seen = {}
            ForGroup(g, function()
              seen[#seen + 1] = L(GetEnumUnit())
              if #seen == 1 then GroupRemoveUnit(g, u[3]) GroupAddUnit(g, u[5]) end
            end)
            print(table.concat(seen, ","), at(g))
            seen = {}
            ForGroup(g, function()
              seen[#seen + 1] = L(GetEnumUnit())
              if #seen == 1 then GroupClear(g) end
            end)
            print(table.concat(seen, ","), BlzGroupGetSize(g))
            for _, n in ipairs({ 1, 2, 3 }) do GroupAddUnit(g, u[n]) end
            local h = CreateGroup()
            GroupAddUnit(h, u[6])
            GroupAddUnit(h, u[7])
            seen = {}
            ForGroup(g, function()
              local outer = L(GetEnumUnit())
              if outer == "2" then ForGroup(h, function() seen[#seen + 1] = "in" .. L(GetEnumUnit()) end) end
              seen[#seen + 1] = outer .. "/" .. L(GetEnumUnit())
            end)
            print(table.concat(seen, ","), L(GetEnumUnit()))
            """));
    }

    // probe units-groups: a dead unit stays and is visited; a removed or exploded unit reads as before for the rest of
    // its step, then stays in the group, counted, but reads nil there, FirstOfGroup nil while it is first, and ForGroup
    // passes it over, until the group is cleared
    [Fact]
    public void A_unit_gone_stays_in_its_groups_as_nil_until_they_are_cleared()
    {
        Assert.Equal([
            "1,2,3,4\t1,2,3,4\tfalse",
            "4\t1,2,3,4\t1,2,3,4\t1\t8,9\t8",
            "4\tnil,nil,3,4\tnil\ttrue",
            "3,4\t4\tnil,nil,3,4",
            "2\tnil,9\tnil\t9\t2",
            "5\tnil,nil,3,4,5",
            "true\t4\tnil,nil,4,5",
            "\t4",
            "0",
            "nil,nil,3,4",
        ], Run("""
            local u = {}
            for n = 1, 5 do u[n] = make(n, "hfoo", n * 150, 0) end
            local dead, removed, first, exploded = CreateGroup(), CreateGroup(), CreateGroup(), CreateGroup()
            for n = 1, 4 do GroupAddUnit(dead, u[n]) GroupAddUnit(removed, u[n]) GroupAddUnit(first, u[n]) end
            local e1, e2 = make(8, "hfoo", 0, 300), make(9, "hfoo", 200, 300)
            GroupAddUnit(exploded, e1)
            GroupAddUnit(exploded, e2)
            KillUnit(u[4])
            print(at(dead), each(dead), UnitAlive(u[4]))
            RemoveUnit(u[2])
            RemoveUnit(u[1])
            SetUnitExploded(e1, true)
            KillUnit(e1)
            print(BlzGroupGetSize(first), at(first), each(first), L(FirstOfGroup(first)), at(exploded), L(FirstOfGroup(exploded)))
            TimerStart(CreateTimer(), 0, false, function()
              print(BlzGroupGetSize(first), at(first), L(FirstOfGroup(first)), IsUnitInGroup(u[3], first))
              print(each(first), BlzGroupGetSize(first), at(first))
              print(BlzGroupGetSize(exploded), at(exploded), L(FirstOfGroup(exploded)), each(exploded), BlzGroupGetSize(exploded))
              GroupAddUnit(removed, u[5])
              print(BlzGroupGetSize(removed), at(removed))
              print(GroupRemoveUnit(removed, u[3]), BlzGroupGetSize(removed), at(removed))
              local seen = {}
              while FirstOfGroup(removed) do
                seen[#seen + 1] = L(FirstOfGroup(removed))
                GroupRemoveUnit(removed, FirstOfGroup(removed))
              end
              print(table.concat(seen, ","), BlzGroupGetSize(removed))
              GroupClear(removed)
              print(BlzGroupGetSize(removed))
              print(at(dead))
            end)
            """));
    }

    // The probe's units 21 to 36 (units-groups): players 5 and 6, a hidden peasant (34), a dead footman (35) and one
    // removed in this step (36)
    const string Spread = """
        local made = {}
        for _, p in ipairs({
          { 21, "hfoo", 300, 300, 5 }, { 22, "hfoo", -300, -300, 5 }, { 23, "hfoo", 10, 10, 5 }, { 24, "hfoo", 100, 100, 5 },
          { 25, "hfoo", -300, 300, 5 }, { 26, "hfoo", 300, -300, 5 }, { 27, "hfoo", 600, 10, 5 }, { 28, "hfoo", 10, 600, 5 },
          { 29, "hfoo", 150, 10, 5 }, { 30, "hfoo", -600, -10, 5 }, { 31, "hfoo", 500, 500, 6 }, { 32, "hfoo", -500, -500, 6 },
          { 33, "Hpal", 700, -700, 5 }, { 34, "hpea", -700, 700, 5 }, { 35, "hfoo", -650, -650, 5 }, { 36, "hfoo", 650, 650, 5 },
        }) do made[p[1]] = make(p[1], p[2], p[3], p[4], p[5]) end
        ShowUnit(made[34], false)
        KillUnit(made[35])
        RemoveUnit(made[36])
        local g, r = CreateGroup(), Rect(-1000, -1000, 1000, 1000)
        local calls, inside = {}, nil
        local odd = Filter(function()
          calls[#calls + 1] = L(GetFilterUnit())
          inside = inside or L(GetFilterUnit()) .. "/" .. L(GetEnumUnit()) .. "/" .. L(GetTriggerUnit())
          return GetUnitUserData(GetFilterUnit()) % 2 == 1
        end)
        local function called() local c = table.concat(calls, ",") calls = {} return c end

        """;

    // probe units-groups: by place, neither a hidden unit nor one removed in this step, but a dead one, offered to the
    // filter in the grid's order; of a player or a type, every unit, newest first; each enumeration empties its group
    // first, and a nil rect leaves it as it is; the counts are passed over
    [Fact]
    public void The_enumerations_take_the_units_the_game_takes_in_its_order()
    {
        Assert.Equal([
            "21,23,25,27,29,31,33,35\t35,32,33,22,26,30,23,29,27,24,25,31,21,28\t35/nil/nil",
            "21,23,25,27,29,31,33,35\t35,32,33,22,26,30,23,29,27,24,25,31,21,28",
            "21,23,25,27,29,33,35\t36,35,34,33,30,29,28,27,26,25,24,23,22,21",
            "31,32",
            "14\t14\t14\t14\t14",
            "false\tfalse\t22\tfalse\t",
            "nil\tnil\t14\t0\t14\t14",
            "21",
        ], Run(Spread + """
            GroupEnumUnitsInRange(g, 0, 0, 1500, odd)
            print(at(g), called(), inside)
            GroupEnumUnitsInRect(g, r, odd)
            print(at(g), called())
            GroupEnumUnitsOfPlayer(g, Player(5), odd)
            print(at(g), called())
            GroupEnumUnitsOfPlayer(g, Player(6), nil)
            print(at(g))
            local sizes = {}
            GroupEnumUnitsInRangeCounted(g, 0, 0, 1500, nil, 3) sizes[1] = BlzGroupGetSize(g)
            GroupEnumUnitsInRangeCounted(g, 0, 0, 1500, nil, 0) sizes[2] = BlzGroupGetSize(g)
            GroupEnumUnitsInRangeCounted(g, 0, 0, 1500, nil, -1) sizes[3] = BlzGroupGetSize(g)
            GroupEnumUnitsInRectCounted(g, r, nil, 2) sizes[4] = BlzGroupGetSize(g)
            GroupEnumUnitsInRangeOfLocCounted(g, Location(0, 0), 1500, nil, 1) sizes[5] = BlzGroupGetSize(g)
            print(table.unpack(sizes))
            GroupClear(g) GroupAddUnit(g, made[31])
            GroupEnumUnitsOfPlayer(g, Player(5), nil)
            local player = IsUnitInGroup(made[31], g)
            GroupClear(g) GroupAddUnit(g, made[31])
            GroupEnumUnitsInRange(g, -300, -300, 10, nil)
            local range, near = IsUnitInGroup(made[31], g), at(g)
            GroupClear(g) GroupAddUnit(g, made[31])
            GroupEnumUnitsInRect(g, Rect(-310, -310, -290, -290), nil)
            print(player, range, near, IsUnitInGroup(made[31], g), at(g))
            GroupEnumUnitsInRange(g, 0, 0, 1500, nil)
            local all = BlzGroupGetSize(g)
            GroupEnumUnitsInRange(g, 0, 0, 1500, Filter(function() return nil end))
            local none = BlzGroupGetSize(g)
            GroupEnumUnitsInRange(g, 0, 0, 1500, Filter(nil))
            local unfiltered = BlzGroupGetSize(g)
            GroupEnumUnitsInRange(nil, 0, 0, 1500, nil)
            GroupEnumUnitsInRect(g, nil, nil)
            print(L(GetFilterUnit()), L(GetEnumUnit()), all, none, unfiltered, BlzGroupGetSize(g))
            GroupEnumUnitsInRangeOfLoc(g, Location(300, 300), 100, nil)
            print(at(g))
            """));
    }

    // probe units-groups: a filter that enumerates gets its own unit back after the inner enumeration
    [Fact]
    public void A_filter_enumerating_gets_its_unit_back()
    {
        Assert.Equal(["in22,21/21\t21\t22"], Run(Spread + """
            local inner = CreateGroup()
            GroupEnumUnitsInRange(g, 300, 300, 10, Filter(function()
              local before = L(GetFilterUnit())
              GroupEnumUnitsInRange(inner, -300, -300, 10, Filter(function() calls[#calls + 1] = "in" .. L(GetFilterUnit()) return true end))
              calls[#calls + 1] = before .. "/" .. L(GetFilterUnit())
              return true
            end))
            print(called(), at(g), at(inner))
            """));
    }

    // probe units-order: 64 footmen on a grid 128 apart made out of order: by place they come by squares of 256 (rows
    // from the south, west to east, the last made first in a square), of the player newest first; a unit moved into
    // another square comes first there, one moved within its square keeps its place
    [Fact]
    public void Units_come_by_the_games_grid_or_newest_first()
    {
        var lines = Run("""
            local made = {}
            for k = 0, 63 do
              local n = 37 * k % 64
              made[k + 1] = make(n, "hfoo", (n % 8) * 128 - 448, (n // 8) * 128 - 448)
            end
            local calls = {}
            local filter = Filter(function() calls[#calls + 1] = L(GetFilterUnit()) return true end)
            local g = CreateGroup()
            local function show() print(table.concat(calls, ","), at(g)) calls = {} end
            GroupEnumUnitsInRect(g, Rect(-1000, -1000, 1000, 1000), filter)
            show()
            GroupEnumUnitsInRange(g, 100, -100, 2000, filter)
            show()
            GroupEnumUnitsOfPlayer(g, Player(0), filter)
            show()
            SetUnitPosition(made[1], 448, 448)
            SetUnitX(made[2], GetUnitX(made[2]) + 10)
            GroupEnumUnitsInRect(g, Rect(-1000, -1000, 1000, 1000), filter)
            show()
            """);
        const string grid = "1,8,9,0,11,2,3,10,4,5,12,13,7,14,15,6,17,24,25,16,27,18,19,26,21,28,29,20,31,22,23,30,41,32,33,40,34,35,42,43,44,45,36,37,38,39,46,47,48,49,56,57,51,58,59,50,61,52,53,60,54,55,62,63";
        var made = string.Join(",", Enumerable.Range(0, 64).Select(k => 37 * k % 64));
        Assert.Equal($"{grid}\t{made}", lines[0]);
        Assert.Equal($"{grid}\t{made}", lines[1]);
        Assert.Equal($"{string.Join(",", Enumerable.Range(0, 64).Select(k => 37 * (63 - k) % 64))}\t{made}", lines[2]);
        Assert.StartsWith("1,8,9,11,2,3,10,4,", lines[3]);
        Assert.Contains(",61,52,53,60,0,54,55,62,63\t", lines[3]);
    }

    // probe units-groups: one footman (collision 31) at (40, 50) against rects, its 32 cell in the rect with its west
    // and south edges past the rect's; a footman at (200, 0) against ranges from its place alone, the edge in; and the
    // range natives, which add the collision sizes and compare squared
    [Fact]
    public void Where_the_edges_of_a_rect_a_range_and_the_range_natives_fall()
    {
        Assert.Equal(["31.0", "000000010", "0000010", "011001010", "0011100\t1111", "01", "011111\t011111\t1100"], Run("""
            local u = make(41, "hfoo", 40, 50, 7)
            local g = CreateGroup()
            local function rect(a, b, c, d) GroupEnumUnitsInRect(g, Rect(a, b, c, d), nil) return BlzGroupGetSize(g) end
            print(BlzGetUnitCollisionSize(u))
            print(rect(40, 0, 100, 100) .. rect(0, 0, 40, 100) .. rect(40.01, 0, 100, 100) .. rect(0, 0, 39.99, 100) .. rect(33, 0, 100, 100)
              .. rect(32, 0, 100, 100) .. rect(0, 0, 63, 100) .. rect(0, 0, 64, 100) .. rect(39, 0, 41, 100))
            print(rect(0, 50, 100, 100) .. rect(0, 0, 100, 50) .. rect(0, 50.01, 100, 100) .. rect(0, 0, 100, 49.99) .. rect(0, 49, 100, 51)
              .. rect(100, 100, 0, 0) .. rect(40, 50, 40, 50))
            RemoveUnit(u)
            local v, w = make(42, "hfoo", 200, 0, 7), make(43, "Hpal", 500, 0, 7)
            local function range(x, y, d) GroupEnumUnitsInRange(g, x, y, d, nil) return BlzGroupGetSize(g) end
            print(range(0, 0, 199.99) .. range(0, 0, 200) .. range(0, 0, 200.01) .. range(0, 0, 169) .. range(0, 0, 0) .. range(200, 0, 0)
              .. range(200, 0, -1) .. range(170, 0, 30) .. range(170, 0, 29.99))
            local function b(x) return x and "1" or "0" end
            local function xy(d) return b(IsUnitInRangeXY(v, 0, 0, d)) end
            print(xy(168) .. xy(168.9) .. xy(169) .. xy(169.1) .. xy(200) .. xy(0) .. xy(-1),
              b(IsUnitInRangeXY(v, 200, 0, 0)) .. b(IsUnitInRangeXY(v, 200, 0, -1)) .. b(IsUnitInRangeXY(v, 200, 0, -31)) .. b(IsUnitInRangeXY(v, 200, 0, -32)))
            local here = Location(0, 0)
            print(b(IsUnitInRangeLoc(v, here, 168.9)) .. b(IsUnitInRangeLoc(v, here, 169)))
            local function pair(a, c, d) return b(IsUnitInRange(a, c, d)) end
            print(pair(v, w, 236.9) .. pair(v, w, 237) .. pair(v, w, 268.9) .. pair(v, w, 269) .. pair(v, w, 299.9) .. pair(v, w, 300),
              pair(w, v, 236.9) .. pair(w, v, 237) .. pair(w, v, 267.9) .. pair(w, v, 268) .. pair(w, v, 268.9) .. pair(w, v, 269),
              pair(v, v, 0) .. pair(v, v, -1) .. pair(v, nil, 1000) .. pair(nil, v, 1000))
            """));
    }
}
