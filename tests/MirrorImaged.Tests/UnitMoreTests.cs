using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// The rest of the units, against the probes units-more, units-more-2, units-types, units-bones and units-gold
/// (docs/facts.md, "Units"), on maps with the data the test gives (decisions D207): the standard values the probes read,
/// under the probe map's balance set (custom_v1).
/// </summary>
public sealed class UnitMoreTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("name=footman;unam=Footman;uhpm=420;udtm=3.04;udea=3;ufoo=2;umvt=foot;uaen=1;ua1w=normal;ua1g=ground;uabi=Adef,Aihn"))
            .Add(ObjectKind.Unit, "hrif", Values("name=rifleman;unam=Rifleman;uhpm=520;udtm=1.7;udea=3;ufoo=3;uaen=1;ua1w=instant;ua1g=air,ground;uabi=Aihn"))
            .Add(ObjectKind.Unit, "Hpal", Values("name=paladin;unam=Paladin;uhpm=100;umpm=0;umpi=100;ustr=22;uint=17;ufoo=5;udtm=1.5;udea=2;uaen=1;ua1w=normal;ua1g=ground"))
            .Add(ObjectKind.Unit, "Hssa", Values("name=paladin;unam=Sir Gregory"))
            .Add(ObjectKind.Unit, "hbar", Values("name=humanbarracks;ubdg=1;utyp=Mechanical;udtm=2.34;udea=2;uhpm=1500"))
            .Add(ObjectKind.Unit, "hhou", Values("name=farm;ubdg=1;ufma=6;uhpm=500;udtm=2.34;udea=2"))
            .Add(ObjectKind.Unit, "htow", Values("name=townhall;ubdg=1;ufma=12;uupt=hkee;uhpm=1500;utyp=townhall"))
            .Add(ObjectKind.Unit, "hkee", Values("name=keep;ubdg=1;ufma=12;uupt=hcas;uhpm=1800;utyp=townhall"))
            .Add(ObjectKind.Unit, "hcas", Values("name=castle;ubdg=1;ufma=12;uhpm=2500;utyp=townhall"))
            .Add(ObjectKind.Unit, "hgry", Values("name=gryphonrider;uhpm=825;umvt=fly;uaen=1;ua1w=missile;ua1g=air,ground"))
            .Add(ObjectKind.Unit, "nmrl", Values("name=murloctiderunner;uhpm=210;usle=1;udtm=3;udea=3"))
            .Add(ObjectKind.Unit, "ngol", Values("name=goldmine;ubdg=1;uhpm=2000;uabi=Agld,Avul"))
            .Add(ObjectKind.Unit, "ugol", Values("name=hauntedgoldmine;ubdg=1;uhpm=800;uabi=Abgl,Abgm"))
            .Add(ObjectKind.Ability, "Adef", Values("anam=Defend"))
            .Add(ObjectKind.Ability, "Aihn", Values("anam=Inventory"))
            .Add(ObjectKind.Ability, "Agld", Values("anam=Gold Mine;alev=1;Gld1:1=12500"))
            .Add(ObjectKind.Ability, "Abgm", Values("anam=Haunted Gold Mine"));
        foreach (var (key, value) in Values("StrHitPointBonus=25;IntManaBonus=15;DecayTime=2;BoneDecayTime=88;StructureDecayTime=30;Dawn=6;Dusk=18"))
            data.Constants[key] = value;
        return data;
    }

    Simulation Run(string main) => Simulation.Load(_maps.Build($$"""
        local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
        local function tf(v) return v and "t" or "f" end
        local function make(id, p) return CreateUnit(p or Player(0), FourCC(id), 0, 0, 0) end
        local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
        function config() end
        function main()
        clock = CreateTimer()
        TimerStart(clock, 1000, false, nil)
        {{main}}
        end
        """), "", data: Standard());

    static List<string> Printed(Simulation simulation) => [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];

    // probe units-more: a type by its unitui.slk name in any case, the first in the tables of two (paladin), never by id
    [Fact]
    public void Units_go_by_their_own_names()
    {
        using var simulation = Run("""
            local function code(i) return i == 0 and "0" or string.pack(">i4", i) end
            print(code(UnitId("footman")), code(UnitId("FOOTMAN")), code(UnitId("paladin")), code(UnitId("hfoo")), code(UnitId("")), code(UnitId("nothing")))
            print(UnitId2String(FourCC("hfoo")), UnitId2String(FourCC("htow")), UnitId2String(FourCC("Adef")), UnitId2String(0) == "")
            local u = CreateUnitByName(Player(0), "Footman", 10, 20, 45)
            print(code(GetUnitTypeId(u)), GetUnitX(u), GetUnitFacing(u), CreateUnitByName(Player(0), "hfoo", 0, 0, 0))
            """);

        Assert.Equal(["hfoo\thfoo\tHpal\t0\t0\t0", "footman\ttownhall\t\ttrue", "hfoo\t10.0\t45.0\tnil"], Printed(simulation));
    }

    // probes units-more and units-types: sameness, no illusions, who can be selected, a type's kinds (never ground) and
    // the classification kinds a unit takes and gives up
    [Fact]
    public void Kinds_sameness_and_selection()
    {
        using var simulation = Run("""
            local u, v, d, h, r = make("hfoo"), make("hfoo"), make("hfoo"), make("hfoo"), make("hfoo")
            KillUnit(d) ShowUnit(h, false) RemoveUnit(r)
            print(tf(IsUnit(u, u)) .. tf(IsUnit(u, v)) .. tf(IsUnit(u, nil)) .. tf(IsUnit(nil, nil)), tf(IsUnitIllusion(u)))
            print(tf(BlzIsUnitSelectable(u)) .. tf(BlzIsUnitSelectable(d)) .. tf(BlzIsUnitSelectable(h)) .. tf(BlzIsUnitSelectable(r)) .. tf(BlzIsUnitSelectable(make("hbar"))))
            local function kinds(is) local s = "" for t = 0, 20 do s = s .. (is(ConvertUnitType(t)) and "1" or "0") end return s end
            print(kinds(function(t) return IsUnitIdType(FourCC("hfoo"), t) end), kinds(function(t) return IsUnitIdType(FourCC("hgry"), t) end),
              kinds(function(t) return IsUnitIdType(FourCC("hbar"), t) end), kinds(function(t) return IsUnitIdType(FourCC("zzzz"), t) end))
            print(tf(UnitAddType(u, UNIT_TYPE_PEON)) .. tf(UnitAddType(u, UNIT_TYPE_GIANT)) .. tf(UnitAddType(u, UNIT_TYPE_HERO)) .. tf(UnitAddType(u, UNIT_TYPE_GROUND)),
              kinds(function(t) return IsUnitType(u, t) end), BlzGetUnitIntegerField(u, UNIT_IF_UNIT_CLASSIFICATION), kinds(function(t) return IsUnitType(v, t) end))
            local b = make("hbar")
            print(tf(UnitRemoveType(b, UNIT_TYPE_MECHANICAL)) .. tf(IsUnitType(b, UNIT_TYPE_MECHANICAL)) .. tf(UnitRemoveType(b, UNIT_TYPE_STRUCTURE)) .. tf(IsUnitType(b, UNIT_TYPE_STRUCTURE)))
            """);

        Assert.Equal([
            "tfft\tf",
            "tffft",
            "000000110000000000000\t000101101000000000000\t001000000000000100000\t000000000000000000000",
            "ttff\t000010110100000010000\t17\t000010110000000000000",
            "tfft",
        ], Printed(simulation));
    }

    // probes units-more and units-more-2: the alarm is a structure's; the rest only shows or waits for its wave
    [Fact]
    public void Only_a_structure_ignores_the_alarm()
    {
        using var simulation = Run("""
            local b, f = make("hbar"), make("hfoo")
            print(tf(UnitIgnoreAlarmToggled(b)), tf(UnitIgnoreAlarm(b, true)) .. tf(UnitIgnoreAlarmToggled(b)), tf(UnitIgnoreAlarm(b, false)) .. tf(UnitIgnoreAlarmToggled(b)))
            print(tf(UnitIgnoreAlarm(f, true)) .. tf(UnitIgnoreAlarmToggled(f)))
            SetUnitCreepGuard(f, true) UnitSetUsesAltIcon(f, true) BlzShowUnitTeamGlow(f, false) SetPlayerUnitsOwner(Player(0), 1)
            SetUnitRescuable(f, Player(1), true) SetUnitRescueRange(f, 50)
            print(pid(GetOwningPlayer(f)), GetRescuer())
            """);

        Assert.Equal(["f\ttt\ttf", "ff", "0\tnil"], Printed(simulation));
    }

    // probe units-more: a living unit's food counts for its owner from when it is made until it dies or is gone, each
    // change firing the player's state events inside the call; the food it makes too; a corpse counts it and gives it
    // back; SetUnitUseFood takes only the food used; a set state is added to
    [Fact]
    public void Food_follows_the_living_units()
    {
        using var simulation = Run("""
            local p, q = Player(9), Player(10)
            local function food(label) print(label, GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_USED), GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP), GetPlayerState(q, PLAYER_STATE_RESOURCE_FOOD_USED)) end
            local t = CreateTrigger()
            TriggerRegisterPlayerStateEvent(t, p, PLAYER_STATE_RESOURCE_FOOD_USED, GREATER_THAN_OR_EQUAL, 0)
            TriggerAddAction(t, function() print("event", GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_USED)) end)
            local f = make("hfoo", p) make("Hpal", p) local farm = make("hhou", p)
            food("made")
            SetUnitUseFood(f, false) SetUnitUseFood(f, false) food("unused")
            SetUnitUseFood(f, true) SetUnitUseFood(farm, false) food("used")
            KillUnit(make("hfoo", p)) RemoveUnit(make("hfoo", p)) CreateCorpse(p, FourCC("hfoo"), 0, 0, 0) food("killed, removed, corpse")
            SetUnitOwner(f, q, false) food("given")
            SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_USED, 50) make("hfoo", p) food("set")
            print(GetFoodUsed(FourCC("hfoo")), GetFoodMade(FourCC("hhou")), GetFoodUsed(0))
            TimerStart(CreateTimer(), 0, false, function() food("after step") end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "event\t2", "event\t7", "made\t7\t6\t0",
            "event\t5", "unused\t5\t6\t0",
            "event\t7", "used\t7\t6\t0",
            "event\t9", "event\t7", "event\t9", "event\t11", "event\t9", "killed, removed, corpse\t9\t6\t0",
            "event\t7", "given\t7\t6\t2",
            "event\t50", "event\t52", "set\t52\t6\t2",
            "2\t6\t0",
            "event\t50", "after step\t50\t6\t2",
        ], Printed(simulation));
    }

    // probe units-more: the living units but structures, the structures, a type by name with its upgrades, and the tech
    // count of a unit type; a dead unit doesn't count, a removed one until it is gone
    [Fact]
    public void Counts_are_the_living_units()
    {
        using var simulation = Run("""
            local p, q = Player(11), Player(12)
            local function count(label)
              print(label, GetPlayerUnitCount(p, true), GetPlayerStructureCount(p, false), GetPlayerTypedUnitCount(p, "FOOTMAN", false, false),
                GetPlayerTypedUnitCount(p, "townhall", false, false), GetPlayerTypedUnitCount(p, "townhall", false, true), GetPlayerTypedUnitCount(p, "hfoo", false, false),
                GetPlayerTechCount(p, FourCC("htow"), true), GetPlayerTechCount(p, FourCC("htow"), false), GetPlayerUnitCount(q, false))
            end
            local f1, f2, f3 = make("hfoo", p), make("hfoo", p), make("hfoo", p)
            make("Hpal", p) make("htow", p) make("hkee", p) make("hcas", p) make("hbar", p)
            count("made")
            KillUnit(f1) RemoveUnit(f2) SetUnitOwner(f3, q, false) CreateCorpse(p, FourCC("hfoo"), 0, 0, 0)
            count("killed")
            TimerStart(CreateTimer(), 0, false, function() count("after step") end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "made\t4\t4\t3\t1\t3\t0\t1\t3\t0",
            "killed\t2\t4\t1\t1\t3\t0\t1\t3\t1",
            "after step\t1\t4\t0\t1\t3\t0\t1\t3\t1",
        ], Printed(simulation));
    }

    // probes units-more and units-more-2: timed life kills its duration later, the sooner of two; 0 makes the unit
    // summoned and never kills; it pauses and goes on; cancelling kills at once; the body decays as any other
    [Fact]
    public void Timed_life_kills_when_it_runs_out()
    {
        using var simulation = Run("""
            local function watch(label, u)
              local t = CreateTrigger()
              TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DEATH)
              TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DECAY)
              TriggerAddAction(t, function() print(label, GetTriggerEventId() == EVENT_UNIT_DEATH and "death" or "decay", now(), GetKillingUnit()) end)
              return u
            end
            local a = watch("a", make("hfoo")) UnitApplyTimedLife(a, FourCC("BTLF"), 1)
            local s = watch("sooner", make("hfoo")) UnitApplyTimedLife(s, FourCC("BTLF"), 3) UnitApplyTimedLife(s, FourCC("BTLF"), 0.5)
            local l = watch("later", make("hfoo")) UnitApplyTimedLife(l, FourCC("BTLF"), 0.5) UnitApplyTimedLife(l, FourCC("BTLF"), 2)
            local z = watch("zero", make("hfoo")) UnitApplyTimedLife(z, 0, 0)
            local p = watch("paused", make("hfoo")) UnitApplyTimedLife(p, FourCC("BTLF"), 1)
            local c = watch("cancelled", make("hfoo")) UnitApplyTimedLife(c, FourCC("BTLF"), 5)
            print(tf(IsUnitType(a, UNIT_TYPE_SUMMONED)) .. tf(IsUnitType(z, UNIT_TYPE_SUMMONED)), BlzGetUnitIntegerField(z, UNIT_IF_UNIT_CLASSIFICATION))
            BlzUnitCancelTimedLife(make("hfoo"))
            TimerStart(CreateTimer(), 0.25, false, function() UnitPauseTimedLife(p, true) end)
            TimerStart(CreateTimer(), 2, false, function() UnitPauseTimedLife(p, false) BlzUnitCancelTimedLife(c) end)
            TimerStart(CreateTimer(), 4, false, function() print("end", tf(UnitAlive(z))) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(4.5));

        Assert.Equal([
            "tt\t4",
            "sooner\tdeath\t0.50\tnil",
            "later\tdeath\t0.50\tnil",
            "a\tdeath\t1.00\tnil",
            "cancelled\tdeath\t2.00\tnil",
            "paused\tdeath\t2.75\tnil",
            "sooner\tdecay\t3.54\tnil",
            "later\tdecay\t3.54\tnil",
            "end\tt",
            "a\tdecay\t4.04\tnil",
        ], Printed(simulation));
    }

    // probes units-more and units-more-2: a state event fires inside the set when the value comes to pass the
    // comparison, for a death too; EQUAL, NOT_EQUAL and the most life never; mana that still passes fires again a step
    // later, once; a unit passing when registered fires nothing; outside, the event's state is life
    [Fact]
    public void A_state_event_fires_as_the_value_comes_to_pass()
    {
        using var simulation = Run("""
            local u, h, k, g = make("hfoo"), make("Hpal"), make("hfoo"), make("hfoo")
            SetWidgetLife(g, 100)
            local t = CreateTrigger()
            TriggerRegisterUnitStateEvent(t, u, UNIT_STATE_LIFE, LESS_THAN, 200)
            TriggerRegisterUnitStateEvent(t, u, UNIT_STATE_LIFE, EQUAL, 250)
            TriggerRegisterUnitStateEvent(t, u, UNIT_STATE_LIFE, NOT_EQUAL, 300)
            TriggerRegisterUnitStateEvent(t, h, UNIT_STATE_MANA, LESS_THAN_OR_EQUAL, 100)
            TriggerRegisterUnitStateEvent(t, h, UNIT_STATE_MAX_LIFE, GREATER_THAN, 700)
            TriggerRegisterUnitStateEvent(t, k, UNIT_STATE_LIFE, LESS_THAN, 200)
            TriggerRegisterUnitStateEvent(t, g, UNIT_STATE_LIFE, LESS_THAN, 200)
            TriggerAddAction(t, function()
              print("event", GetHandleId(GetTriggerEventId()), GetHandleId(GetEventUnitState()), pid(GetTriggerPlayer()), GetWidgetLife(GetTriggerUnit()), now())
            end)
            for _, v in ipairs({ 300, 250, 150, 100, 250, 150 }) do SetWidgetLife(u, v) print("life", v) end
            SetUnitState(h, UNIT_STATE_MANA, 100) print("mana")
            BlzSetUnitMaxHP(h, 800) KillUnit(k) SetWidgetLife(g, 50)
            print("outside", GetHandleId(GetEventUnitState()))
            """);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "life\t300", "life\t250",
            "event\t59\t0\t0\t150.0\t0.00", "life\t150",
            "life\t100", "life\t250",
            "event\t59\t0\t0\t150.0\t0.00", "life\t150",
            "event\t59\t2\t0\t650.0\t0.00", "mana",
            "event\t59\t0\t0\t0.0\t0.00",
            "outside\t0",
            "event\t59\t2\t0\t650.0\t0.00",
        ], Printed(simulation));
    }

    // probes units-more and units-more-2: a filter unit event's filter runs first and holds it back when it fails; a
    // death registered so never fires
    [Fact]
    public void A_filter_holds_a_unit_event_back_and_a_death_never_fires()
    {
        using var simulation = Run("""
            local u, v, w, x = make("hfoo"), make("hfoo"), make("hfoo"), make("hfoo")
            local t = CreateTrigger()
            TriggerRegisterFilterUnitEvent(t, u, EVENT_UNIT_DEATH, nil)
            TriggerRegisterFilterUnitEvent(t, v, EVENT_UNIT_CHANGE_OWNER, Filter(function() print("v runs", GetTriggerUnit() == v) return true end))
            TriggerRegisterFilterUnitEvent(t, w, EVENT_UNIT_CHANGE_OWNER, Filter(function() print("w runs") return false end))
            TriggerRegisterFilterUnitEvent(t, x, EVENT_UNIT_CHANGE_OWNER, nil)
            TriggerAddAction(t, function() print("event", GetHandleId(GetTriggerEventId()), GetTriggerUnit() == v, GetTriggerUnit() == x) end)
            KillUnit(u) SetUnitOwner(v, Player(1), false) SetUnitOwner(w, Player(1), false) SetUnitOwner(x, Player(1), false)
            local d = make("hfoo")
            local off = CreateTrigger()
            TriggerRegisterFilterUnitEvent(off, d, EVENT_UNIT_CHANGE_OWNER, Filter(function() print("disabled runs") return true end))
            DisableTrigger(off)
            SetUnitOwner(d, Player(1), false)
            print("disabled", GetTriggerEvalCount(off), GetTriggerExecCount(off))
            """);

        // a disabled trigger's filter doesn't run (from the code review); its event is counted, as without a filter
        Assert.Equal(["v runs\ttrue", "event\t287\ttrue\tfalse", "w runs", "event\t287\tfalse\ttrue", "disabled\t1\t0"], Printed(simulation));
    }

    // From the code review: a death's life state event, a food event and a stunned order's event that give the unit
    // away or kill it find the counts and food whole, and SetUnitOwner run again from inside one ends the outer call
    [Fact]
    public void Handlers_that_give_a_unit_away_leave_counts_and_food_whole()
    {
        using var simulation = Run("""
            local p9, p10, p11 = Player(9), Player(10), Player(11)
            local function state(label)
              print(label, GetPlayerUnitCount(p9, false), GetPlayerUnitCount(p10, false), GetPlayerUnitCount(p11, false),
                GetPlayerState(p9, PLAYER_STATE_RESOURCE_FOOD_USED), GetPlayerState(p10, PLAYER_STATE_RESOURCE_FOOD_USED), GetPlayerState(p11, PLAYER_STATE_RESOURCE_FOOD_USED))
            end
            local d = make("hfoo", p9)
            local s = CreateTrigger()
            TriggerRegisterUnitStateEvent(s, d, UNIT_STATE_LIFE, LESS_THAN, 200)
            TriggerAddAction(s, function() SetUnitOwner(d, p10, false) end)
            KillUnit(d)
            state("death given away")
            local g = make("hfoo", p9)
            local f, once = CreateTrigger(), true
            TriggerRegisterPlayerStateEvent(f, p9, PLAYER_STATE_RESOURCE_FOOD_USED, LESS_THAN, 1)
            TriggerAddAction(f, function() if once then once = false SetUnitOwner(g, p11, false) end end)
            local c = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(c, p10, EVENT_PLAYER_UNIT_CHANGE_OWNER, nil)
            TriggerRegisterPlayerUnitEvent(c, p11, EVENT_PLAYER_UNIT_CHANGE_OWNER, nil)
            TriggerAddAction(c, function() print("change", pid(GetChangingUnitPrevOwner()), pid(GetOwningPlayer(g))) end)
            SetUnitOwner(g, p10, false)
            state("given again inside")
            local k, l = make("hfoo"), make("hfoo")
            local o = CreateTrigger()
            TriggerRegisterUnitEvent(o, k, EVENT_UNIT_ISSUED_POINT_ORDER)
            TriggerRegisterUnitEvent(o, l, EVENT_UNIT_ISSUED_POINT_ORDER)
            TriggerRegisterUnitEvent(o, k, EVENT_UNIT_ISSUED_ORDER)
            TriggerRegisterUnitEvent(o, l, EVENT_UNIT_ISSUED_ORDER)
            TriggerAddAction(o, function()
              print("order", GetIssuedOrderId(), GetTriggerUnit() == k)
              if GetIssuedOrderId() == 851973 then
                if GetTriggerUnit() == k then KillUnit(k) else PauseUnit(l, false) end
              end
            end)
            PauseUnit(k, true) PauseUnit(l, true)
            print("end")
            """);

        Assert.Equal([
            "death given away\t0\t0\t0\t0\t0\t0",
            "change\t10\t11",
            "given again inside\t0\t0\t1\t0\t0\t2",
            "order\t851973\ttrue", "order\t852056\ttrue",
            "order\t851973\tfalse",
            "end",
        ], Printed(simulation));
    }

    // From the code review: what changes what a creep's sleep reads settles its sleep first, so dusk and dawn find it as
    // it was: a creep awake at noon, night set, then no creep sleep set, sleeps; one asleep, day set, then paused, wakes
    [Fact]
    public void Sleep_is_settled_before_what_it_reads_changes()
    {
        using var simulation = Run("""
            local na = Player(PLAYER_NEUTRAL_AGGRESSIVE)
            SetFloatGameState(GAME_STATE_TIME_OF_DAY, 12)
            local a = make("nmrl", na)
            SetFloatGameState(GAME_STATE_TIME_OF_DAY, 23)
            local c = make("nmrl", na)
            SetPlayerState(na, PLAYER_STATE_NO_CREEP_SLEEP, 1)
            print("no creep sleep set at night", tf(UnitIsSleeping(a)))
            SetFloatGameState(GAME_STATE_TIME_OF_DAY, 12)
            PauseUnit(c, true)
            print("paused at day", tf(UnitIsSleeping(c)))
            """);

        Assert.Equal(["no creep sleep set at night\tt", "paused at day\tf"], Printed(simulation));
    }

    // probes units-more and units-more-2: the order events of a Defend: a death's undefend before its death events; a
    // removal's twice for a living unit and once for a dead one as it goes, reading dead; a corpse's on its owner's; a
    // pause's stunned point order then undefend, an unpause's undefend 0.01 s later, or at once on a pause before it;
    // a paused unit given away gets no stop or undefend, and the stop on unpausing; a unit without Defend only stunned
    [Fact]
    public void A_Defend_gives_undefend_as_the_unit_dies_goes_and_is_paused()
    {
        using var simulation = Run("""
            local names = {}
            local o = CreateTrigger()
            for _, e in ipairs({ EVENT_PLAYER_UNIT_ISSUED_ORDER, EVENT_PLAYER_UNIT_ISSUED_POINT_ORDER, EVENT_PLAYER_UNIT_DEATH, EVENT_PLAYER_UNIT_CHANGE_OWNER }) do
              TriggerRegisterPlayerUnitEvent(o, Player(0), e, nil)
              TriggerRegisterPlayerUnitEvent(o, Player(1), e, nil)
            end
            TriggerAddAction(o, function()
              local u = GetTriggerUnit()
              print(names[u] or "?", GetHandleId(GetTriggerEventId()), GetIssuedOrderId(), pid(GetTriggerPlayer()), tf(IsUnitType(u, UNIT_TYPE_DEAD)) .. tf(IsUnitPaused(u)), now())
            end)
            local function unit(name, id) local u = make(id) names[u] = name TriggerRegisterUnitEvent(o, u, EVENT_UNIT_ISSUED_ORDER) return u end
            local k, r, d, p, m, g, rif = unit("k", "hfoo"), unit("r", "hfoo"), unit("d", "hfoo"), unit("p", "hfoo"), unit("m", "hfoo"), unit("g", "hfoo"), unit("rif", "hrif")
            print("kill") KillUnit(k)
            print("remove") RemoveUnit(r) KillUnit(d) RemoveUnit(d)
            print("corpse") CreateCorpse(Player(0), FourCC("hfoo"), 0, 0, 0)
            print("pause") PauseUnit(p, true) PauseUnit(p, true) PauseUnit(p, false) PauseUnit(p, false)
            print("again") PauseUnit(m, true) PauseUnit(m, false) PauseUnit(m, true)
            print("given") PauseUnit(g, true) SetUnitOwner(g, Player(1), false) PauseUnit(g, false)
            print("rifleman") PauseUnit(rif, true) PauseUnit(rif, false)
            print("end")
            """);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "kill", "k\t38\t852056\t0\ttf\t0.00", "k\t75\t852056\t0\ttf\t0.00", "k\t20\t0\t0\ttf\t0.00",
            "remove", "d\t38\t852056\t0\ttf\t0.00", "d\t75\t852056\t0\ttf\t0.00", "d\t20\t0\t0\ttf\t0.00",
            "corpse", "?\t38\t852056\t0\ttf\t0.00",
            "pause", "p\t39\t851973\t0\tft\t0.00", "p\t38\t852056\t0\tft\t0.00", "p\t75\t852056\t0\tft\t0.00",
            "again", "m\t39\t851973\t0\tft\t0.00", "m\t38\t852056\t0\tft\t0.00", "m\t75\t852056\t0\tft\t0.00",
            "m\t38\t852056\t0\tft\t0.00", "m\t75\t852056\t0\tft\t0.00",
            "given", "g\t39\t851973\t0\tft\t0.00", "g\t38\t852056\t0\tft\t0.00", "g\t75\t852056\t0\tft\t0.00", "g\t270\t0\t1\tft\t0.00",
            "rifleman", "rif\t39\t851973\t0\tft\t0.00",
            "end",
            "r\t38\t852056\t0\ttf\t0.00", "r\t75\t852056\t0\ttf\t0.00", "r\t38\t852056\t0\ttf\t0.00", "r\t75\t852056\t0\ttf\t0.00",
            "d\t38\t852056\t0\ttf\t0.00", "d\t75\t852056\t0\ttf\t0.00",
            "p\t38\t852056\t0\tff\t0.01", "p\t75\t852056\t0\tff\t0.01",
            "g\t38\t851972\t1\tff\t0.01", "g\t75\t851972\t1\tff\t0.01",
        ], Printed(simulation));
    }

    // probes units-more and units-more-2: a creep that can sleep, made at night, sleeps, dead too; a footman can't and
    // another player's murloc doesn't; woken, it sleeps again just over 2 s later; UnitAddSleep false wakes it and true
    // puts it to sleep; day wakes the living unpaused ones and night puts them to sleep
    [Fact]
    public void Creeps_sleep_at_night()
    {
        using var simulation = Run("""
            local na = Player(PLAYER_NEUTRAL_AGGRESSIVE)
            local all = { make("nmrl", na), make("hfoo", na), make("nmrl"), make("nmrl", na), make("nmrl", na), make("nmrl", na), make("nmrl", na) }
            KillUnit(all[5]) PauseUnit(all[6], true)
            local function read(label)
              local s = ""
              for _, u in ipairs(all) do s = s .. tf(UnitIsSleeping(u)) end
              print(label, s, tf(IsUnitType(all[1], UNIT_TYPE_SLEEPING)) .. tf(UnitCanSleep(all[3])) .. tf(UnitCanSleep(all[2])) .. tf(UnitCanSleepPerm(all[1])))
            end
            read("made")
            UnitWakeUp(all[1]) UnitAddSleep(all[7], false) read("woken")
            TimerStart(CreateTimer(), 2, false, function() read("2s") end)
            TimerStart(CreateTimer(), 2.05, false, function()
              read("2.05s")
              UnitAddSleep(all[7], true) read("let sleep")
              SetFloatGameState(GAME_STATE_TIME_OF_DAY, 12) read("noon")
              SetFloatGameState(GAME_STATE_TIME_OF_DAY, 23) read("night")
            end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal([
            "made\ttfftttt\tttff",
            "woken\tffftttf\tftff",
            "2s\tffftttf\tftff",
            "2.05s\ttfftttf\tttff",
            "let sleep\ttfftttt\tttff",
            "noon\tffffttf\tftff",
            "night\ttfftttt\tttff",
        ], Printed(simulation));
    }

    // probe units-gold: a gold mine's gold is its Max Gold until set, any amount, wrapping; other units keep none, a
    // haunted mine does; a dead mine keeps its gold
    [Fact]
    public void A_gold_mine_keeps_its_gold()
    {
        using var simulation = Run("""
            local m, h, f, b = make("ngol", Player(PLAYER_NEUTRAL_PASSIVE)), make("ugol"), make("hfoo"), make("hbar")
            print(GetResourceAmount(m), GetResourceAmount(h), GetResourceAmount(f), GetResourceAmount(nil))
            SetResourceAmount(m, 5000) AddResourceAmount(m, -6000)
            local low = GetResourceAmount(m)
            SetResourceAmount(m, 2147483647) AddResourceAmount(m, 1)
            SetResourceAmount(h, 300) SetResourceAmount(f, 100) AddResourceAmount(b, 5)
            print(low, GetResourceAmount(m), tf(UnitAlive(m)), GetResourceAmount(h), GetResourceAmount(f), GetResourceAmount(b))
            local d = make("ngol")
            KillUnit(d)
            print(GetResourceAmount(d))
            """);

        Assert.Equal(["12500\t0\t0\t0", "-1000\t-2147483648\tt\t300\t0\t0", "12500"], Printed(simulation));
    }

    // probe units-bones: a body's bones go DecayTime and BoneDecayTime (90 s) after its decay, a Defend giving undefend
    // then; a corpse 90 s after it is made
    [Fact]
    public void Bones_go_90_s_after_the_decay()
    {
        using var simulation = Run("""
            local f = make("hfoo")
            local c = CreateCorpse(Player(0), FourCC("hrif"), 0, 0, 0)
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, f, EVENT_UNIT_ISSUED_ORDER)
            TriggerRegisterUnitEvent(t, f, EVENT_UNIT_DECAY)
            TriggerAddAction(t, function() print(GetHandleId(GetTriggerEventId()), GetIssuedOrderId(), now(), GetUnitTypeId(f) ~= 0) end)
            KillUnit(f)
            TimerStart(CreateTimer(), 89.9, false, function() print("corpse", GetUnitTypeId(c) ~= 0) end)
            TimerStart(CreateTimer(), 90.1, false, function() print("corpse", GetUnitTypeId(c) ~= 0) end)
            TimerStart(CreateTimer(), 93.1, false, function() print("footman", GetUnitTypeId(f) ~= 0) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(94));

        Assert.Equal([
            "75\t852056\t0.00\ttrue",
            "54\t0\t3.04\ttrue",
            "corpse\ttrue",
            "corpse\tfalse",
            "75\t852056\t93.04\ttrue",
            "footman\tfalse",
        ], Printed(simulation));
    }
}
