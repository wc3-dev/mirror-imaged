using System.Numerics;
using System.Text.RegularExpressions;
using MirrorImaged.Data;
using MirrorImaged.Natives;
using War3Net.Build;
using War3Net.Build.Info;
using War3Net.Build.Widget;
using War3Net.Common.Extensions;

namespace MirrorImaged.Tests;

/// <summary>
/// Destructables, trees and gates against the game's lines of the destructables probe (docs/facts.md, "Destructables";
/// probes/expected/destructables.txt) and the facts' "Units", with the standard data of the types it makes given here.
/// </summary>
public sealed class DestructableTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static readonly StandardData Standard = Data();

    static StandardData Data()
    {
        static Dictionary<string, string> Type(string? life = null, string? rotation = null, string? texture = null, string? name = null, string? occluder = null) =>
            new[] { ("bhps", life), ("bfxr", rotation), ("bptx", texture), ("bnam", name), ("boch", occluder) }
                .Where(field => field.Item2 is not null).ToDictionary(field => field.Item1, field => field.Item2!);
        var data = new StandardData()
            .Add(ObjectKind.Destructable, "LTlt", Type("50", "270", @"PathTextures\4x4Default.tga", "WESTRING_DEST_SUMMER_TREE_WALL", "230"))
            .Add(ObjectKind.Destructable, "LTbr", Type("20", "-1", @"PathTextures\2x2Default.tga", "WESTRING_DEST_BARREL"))
            .Add(ObjectKind.Destructable, "YT16", Type("2500", "0", @"PathTextures\CityBridgeLarge0.tga", "WESTRING_DEST_LONG_STONE_CLIFF_1_VERTICAL"))
            .Add(ObjectKind.Destructable, "LT06", Type("2500", "90", @"PathTextures\CityBridgeLarge0.tga"))
            .Add(ObjectKind.Destructable, "LTg1", Type("500", "270", @"PathTextures\Gate1Path.tga", occluder: "400"))
            .Add(ObjectKind.Destructable, "XTmx", Type("50", "-1", @"PathTextures\8x2Default.tga"))
            .Add(ObjectKind.Destructable, "OTsp", Type("50", "-1", name: "WESTRING_DOOD_OZSP"))
            .Add(ObjectKind.Destructable, "YTc1", Type("50", "270"))
            .Add(ObjectKind.Unit, "hfoo", new Dictionary<string, string> { ["uhpm"] = "420" });
        data.EditorStrings["WESTRING_DEST_SUMMER_TREE_WALL"] = "Summer Tree Wall";
        data.EditorStrings["WESTRING_DEST_BARREL"] = "Barrel";
        data.EditorStrings["WESTRING_DEST_LONG_STONE_CLIFF_1_VERTICAL"] = "Long Stone Bridge";
        data.EditorStrings["WESTRING_DOOD_OZSP"] = "Shimmering Portal";
        data.PathingSizes[@"PathTextures\4x4Default.tga"] = (4, 4);
        data.PathingSizes[@"PathTextures\2x2Default.tga"] = (2, 2);
        data.PathingSizes[@"PathTextures\CityBridgeLarge0.tga"] = (32, 18);
        data.PathingSizes[@"PathTextures\Gate1Path.tga"] = (20, 4);
        data.PathingSizes[@"PathTextures\8x2Default.tga"] = (8, 2);
        return data;
    }

    // The probe's helpers: each death event prints which destructable died with the life the event saw.
    const string Helpers = """
        local function show(v) return v == nil and "nil" or tostring(v) end
        local function n(x) return string.format("%.3f", x) end
        local id = FourCC
        local function at(d) return n(GetDestructableX(d)) .. " " .. n(GetDestructableY(d)) end
        local function life(d) return n(GetDestructableLife(d)) .. "/" .. n(GetDestructableMaxLife(d)) end
        local names = {}
        local deaths = CreateTrigger()
        TriggerAddAction(deaths, function()
          local d = GetTriggerDestructable()
          print("  death " .. show(names[GetHandleId(d)]) .. " life " .. n(GetDestructableLife(d)) .. " widget " .. tostring(GetTriggerWidget() == d)
            .. " event " .. GetHandleId(GetTriggerEventId()))
        end)
        local function make(name, type, x, y, face)
          local d = CreateDestructable(id(type), x or 0, y or 0, face or 270, 1, 0)
          names[GetHandleId(d)] = name
          TriggerRegisterDeathEvent(deaths, d)
          return d
        end
        """;

    Simulation Load(string main, Action<Map>? map = null, IReadOnlyDictionary<string, string>? files = null) =>
        Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{Helpers}\n{main}\nend\n", map: map, files: files), "", data: Standard);

    // What a run printed, without the time and source
    static IEnumerable<string> Printed(Simulation simulation) => simulation.Log.Select(entry => Regex.Replace(entry, @"^[0-9.]+ print: ", ""));

    List<string> Run(string main, Action<Map>? map = null, IReadOnlyDictionary<string, string>? files = null)
    {
        using var simulation = Load(main, map, files);
        Assert.Null(simulation.Stopped);
        return [.. Printed(simulation)];
    }

    // probe destructables; game-facts, "Units": nil for hfoo and for 0, and a variation of 15 of LTlt's 10, or -1, makes one
    [Fact]
    public void A_units_id_or_0_makes_none_and_any_variation_makes_one()
    {
        Assert.Equal(["hfoo nil zero nil", "variation 15 true", "variation -1 true"], Run("""
            print("hfoo " .. show(CreateDestructable(id("hfoo"), 0, 0, 270, 1, 0)) .. " zero " .. show(CreateDestructable(0, 0, 0, 270, 1, 0)))
            for _, v in ipairs({ 15, -1 }) do print("variation " .. v .. " " .. tostring(CreateDestructable(id("LTlt"), 0, 0, 270, 1, v) ~= nil)) end
            """));
    }

    // probe destructables: a place is cut to a quarter, then put on the 64 grid toward 0, or 32 off it away from 0 where
    // half the pathing texture's cells that way is odd; its sides swap at a rotation of exactly 0 or 180 (game-facts: LTlt
    // at 1000,1000 stood at 960,960, YT16 at 2600,-1200 at 2592,-1152)
    [Fact]
    public void Each_type_is_put_on_the_grid_of_its_pathing_texture()
    {
        Assert.Equal([
            "snap LTlt 960.0,960.0 0.0,0.0 0.0,0.0 0.0,0.0 0.0,0.0 64.0,-64.0 -64.0,64.0 1536.0,-1536.0 -960.0,1152.0 0.0,0.0 0.0,0.0 64.0,-192.0",
            "snap LTbr 992.0,992.0 32.0,32.0 -32.0,-32.0 32.0,-32.0 32.0,32.0 96.0,-96.0 -96.0,96.0 1568.0,-1568.0 -992.0,1184.0 32.0,32.0 -32.0,-32.0 96.0,-224.0",
            "snap YT16 992.0,960.0 32.0,0.0 -32.0,0.0 32.0,0.0 32.0,0.0 96.0,-64.0 -96.0,64.0 1568.0,-1536.0 -992.0,1152.0 32.0,0.0 -32.0,0.0 96.0,-192.0",
            "snap LT06 960.0,992.0 0.0,32.0 0.0,-32.0 0.0,-32.0 0.0,32.0 64.0,-96.0 -64.0,96.0 1536.0,-1568.0 -960.0,1184.0 0.0,32.0 0.0,-32.0 64.0,-224.0",
            "snap LTg1 960.0,960.0 0.0,0.0 0.0,0.0 0.0,0.0 0.0,0.0 64.0,-64.0 -64.0,64.0 1536.0,-1536.0 -960.0,1152.0 0.0,0.0 0.0,0.0 64.0,-192.0",
            "snap XTmx 960.0,992.0 0.0,32.0 0.0,-32.0 0.0,-32.0 0.0,32.0 64.0,-96.0 -64.0,96.0 1536.0,-1568.0 -960.0,1184.0 0.0,32.0 0.0,-32.0 64.0,-224.0",
            "snap OTsp 1000.0,1000.0 0.0,0.0 -10.0,-10.0 10.0,-40.0 63.0,63.0 64.0,-64.0 -96.0,96.0 1536.0,-1536.0 -1000.0,1200.0 31.75,32.0 -10.25,-0.5 100.5,-201.0",
            "snap YTc1 1000.0,1000.0 0.0,0.0 -10.0,-10.0 10.0,-40.0 63.0,63.0 64.0,-64.0 -96.0,96.0 1536.0,-1536.0 -1000.0,1200.0 31.75,32.0 -10.25,-0.5 100.5,-201.0",
            "facing XTmx 0 32.0,0.0 992.0,960.0", "facing XTmx 30 0.0,32.0 960.0,992.0", "facing XTmx 45 0.0,32.0 960.0,992.0",
            "facing XTmx 60 0.0,32.0 960.0,992.0", "facing XTmx 90 0.0,32.0 960.0,992.0", "facing XTmx 135 0.0,32.0 960.0,992.0",
            "facing XTmx 180 32.0,0.0 992.0,960.0", "facing XTmx 300 0.0,32.0 960.0,992.0", "facing XTmx 360 0.0,32.0 960.0,992.0",
            "facing XTmx -90 0.0,32.0 960.0,992.0", "facing XTmx 0.5 0.0,32.0 960.0,992.0", "facing XTmx 179.9 0.0,32.0 960.0,992.0",
            "facing XTmx 270 0.0,32.0 960.0,992.0", "facing XTmx 540 0.0,32.0 960.0,992.0", "facing XTmx -180 0.0,32.0 960.0,992.0",
            "zero place 0.000 0.000 z 960.000 960.000 50.000/50.000",
        ], Run("""
            local points = { { 1000, 1000 }, { 0, 0 }, { -10, -10 }, { 10, -40 }, { 63, 63 }, { 64, -64 }, { -96, 96 }, { 1536, -1536 },
                { -1000, 1200 }, { 31.9, 32.1 }, { -10.1, -0.3 }, { 100.6, -200.9 } }
            for _, type in ipairs({ "LTlt", "LTbr", "YT16", "LT06", "LTg1", "XTmx", "OTsp", "YTc1" }) do
              local line = {}
              for _, p in ipairs(points) do
                local d = CreateDestructable(id(type), p[1], p[2], 270, 1, 0)
                line[#line + 1] = GetDestructableX(d) .. "," .. GetDestructableY(d)
                RemoveDestructable(d)
              end
              print("snap " .. type .. " " .. table.concat(line, " "))
            end
            for _, face in ipairs({ 0, 30, 45, 60, 90, 135, 180, 300, 360, -90, 0.5, 179.9, 270, 540, -180 }) do
              local line = {}
              for _, p in ipairs({ { 0, 0 }, { 1000, 1000 } }) do
                local d = CreateDestructable(id("XTmx"), p[1], p[2], face, 1, 0)
                line[#line + 1] = GetDestructableX(d) .. "," .. GetDestructableY(d)
              end
              print("facing XTmx " .. face .. " " .. table.concat(line, " "))
            end
            local small, z = CreateDestructable(id("LTlt"), -10, -10, 270, 1, 0), CreateDestructableZ(id("LTlt"), 1000, 1000, 300, 270, 1, 0)
            print("zero place " .. n(GetDestructableX(small)) .. " " .. n(GetWidgetY(small)) .. " z " .. at(z) .. " " .. life(z))
            """));
    }

    // probe destructables; game-facts, "Units" (11-02): a fraction kept, held to the most life, alive at 0.405 and dead
    // below it with the death event inside the call reading 0; on a dead one a life of 20 reads and Kill fires again
    [Fact]
    public void Life_is_held_to_the_most_and_below_0_405_kills_inside_the_call()
    {
        Assert.Equal([
            "new 50.000/50.000 widget 50.000 0.000 type 1280601204", "set 20.5 20.500/50.000", "set 999 50.000/50.000",
            "set 0.405 0.405/50.000", "set 0.4", "  death tree life 0.000 widget true event 89", "after 0.000/50.000",
            "dead set 20 20.000/50.000", "  death tree life 0.000 widget true event 89", "killed again 0.000/50.000",
            "killed twice 0.000/50.000", "  death set0.1 life 0.000 widget true event 89", "set 0.1 0.000/50.000",
            "  death set0.1 life 0.000 widget true event 89", "  death set0 life 0.000 widget true event 89", "set 0 0.000/50.000",
            "  death set0 life 0.000 widget true event 89", "  death set-5 life 0.000 widget true event 89", "set -5 0.000/50.000",
            "  death set-5 life 0.000 widget true event 89",
        ], Run("""
            local tree = make("tree", "LTlt")
            print("new " .. life(tree) .. " widget " .. n(GetWidgetLife(tree)) .. " " .. n(GetWidgetX(tree)) .. " type " .. GetDestructableTypeId(tree))
            for _, v in ipairs({ 20.5, 999, 0.405 }) do
              SetDestructableLife(tree, v)
              print("set " .. v .. " " .. life(tree))
            end
            print("set 0.4")
            SetDestructableLife(tree, 0.4)
            print("after " .. life(tree))
            SetDestructableLife(tree, 20)
            print("dead set 20 " .. life(tree))
            KillDestructable(tree)
            print("killed again " .. life(tree))
            KillDestructable(tree)
            print("killed twice " .. life(tree))
            for _, v in ipairs({ 0.1, 0, -5 }) do
              local d = make("set" .. v, "LTlt")
              SetDestructableLife(d, v)
              print("set " .. v .. " " .. life(d))
              RemoveDestructable(d)
            end
            """));
    }

    // probe destructables; game-facts, "Units": the most life rounded (80.7 and 80.5 are 81, 80.4 80), 0 and below kept,
    // the life keeping its share (20 of 50 is 40 of 100), nothing dying; a life of 0 from it is no death to Kill
    [Fact]
    public void The_most_life_is_rounded_and_the_life_keeps_its_share()
    {
        Assert.Equal([
            "max 50 80.7 81.000/81.000", "max 50 80.5 81.000/81.000", "max 50 80.4 80.000/80.000", "max 50 10 10.000/10.000",
            "max 50 0 0.000/0.000", "max 50 -5 0.000/-5.000", "max 20 100 40.000/100.000", "max 20 25 10.000/25.000",
            "max 20 10 4.000/10.000", "  death max life 0.000 widget true event 89", "max 0 70 0.000/70.000",
            "max 0 then 50 0.000/50.000", "max 0 killed 0.000/50.000",
        ], Run("""
            for _, case in ipairs({ { 50, 80.7 }, { 50, 80.5 }, { 50, 80.4 }, { 50, 10 }, { 50, 0 }, { 50, -5 }, { 20, 100 }, { 20, 25 }, { 20, 10 }, { 0, 70 } }) do
              local d = make("max", "LTlt")
              if case[1] == 0 then KillDestructable(d) else SetDestructableLife(d, case[1]) end
              SetDestructableMaxLife(d, case[2])
              print("max " .. case[1] .. " " .. case[2] .. " " .. life(d))
            end
            local zero = make("zero", "LTlt")
            SetDestructableMaxLife(zero, 0)
            SetDestructableMaxLife(zero, 50)
            print("max 0 then 50 " .. life(zero))
            KillDestructable(zero)
            print("max 0 killed " .. life(zero))
            """));
    }

    // probe destructables; game-facts, "Units": a dead one comes back with 30, the most for 0 and 999, 0.5 for 0.3, 0.5
    // and -5; a living one stays; its registration still holds; RemoveDestructable fires the death event with the life
    // it had, then every native reads 0; one made dead fires nothing when killed
    [Fact]
    public void Restoring_brings_back_a_dead_one_and_removing_fires_its_death()
    {
        Assert.Equal([
            "  death restore life 0.000 widget true event 89", "restore 30 30.000/50.000", "  death restore life 0.000 widget true event 89",
            "restore 0 50.000/50.000", "  death restore life 0.000 widget true event 89", "restore 999 50.000/50.000",
            "  death restore life 0.000 widget true event 89", "restore 0.3 0.500/50.000", "  death restore life 0.000 widget true event 89",
            "restore 0.5 0.500/50.000", "  death restore life 0.000 widget true event 89", "restore -5 0.500/50.000",
            "restore living 20.000/50.000", "  death living life 0.000 widget true event 89", "restored, killed again",
            "  death living life 0.000 widget true event 89", "remove living", "  death living life 20.000 widget true event 89",
            "removed 0 0.000/0.000 0.000 0.000", "dead 0.000/50.000", "dead restored 50.000/50.000",
        ], Run("""
            for _, v in ipairs({ 30, 0, 999, 0.3, 0.5, -5 }) do
              local d = make("restore", "LTlt")
              KillDestructable(d)
              DestructableRestoreLife(d, v, v == 30)
              print("restore " .. v .. " " .. life(d))
            end
            local living = make("living", "LTlt")
            SetDestructableLife(living, 20)
            DestructableRestoreLife(living, 40, true)
            print("restore living " .. life(living))
            KillDestructable(living)
            DestructableRestoreLife(living, 50, false)
            print("restored, killed again")
            KillDestructable(living)
            SetDestructableLife(living, 0)
            DestructableRestoreLife(living, 20, false)
            print("remove living")
            RemoveDestructable(living)
            print("removed " .. GetDestructableTypeId(living) .. " " .. life(living) .. " " .. at(living))
            local dead = CreateDeadDestructable(id("LTlt"), 0, 0, 270, 1, 0)
            names[GetHandleId(dead)] = "dead"
            TriggerRegisterDeathEvent(deaths, dead)
            print("dead " .. life(dead))
            KillDestructable(dead)
            DestructableRestoreLife(dead, 0, false)
            print("dead restored " .. life(dead))
            """));
    }

    // probe destructables: the widget natives read and set a destructable as its own natives do; invulnerability is a
    // flag that stops no death; the occluder height is the type's until set; the name is the World Editor string, nil's
    // ""; hiding and the animation natives change nothing read back, and a hidden one is still listed; a gate opens and
    // closes as ModifyGateBJ does it, killed and restored
    [Fact]
    public void Widgets_invulnerability_occluders_names_and_gates_read_as_in_the_game()
    {
        Assert.Equal([
            "widget 10.500 32.000 32.000 10.500/20.000", "  death widget life 0.000 widget true event 89", "widget killed 0.000/20.000",
            "invulnerable false", "invulnerable true", "  death guarded life 0.000 widget true event 89", "invulnerable set 0 0.000/50.000",
            "  death guarded life 0.000 widget true event 89", "invulnerable killed 0.000/50.000",
            "occluder LTlt 230.000 100.700 name Summer Tree Wall", "occluder LTlt -5 -5.000", "occluder LTbr 0.000 100.700 name Barrel",
            "occluder LTbr -5 -5.000", "occluder YT16 0.000 100.700 name Long Stone Bridge", "occluder YT16 -5 -5.000",
            "occluder OTsp 0.000 100.700 name Shimmering Portal", "occluder OTsp -5 -5.000", "name of nil true []",
            "hidden 50.000/50.000 found 1", "  death gate life 0.000 widget true event 89", "gate open 0.000/500.000", "gate closed 500.000/500.000",
        ], Run("""
            local w = make("widget", "LTbr")
            SetWidgetLife(w, 10.5)
            print("widget " .. n(GetWidgetLife(w)) .. " " .. n(GetWidgetX(w)) .. " " .. n(GetWidgetY(w)) .. " " .. life(w))
            SetWidgetLife(w, 0)
            print("widget killed " .. life(w))
            local guarded = make("guarded", "LTlt")
            print("invulnerable " .. tostring(IsDestructableInvulnerable(guarded)))
            SetDestructableInvulnerable(guarded, true)
            print("invulnerable " .. tostring(IsDestructableInvulnerable(guarded)))
            SetDestructableLife(guarded, 0)
            print("invulnerable set 0 " .. life(guarded))
            DestructableRestoreLife(guarded, 50, false)
            KillDestructable(guarded)
            print("invulnerable killed " .. life(guarded))
            for _, type in ipairs({ "LTlt", "LTbr", "YT16", "OTsp" }) do
              local d = CreateDestructable(id(type), 0, 0, 270, 1, 0)
              local before = GetDestructableOccluderHeight(d)
              SetDestructableOccluderHeight(d, 100.7)
              print("occluder " .. type .. " " .. n(before) .. " " .. n(GetDestructableOccluderHeight(d)) .. " name " .. show(GetDestructableName(d)))
              SetDestructableOccluderHeight(d, -5)
              print("occluder " .. type .. " -5 " .. n(GetDestructableOccluderHeight(d)))
            end
            print("name of nil " .. tostring(pcall(GetDestructableName, nil)) .. " [" .. show(select(2, pcall(GetDestructableName, nil))) .. "]")
            local shown = CreateDestructable(id("LTlt"), -512, -512, 270, 1, 0)
            ShowDestructable(shown, false)
            SetDestructableAnimation(shown, "death")
            QueueDestructableAnimation(shown, "stand")
            SetDestructableAnimationSpeed(shown, 2)
            SetDestructableColor(shown, ConvertPlayerColor(1))
            SetDestructableVertexColor(shown, 255, 0, 0, 128)
            SetDoodadAnimation(0, 0, 500, id("LTlt"), false, "death", false)
            SetDoodadAnimationRect(Rect(-100, -100, 100, 100), id("LTlt"), "stand", true)
            local found = 0
            EnumDestructablesInRect(Rect(-600, -600, -400, -400), nil, function() found = found + 1 end)
            print("hidden " .. life(shown) .. " found " .. found)
            local gate = make("gate", "LTg1", 512, -512)
            KillDestructable(gate)
            SetDestructableAnimation(gate, "death alternate")
            print("gate open " .. life(gate))
            DestructableRestoreLife(gate, GetDestructableMaxLife(gate), true)
            SetDestructableAnimation(gate, "stand")
            print("gate closed " .. life(gate))
            """));
    }

    // probe destructables; game-facts, "Units" (a rect of 32 around a preplaced one found none): a rect finds a
    // destructable when the 32 cell holding its place lies in it with its west and south edges past the rect's
    [Fact]
    public void A_rect_finds_a_destructable_whose_32_cell_lies_in_it()
    {
        Assert.Equal([
            "centred LTlt 1:0 16:0 31:0 32:1 33:1 48:1 63:1 64:1 65:1 96:1 128:1 129:1",
            "below LTlt -128:0 -65:0 -64:0 -63:0 -32:0 -1:0 0:0 1:0 32:1 63:1 64:1 65:1 128:1",
            "above LTlt -128:1 -65:1 -64:1 -63:1 -32:1 -1:1 0:0 1:0 32:0 63:0 64:0 65:0 128:0",
            "strip LTlt -64:0/0 -32:0/0 0:0/0 32:0/0 64:0/0",
            "centred LTbr 1:0 16:0 31:0 32:1 33:1 48:1 63:1 64:1 65:1 96:1 128:1 129:1",
            "below LTbr -128:0 -65:0 -64:0 -63:0 -32:0 -1:0 0:0 1:0 32:1 63:1 64:1 65:1 128:1",
            "above LTbr -128:1 -65:1 -64:1 -63:1 -32:1 -1:1 0:0 1:0 32:0 63:0 64:0 65:0 128:0",
            "strip LTbr -64:0/0 -32:0/0 0:0/0 32:0/0 64:0/0",
            "portal -1000.500 -1000.500 -1000.75..-960.5:0 -1030..-990:1 -1001..-968.5:0 -1001..-968.75:0 -1000.5..-900:0",
        ], Run("""
            for _, type in ipairs({ "LTlt", "LTbr" }) do
              local d = CreateDestructable(id(type), -1024, -1024, 270, 1, 0)
              local x, y = GetDestructableX(d), GetDestructableY(d)
              local function count(x1, y1, x2, y2)
                local r, c = Rect(x + x1, y + y1, x + x2, y + y2), 0
                EnumDestructablesInRect(r, nil, function() c = c + 1 end)
                RemoveRect(r)
                return c
              end
              local line = {}
              for _, h in ipairs({ 1, 16, 31, 32, 33, 48, 63, 64, 65, 96, 128, 129 }) do line[#line + 1] = h .. ":" .. count(-h, -h, h, h) end
              print("centred " .. type .. " " .. table.concat(line, " "))
              line = {}
              for _, a in ipairs({ -128, -65, -64, -63, -32, -1, 0, 1, 32, 63, 64, 65, 128 }) do line[#line + 1] = a .. ":" .. count(-300, -300, a, a) end
              print("below " .. type .. " " .. table.concat(line, " "))
              line = {}
              for _, a in ipairs({ -128, -65, -64, -63, -32, -1, 0, 1, 32, 63, 64, 65, 128 }) do line[#line + 1] = a .. ":" .. count(a, a, 300, 300) end
              print("above " .. type .. " " .. table.concat(line, " "))
              line = {}
              for _, a in ipairs({ -64, -32, 0, 32, 64 }) do line[#line + 1] = a .. ":" .. count(a, -300, a + 1, 300) .. "/" .. count(-300, a, 300, a + 1) end
              print("strip " .. type .. " " .. table.concat(line, " "))
              RemoveDestructable(d)
            end
            local portal = CreateDestructable(id("OTsp"), -1000.5, -1000.5, 270, 1, 0)
            local hits = {}
            for _, r in ipairs({ { -1000.75, -960.5 }, { -1030, -990 }, { -1001, -968.5 }, { -1001, -968.75 }, { -1000.5, -900 } }) do
              local c = 0
              EnumDestructablesInRect(Rect(r[1], r[1], r[2], r[2]), nil, function() c = c + 1 end)
              hits[#hits + 1] = r[1] .. ".." .. r[2] .. ":" .. c
            end
            print("portal " .. at(portal) .. " " .. table.concat(hits, " "))
            """));
    }

    // probe destructables: squares of 256 world units, rows from the south, west to east, the last made first in a square
    // whatever its place in it (five made in one square; sixteen in a grid across x = 0 and y = 0, out of order, with the
    // tree at 0, 0 made first sharing a square with g11); the filter runs for each with GetFilterDestructable it, then
    // the callback, for which both natives give the destructable; nil after
    [Fact]
    public void The_enumeration_goes_by_squares_and_runs_the_filter_then_the_callback()
    {
        Assert.Equal([
            "filtered order4 order2 order5 order3 order1 enum order4=filter order2=filter order5=filter order1=filter after nil nil",
            "no filter order4+filter order2+filter order5+filter order3+filter order1+filter",
            "one square in5 in4 in3 in2 in1",
            "grid g1 g2 g3 g4 g5 g6 g7 g8 g9 g10 g11 tree g12 g13 g14 g15 g16",
        ], Run("""
            local function listed(x1, y1, x2, y2)
              local seen = {}
              EnumDestructablesInRect(Rect(x1, y1, x2, y2), nil, function() seen[#seen + 1] = names[GetHandleId(GetEnumDestructable())] end)
              return table.concat(seen, " ")
            end
            names[GetHandleId(CreateDestructable(id("LTlt"), 0, 0, 270, 1, 0))] = "tree"
            local order = {}
            for i, p in ipairs({ { 1, 128, 0 }, { 2, -128, 0 }, { 3, 0, 128 }, { 4, 0, -128 }, { 5, 0, 0 } }) do
              order[i] = make("order" .. p[1], "LTlt", 1280 + p[2], -1280 + p[3])
            end
            local seen, filtered = {}, {}
            EnumDestructablesInRect(Rect(1000, -1600, 1600, -1000), Filter(function()
              local d = GetFilterDestructable()
              filtered[#filtered + 1] = names[GetHandleId(d)]
              return d ~= order[3]
            end), function()
              local filter = GetFilterDestructable()
              seen[#seen + 1] = names[GetHandleId(GetEnumDestructable())] .. (filter == nil and "" or filter == GetEnumDestructable() and "=filter" or "+filter")
            end)
            print("filtered " .. table.concat(filtered, " ") .. " enum " .. table.concat(seen, " ") .. " after " .. show(GetEnumDestructable()) .. " "
              .. show(GetFilterDestructable()))
            seen = {}
            EnumDestructablesInRect(Rect(1000, -1600, 1600, -1000), nil, function()
              seen[#seen + 1] = names[GetHandleId(GetEnumDestructable())] .. (GetFilterDestructable() == nil and "" or "+filter")
            end)
            print("no filter " .. table.concat(seen, " "))
            for i, p in ipairs({ { 704, 576 }, { 576, 704 }, { 640, 640 }, { 576, 576 }, { 704, 704 } }) do
              names[GetHandleId(CreateDestructable(id("LTlt"), p[1], p[2], 270, 1, 0))] = "in" .. i
            end
            print("one square " .. listed(520, 520, 760, 760))
            local places = { -320, -64, 64, 320 }
            for _, k in ipairs({ 6, 11, 1, 16, 9, 3, 14, 5, 12, 2, 8, 15, 4, 10, 13, 7 }) do
              names[GetHandleId(CreateDestructable(id("LTlt"), places[(k - 1) % 4 + 1], places[(k - 1) // 4 + 1], 270, 1, 0))] = "g" .. k
            end
            print("grid " .. listed(-400, -400, 400, 400))
            """));
    }

    // probe destructables; game-facts, "Units": GetLocationZ reads a bridge's deck (the decks' file in whole units: 287
    // where the game read 286.5 at YT16's middle), still in the instant it is killed and gone 3 s later; restored without
    // birth, not yet in that instant, back after; LT06's fixed rotation lays it east to west; at scale 1.2 it is 1.2
    // times longer and stands 1.2 times higher (game-facts: 344 against 287), and a fixed rotation ignores the facing
    [Fact]
    public void GetLocationZ_reads_a_standing_bridges_deck()
    {
        using var simulation = Load("""
            local where = Location(0, 0)
            local function heights(x, y, offsets)
              local line = {}
              for _, o in ipairs(offsets) do
                MoveLocation(where, x + o[1], y + o[2])
                line[#line + 1] = string.format("%.1f", GetLocationZ(where))
              end
              return table.concat(line, " ")
            end
            local across = { { 0, 0 }, { 0, 128 }, { 0, 256 }, { 0, -256 }, { 0, 512 }, { 0, -640 }, { 128, 0 }, { 256, 0 }, { -256, 0 }, { 300, 0 } }
            local bridge = make("bridge", "YT16", 0, -1280)
            local side = CreateDestructable(id("LT06"), -1280, 0, 270, 1, 0)
            local big = CreateDestructable(id("YT16"), 1280, 0, 90, 1.2, 0)
            local function deck() return heights(GetDestructableX(bridge), GetDestructableY(bridge), across) end
            print("deck YT16 " .. at(bridge) .. " " .. deck())
            print("deck LT06 " .. at(side) .. " " .. heights(GetDestructableX(side), GetDestructableY(side), across))
            print("deck scaled " .. heights(GetDestructableX(big), GetDestructableY(big), { { 0, 0 }, { 0, 768 }, { 0, 800 } }))
            KillDestructable(bridge)
            print("deck killed " .. deck())
            TimerStart(CreateTimer(), 3, false, function()
              print("deck 3 s " .. deck())
              DestructableRestoreLife(bridge, 2500, false)
              print("deck restored " .. deck())
              TimerStart(CreateTimer(), 0.5, false, function() print("deck restored 0.5 s " .. deck()) end)
            end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(4));

        const string standing = "287.0 278.0 255.0 247.0 199.0 158.0 289.0 0.0 0.0 0.0", fallen = "0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0";
        Assert.Equal([
            "deck YT16 32.000 -1280.000 " + standing, // the game: 286.5 277.5 255.0 247.1 198.8 158.2 288.9 0.0 0.0 0.0
            "deck LT06 -1280.000 32.000 274.0 274.0 0.0 0.0 0.0 0.0 269.0 251.0 250.0 243.8", // the game: 274.2 274.2 ... 250.3 243.5
            "deck scaled 344.4 201.6 0.0",
            "  death bridge life 0.000 widget true event 89",
            "deck killed " + standing,
            "deck 3 s " + fallen,
            "deck restored " + fallen,
            "deck restored 0.5 s " + standing,
        ], Printed(simulation));
    }

    // A war3map.doo of Summer trees in each state, one off the grid, one at half life, and two Long Stone Bridges facing
    // north, of states 2 and 3.
    static void Placed(Map map)
    {
        var number = 0;
        DoodadData At(string type, float x, float y, int state, byte life = 100) => new()
        {
            TypeId = type.FromRawcode(), SkinId = type.FromRawcode(), Position = new Vector3(x, y, 0), Rotation = MathF.PI / 2, Scale = Vector3.One,
            State = (DoodadState)state, Life = life, CreationNumber = number++,
        };
        map.Doodads = new MapDoodads(MapWidgetsFormatVersion.v8, MapWidgetsSubVersion.v11, useNewFormat: true)
        {
            Doodads =
            {
                At("LTlt", 512, 512, 2), At("LTlt", 640, 512, 3), At("LTlt", 768, 512, 0), At("LTlt", 896, 512, 6), At("LTlt", 1024, 512, 14),
                At("LTlt", 1152, 512, 1), At("LTlt", 1000, 700, 2), At("LTlt", 512, 768, 2, life: 50), At("YT16", 0, 1280, 2), At("YT16", -1280, 1280, 3),
            },
        };
    }

    // game-facts, "World Editor and map files": the game makes a war3map.doo destructable whose state has bit 0 or 1 set
    // (state 0 is the script's to make), and doesn't list those of state 3 (bit 0) in the enumerations; placed bridges
    // are put on the grid as made ones are (game-facts, "Units": a YT16 written at x 0 stood at 32, its facing ignored);
    // (1)SilverpineSprint's state 14, which War3Net refuses, is read (decisions D258); the life is the file's share
    [Fact]
    public void The_maps_placed_destructables_are_made_as_the_game_makes_them()
    {
        var map = _maps.Build("function config() end\nfunction main() end\n", map: Placed);
        Assert.Empty(MapArchive.Read(map).Unread);
        Assert.Equal(14, (int)MapArchive.Read(map).Files.Doodads!.Doodads[4].State);

        Assert.Equal([
            "1280601204 512.000 512.000 50.000/50.000", "1280601204 960.000 640.000 50.000/50.000", "1280601204 896.000 512.000 50.000/50.000",
            "1280601204 1024.000 512.000 50.000/50.000", "1280601204 512.000 768.000 25.000/50.000", "1498689846 32.000 1280.000 2500.000/2500.000",
            "decks 287.0 287.0",
        ], Run("""
            EnumDestructablesInRect(Rect(-3000, -3000, 3000, 3000), nil, function()
              local d = GetEnumDestructable()
              print(GetDestructableTypeId(d) .. " " .. at(d) .. " " .. life(d))
            end)
            local where = Location(32, 1280)
            local listed = GetLocationZ(where)
            MoveLocation(where, -1248, 1280)
            print(string.format("decks %.1f %.1f", listed, GetLocationZ(where)))
            """, Placed));
    }

    // D252: the widget natives and the death registration take units and destructables alike; an item waits for W2-3
    // and stops the run as the native being Missing
    [Fact]
    public void The_widget_natives_take_units_and_destructables_and_an_item_stops_the_run()
    {
        Assert.Equal(["420.000 -32.000 tree 50.000", "  death footman", "  death tree life 0.000 widget true event 89"], Run("""
            local footman, tree = CreateUnit(Player(0), id("hfoo"), -32, 0, 0), make("tree", "LTlt")
            local t = CreateTrigger()
            TriggerRegisterDeathEvent(t, footman)
            TriggerAddAction(t, function() print("  death footman") end)
            print(n(GetWidgetLife(footman)) .. " " .. n(GetWidgetX(footman)) .. " tree " .. n(GetWidgetLife(tree)))
            SetWidgetLife(footman, 0)
            SetWidgetLife(tree, 0)
            """));

        var natives = new GameNatives(new World.Game(0));
        var potion = new item();
        Assert.Equal("GetWidgetLife", Assert.Throws<MissingNativeException>(() => natives.GetWidgetLife(potion)).Native);
        Assert.Equal("SetWidgetLife", Assert.Throws<MissingNativeException>(() => natives.SetWidgetLife(potion, 1)).Native);
        Assert.Equal("GetWidgetY", Assert.Throws<MissingNativeException>(() => natives.GetWidgetY(potion)).Native);
        Assert.Equal("TriggerRegisterDeathEvent",
            Assert.Throws<MissingNativeException>(() => natives.TriggerRegisterDeathEvent(natives.CreateTrigger(), potion)).Native);
        Assert.Equal(0, natives.GetWidgetLife(null));
    }

    // probe destructables-remove: removed inside its own death event with no guard, as World Editor's "Destructible dies
    // -> Remove (Dying destructible)", the event fires 98 times in a Lua map, killed or removed, and 65 with a pcall
    // around the removal, which catches nothing: the next nested thread doesn't start, past Lua's limit of C calls. As
    // there, it runs in a timer's callback inside a pcall (the probe's guard), as the depth counts from there.
    [Fact]
    public void An_unguarded_removal_in_its_own_death_event_nests_until_lua_stops_it()
    {
        using var simulation = Load("""
            TimerStart(CreateTimer(), 0, false, function() pcall(function()
            local function count(start)
              local n = 0
              local t = CreateTrigger()
              local tree = CreateDestructable(FourCC("LTlt"), 0, 0, 270, 1, 0)
              TriggerRegisterDeathEvent(t, tree)
              TriggerAddAction(t, function()
                n = n + 1
                RemoveDestructable(GetTriggerDestructable())
              end)
              start(tree)
              return n, tree
            end
            local killed, tree = count(KillDestructable)
            print("killed " .. killed .. " type " .. GetDestructableTypeId(tree))
            print("removed " .. count(RemoveDestructable))
            local n, problem = 0, nil
            local t = CreateTrigger()
            local guarded = CreateDestructable(FourCC("LTlt"), 0, 0, 270, 1, 0)
            TriggerRegisterDeathEvent(t, guarded)
            TriggerAddAction(t, function()
              n = n + 1
              local ok, e = pcall(RemoveDestructable, GetTriggerDestructable())
              if not ok and problem == nil then problem = tostring(e) end
            end)
            KillDestructable(guarded)
            print("pcall " .. n .. " " .. tostring(problem))
            end) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(0.1));

        Assert.Equal(["killed 98 type 0", "removed 98", "pcall 65 nil"], Printed(simulation).Where(line => !line.Contains("C stack overflow")));
    }

    // probe destructables-remove-jass: the same in a JASS map closed the game
    [Fact]
    public void An_unguarded_removal_in_its_own_death_event_closes_a_jass_maps_game()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function Dies takes nothing returns nothing
                call RemoveDestructable(GetTriggerDestructable())
            endfunction

            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                local trigger t = CreateTrigger()
                local destructable d = CreateDestructable('LTlt', 0, 0, 270, 1, 0)
                call TriggerRegisterDeathEvent(t, d)
                call TriggerAddAction(t, function Dies)
                call KillDestructable(d)
            endfunction
            """, ScriptLanguage.Jass), "", data: Standard);

        Assert.IsType<GameClosedException>(simulation.Stopped?.InnerException);
    }

    // probe destructables: in a destructable's death event GetTriggerUnit and GetDyingUnit are nil and GetTriggerWidget
    // == GetTriggerDestructable; removed inside its own death event it fires the event again there, nested, each time;
    // a tree brought back a second after it died (tree revival) stands again
    [Fact]
    public void A_destructables_death_event_reads_no_unit_and_a_removal_inside_it_fires_again()
    {
        using var simulation = Load("""
            local selfCount = 0
            local selfTrigger = CreateTrigger()
            TriggerAddAction(selfTrigger, function()
              selfCount = selfCount + 1
              local d = GetTriggerDestructable()
              print("  self death " .. selfCount .. " " .. show(names[GetHandleId(d)]) .. " life " .. n(GetDestructableLife(d)))
              if selfCount < 3 then RemoveDestructable(d) end
            end)
            local function listed(x1, y1, x2, y2)
              local seen = {}
              EnumDestructablesInRect(Rect(x1, y1, x2, y2), nil, function() seen[#seen + 1] = names[GetHandleId(GetEnumDestructable())] end)
              return table.concat(seen, " ")
            end
            local killed = CreateDestructable(id("LTlt"), 512, 512, 270, 1, 0)
            names[GetHandleId(killed)] = "killed"
            TriggerRegisterDeathEvent(selfTrigger, killed)
            KillDestructable(killed)
            print("self killed " .. selfCount .. " " .. GetDestructableTypeId(killed) .. " [" .. listed(400, 400, 600, 600) .. "]")
            selfCount = 0
            local removed = CreateDestructable(id("LTlt"), 512, 512, 270, 1, 0)
            names[GetHandleId(removed)] = "removed"
            TriggerRegisterDeathEvent(selfTrigger, removed)
            RemoveDestructable(removed)
            print("self removed " .. selfCount .. " " .. GetDestructableTypeId(removed))
            local revived = CreateDestructable(id("LTlt"), 768, 768, 270, 1, 0)
            local revival = CreateTrigger()
            TriggerRegisterDeathEvent(revival, revived)
            TriggerAddAction(revival, function()
              print("revival unit " .. show(GetTriggerUnit()) .. " " .. show(GetDyingUnit()) .. " " .. tostring(GetTriggerWidget() == GetTriggerDestructable()))
              TriggerSleepAction(1)
              local d = GetTriggerDestructable()
              DestructableRestoreLife(d, GetDestructableMaxLife(d), true)
            end)
            KillDestructable(revived)
            TimerStart(CreateTimer(), 3, false, function() print("revived " .. life(revived)) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal([
            "  self death 1 killed life 0.000", "  self death 2 killed life 0.000", "  self death 3 killed life 0.000", "self killed 3 0 []",
            "  self death 1 removed life 50.000", "  self death 2 removed life 50.000", "  self death 3 removed life 50.000", "self removed 3 0",
            "revival unit nil nil true", "revived 50.000/50.000",
        ], Printed(simulation));
    }

    // A map's own pathing texture comes before the game's: a 4x4 one made 2x2 puts the tree 32 off the 64 grid
    [Fact]
    public void A_maps_own_pathing_texture_comes_first()
    {
        var tga = "\0\0\u0002\0\0\0\0\0\0\0\0\0\u0002\0\u0002\0\u0020\0";
        Assert.Equal(["992.0,992.0"], Run("""
            local d = CreateDestructable(id("LTlt"), 1000, 1000, 270, 1, 0)
            print(GetDestructableX(d) .. "," .. GetDestructableY(d))
            """, files: new Dictionary<string, string> { [@"PathTextures\4x4Default.tga"] = tga }));
    }
}

/// <summary>Gates through the installed game's blizzard.j, as World Editor's triggers open and close them.</summary>
[Trait("Needs", "Game")]
public sealed class DestructableBlizzardTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // probe destructables: ModifyGateBJ opens a gate by killing it, which fires its death event, and closes it by
    // restoring it to its most life
    [InstallFact]
    public void ModifyGateBJ_kills_a_gate_to_open_it_and_restores_it_to_close_it()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
              local gate = CreateDestructable(FourCC("LTg1"), 512, -512, 270, 1, 0)
              local t = CreateTrigger()
              TriggerRegisterDeathEvent(t, gate)
              TriggerAddAction(t, function() print("death") end)
              ModifyGateBJ(bj_GATEOPERATION_OPEN, gate)
              print("open", GetDestructableLife(gate))
              ModifyGateBJ(bj_GATEOPERATION_CLOSE, gate)
              print("closed", GetDestructableLife(gate))
            end
            """));

        Assert.Equal(["0.000 print: death", "0.000 print: open\t0.0", "0.000 print: closed\t500.0"], simulation.Log);
    }
}
