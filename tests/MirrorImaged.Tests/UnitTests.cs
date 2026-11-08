using MirrorImaged.Data;
using MirrorImaged.Lua;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

/// <summary>
/// Units without movement, against the probes units, units-decay, units-attribute and units-primary (docs/facts.md,
/// "Units") and the facts' "Units" and "Orders and events", on maps with the data the test gives (decisions D207): the
/// standard values of the units the probes made, under the probe map's balance set (custom_v1).
/// </summary>
public sealed class UnitTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;ulev=2;umvs=270;umvr=0.6;uacq=500;uprw=60;udtm=3.04;udea=3;ufoo=2;upoi=100;urac=human;umvt=foot;udty=large;uarm=Metal;utar=ground;uaen=1;ua1w=normal;ua1g=ground,structure,debris,item,ward;usca=0.8;uhrt=always;usid=1400;usin=800;udef=2;ubdg=0;umxp=10"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;upro=Granis Darkhammer,Jorn the Redeemer;uhpm=100;umpm=0;umpi=100;ustr=22;uagi=13;uint=17;upra=STR;ulev=5;umvs=270;umvr=0.6;uacq=500;udtm=1.5;udea=2;urac=human;umvt=foot;udty=hero;uaen=1;ua1w=normal;ua1g=ground;udef=2;uagp=1.5"))
            .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;udtm=2.34;udea=2;urac=human;udty=fort;uarm=Stone;utar=structure;utyp=Mechanical;ubdg=1;usid=900;usin=600;uaen=0;upap=unbuildable;uhrt=none"))
            .Add(ObjectKind.Unit, "hgry", Values("unam=Gryphon Rider;uhpm=825;ulev=5;umvs=320;umvr=0.4;uacq=500;udtm=1.67;udea=0;umvt=fly;umvh=240;umvf=90;utar=air;uaen=1;ua1w=missile;ua1g=air,ground,structure"))
            .Add(ObjectKind.Unit, "hkni", Values("unam=Knight;uhpm=800;umvr=0.5;udtm=5.1;udea=3;uaen=1;ua1w=normal;ua1g=ground"))
            .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;uaen=1;ua1w=normal;ua1g=ground;utyp=Peon;udea=3"))
            .Add(ObjectKind.Unit, "ugol", Values("unam=Haunted Gold Mine;ubdg=1;utyp=undead,Mechanical;uaen=0"))
            .Add(ObjectKind.Unit, "nshe", Values("unam=Sheep;uhpm=15;urac=critters;umvs=100;umvr=0.5;uacq=500;uaen=0"))
            .Add(ObjectKind.Item, "phea", Values("unam=Potion of Healing"))
            .Add(ObjectKind.Ability, "AHhb", Values("anam=Holy Light"))
            .Add(ObjectKind.Upgrade, "Rhme", Values("glvl=3;gnam:1=Iron Forged Swords"))
            .Add(ObjectKind.Buff, "Bdef", Values("fnam=Defend"));
        foreach (var (key, value) in Values("StrHitPointBonus=25;IntManaBonus=15;MinUnitSpeed=150;MaxUnitSpeed=400;DecayTime=2;BoneDecayTime=88;StructureDecayTime=30;AgiDefenseBase=-2;AgiDefenseBonus=0.3;Dawn=6;Dusk=18"))
            data.Constants[key] = value;
        return data;
    }

    Simulation Run(string main, StandardData? data = null, Action<War3Net.Build.Map>? map = null) => Simulation.Load(_maps.Build($$"""
        local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
        local function make(id, x, y, face) return CreateUnit(Player(0), FourCC(id), x or 0, y or 0, face or 0) end
        function config() end
        function main()
        {{main}}
        end
        """, map: map), "", data: data ?? Standard());

    static List<string> Printed(Simulation simulation) => [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];

    // probe units: a unit takes one id; a footman is made where asked, alive at full life, its level, speeds and ranges
    // its type's; a Paladin counts 25 life a strength and 15 mana an intelligence; nil for an id that is no unit's
    [Fact]
    public void A_unit_is_made_as_its_type_with_one_id()
    {
        using var simulation = Run("""
            local before = CreateTimer()
            local u = make("hfoo", 1500.5, -1700.25, 30)
            local after = CreateTimer()
            print(GetHandleId(u) - GetHandleId(before), GetHandleId(after) - GetHandleId(u), GetUnitTypeId(u) == FourCC("hfoo"), GetUnitName(u), pid(GetOwningPlayer(u)))
            print(GetUnitX(u), GetUnitY(u), GetWidgetX(u), GetWidgetY(u), GetUnitFacing(u), BlzGetUnitZ(u))
            print(GetWidgetLife(u), GetUnitState(u, UNIT_STATE_MAX_LIFE), BlzGetUnitMaxHP(u), GetUnitState(u, UNIT_STATE_MANA), BlzGetUnitMaxMana(u), GetUnitLevel(u), UnitAlive(u), BlzUnitIsAlive(u))
            print(GetUnitMoveSpeed(u), GetUnitDefaultMoveSpeed(u), GetUnitTurnSpeed(u), GetUnitAcquireRange(u), GetUnitPropWindow(u), GetUnitDefaultPropWindow(u))
            print(GetUnitFoodUsed(u), GetUnitPointValue(u), GetUnitPointValueByType(FourCC("hfoo")), GetHandleId(GetUnitRace(u)), IsUnitRace(u, RACE_HUMAN), BlzGetUnitSkin(u) == FourCC("hfoo"))
            local h = make("Hpal")
            print(GetWidgetLife(h), BlzGetUnitMaxHP(h), GetUnitState(h, UNIT_STATE_MANA), BlzGetUnitMaxMana(h), GetUnitLevel(h), GetUnitName(h))
            print(make("zzzz"), make("phea"), CreateUnit(Player(0), 0, 0, 0, 0), CreateUnit(nil, FourCC("hfoo"), 0, 0, 0), GetHandleId(GetUnitRace(make("nshe"))))
            local s = BlzCreateUnitWithSkin(Player(0), FourCC("hfoo"), 0, 0, 0, FourCC("hkni"))
            print(GetUnitTypeId(s) == FourCC("hfoo"), BlzGetUnitSkin(s) == FourCC("hkni"), GetUnitName(s))
            """);

        Assert.Equal([
            "1\t1\ttrue\tFootman\t0",
            "1500.5\t-1700.25\t1500.5\t-1700.25\t30.0\t0.0",
            "420.0\t420.0\t420\t0.0\t0\t2\ttrue\ttrue",
            "270.0\t270.0\t0.6\t500.0\t1.047197\t60.0",
            "2\t100\t100\t1\ttrue\ttrue",
            "650.0\t650\t255.0\t255\t1\tPaladin",
            "nil\tnil\tnil\tnil\t10",
            "true\ttrue\tFootman",
        ], Printed(simulation));
    }

    // facts, Units: BlzGetUnitZ reads the ground under the unit (W2-1's Terrain), not its height above it
    [Fact]
    public void A_units_z_is_the_ground_under_it()
    {
        using var simulation = Run("""
            local u, g = make("hfoo", 0, 0), make("hgry", 64, 0)
            SetUnitFlyHeight(u, 200, 0)
            print(BlzGetUnitZ(u), BlzGetUnitZ(g), BlzGetLocalUnitZ(g), GetUnitFlyHeight(g))
            SetUnitX(u, 128)
            print(BlzGetUnitZ(u))
            """, map: map =>
        {
            var ground = map.Environment!;
            ground.TerrainTiles[(int)((0 - ground.Bottom) / 128 * ground.Width + (0 - ground.Left) / 128)].CliffLevel++;
        });

        Assert.Equal(["128.0	64.0	64.0	240.0", "0.0"], Printed(simulation));
    }

    // probe units: a facing is kept from 0 below 360 (400 read 40.00002 in the game, a float step off: Close)
    [Fact]
    public void A_facing_is_an_angle_from_0_below_360()
    {
        using var simulation = Run("""
            for _, face in ipairs({ 400, -90, 720, 359.9, -0.5 }) do print(GetUnitFacing(make("hfoo", 0, 0, face))) end
            """);

        Assert.Equal(["40.0", "270.0", "0.0", "359.9", "359.5"], Printed(simulation));
    }

    // probe units: life is the most life less the damage that leaves, in floats; below 0.405 the unit dies (0.405 on a
    // footman of 420 reads 0.40499878 and dies, on a Paladin of 650 0.4050293 and lives); a dead unit's life can be set
    // but it stays dead
    [Fact]
    public void Life_is_the_most_less_the_damage_and_a_unit_dies_below_0_405()
    {
        using var simulation = Run("""
            for _, v in ipairs({ 0.41, 0.405, 0.4049, 0.4, 1.1, 419.99, 100.5, 500 }) do
              local f, h = make("hfoo"), make("Hpal")
              SetWidgetLife(f, v)
              SetUnitState(h, UNIT_STATE_LIFE, v)
              print(v, GetWidgetLife(f), UnitAlive(f), GetWidgetLife(h), UnitAlive(h))
            end
            local d = make("hfoo")
            KillUnit(d)
            SetWidgetLife(d, 100)
            print(GetWidgetLife(d), UnitAlive(d))
            BlzSetUnitMaxHP(d, 50)
            print(BlzGetUnitMaxHP(d), GetWidgetLife(d))
            SetWidgetLife(d, -5)
            print(GetWidgetLife(d), UnitAlive(d))
            """);

        Assert.Equal([
            "0.41\t0.4100037\ttrue\t0.4099731\ttrue",
            "0.405\t0.0\tfalse\t0.4050293\ttrue",
            "0.4049\t0.0\tfalse\t0.0\tfalse",
            "0.4\t0.0\tfalse\t0.0\tfalse",
            "1.1\t1.100006\ttrue\t1.099976\ttrue",
            "419.99\t419.99\ttrue\t419.99\ttrue",
            "100.5\t100.5\ttrue\t100.5\ttrue",
            "500\t420.0\ttrue\t500.0\ttrue",
            "100.0\tfalse",
            "50\t50.0",
            "0.0\tfalse",
        ], Printed(simulation));
    }

    // facts, Units: SetUnitState of the maxima changes nothing; BlzSetUnitMaxHP keeps life, clamping it, and 0 kills;
    // mana clamps to the most, a unit without mana keeps 0 (probe units too)
    [Fact]
    public void The_most_life_and_mana_are_set_only_by_their_own_natives()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            SetUnitState(u, UNIT_STATE_MAX_LIFE, 600)
            print(BlzGetUnitMaxHP(u), GetWidgetLife(u))
            BlzSetUnitMaxHP(u, 150) print(GetWidgetLife(u), BlzGetUnitMaxHP(u))
            BlzSetUnitMaxHP(u, 600) print(GetWidgetLife(u), BlzGetUnitMaxHP(u))
            BlzSetUnitMaxHP(u, 0) print(GetWidgetLife(u), UnitAlive(u))
            local m = make("hfoo")
            SetUnitState(m, UNIT_STATE_MANA, 10) print(GetUnitState(m, UNIT_STATE_MANA))
            BlzSetUnitMaxMana(m, 50) print(GetUnitState(m, UNIT_STATE_MANA), BlzGetUnitMaxMana(m))
            SetUnitState(m, UNIT_STATE_MANA, 70) print(GetUnitState(m, UNIT_STATE_MANA))
            SetUnitState(m, UNIT_STATE_MANA, 20.5) BlzSetUnitMaxMana(m, 10) print(GetUnitState(m, UNIT_STATE_MANA))
            SetUnitState(m, UNIT_STATE_MANA, -5) SetUnitState(m, UNIT_STATE_MAX_MANA, 90) print(GetUnitState(m, UNIT_STATE_MANA), BlzGetUnitMaxMana(m))
            """);

        Assert.Equal(["420\t420.0", "150.0\t150", "150.0\t600", "0.0\tfalse", "0.0", "0.0\t50", "50.0", "10.0", "0.0\t10"], Printed(simulation));
    }

    // probe units: KillUnit fires the widget's death event, then the owner's player-unit events, then the unit's own,
    // each in the order registered, inside the call, life 0 and the killer nil; a player-unit event's filter never runs
    // and doesn't hold it back; a second KillUnit fires nothing
    [Fact]
    public void A_death_fires_the_widget_then_the_player_then_the_unit_events_inside_the_call()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local function respond(label)
              return function()
                print(label, GetHandleId(GetTriggerEventId()), GetTriggerUnit() == u, GetTriggerWidget() == u, GetDyingUnit() == u,
                  GetKillingUnit(), pid(GetTriggerPlayer()), GetWidgetLife(u), UnitAlive(u))
              end
            end
            local function on(label, register) local t = CreateTrigger() register(t) TriggerAddAction(t, respond(label)) end
            on("unit", function(t) TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DEATH) end)
            on("player", function(t) TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_DEATH, Filter(function() print("filter") return false end)) end)
            on("widget", function(t) TriggerRegisterDeathEvent(t, u) end)
            on("other", function(t) TriggerRegisterPlayerUnitEvent(t, Player(1), EVENT_PLAYER_UNIT_DEATH, nil) end)
            print("kill")
            KillUnit(u)
            print("killed")
            KillUnit(u)
            print("again", GetTriggerUnit(), GetDyingUnit())
            """);

        Assert.Equal([
            "kill",
            "widget\t89\ttrue\ttrue\tfalse\tnil\tnil\t0.0\tfalse",
            "player\t20\ttrue\tfalse\ttrue\tnil\t0\t0.0\tfalse",
            "unit\t53\ttrue\tfalse\ttrue\tnil\t0\t0.0\tfalse",
            "killed",
            "again\tnil\tnil",
        ], Printed(simulation));
    }

    // probe units: SetWidgetLife and SetUnitState below 0.405 and BlzSetUnitMaxHP 0 kill, inside the call
    [Fact]
    public void Life_set_below_the_edge_kills_inside_the_call()
    {
        using var simulation = Run("""
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_DEATH, nil)
            TriggerAddAction(t, function() print("death", GetUnitName(GetTriggerUnit()), GetWidgetLife(GetTriggerUnit())) end)
            local a, b, c = make("hfoo"), make("Hpal"), make("hkni")
            SetWidgetLife(a, 0.3) print("set")
            SetUnitState(b, UNIT_STATE_LIFE, -5) print("state")
            BlzSetUnitMaxHP(c, 0) print("max")
            """);

        Assert.Equal(["death\tFootman\t0.0", "set", "death\tPaladin\t0.0", "state", "death\tKnight\t0.0", "max"], Printed(simulation));
    }

    // probe units-decay: a footman decays its death time after it died (3.04), firing the player-unit then the unit
    // decay event, and the body goes BoneDecayTime and DecayTime later (game-facts: about 90 s); a building is gone its
    // death time plus StructureDecayTime later and a unit that doesn't decay (the gryphon rider) its death time later,
    // neither with a decay event; a hero, a corpse and a unit whose decay is suspended stay; an exploded or removed one
    // is gone after the step
    [Fact]
    public void A_body_decays_and_goes_as_its_death_type_says()
    {
        using var simulation = Run("""
            local clock = CreateTimer()
            TimerStart(clock, 1000, false, nil)
            local watched = {}
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_DECAY, nil)
            TriggerAddAction(t, function()
              print("decay", GetUnitName(GetDecayingUnit()), GetHandleId(GetTriggerEventId()), string.format("%.2f", TimerGetElapsed(clock)))
            end)
            local function watch(label, id)
              local u = make(id)
              local own = CreateTrigger()
              TriggerRegisterUnitEvent(own, u, EVENT_UNIT_DECAY)
              TriggerAddAction(own, function() print("own decay", label, GetDecayingUnit() == u) end)
              watched[#watched + 1] = { label, u }
              return u
            end
            KillUnit(watch("footman", "hfoo"))
            KillUnit(watch("barracks", "hbar"))
            KillUnit(watch("gryphon", "hgry"))
            KillUnit(watch("paladin", "Hpal"))
            local x = watch("exploded", "hfoo") SetUnitExploded(x, true) KillUnit(x)
            local s = watch("suspended", "hfoo") KillUnit(s) UnitSuspendDecay(s, true)
            local r = watch("removed", "hfoo") KillUnit(r) RemoveUnit(r)
            local c = CreateCorpse(Player(0), FourCC("hfoo"), 0, 0, 0)
            print("corpse", GetUnitTypeId(c) == FourCC("hfoo"), GetWidgetLife(c), UnitAlive(c), IsUnitType(c, UNIT_TYPE_DEAD))
            watched[#watched + 1] = { "corpse", c }
            TimerStart(CreateTimer(), 0.25, true, function()
              for i = #watched, 1, -1 do
                if GetUnitTypeId(watched[i][2]) == 0 then
                  print("gone", watched[i][1], string.format("%.2f", TimerGetElapsed(clock)))
                  table.remove(watched, i)
                end
              end
            end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(100));

        Assert.Equal([
            "corpse\ttrue\t0.0\tfalse\ttrue",
            "gone\tremoved\t0.25",
            "gone\texploded\t0.25",
            "gone\tgryphon\t1.75",
            "decay\tFootman\t21\t3.04",
            "own decay\tfootman\ttrue",
            "decay\tFootman\t21\t3.04",
            "own decay\tsuspended\ttrue",
            "gone\tbarracks\t32.50",
            "gone\tfootman\t93.25", // inferred from DecayTime and BoneDecayTime (D228; W2-10 to probe)
        ], Printed(simulation));
    }

    // D228, not measured: a body whose time came while its decay was suspended goes once the decay is let go
    [Fact]
    public void A_body_let_decay_late_goes_then()
    {
        using var simulation = Run("""
            local u = make("hgry")
            KillUnit(u)
            UnitSuspendDecay(u, true)
            TimerStart(CreateTimer(), 5, false, function()
              print("held", GetUnitTypeId(u) == FourCC("hgry"))
              UnitSuspendDecay(u, false)
              print("let go", GetUnitTypeId(u) == FourCC("hgry"))
              TimerStart(CreateTimer(), 0, false, function() print("after", GetUnitTypeId(u)) end)
            end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(6));

        Assert.Equal(["held\ttrue", "let go\ttrue", "after\t0"], Printed(simulation));
    }

    // probe units: RemoveUnit leaves the unit as it was for the rest of the step, alive or dead, and gone after it; a
    // unit removed and then killed in the step dies, firing its death, and stays as a body; no other event fires
    [Fact]
    public void A_removed_unit_reads_as_it_was_until_the_step_ends()
    {
        using var simulation = Run("""
            local u, d, k = make("hfoo", 1700, 1700), make("hfoo"), make("hfoo")
            KillUnit(d)
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DEATH)
            TriggerRegisterUnitEvent(t, k, EVENT_UNIT_DEATH)
            TriggerAddAction(t, function() print("death", GetTriggerUnit() == k) end)
            RemoveUnit(u) RemoveUnit(d) RemoveUnit(k)
            print(GetUnitTypeId(u) == FourCC("hfoo"), GetWidgetLife(u), UnitAlive(u), pid(GetOwningPlayer(u)), GetUnitX(u), GetUnitTypeId(d) == FourCC("hfoo"), UnitAlive(d))
            KillUnit(k)
            TimerStart(CreateTimer(), 0, false, function()
              print(GetUnitTypeId(u), GetWidgetLife(u), UnitAlive(u), pid(GetOwningPlayer(u)), GetUnitX(u), GetUnitName(u), GetUnitTypeId(d), GetUnitTypeId(k) == FourCC("hfoo"), UnitAlive(k))
            end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "true\t420.0\ttrue\t0\t1700.0\ttrue\tfalse",
            "death\ttrue",
            "0\t0.0\tfalse\tnil\t0.0\tnil\t0\ttrue\tfalse",
        ], Printed(simulation));
    }

    // probe units and the facts' "Orders and events": SetUnitOwner gives a living unit a stop (851972) whose events read
    // the old owner, then changes hands and fires the owner change on the new owner's registrations; to its owner,
    // nothing; a dead unit changes hands with no stop
    [Fact]
    public void A_unit_given_away_stops_and_then_changes_hands_inside_the_call()
    {
        using var simulation = Run("""
            local u, d = make("hfoo"), make("hfoo")
            KillUnit(d)
            local o = CreateTrigger()
            TriggerRegisterUnitEvent(o, u, EVENT_UNIT_ISSUED_ORDER)
            TriggerRegisterPlayerUnitEvent(o, Player(0), EVENT_PLAYER_UNIT_ISSUED_ORDER, nil)
            TriggerRegisterPlayerUnitEvent(o, Player(1), EVENT_PLAYER_UNIT_ISSUED_ORDER, nil)
            TriggerAddAction(o, function()
              print("order", GetHandleId(GetTriggerEventId()), GetIssuedOrderId(), GetOrderedUnit() == GetTriggerUnit(), pid(GetOwningPlayer(GetTriggerUnit())), pid(GetTriggerPlayer()))
            end)
            local c = CreateTrigger()
            for _, w in ipairs({ u, d }) do TriggerRegisterUnitEvent(c, w, EVENT_UNIT_CHANGE_OWNER) end
            for i = 0, 2 do TriggerRegisterPlayerUnitEvent(c, Player(i), EVENT_PLAYER_UNIT_CHANGE_OWNER, nil) end
            TriggerAddAction(c, function()
              print("change", GetHandleId(GetTriggerEventId()), GetChangingUnit() == GetTriggerUnit(), pid(GetChangingUnitPrevOwner()), pid(GetOwningPlayer(GetTriggerUnit())), pid(GetTriggerPlayer()))
            end)
            SetUnitOwner(u, Player(1), true)
            print("given", pid(GetOwningPlayer(u)), IsUnitOwnedByPlayer(u, Player(1)), GetIssuedOrderId(), GetChangingUnitPrevOwner())
            SetUnitOwner(u, Player(1), false)
            print("again")
            SetUnitOwner(d, Player(2), true)
            print("dead", pid(GetOwningPlayer(d)))
            """);

        Assert.Equal([
            "order\t38\t851972\ttrue\t0\t0",
            "order\t75\t851972\ttrue\t0\t0",
            "change\t270\ttrue\t0\t1\t1",
            "change\t287\ttrue\t0\t1\t1",
            "given\t1\ttrue\t0\tnil",
            "again",
            "change\t270\ttrue\t0\t2\t2",
            "change\t287\ttrue\t0\t2\t2",
            "dead\t2",
        ], Printed(simulation));
    }

    // probe units: SetUnitFacing reads the old facing at once and turns from the next frame (Close: here at 16 x the
    // turn rate less 3.2 radians a second, from 1/32 s); BlzSetUnitFacingEx turns at once
    [Fact]
    public void A_unit_turns_at_its_turn_rate_and_BlzSetUnitFacingEx_at_once()
    {
        using var simulation = Run("""
            local u, w, e = make("hfoo"), make("hfoo"), make("hfoo")
            SetUnitFacing(u, 180)
            SetUnitFacing(w, -90)
            BlzSetUnitFacingEx(e, -30)
            print(GetUnitFacing(u), GetUnitFacing(w), GetUnitFacing(e))
            TimerStart(CreateTimer(), 0.25, false, function() print(string.format("%.1f %.1f", GetUnitFacing(u), GetUnitFacing(w))) end)
            TimerStart(CreateTimer(), 1, false, function() print(GetUnitFacing(u), GetUnitFacing(w)) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(2));

        // 6.4 rad/s is 366.7 degrees a second: 80.2 degrees by 0.25 s; the game read 80.8 there
        Assert.Equal(["0.0\t0.0\t330.0", "80.2 279.8", "180.0\t270.0"], Printed(simulation));
    }

    // probe units: move speed kept from MinUnitSpeed to MaxUnitSpeed, a building's 0; the turn rate an angle from 0.001
    // below 2 pi; an acquisition range of 50 or less not taken, nor any by a unit without an attack, which reads 0; the
    // propulsion window set in radians; the fly height set read at once whatever the rate
    [Fact]
    public void Speeds_ranges_and_heights_keep_to_their_bounds()
    {
        using var simulation = Run("""
            local u, b, s = make("hfoo"), make("hbar"), make("nshe")
            SetUnitMoveSpeed(u, 1000) local fast = GetUnitMoveSpeed(u)
            SetUnitMoveSpeed(u, -10) SetUnitMoveSpeed(b, 200)
            print(fast, GetUnitMoveSpeed(u), GetUnitDefaultMoveSpeed(u), GetUnitMoveSpeed(b))
            SetUnitTurnSpeed(u, -1) local turned = GetUnitTurnSpeed(u)
            SetUnitTurnSpeed(b, 0)
            print(turned, GetUnitTurnSpeed(b), GetUnitDefaultTurnSpeed(b), GetUnitTurnSpeed(make("hbar")))
            SetUnitAcquireRange(u, 50) local low = GetUnitAcquireRange(u)
            SetUnitAcquireRange(u, 55) SetUnitAcquireRange(s, 300)
            print(low, GetUnitAcquireRange(u), GetUnitDefaultAcquireRange(u), GetUnitAcquireRange(b), GetUnitDefaultAcquireRange(b), GetUnitAcquireRange(s))
            SetUnitPropWindow(u, 1)
            SetUnitFlyHeight(u, 200, 0)
            local g = make("hgry")
            SetUnitFlyHeight(g, 300, 100)
            print(GetUnitPropWindow(u), GetUnitDefaultPropWindow(u), GetUnitFlyHeight(u), GetUnitDefaultFlyHeight(u), GetUnitFlyHeight(g), GetUnitDefaultFlyHeight(g))
            """);

        Assert.Equal([
            "400.0\t150.0\t270.0\t0.0",
            "5.283185\t0.001\t0.0\t0.001",
            "500.0\t55.0\t500.0\t0.0\t900.0\t0.0",
            "1.0\t60.0\t200.0\t0.0\t300.0\t240.0",
        ], Printed(simulation));
    }

    // probe units: PauseUnit is a flag, BlzPauseUnitEx a count IsUnitPaused doesn't read; a hidden unit keeps its place;
    // the unit's own name, its data and invulnerability read back
    [Fact]
    public void Flags_name_and_data_read_back()
    {
        using var simulation = Run("""
            local u = make("hfoo", 900, 1300)
            PauseUnit(u, true) PauseUnit(u, true) PauseUnit(u, false)
            local paused = IsUnitPaused(u)
            BlzPauseUnitEx(u, true)
            print(paused, IsUnitPaused(u))
            ShowUnit(u, false)
            print(IsUnitHidden(u), GetUnitX(u), GetUnitY(u))
            ShowUnit(u, true) SetUnitInvulnerable(u, true) SetUnitUserData(u, 42) BlzSetUnitName(u, "Renamed")
            print(IsUnitHidden(u), BlzIsUnitInvulnerable(u), GetUnitUserData(u), GetUnitName(u), GetObjectName(FourCC("hfoo")), GetUnitName(make("hfoo")))
            SetUnitX(u, 5096.5) SetUnitY(u, -300.25)
            SetUnitScale(u, 2.5, 1, 1) SetUnitVertexColor(u, 300, -50, 100, 128) SetUnitColor(u, ConvertPlayerColor(3))
            print(GetUnitX(u), GetUnitY(u), BlzGetUnitRealField(u, UNIT_RF_SCALING_VALUE), BlzGetUnitIntegerField(u, UNIT_IF_TINTING_COLOR_RED))
            """);

        Assert.Equal([
            "false\tfalse",
            "true\t900.0\t1300.0",
            "false\ttrue\t42\tRenamed\tFootman\tFootman",
            "5096.5\t-300.25\t0.8\t255",
        ], Printed(simulation));
    }

    // facts, Units: SetUnitScale draws by x alone and SetUnitVertexColor keeps each part from 0 to 255; with the
    // colour, kept on the unit for the viewer, as no native reads them back (D226)
    [Fact]
    public void Colour_scale_and_tint_are_kept_on_the_unit()
    {
        var natives = new GameNatives(new World.Game(0)) { Data = new GameData(Standard()) };
        var u = natives.CreateUnit(natives.Player(0), Ids.Of("hfoo"), 0, 0, 0)!;

        Assert.Equal((0.8f, 0), (u.Scale, u.Color));
        natives.SetUnitScale(u, 2.5f, 1, 1);
        natives.SetUnitVertexColor(u, 300, -50, 100, 128);
        natives.SetUnitColor(u, natives.ConvertPlayerColor(3));
        natives.SetUnitOwner(u, natives.Player(5), true);
        Assert.Equal((2.5f, 5), (u.Scale, u.Color));
        Assert.Equal([255, 0, 100, 128], u.Tint);
    }

    // D226, not measured: a stop's event that gives the unit away first leaves SetUnitOwner nothing more to do
    [Fact]
    public void A_unit_given_away_in_its_stop_event_changes_hands_once()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local o = CreateTrigger()
            TriggerRegisterUnitEvent(o, u, EVENT_UNIT_ISSUED_ORDER)
            local given = false
            TriggerAddAction(o, function() if not given then given = true SetUnitOwner(u, Player(2), false) end end)
            local c = CreateTrigger()
            TriggerRegisterUnitEvent(c, u, EVENT_UNIT_CHANGE_OWNER)
            TriggerAddAction(c, function() print("change", pid(GetChangingUnitPrevOwner()), pid(GetOwningPlayer(u))) end)
            SetUnitOwner(u, Player(1), false)
            print("owner", pid(GetOwningPlayer(u)))
            """);

        Assert.Equal(["change\t0\t2", "owner\t2"], Printed(simulation));
    }

    // Edges no probe measured, the model's: a most mana below 0 keeps mana at 0, a least move speed above the most
    // gives the most, a turn rate comes round by whole turns, and a field id given as another type reads the type's
    [Fact]
    public void Odd_values_keep_their_bounds_without_throwing()
    {
        var data = Standard();
        (data.Constants["MinUnitSpeed"], data.Constants["MaxUnitSpeed"]) = ("500", "400");
        using var simulation = Run("""
            local u = make("hfoo")
            BlzSetUnitMaxMana(u, -5)
            SetUnitState(u, UNIT_STATE_MANA, 10)
            SetUnitMoveSpeed(u, 300)
            print(GetUnitState(u, UNIT_STATE_MANA), BlzGetUnitMaxMana(u), GetUnitMoveSpeed(u))
            SetUnitTurnSpeed(u, 20) local up = GetUnitTurnSpeed(u)
            SetUnitTurnSpeed(u, -20)
            print(up, GetUnitTurnSpeed(u))
            print(BlzSetUnitIntegerField(u, ConvertUnitIntegerField(FourCC("usca")), 7), BlzGetUnitRealField(u, UNIT_RF_SCALING_VALUE),
              BlzSetUnitRealField(u, ConvertUnitRealField(FourCC("ulev")), 7.5), GetUnitLevel(u))
            """, data);

        Assert.Equal(["0.0\t-5\t400.0", "1.150444\t5.132742", "true\t0.8\ttrue\t2"], Printed(simulation));
    }

    // Unit events are kept by unit and by player, so a death reads only its own unit's and owner's, and a destroyed
    // trigger lets go of only its own registrations (the code review's case: a unit's trigger destroyed in its own
    // death action); a list goes once it is empty, so the bodies that stay keep none
    [Fact]
    public void Unit_event_lists_are_each_units_and_go_with_their_registrations()
    {
        var natives = new GameNatives(new World.Game(0)) { Data = new GameData(Standard()) };
        var units = Enumerable.Range(0, 100).Select(_ => natives.CreateUnit(natives.Player(0), Ids.Of("hfoo"), 0, 0, 0)!).ToList();
        var deaths = 0;
        foreach (var u in units)
        {
            var own = natives.CreateTrigger()!;
            natives.TriggerRegisterDeathEvent(own, u);
            natives.TriggerRegisterUnitEvent(own, u, natives.ConvertUnitEvent(53));
            natives.TriggerAddAction(own, () =>
            {
                deaths++;
                natives.DestroyTrigger(own);
            });
        }
        var everyone = natives.CreateTrigger();
        natives.TriggerRegisterPlayerUnitEvent(everyone, natives.Player(0), natives.ConvertPlayerUnitEvent(20), null);
        Assert.Equal(101, natives.UnitEventLists);

        foreach (var u in units.Take(50)) natives.KillUnit(u);
        Assert.Equal((50, 51), (deaths, natives.UnitEventLists));
        foreach (var u in units.Skip(50)) natives.KillUnit(u);
        natives.DestroyTrigger(everyone);

        // the widget death ran first and destroyed the trigger, so its unit death never ran
        Assert.Equal((100, 0), (deaths, natives.UnitEventLists));
    }

    // probe units: the kinds a unit reads as
    [Theory]
    [InlineData("hfoo", "000010110000000000000000000")]
    [InlineData("Hpal", "100010110000000000000000000")]
    [InlineData("hbar", "001000000000000100000000000")]
    [InlineData("hgry", "000101101000000000000000000")]
    [InlineData("hpea", "000010110000000010000000000")]
    [InlineData("nshe", "000010000000000000000000000")]
    [InlineData("ugol", "001000000000001100000000000")]
    public void A_unit_is_of_the_kinds_its_type_and_weapons_say(string id, string kinds)
    {
        using var simulation = Run($$"""
            local u, bits = make("{{id}}"), {}
            for t = 0, 26 do bits[#bits + 1] = IsUnitType(u, ConvertUnitType(t)) and "1" or "0" end
            print(table.concat(bits))
            KillUnit(u)
            print(IsUnitType(u, UNIT_TYPE_DEAD))
            """);

        Assert.Equal([kinds, "true"], Printed(simulation));
    }

    // probe units: hero ids start with a capital letter; GetObjectName reads a unit's, item's, ability's or upgrade's
    // name and "Default string" for anything else
    [Fact]
    public void Object_names_are_units_items_abilities_and_upgrades()
    {
        var data = Standard();
        data.Absent[(ObjectKind.Unit, Ids.Of("fapo"))] = new StandardData().Add(ObjectKind.Unit, "fapo", Values("unam=Apothecary")).Objects.Values.Single();
        using var simulation = Run("""
            print(IsHeroUnitId(FourCC("Hpal")), IsHeroUnitId(FourCC("hfoo")), IsHeroUnitId(FourCC("Ozzz")))
            for _, id in ipairs({ "hfoo", "phea", "AHhb", "Rhme", "Bdef", "zzzz", "fapo" }) do print(id, GetObjectName(FourCC(id))) end
            print(GetObjectName(0), make("fapo"))
            """, data);

        Assert.Equal([
            "true\tfalse\ttrue",
            "hfoo\tFootman", "phea\tPotion of Healing", "AHhb\tHoly Light", "Rhme\tIron Forged Swords",
            "Bdef\tDefault string", "zzzz\tDefault string", "fapo\tApothecary",
            "Default string\tnil",
        ], Printed(simulation));
    }

    // probe units: integer fields read their type's words as common.j's numbers and their lists as flags; most sets
    // take, the tint and the primary attribute don't, a hero's level reads one more than set (from level 1 only:
    // adding the value fits too, D227)
    [Fact]
    public void Integer_fields_read_the_types_words_as_numbers()
    {
        using var simulation = Run("""
            local names = { "ARMOR_TYPE", "DEFENSE_TYPE", "HIT_POINTS_REGENERATION_TYPE", "MOVE_TYPE", "TARGETED_AS", "UNIT_CLASSIFICATION",
              "PLACEMENT_PREVENTED_BY", "PRIMARY_ATTRIBUTE", "LEVEL", "TINTING_COLOR_ALPHA" }
            for _, id in ipairs({ "hfoo", "Hpal", "hbar", "hgry" }) do
              local u, read = make(id), {}
              for _, name in ipairs(names) do read[#read + 1] = BlzGetUnitIntegerField(u, _G["UNIT_IF_" .. name]) end
              print(id, table.concat(read, " "))
            end
            local u, h = make("hfoo"), make("Hpal")
            print(BlzSetUnitIntegerField(u, UNIT_IF_DEFENSE_TYPE, 7), BlzGetUnitIntegerField(u, UNIT_IF_DEFENSE_TYPE),
              BlzSetUnitIntegerField(u, UNIT_IF_TINTING_COLOR_RED, 7), BlzGetUnitIntegerField(u, UNIT_IF_TINTING_COLOR_RED),
              BlzSetUnitIntegerField(u, UNIT_IF_PRIMARY_ATTRIBUTE, 7), BlzGetUnitIntegerField(u, UNIT_IF_PRIMARY_ATTRIBUTE),
              BlzSetUnitIntegerField(u, UNIT_IF_LEVEL, 5), GetUnitLevel(u))
            print(BlzSetUnitIntegerField(h, UNIT_IF_LEVEL, 3), GetUnitLevel(h), BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH),
              BlzSetUnitIntegerField(h, UNIT_IF_STRENGTH_WITH_BONUS, 30), BlzSetUnitIntegerField(h, UNIT_IF_PRIMARY_ATTRIBUTE, 2), BlzGetUnitIntegerField(h, UNIT_IF_PRIMARY_ATTRIBUTE))
            """);

        Assert.Equal([
            "hfoo\t2 2 1 1 2 0 0 1 2 255",
            "Hpal\t0 5 0 1 0 0 0 1 1 255",
            "hbar\t5 3 0 0 8 8 8 1 0 255",
            "hgry\t0 0 0 2 4 0 0 1 5 255",
            "true\t7\ttrue\t255\ttrue\t1\ttrue\t5",
            "true\t4\t22\tfalse\ttrue\t1",
        ], Printed(simulation));
    }

    // probe units: real fields read the unit's state where they are one; the hero gains refuse other units, pitch and
    // roll refuse all, the least attack range a unit without an attack; the shadow's size and sight take no set; a
    // flier's height keeps between its least and the most set
    [Fact]
    public void Real_fields_read_and_set_the_units_state()
    {
        using var simulation = Run("""
            local u, h, b, g = make("hfoo"), make("Hpal"), make("hbar"), make("hgry")
            print(BlzGetUnitRealField(u, UNIT_RF_HP), BlzGetUnitRealField(h, UNIT_RF_MANA), BlzGetUnitRealField(u, UNIT_RF_SPEED), BlzGetUnitRealField(u, UNIT_RF_DEFENSE),
              BlzGetUnitRealField(h, UNIT_RF_DEFENSE), BlzGetUnitRealField(u, UNIT_RF_SIGHT_RADIUS), BlzGetUnitRealField(u, UNIT_RF_MAXIMUM_PITCH_ANGLE_DEGREES),
              BlzGetUnitRealField(u, UNIT_RF_AGILITY_PER_LEVEL), BlzGetUnitRealField(h, UNIT_RF_AGILITY_PER_LEVEL), BlzGetUnitRealField(b, UNIT_RF_ACQUISITION_RANGE))
            print(BlzSetUnitRealField(u, UNIT_RF_HP, 100), GetWidgetLife(u), BlzSetUnitRealField(u, UNIT_RF_SPEED, 100), GetUnitMoveSpeed(u),
              BlzSetUnitRealField(u, UNIT_RF_TURN_RATE, 7.5), GetUnitTurnSpeed(u), BlzSetUnitRealField(u, UNIT_RF_FLY_HEIGHT, 50), GetUnitFlyHeight(u))
            print(BlzSetUnitRealField(u, UNIT_RF_AGILITY_PER_LEVEL, 7.5), BlzSetUnitRealField(u, UNIT_RF_MAXIMUM_ROLL_ANGLE_DEGREES, 7.5),
              BlzSetUnitRealField(b, UNIT_RF_MINIMUM_ATTACK_RANGE, 7.5), BlzSetUnitRealField(u, UNIT_RF_MINIMUM_ATTACK_RANGE, 7.5), BlzGetUnitRealField(u, UNIT_RF_MINIMUM_ATTACK_RANGE),
              BlzSetUnitRealField(u, UNIT_RF_SIGHT_RADIUS, 7.5), BlzGetUnitRealField(u, UNIT_RF_SIGHT_RADIUS), BlzSetUnitRealField(u, UNIT_RF_DEATH_TIME, 0.5), BlzGetUnitRealField(u, UNIT_RF_DEATH_TIME))
            BlzSetUnitRealField(g, UNIT_RF_FLY_HEIGHT, 100)
            local first = GetUnitFlyHeight(g)
            BlzSetUnitRealField(g, UNIT_RF_FLY_MAX_HEIGHT, 50)
            print(first, GetUnitFlyHeight(g), BlzGetUnitRealField(g, UNIT_RF_FLY_MAX_HEIGHT), GetUnitDefaultFlyHeight(g))
            """);

        Assert.Equal([
            "420.0\t255.0\t270.0\t2.0\t3.9\t800.0\t0.0\t0.0\t1.5\t0.0",
            "true\t100.0\ttrue\t150.0\ttrue\t1.216815\ttrue\t0.0",
            "false\tfalse\tfalse\ttrue\t7.5\ttrue\t800.0\ttrue\t0.5",
            "100.0\t90.0\t50.0\t240.0",
        ], Printed(simulation));
    }

    // probe units: boolean fields read the type's, decayable and raisable from the death type; string fields the
    // type's text and the unit's name; a unit that isn't a hero has no proper names and keeps none
    [Fact]
    public void Boolean_and_string_fields_read_the_types_and_take_sets()
    {
        using var simulation = Run("""
            for _, id in ipairs({ "hfoo", "Hpal", "hbar", "hgry" }) do
              local u = make(id)
              print(id, BlzGetUnitBooleanField(u, UNIT_BF_DECAYABLE), BlzGetUnitBooleanField(u, UNIT_BF_RAISABLE), BlzGetUnitBooleanField(u, UNIT_BF_IS_A_BUILDING),
                BlzGetUnitStringField(u, UNIT_SF_NAME), "[" .. BlzGetUnitStringField(u, UNIT_SF_PROPER_NAMES) .. "]")
            end
            local u, h = make("hfoo"), make("Hpal")
            print(BlzSetUnitBooleanField(u, UNIT_BF_DECAYABLE, false), BlzGetUnitBooleanField(u, UNIT_BF_DECAYABLE),
              BlzSetUnitStringField(u, UNIT_SF_NAME, "Field Name"), GetUnitName(u), BlzSetUnitStringField(u, UNIT_SF_PROPER_NAMES, "Probe"),
              "[" .. BlzGetUnitStringField(u, UNIT_SF_PROPER_NAMES) .. "]", BlzSetUnitStringField(h, UNIT_SF_PROPER_NAMES, "Probe"), BlzGetUnitStringField(h, UNIT_SF_PROPER_NAMES))
            """);

        Assert.Equal([
            "hfoo\ttrue\ttrue\tfalse\tFootman\t[]",
            "Hpal\ttrue\tfalse\tfalse\tPaladin\t[Granis Darkhammer]",
            "hbar\ttrue\tfalse\ttrue\tBarracks\t[]",
            "hgry\tfalse\tfalse\tfalse\tGryphon Rider\t[]",
            "true\tfalse\ttrue\tField Name\ttrue\t[]\ttrue\tProbe",
        ], Printed(simulation));
    }

    // probes units-attribute and units-primary: a hero attribute read on a unit that isn't a hero, and its primary
    // attribute set to one, close the game; a hero attribute set there is inferred to (D227), as only the read is
    // measured; on a hero they read and set
    [Theory]
    [InlineData("print(BlzGetUnitIntegerField(make('hfoo'), UNIT_IF_AGILITY))", "AGILITY", "uagc")]
    [InlineData("print(BlzSetUnitIntegerField(make('hbar'), UNIT_IF_STRENGTH_PERMANENT, 1))", "STRENGTH_PERMANENT", "ustm")]
    [InlineData("print(BlzSetUnitIntegerField(make('hfoo'), UNIT_IF_PRIMARY_ATTRIBUTE, 3))", "PRIMARY_ATTRIBUTE", "upra")]
    public void A_hero_attribute_on_a_unit_that_is_no_hero_closes_the_game(string main, string name, string code)
    {
        using var simulation = Run(main);

        var stop = Assert.IsType<GameClosedException>(simulation.Stopped?.InnerException);
        Assert.Empty(simulation.Log);
        Assert.Contains(name == "PRIMARY_ATTRIBUTE" ? "primary attribute of hfoo" : $"({code})", stop.Message);
    }

    [Fact]
    public void Hero_attributes_read_on_a_hero()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            print(BlzGetUnitIntegerField(h, UNIT_IF_AGILITY), BlzGetUnitIntegerField(h, UNIT_IF_INTELLIGENCE_WITH_BONUS), BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_PERMANENT),
              BlzSetUnitIntegerField(h, UNIT_IF_AGILITY, 7), BlzGetUnitIntegerField(h, UNIT_IF_AGILITY), BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_WITH_BONUS),
              BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_PERMANENT))
            """);

        // with no bonuses yet, the attribute with its bonus reads the attribute (W2-3 brings bonuses and the permanent ones)
        Assert.Equal(["13\t17\t22\ttrue\t7\t7\t7"], Printed(simulation));
    }
}
