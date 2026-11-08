using MirrorImaged.Data;
using MirrorImaged.Natives;
using MirrorImaged.World;

namespace MirrorImaged.Tests;

/// <summary>
/// Abilities on units without casting, against the probes abilities, abilities-fields, abilities-columns,
/// abilities-sets, abilities-types, abilities-lists, abilities-upgrades and abilities-boolean (docs/facts.md,
/// "Abilities") and the facts' "Spells and abilities" and "Files and Preload", on maps with the data the test gives
/// (decisions D207): the probe map's values (custom_v1) of what the probes used, with the metadata rows they need.
/// </summary>
public sealed class AbilityTests : IDisposable
{
    readonly TestMaps _maps = new();
    readonly Documents _documents = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Text(string column, PerLevel perLevel = PerLevel.None, int element = 0, string type = "string") => new(column, perLevel, element, type, true);

    static Field Column(string column, string type, params string[] specific) => new(column, PerLevel.Column, -1, type, false, [.. specific.Select(Ids.Of)]);

    // Each level's text in one key: "Tip=a|b|c" for levels 1 to 3
    static string Levels(string key, params string[] texts) => string.Join(";", texts.Select((text, i) => $"{key}:{i + 1}={text}"));

    const string Temp = @"ReplaceableTextures\CommandButtons\BTNTemp.blp";

    internal static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("arlv", new("reqLevel", PerLevel.None, -1, "int")), ("alsk", new("levelSkip", PerLevel.None, -1, "int")),
            ("apri", new("priority", PerLevel.None, -1, "int")), ("aher", new("hero", PerLevel.None, -1, "bool")), ("aite", new("item", PerLevel.None, -1, "bool")),
            ("achd", new("checkDep", PerLevel.None, -1, "bool")), ("code", new("code", PerLevel.None, -1)),
            ("anam", Text("Name")), ("atp1", Text("Tip", PerLevel.List, -1)), ("aub1", Text("Ubertip", PerLevel.List, -1)), ("aut1", Text("Untip", PerLevel.List, -1)),
            ("auu1", Text("Unubertip", PerLevel.List, -1)), ("aret", Text("Researchtip")), ("arut", Text("Researchubertip")), ("aart", Text("Art", type: "icon")),
            ("auar", Text("Unart", type: "icon")), ("arar", Text("ResearchArt", type: "icon")), ("aeat", Text("EffectArt", element: -1, type: "modelList")),
            ("abpx", Text("Buttonpos", type: "int")), ("abpy", Text("Buttonpos", element: 1, type: "int")), ("aubx", Text("UnButtonpos", type: "int")),
            ("auby", Text("UnButtonpos", element: 1, type: "int")), ("arpx", Text("Researchbuttonpos", type: "int")), ("arpy", Text("Researchbuttonpos", element: 1, type: "int")),
            ("atac", Text("Targetattachcount", type: "int")), ("acac", Text("Casterattachcount", type: "int")), ("amsp", Text("Missilespeed", type: "int")),
            ("amac", Text("Missilearc", type: "unreal")), ("aefs", Text("Effectsound", type: "soundLabel")), ("aefl", Text("Effectsoundlooped", type: "soundLabel")),
            ("acdn", Column("Cool", "unreal")), ("amcs", Column("Cost", "int")), ("aran", Column("Rng", "unreal")),
            ("Hhb1", Column("DataA", "unreal", "AHhb")), ("Htb1", Column("DataA", "unreal", "AHtb")), ("Hbz1", Column("DataA", "int", "AHbz")),
            ("Hbz2", Column("DataB", "unreal", "AHbz")), ("Hds1", Column("DataA", "bool", "AHds")), ("Def1", Column("DataA", "unreal", "Adef")),
        })
            data.Add(ObjectKind.Ability, id, field);
        data.Add(ObjectKind.Buff, "fefl", Text("Effectsoundlooped", type: "soundLabel"));
        data.Add(ObjectKind.Unit, "utip", Text("Tip")).Add(ObjectKind.Unit, "uico", Text("Art", type: "icon"));

        const string HolyTips = "Holy Light - [|cffffcc00Level 1|r]|Holy Light - [|cffffcc00Level 2|r]|Holy Light - [|cffffcc00Level 3|r]";
        const string BlizzardTips = "Blizzard - [|cffffcc00Level 1|r]|Blizzard - [|cffffcc00Level 2|r]|Blizzard - [|cffffcc00Level 3|r]";
        data.Add(ObjectKind.Ability, "AHhb", Values($"""
                code=AHhb;alev=3;arlv=1;aher=1;achd=1;anam=Holy Light;{Levels("atp1", HolyTips.Split('|'))};{Levels("aub1", "heal <AHhb,DataA1>", "heal <AHhb,DataA2>", "heal <AHhb,DataA3>")};
                aret=Learn Holy Light - [|cffffcc00Level %d|r];arut=learn <AHhb,DataA1>;aart=ReplaceableTextures\CommandButtons\BTNHolyBolt.blp;
                arar=ReplaceableTextures\CommandButtons\BTNHolyBolt.blp;abpx=0;abpy=2;arpx=0;arpy=0;acdn:1=5;acdn:2=5;acdn:3=5;amcs:1=65;amcs:2=65;amcs:3=65;
                aran:1=800;aran:2=800;aran:3=800;Hhb1:1=200;Hhb1:2=400;Hhb1:3=600
                """))
            .Add(ObjectKind.Ability, "AHbz", Values($"""
                code=AHbz;alev=3;arlv=1;aher=1;achd=1;anam=Blizzard;{Levels("atp1", BlizzardTips.Split('|'))};aret=Learn Blizzard - [|cffffcc00Level %d|r];
                abpx=0;abpy=2;acdn:1=6;acdn:2=6;acdn:3=6;amcs:1=75;amcs:2=75;amcs:3=75;Hbz1:1=6;Hbz1:2=8;Hbz1:3=10;Hbz2:1=30;Hbz2:2=40;Hbz2:3=50
                """))
            .Add(ObjectKind.Ability, "AHtb", Values("code=AHtb;alev=3;aher=1;achd=1;amsp=1000;Htb1:1=100;Htb1:2=225;Htb1:3=350;aart=ReplaceableTextures\\CommandButtons\\BTNStormBolt.blp"))
            .Add(ObjectKind.Ability, "ANsb", Values("code=AHtb;alev=4;Htb1:1=100"))
            .Add(ObjectKind.Ability, "AHds", Values($"code=AHds;alev=3;aher=1;achd=1;Hds1:1=0;Hds1:2=0;Hds1:3=0;aut1:1=Deactivate Divine Shield;acdn:1=35;acdn:2=50;acdn:3=65;aubx=1;auby=2"))
            .Add(ObjectKind.Ability, "Adef", Values("code=Adef;alev=1;achd=1;anam=Defend;atp1:1=Defend;aut1:1=Stop Defend;Def1:1=0.5;abpx=0;abpy=2;aubx=0;auby=2;aart=ReplaceableTextures\\CommandButtons\\BTNDefend.blp;auar=ReplaceableTextures\\CommandButtons\\BTNDefendStop.blp"))
            .Add(ObjectKind.Ability, "Aihn", Values("code=AInv;alev=1"))
            .Add(ObjectKind.Ability, "AInv", Values("code=AInv;alev=1"))
            .Add(ObjectKind.Ability, "Avul", Values("code=Avul;alev=1"))
            .Add(ObjectKind.Ability, "Aatk", Values("code=Aatk;alev=0;achd=1"))
            .Add(ObjectKind.Ability, "Amov", Values("code=Amov;alev=0"))
            .Add(ObjectKind.Ability, "Abds", Values("code=Abli;alev=1"))
            .Add(ObjectKind.Ability, "Aneu", Values("code=Aneu;alev=1"))
            .Add(ObjectKind.Ability, "Arai", Values("code=Arai;alev=2"))
            .Add(ObjectKind.Ability, "Arsp", Values("code=ANst;alev=1;aefs=StampedeCast;aefl=StampedeLoop"))
            .Add(ObjectKind.Buff, "Bmlc", Values("fefl=AerialShacklesLoop"))
            .Add(ObjectKind.Upgrade, "Rusm", Values("glvl=1;gef1=rlev;gco1=Arai;gba1=1;gmo1=1"))
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;uabi=Adef,Aihn;uaen=1;umvs=270;urac=human;upgr=Rhar,Rhme,Rhde;utip=Train Footman;uico=ReplaceableTextures\\CommandButtons\\BTNFootman.blp"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;ustr=22;uint=17;uabi=AInv;uhab=AHhb,AHds,AHre,AHad;uaen=1;umvs=270;urac=human"))
            .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;uabi=Ahar,Amil,Ahrp;uaen=1;umvs=190;urac=human;ubui=htow,hhou"))
            .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;uabi=Abds;ubdg=1;utyp=Mechanical;urac=human;utra=hfoo,hrif,hkni"))
            .Add(ObjectKind.Unit, "hgry", Values("unam=Gryphon Rider;uabi=Asth;umvt=fly;uaen=1;umvs=320;urac=human"))
            .Add(ObjectKind.Unit, "nshe", Values("unam=Sheep;uabi=Awan;umvs=100;urac=critters"))
            .Add(ObjectKind.Unit, "unec", Values("unam=Necromancer;uabi=Acri,Arai,Auhf,Aiun;uaen=1;umvs=270;urac=undead;upgr=Rusl,Rune,Rusm,Rupm"))
            .Add(ObjectKind.Unit, "ngme", Values("unam=Goblin Merchant;uabi=Aneu,Avul,Apit;ubdg=1;utyp=Mechanical,neutral;urac=other;usei=stwp,bspd"))
            .Add(ObjectKind.Unit, "nmrl", Values("unam=Murloc;usle=1;uaen=1;umvs=270;urac=creeps"))
            .Add(ObjectKind.Unit, "hmtt", Values("unam=Siege Engine;uabi=Srtt,Aroc;utyp=Mechanical;uaen=1;umvs=220;urac=human"));
        foreach (var id in new[] { "Ahar", "Amil", "Ahrp", "Asth", "Awan", "Acri", "Auhf", "Aiun", "Apit", "Srtt", "Aroc" })
            data.Add(ObjectKind.Ability, id, Values("alev=1"));
        return data;
    }

    Simulation Run(string main, StandardData? data = null) => Simulation.Load(_maps.Build($$"""
        local function show(v) return v == nil and "nil" or tostring(v) end
        local function make(id, p) return CreateUnit(p or Player(0), FourCC(id), 0, 0, 0) end
        local function lv(u, id) return GetUnitAbilityLevel(u, FourCC(id)) end
        local function with(id) local u = make("hfoo") UnitAddAbility(u, FourCC(id)) return u, BlzGetUnitAbility(u, FourCC(id)) end
        local function name(id) return id == 0 and "0" or string.pack(">I4", id) end
        local function list(u)
          local ids = {}
          for i = 0, 15 do
            local a = BlzGetUnitAbilityByIndex(u, i)
            if a == nil then break end
            ids[#ids + 1] = name(BlzGetAbilityId(a))
          end
          return table.concat(ids, ",") .. " " .. show(BlzGetUnitAbilityByIndex(u, -1))
        end
        function write(path, ...)
          PreloadGenClear()
          PreloadGenStart()
          for _, text in ipairs({...}) do Preload(text) end
          PreloadGenEnd(path)
        end
        function config() end
        function main()
        {{main}}
        end
        """), "", new SimulationOptions { Documents = _documents }, data ?? Standard());

    static List<string> Printed(Simulation simulation) => [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];

    // probe abilities, add: true and level 1 once, false for what is no ability and for one of no levels (Attack, Move);
    // a hero's skills it hasn't learned are no abilities of it; a dead unit takes one; a level read by its code too
    [Fact]
    public void An_ability_is_added_once_and_taken_off()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local hb = FourCC("AHhb")
            print("add " .. show(UnitAddAbility(u, hb)) .. " " .. lv(u, "AHhb") .. " again " .. show(UnitAddAbility(u, hb)) .. " " .. lv(u, "AHhb"))
            print("add other " .. show(UnitAddAbility(u, FourCC("zzzz"))) .. " " .. show(UnitAddAbility(u, FourCC("Bmlc"))) .. " " .. show(UnitAddAbility(u, FourCC("hfoo")))
              .. " " .. show(UnitAddAbility(u, FourCC("Rusm"))) .. " " .. show(UnitAddAbility(u, 0)) .. " " .. show(UnitAddAbility(nil, hb)) .. " " .. GetUnitAbilityLevel(nil, hb))
            print("own " .. lv(u, "Adef") .. " " .. lv(u, "Aihn") .. " " .. lv(u, "Aatk") .. " " .. lv(u, "Amov") .. " " .. lv(u, "AInv") .. " " .. lv(u, "Avul"))
            local p = make("Hpal")
            print("Hpal " .. lv(p, "AHhb") .. " " .. lv(p, "AHds") .. " " .. lv(p, "AInv") .. " " .. lv(p, "Aatk") .. " " .. show(BlzGetUnitAbility(p, hb)))
            local b = make("hbar")
            print("add Aatk " .. show(UnitAddAbility(b, FourCC("Aatk"))) .. " " .. lv(b, "Aatk") .. " Amov " .. show(UnitAddAbility(b, FourCC("Amov"))) .. " " .. lv(b, "Amov"))
            local d = make("hfoo")
            KillUnit(d)
            print("dead add " .. show(UnitAddAbility(d, hb)) .. " " .. lv(d, "AHhb") .. " " .. lv(d, "Adef") .. " remove " .. show(UnitRemoveAbility(d, hb)) .. " " .. lv(d, "AHhb"))
            print("remove " .. show(UnitRemoveAbility(u, hb)) .. " " .. lv(u, "AHhb") .. " again " .. show(UnitRemoveAbility(u, hb)))
            print("remove own " .. show(UnitRemoveAbility(u, FourCC("Adef"))) .. " " .. lv(u, "Adef") .. " add back " .. show(UnitAddAbility(u, FourCC("Adef"))) .. " " .. lv(u, "Adef"))
            print("remove Aatk " .. show(UnitRemoveAbility(u, FourCC("Aatk"))) .. " " .. lv(u, "Aatk") .. " Amov " .. show(UnitRemoveAbility(u, FourCC("Amov"))) .. " " .. lv(u, "Amov"))
            """);

        Assert.Equal([
            "add true 1 again false 1",
            "add other false false false false false false 0",
            "own 1 1 1 1 1 0",
            "Hpal 0 0 1 1 nil",
            "add Aatk false 0 Amov false 0",
            "dead add true 1 1 remove true 0",
            "remove true 0 again false",
            "remove own true 0 add back true 1",
            "remove Aatk true 0 Amov true 0",
        ], Printed(simulation));
    }

    // probe abilities, levels: a set kept from 1 to the levels, an increase one past them, a decrease to 1, each answering
    // the level; 0 for an ability the unit lacks; probe abilities-sets: the level count a field set gives
    [Fact]
    public void Levels_stay_between_one_and_one_past_the_last()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local hb = FourCC("AHhb")
            print("lacking " .. SetUnitAbilityLevel(u, hb, 2) .. " " .. IncUnitAbilityLevel(u, hb) .. " " .. DecUnitAbilityLevel(u, hb) .. " " .. lv(u, "AHhb"))
            UnitAddAbility(u, hb)
            local r = {}
            for _, n in ipairs({ 2, 4, 100, 0, -1, 3, 1 }) do r[#r + 1] = SetUnitAbilityLevel(u, hb, n) end
            print("set " .. table.concat(r, " ") .. " dec " .. DecUnitAbilityLevel(u, hb))
            r = {}
            for _ = 1, 4 do r[#r + 1] = IncUnitAbilityLevel(u, hb) end
            print("inc " .. table.concat(r, " ") .. " " .. lv(u, "AHhb") .. " set " .. SetUnitAbilityLevel(u, hb, 2) .. " " .. SetUnitAbilityLevel(u, hb, 5))
            print("defend " .. SetUnitAbilityLevel(u, FourCC("Adef"), 2) .. " " .. IncUnitAbilityLevel(u, FourCC("Adef")) .. " " .. IncUnitAbilityLevel(u, FourCC("Adef")))
            local a = BlzGetUnitAbility(u, hb)
            print("levels 1 " .. show(BlzSetAbilityIntegerField(a, ABILITY_IF_LEVELS, 1)) .. " " .. SetUnitAbilityLevel(u, hb, 3) .. " " .. IncUnitAbilityLevel(u, hb) .. " " .. IncUnitAbilityLevel(u, hb))
            BlzSetAbilityIntegerField(a, ABILITY_IF_LEVELS, 5)
            print("levels 5 " .. SetUnitAbilityLevel(u, hb, 9) .. " " .. BlzGetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 4))
            """);

        Assert.Equal([
            "lacking 0 0 0 0",
            "set 2 3 3 1 1 3 1 dec 1",
            "inc 2 3 4 4 4 set 2 3",
            "defend 1 2 2",
            "levels 1 true 1 2 2",
            "levels 5 5 0.0",
        ], Printed(simulation));
    }

    // probe abilities, handles: an ability takes its id when first given out and is the same handle after; taken off or
    // gone with its unit, its id reads 0
    [Fact]
    public void An_ability_is_a_handle_given_out_once()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local before = CreateTimer()
            local a = BlzGetUnitAbility(u, FourCC("Adef"))
            local between = CreateTimer()
            local b = BlzGetUnitAbility(u, FourCC("Adef"))
            print("ids " .. (GetHandleId(a) - GetHandleId(before)) .. " " .. (GetHandleId(between) - GetHandleId(a)) .. " " .. (GetHandleId(b) - GetHandleId(a))
              .. " " .. tostring(a):match("^%a+") .. " " .. show(a == b) .. " " .. show(rawequal(a, b)) .. " " .. name(BlzGetAbilityId(a)) .. " " .. BlzGetAbilityId(nil))
            UnitAddAbility(u, FourCC("AHhb"))
            UnitAddAbility(u, FourCC("AHtb"))
            print("added " .. list(u))
            UnitRemoveAbility(u, FourCC("Adef"))
            print("removed " .. list(u) .. " " .. BlzGetAbilityId(a))
            UnitAddAbility(u, FourCC("Adef"))
            local c = BlzGetUnitAbility(u, FourCC("Adef"))
            print("added back " .. list(u) .. " " .. show(GetHandleId(c) > GetHandleId(a)) .. " " .. show(a == c))
            local g = make("hfoo")
            local ga = BlzGetUnitAbility(g, FourCC("Adef"))
            RemoveUnit(g)
            print("unit removed " .. name(BlzGetAbilityId(ga)))
            TimerStart(CreateTimer(), 0, false, function() print("unit gone " .. BlzGetAbilityId(ga)) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(0.1));

        Assert.Equal([
            "ids 1 1 0 ability true true Adef 0",
            "added AHtb,AHhb,Aihn,Adef,Aatk,Amov nil",
            "removed AHtb,AHhb,Aihn,Aatk,Amov nil 0",
            "added back Adef,AHtb,AHhb,Aihn,Aatk,Amov nil true false",
            "unit removed Adef",
            "unit gone 0",
        ], Printed(simulation));
    }

    // probes abilities and abilities-lists: the abilities the game gives a unit for what it is, around its type's list,
    // which it holds last first, a neutral building's selling after it
    [Theory]
    [InlineData("hfoo", "Aihn,Adef,Aatk,Amov")]
    [InlineData("Hpal", "AInv,AHer,Aatk,Amov")]
    [InlineData("hpea", "Ahrp,Amil,Ahar,Aatk,Amov,AHbu")]
    [InlineData("hbar", "Aalr,Afih,ARal,Abds,Aque")]
    [InlineData("hgry", "Abdt,Asth,Aatk,Amov")]
    [InlineData("nshe", "Awan,Amov")]
    [InlineData("unec", "Aiun,Auhf,Arai,Acri,Aatk,Amov,Ablr")]
    [InlineData("ngme", "Aalr,Afih,Apit,Avul,Aneu,Anei,Asei")]
    [InlineData("nmrl", "ACsp,Aatk,Amov")]
    [InlineData("hmtt", "Afih,Aroc,Srtt,Aatk,Amov")]
    public void A_unit_starts_with_its_abilities_in_the_games_order(string id, string abilities)
    {
        using var simulation = Run($"""print(list(make("{id}")))""");

        Assert.Equal([abilities + " nil"], Printed(simulation));
    }

    // probe abilities: hiding, disabling, an unavailable ability and permanence change nothing a native reads; permanent
    // answers whether the unit has it
    [Fact]
    public void Hiding_disabling_and_permanence_leave_the_ability()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local df = FourCC("Adef")
            BlzUnitHideAbility(u, df, true)
            BlzUnitDisableAbility(u, df, true, true)
            print("hidden " .. lv(u, "Adef") .. " " .. list(u))
            BlzUnitHideAbility(u, FourCC("AHhb"), true)
            BlzUnitDisableAbility(u, FourCC("AHtb"), true, true)
            print("lacking " .. show(UnitAddAbility(u, FourCC("AHhb"))) .. " " .. show(UnitAddAbility(u, FourCC("AHtb"))))
            print("permanent " .. show(UnitMakeAbilityPermanent(u, true, df)) .. " " .. show(UnitMakeAbilityPermanent(u, false, df)) .. " "
              .. show(UnitMakeAbilityPermanent(u, true, FourCC("AHds"))) .. " " .. lv(u, "AHds") .. " " .. show(UnitMakeAbilityPermanent(nil, true, df)))
            UnitMakeAbilityPermanent(u, true, df)
            print("permanent removed " .. show(UnitRemoveAbility(u, df)))
            SetPlayerAbilityAvailable(Player(0), FourCC("AHds"), false)
            local v = make("hfoo")
            print("unavailable " .. show(UnitAddAbility(v, FourCC("AHds"))) .. " " .. SetUnitAbilityLevel(v, FourCC("AHds"), 2) .. " " .. lv(make("hfoo"), "Adef"))
            """);

        Assert.Equal([
            "hidden 1 Aihn,Adef,Aatk,Amov nil",
            "lacking true true",
            "permanent true true false 0 false",
            "permanent removed true",
            "unavailable true 2 1",
        ], Printed(simulation));
    }

    // probe abilities, invulnerable: invulnerable is having Avul, whichever way it came and goes
    [Fact]
    public void Invulnerable_is_having_Avul()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local vul = FourCC("Avul")
            SetUnitInvulnerable(u, true)
            print("set " .. lv(u, "Avul") .. " " .. show(BlzIsUnitInvulnerable(u)) .. " " .. list(u))
            print("remove " .. show(UnitRemoveAbility(u, vul)) .. " " .. show(BlzIsUnitInvulnerable(u)))
            print("add " .. show(UnitAddAbility(u, vul)) .. " " .. show(BlzIsUnitInvulnerable(u)))
            SetUnitInvulnerable(u, false)
            print("unset added " .. lv(u, "Avul") .. " " .. show(BlzIsUnitInvulnerable(u)))
            """);

        Assert.Equal([
            "set 1 true Avul,Aihn,Adef,Aatk,Amov nil",
            "remove true false",
            "add true true",
            "unset added 0 false",
        ], Printed(simulation));
    }

    // probe abilities-upgrades: Skeletal Mastery raises Raise Dead on the units whose type uses it, by its change since
    // it last counted; a new owner's research only adds
    [Fact]
    public void An_upgrade_raises_its_ability_on_the_units_using_it()
    {
        using var simulation = Run("""
            local p = Player(0)
            local n = make("unec")
            local f = make("hfoo")
            UnitAddAbility(f, FourCC("Arai"))
            local o = make("unec", Player(1))
            SetPlayerTechResearched(p, FourCC("Rusm"), 1)
            local m = make("unec")
            print("researched " .. lv(n, "Arai") .. " " .. lv(m, "Arai") .. " " .. lv(f, "Arai") .. " " .. lv(o, "Arai"))
            print("set " .. SetUnitAbilityLevel(n, FourCC("Arai"), 1) .. " inc " .. IncUnitAbilityLevel(m, FourCC("Arai")))
            SetPlayerTechResearched(p, FourCC("Rusm"), 0)
            print("unresearched " .. lv(n, "Arai") .. " " .. lv(m, "Arai") .. " new " .. lv(make("unec"), "Arai"))
            local k = make("unec")
            SetPlayerTechResearched(p, FourCC("Rusm"), 2)
            print("level 2 " .. lv(k, "Arai") .. " " .. lv(n, "Arai"))
            SetUnitOwner(k, Player(1), true)
            SetUnitOwner(o, p, true)
            print("given away " .. lv(k, "Arai") .. " given here " .. lv(o, "Arai"))
            local h = make("unec")
            UnitRemoveAbility(h, FourCC("Arai"))
            UnitAddAbility(h, FourCC("Arai"))
            print("re-added " .. lv(h, "Arai"))
            """);

        Assert.Equal([
            "researched 2 2 1 1",
            "set 1 inc 3",
            "unresearched 0 2 new 1",
            "level 2 2 1",
            "given away 2 given here 2",
            "re-added 2",
        ], Printed(simulation));
    }

    // probe abilities-fields, plain: the fields without levels as the data gives them, 1 attachment where it has none,
    // BTNTemp for an icon it lacks, the item flag false on a unit; probe abilities-sets: which refuse a set
    [Fact]
    public void Fields_without_levels_read_the_data_and_take_sets()
    {
        using var simulation = Run("""
            local _, a = with("AHhb")
            local ints = {}
            for _, f in ipairs({ ABILITY_IF_LEVELS, ABILITY_IF_REQUIRED_LEVEL, ABILITY_IF_BUTTON_POSITION_NORMAL_X, ABILITY_IF_BUTTON_POSITION_NORMAL_Y,
              ABILITY_IF_MISSILE_SPEED, ABILITY_IF_TARGET_ATTACHMENTS, ABILITY_IF_CASTER_ATTACHMENTS }) do ints[#ints + 1] = BlzGetAbilityIntegerField(a, f) end
            print(table.concat(ints, " ") .. " " .. BlzGetAbilityRealField(a, ABILITY_RF_ARF_MISSILE_ARC) .. " " .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_HERO_ABILITY))
              .. " " .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_ITEM_ABILITY)) .. " " .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_CHECK_DEPENDENCIES)))
            print(BlzGetAbilityStringField(a, ABILITY_SF_NAME) .. "|" .. BlzGetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND) .. "|" .. BlzGetAbilityStringField(a, ABILITY_SF_ICON_ACTIVATED)
              .. "|" .. BlzGetAbilityStringField(a, ABILITY_SF_ICON_RESEARCH) .. "|" .. BlzGetAbilityStringField(nil, ABILITY_SF_NAME) .. "|" .. BlzGetAbilityIntegerField(nil, ABILITY_IF_LEVELS))
            local r = {}
            for _, f in ipairs({ ABILITY_IF_LEVEL_SKIP_REQUIREMENT, ABILITY_IF_PRIORITY, ABILITY_IF_BUTTON_POSITION_NORMAL_X, ABILITY_IF_TARGET_ATTACHMENTS }) do
              r[#r + 1] = show(BlzSetAbilityIntegerField(a, f, 2)) .. BlzGetAbilityIntegerField(a, f)
            end
            print("sets " .. table.concat(r, " ") .. " hero " .. show(BlzSetAbilityBooleanField(a, ABILITY_BF_HERO_ABILITY, false)) .. " item "
              .. show(BlzSetAbilityBooleanField(a, ABILITY_BF_ITEM_ABILITY, true)) .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_ITEM_ABILITY))
              .. " name " .. show(BlzSetAbilityStringField(a, ABILITY_SF_NAME, "x")) .. " icon " .. show(BlzSetAbilityStringField(a, ABILITY_SF_ICON_ACTIVATED, "s"))
              .. BlzGetAbilityStringField(a, ABILITY_SF_ICON_ACTIVATED) .. " arc " .. show(BlzSetAbilityRealField(a, ABILITY_RF_ARF_MISSILE_ARC, 0.5)) .. BlzGetAbilityRealField(a, ABILITY_RF_ARF_MISSILE_ARC))
            print("other kind " .. show(BlzSetAbilityIntegerField(a, ConvertAbilityIntegerField(FourCC("amac")), 2)) .. " " .. BlzGetAbilityIntegerField(a, ConvertAbilityIntegerField(FourCC("anam")))
              .. " type " .. BlzGetAbilityPosX(FourCC("AHhb")) .. " other " .. BlzGetAbilityIntegerField(select(2, with("AHhb")), ABILITY_IF_BUTTON_POSITION_NORMAL_X))
            """);

        Assert.Equal([
            "3 1 0 2 0 1 1 0.0 true false true",
            @"Holy Light||" + Temp + @"|ReplaceableTextures\CommandButtons\BTNHolyBolt.blp||0",
            "sets false0 true2 true2 false1 hero false item truetrue name false icon trues arc true0.5",
            "other kind false 0 type 0 other 0",
        ], Printed(simulation));
    }

    // probes abilities-fields and abilities-columns: a level field from 0, 0 outside the levels and for a field of another
    // kind; a column read through another ability's field of its type, and not of another type; a set types the column
    [Fact]
    public void Level_fields_read_and_set_the_abilitys_columns()
    {
        using var simulation = Run("""
            local function levels(get, a, f) local r = {} for l = -1, 3 do r[#r + 1] = show(get(a, f, l)) end return table.concat(r, " ") end
            local I, R, B = BlzGetAbilityIntegerLevelField, BlzGetAbilityRealLevelField, BlzGetAbilityBooleanLevelField
            local hu, hb = with("AHhb")
            local _, bz = with("AHbz")
            print("mana " .. levels(I, hb, ABILITY_ILF_MANA_COST) .. " | cooldown " .. levels(R, hb, ABILITY_RLF_COOLDOWN) .. " | Hhb1 " .. levels(R, hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1))
            print("Htb1 " .. levels(R, hb, ABILITY_RLF_DAMAGE_HTB1) .. " | Hbz1 " .. levels(I, hb, ABILITY_ILF_NUMBER_OF_WAVES) .. " | Hds1 " .. levels(B, hb, ABILITY_BLF_CAN_DEACTIVATE)
              .. " | Def1 " .. levels(R, hb, ABILITY_RLF_DAMAGE_TAKEN_PERCENT_DEF1))
            print("kinds " .. levels(I, hb, ConvertAbilityIntegerLevelField(FourCC("acdn"))) .. " | " .. levels(R, bz, ConvertAbilityRealLevelField(FourCC("Hbz1")))
              .. " | " .. levels(R, bz, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1) .. " | " .. levels(I, bz, ABILITY_ILF_NUMBER_OF_WAVES))
            local _, other = with("AHhb")
            print("cooldown " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_COOLDOWN, 0, 2.5)) .. " " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_COOLDOWN, 3, 7))
              .. " " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_COOLDOWN, -1, 7)) .. " " .. levels(R, hb, ABILITY_RLF_COOLDOWN) .. " other " .. R(other, ABILITY_RLF_COOLDOWN, 0))
            print("Htb1 " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_DAMAGE_HTB1, 0, 5)) .. " " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0)
              .. " int " .. show(BlzSetAbilityIntegerLevelField(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0, 9)) .. " " .. I(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0) .. " " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0)
              .. " " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 1) .. " real " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0, 7.5))
              .. " " .. I(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0) .. " wrong kind " .. show(BlzSetAbilityIntegerLevelField(hb, ConvertAbilityIntegerLevelField(FourCC("acdn")), 0, 4)))
            print("level kept " .. SetUnitAbilityLevel(hu, FourCC("AHhb"), 2) .. " " .. R(hb, ABILITY_RLF_COOLDOWN, 0))
            local u = select(1, with("AHhb"))
            local a = BlzGetUnitAbility(u, FourCC("AHhb"))
            BlzSetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0, 1)
            UnitRemoveAbility(u, FourCC("AHhb"))
            UnitAddAbility(u, FourCC("AHhb"))
            print("re-added " .. R(BlzGetUnitAbility(u, FourCC("AHhb")), ABILITY_RLF_COOLDOWN, 0) .. " old " .. R(a, ABILITY_RLF_COOLDOWN, 0) .. " " .. show(BlzSetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0, 1)))
            """);

        Assert.Equal([
            "mana 0 65 65 65 0 | cooldown 0.0 5.0 5.0 5.0 0.0 | Hhb1 0.0 200.0 400.0 600.0 0.0",
            "Htb1 0.0 200.0 400.0 600.0 0.0 | Hbz1 0 0 0 0 0 | Hds1 false false false false false | Def1 0.0 200.0 400.0 600.0 0.0",
            "kinds 0 0 0 0 0 | 0.0 0.0 0.0 0.0 0.0 | 0.0 0.0 0.0 0.0 0.0 | 0 6 8 10 0",
            "cooldown true false false 0.0 2.5 5.0 5.0 0.0 other 5.0",
            "Htb1 true 5.0 int true 9 0.0 400.0 real true 0 wrong kind false",
            "level kept 2 2.5",
            "re-added 5.0 old 0.0 false",
        ], Printed(simulation));
    }

    // probes abilities-boolean and abilities-columns: a boolean level set where the column holds a boolean closes the game;
    // on another column it answers true, and reading it back closes the game; past the levels and of an unnamed field, false
    [Theory]
    [InlineData("BlzSetAbilityBooleanLevelField(select(2, with('AHds')), ABILITY_BLF_CAN_DEACTIVATE, 0, true)", "set on AHds")]
    [InlineData("""
        local _, a = with("AHhb")
        print(BlzSetAbilityBooleanLevelField(a, ABILITY_BLF_CAN_DEACTIVATE, 0, true))
        print(BlzGetAbilityBooleanLevelField(a, ABILITY_BLF_CAN_DEACTIVATE, 0))
        """, "read back")]
    public void A_boolean_level_field_set_closes_the_game(string main, string why)
    {
        using var simulation = Run(main);

        Assert.Contains(why, Assert.IsType<GameClosedException>(simulation.Stopped?.InnerException).Message);
        string[] printed = why == "read back" ? ["true"] : [];
        Assert.Equal(printed, Printed(simulation));
    }

    [Fact]
    public void A_boolean_level_field_set_past_the_levels_or_of_an_unnamed_field_is_refused()
    {
        using var simulation = Run("""
            local _, a = with("AHds")
            print(BlzSetAbilityBooleanLevelField(a, ABILITY_BLF_CAN_DEACTIVATE, 9, true), BlzSetAbilityBooleanLevelField(a, ConvertAbilityBooleanLevelField(FourCC("zzzz")), 0, true))
            """);

        Assert.Equal(["false\tfalse"], Printed(simulation));
    }

    // probes abilities-fields, abilities-columns and abilities-types: a text field reads its level's entry, the last outside
    // them and at -1; one the data lacks reads its default; a set within the entries is the ability's own, else lost
    [Fact]
    public void Text_fields_read_the_types_entries_and_keep_their_own()
    {
        using var simulation = Run("""
            local S = BlzGetAbilityStringLevelField
            local _, bz = with("AHbz")
            local _, other = with("AHbz")
            print(S(bz, ABILITY_SLF_TOOLTIP_NORMAL, -1) .. "|" .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 0) .. "|" .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 5)
              .. "|" .. S(bz, ABILITY_SLF_TOOLTIP_TURN_OFF, 0) .. "|" .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL_EXTENDED, 0) .. "|" .. S(bz, ABILITY_SLF_ICON_NORMAL, 1)
              .. "|" .. S(bz, ABILITY_SLF_TOOLTIP_LEARN, 2) .. "|" .. S(bz, ConvertAbilityStringLevelField(FourCC("anam")), 0))
            print(show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_NORMAL, 5, "five")) .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_NORMAL, -1, "minus"))
              .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_NORMAL, 2, "two")) .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_TURN_OFF, 0, "off"))
              .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_EFFECT, 0, "e.mdl")) .. show(BlzSetAbilityStringLevelField(bz, ConvertAbilityStringLevelField(FourCC("anam")), 0, "n"))
              .. " " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 2) .. "|" .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 9) .. "|" .. S(bz, ABILITY_SLF_TOOLTIP_TURN_OFF, 0) .. "|" .. S(bz, ABILITY_SLF_EFFECT, 0)
              .. "|" .. S(other, ABILITY_SLF_TOOLTIP_NORMAL, 2) .. "|" .. BlzGetAbilityTooltip(FourCC("AHbz"), 2))
            """);

        Assert.Equal([
            "Blizzard - [|cffffcc00Level 3|r]|Blizzard - [|cffffcc00Level 1|r]|Blizzard - [|cffffcc00Level 3|r]|Tool tip missing!||" + Temp
                + "|Learn Blizzard - [|cffffcc00Level %d|r]|",
            "truetruetruetruetruefalse two|two|Tool tip missing!||Blizzard - [|cffffcc00Level 3|r]|Blizzard - [|cffffcc00Level 3|r]",
        ], Printed(simulation));
    }

    // probes abilities-fields and abilities-sets: a cooldown and mana cost a level from 0, per unit, and a cooldown started
    // without a cast counting down with the game's time
    [Fact]
    public void Cooldowns_and_mana_costs_are_each_units()
    {
        using var simulation = Run("""
            local u = make("hfoo")
            local hb = FourCC("AHhb")
            local function cds(f) local r = {} for l = -1, 3 do r[#r + 1] = show(f(l)) end return table.concat(r, " ") end
            print("lacking " .. cds(function(l) return BlzGetUnitAbilityCooldown(u, hb, l) end) .. " type " .. cds(function(l) return BlzGetAbilityCooldown(hb, l) end)
              .. " " .. cds(function(l) return BlzGetAbilityManaCost(hb, l) end) .. " " .. BlzGetAbilityCooldown(FourCC("zzzz"), 0))
            UnitAddAbility(u, hb)
            BlzSetUnitAbilityCooldown(u, hb, 1, 7)
            BlzSetUnitAbilityManaCost(u, hb, 1, 12)
            BlzSetUnitAbilityCooldown(u, hb, 3, 7)
            print("set " .. cds(function(l) return BlzGetUnitAbilityCooldown(u, hb, l) end) .. " " .. cds(function(l) return BlzGetUnitAbilityManaCost(u, hb, l) end)
              .. " field " .. BlzGetAbilityRealLevelField(BlzGetUnitAbility(u, hb), ABILITY_RLF_COOLDOWN, 1) .. " type " .. BlzGetAbilityCooldown(hb, 1))
            print("remaining " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
            BlzStartUnitAbilityCooldown(u, hb, 4)
            BlzStartUnitAbilityCooldown(u, FourCC("AHtb"), 4)
            local w = make("hfoo")
            UnitAddAbility(w, FourCC("AHds"))
            BlzSetUnitAbilityCooldownRemaining(w, FourCC("AHds"), 3)
            print("started " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownRemaining(u, FourCC("AHtb")) .. " idle " .. BlzGetUnitAbilityCooldownRemaining(w, FourCC("AHds")))
            TimerStart(CreateTimer(), 1.0, false, function()
              print("1 s " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
              BlzAdjustUnitAbilityCooldownRemaining(u, hb, 2)
              print("adjusted " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
              BlzSetUnitAbilityCooldownPercent(u, hb, 0.25)
              print("percent " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
              BlzAdjustUnitAbilityCooldownPercent(u, hb, 0.25)
              print("adjusted percent " .. BlzGetUnitAbilityCooldownRemaining(u, hb))
              BlzSetUnitAbilityCooldownRemaining(u, hb, 2)
              print("set remaining " .. BlzGetUnitAbilityCooldownRemaining(u, hb))
              BlzAdjustUnitAbilityCooldownRemaining(u, hb, -10)
              print("below 0 " .. BlzGetUnitAbilityCooldownRemaining(u, hb))
              BlzStartUnitAbilityCooldown(u, hb, 8)
              BlzEndUnitAbilityCooldown(u, hb)
              print("ended " .. BlzGetUnitAbilityCooldownRemaining(u, hb))
              BlzStartUnitAbilityCooldown(u, hb, -1)
              print("negative " .. BlzGetUnitAbilityCooldownRemaining(u, hb))
              BlzStartUnitAbilityCooldown(u, hb, 8)
              UnitRemoveAbility(u, hb)
              UnitAddAbility(u, hb)
              print("re-added " .. BlzGetUnitAbilityCooldownRemaining(u, hb))
            end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(1.5));

        Assert.Equal([
            "lacking 0.0 0.0 0.0 0.0 0.0 type 0.0 5.0 5.0 5.0 0.0 0 65 65 65 0 0.0",
            "set 0.0 5.0 7.0 5.0 0.0 0 65 12 65 0 field 7.0 type 5.0",
            "remaining 0.0 0.0",
            "started 4.0 0.0 idle 0.0",
            "1 s 3.0 0.25",
            "adjusted 5.0 -0.25",
            "percent 1.0 0.75",
            "adjusted percent 2.0",
            "set remaining 2.0",
            "below 0 0.01",
            "ended 0.0",
            "negative 0.0",
            "re-added 0.0",
        ], Printed(simulation));
    }

    // probe abilities-types: a type's tooltips, icons and places, read for an ability, a unit, a buff and nothing, and set
    // within the entries the data has; a unit's ability reads them unless its own was set
    [Fact]
    public void A_types_tooltips_icons_and_places()
    {
        using var simulation = Run("""
            local bz, hb = FourCC("AHbz"), FourCC("AHhb")
            print(BlzGetAbilityTooltip(hb, -1) .. "|" .. BlzGetAbilityTooltip(hb, 0) .. "|" .. BlzGetAbilityTooltip(hb, 4) .. "|" .. BlzGetAbilityResearchTooltip(hb, 2)
              .. "|" .. BlzGetAbilityActivatedTooltip(hb, 0) .. "|" .. BlzGetAbilityActivatedTooltip(FourCC("AHds"), 3) .. "|" .. BlzGetAbilityActivatedExtendedTooltip(hb, 0))
            print(BlzGetAbilityIcon(hb) .. "|" .. BlzGetAbilityActivatedIcon(hb) .. "|" .. BlzGetAbilityPosX(hb) .. BlzGetAbilityPosY(hb) .. BlzGetAbilityActivatedPosX(FourCC("AHds")) .. BlzGetAbilityActivatedPosY(FourCC("AHds")))
            for _, id in ipairs({ "zzzz", "Bmlc", "hfoo" }) do
              local c = FourCC(id)
              print(id .. " " .. BlzGetAbilityTooltip(c, 0) .. "|" .. BlzGetAbilityExtendedTooltip(c, 0) .. "|" .. BlzGetAbilityResearchTooltip(c, 0) .. "|" .. BlzGetAbilityIcon(c) .. "|" .. BlzGetAbilityPosX(c))
            end
            local before = select(2, with("AHbz"))
            BlzSetAbilityTooltip(bz, "t0", 0)
            BlzSetAbilityTooltip(bz, "t2", 2)
            BlzSetAbilityTooltip(bz, "t5", 5)
            BlzSetAbilityTooltip(bz, "tm", -1)
            BlzSetAbilityTooltip(bz, "", 0)
            BlzSetAbilityResearchTooltip(bz, "r1", 1)
            BlzSetAbilityActivatedTooltip(bz, "a0", 0)
            BlzSetAbilityIcon(bz, "icon.blp")
            BlzSetAbilityActivatedIcon(bz, "on.blp")
            BlzSetAbilityPosX(bz, 3)
            BlzSetAbilityPosY(bz, -1)
            BlzSetAbilityActivatedPosX(bz, 7)
            BlzSetAbilityActivatedPosY(bz, 2)
            BlzSetAbilityTooltip(FourCC("zzzz"), "z", 0)
            BlzSetAbilityPosX(FourCC("zzzz"), 2)
            print(BlzGetAbilityTooltip(bz, -1) .. "|" .. BlzGetAbilityTooltip(bz, 0) .. "|" .. BlzGetAbilityTooltip(bz, 1) .. "|" .. BlzGetAbilityTooltip(bz, 5)
              .. "|" .. BlzGetAbilityResearchTooltip(bz, 1) .. "|" .. BlzGetAbilityActivatedTooltip(bz, 0) .. "|" .. BlzGetAbilityIcon(bz) .. "|" .. BlzGetAbilityActivatedIcon(bz)
              .. "|" .. BlzGetAbilityPosX(bz) .. BlzGetAbilityPosY(bz) .. BlzGetAbilityActivatedPosX(bz) .. BlzGetAbilityActivatedPosY(bz)
              .. "|" .. BlzGetAbilityTooltip(FourCC("zzzz"), 0) .. "|" .. BlzGetAbilityPosX(FourCC("zzzz")))
            local after = select(2, with("AHbz"))
            local S = BlzGetAbilityStringLevelField
            print(S(before, ABILITY_SLF_TOOLTIP_NORMAL, 0) .. "|" .. S(after, ABILITY_SLF_ICON_NORMAL, 0) .. "|" .. BlzGetAbilityIntegerField(before, ABILITY_IF_BUTTON_POSITION_NORMAL_X)
              .. "|" .. BlzGetAbilityStringField(after, ABILITY_SF_ICON_ACTIVATED))
            BlzSetAbilityStringLevelField(after, ABILITY_SLF_TOOLTIP_NORMAL, 1, "own")
            print(BlzGetAbilityTooltip(bz, 1) .. "|" .. S(after, ABILITY_SLF_TOOLTIP_NORMAL, 1) .. "|" .. S(before, ABILITY_SLF_TOOLTIP_NORMAL, 1))
            """);

        Assert.Equal([
            "Holy Light - [|cffffcc00Level 3|r]|Holy Light - [|cffffcc00Level 1|r]|Holy Light - [|cffffcc00Level 3|r]|Learn Holy Light - [|cffffcc00Level %d|r]"
                + "|Tool tip missing!|Deactivate Divine Shield|",
            @"ReplaceableTextures\CommandButtons\BTNHolyBolt.blp|" + Temp + "|0212",
            "zzzz Tool tip missing!||Tool tip missing!|" + Temp + "|0",
            "Bmlc Tool tip missing!||Tool tip missing!|" + Temp + "|0",
            @"hfoo Train Footman||Tool tip missing!|ReplaceableTextures\CommandButtons\BTNFootman.blp|0",
            "t2|t0|Blizzard - [|cffffcc00Level 2|r]|t2|Learn Blizzard - [|cffffcc00Level %d|r]|Tool tip missing!|icon.blp|on.blp|3202|Tool tip missing!|2",
            "t0|icon.blp|3|on.blp",
            "Blizzard - [|cffffcc00Level 2|r]|own|Blizzard - [|cffffcc00Level 2|r]",
        ], Printed(simulation));
    }

    // probe abilities-types: an ability's or a buff's sound labels by id, the effect's for any type but the looped one;
    // GetAbilitySound read "" for every string tried
    [Fact]
    public void Sound_labels_by_id()
    {
        using var simulation = Run("""
            local E, L = SOUND_TYPE_EFFECT, SOUND_TYPE_EFFECT_LOOPED
            print(GetAbilitySoundById(FourCC("Arsp"), E) .. "|" .. GetAbilitySoundById(FourCC("Arsp"), L) .. "|" .. GetAbilitySoundById(FourCC("Arsp"), nil)
              .. "|" .. GetAbilitySoundById(FourCC("AHhb"), E) .. "|" .. GetAbilitySoundById(FourCC("zzzz"), E) .. "|" .. GetAbilitySoundById(FourCC("Bmlc"), L)
              .. "|" .. GetAbilitySound("Arsp", E) .. "|" .. GetAbilitySound("Bmlc", L))
            local _, a = with("Arsp")
            BlzSetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND, "Own")
            print(BlzGetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND) .. "|" .. GetAbilitySoundById(FourCC("Arsp"), E))
            """);

        Assert.Equal(["StampedeCast|StampedeLoop|StampedeCast|||AerialShacklesLoop||", "Own|StampedeCast"], Printed(simulation));
    }

    // game-facts, Files and Preload: the tooltip carrier; a tooltip is kept only for the ability's levels (3 on Blizzard;
    // on a 100-level copy without tooltips, levels above 2 read level 2's; with "-" on all 100, 0 to 99 hold), pieces of
    // 200 come back whole, and tooltips make no handles (probe files: t0 200 200)
    [Fact]
    public void A_file_read_back_through_ability_tooltips()
    {
        var data = Standard()
            .Add(ObjectKind.Ability, "A0P1", Values($"code=AHtb;alev=100;{Levels("atp1", "a", "b", "c")}"))
            .Add(ObjectKind.Ability, "A0P2", Values($"code=AHtb;alev=100;{Levels("atp1", [.. Enumerable.Repeat("-", 100)])}"));
        using var simulation = Run("""
            local function tip(id, level, text) return "\")\ncall BlzSetAbilityTooltip('" .. id .. "', \"" .. text .. "\", " .. level .. ")\n//" end
            local pieces = {}
            for level = 0, 102 do pieces[#pieces + 1] = tip("A0P1", level, "p" .. level) pieces[#pieces + 1] = tip("A0P2", level, "q" .. level) end
            write("tips.pld", tip("AHbz", 0, "t0"), tip("AHbz", 2, string.rep("w", 200)), tip("AHbz", 5, "t5"), table.unpack(pieces))
            local before = CreateTimer()
            Preloader("tips.pld")
            local after = CreateTimer()
            local bz, p1, p2 = FourCC("AHbz"), FourCC("A0P1"), FourCC("A0P2")
            print(BlzGetAbilityTooltip(bz, 0) .. " " .. #BlzGetAbilityTooltip(bz, 2) .. " " .. #BlzGetAbilityTooltip(bz, 5) .. " " .. (GetHandleId(after) - GetHandleId(before)))
            print(BlzGetAbilityTooltip(p1, 2) .. " " .. BlzGetAbilityTooltip(p1, 3) .. " " .. BlzGetAbilityTooltip(p1, 99) .. " " .. BlzGetAbilityTooltip(p2, 99) .. " " .. BlzGetAbilityTooltip(p2, 100))
            """, data);

        Assert.Equal(["t0 200 200 1", "p2 p2 p2 q99 q99"], Printed(simulation));
    }

    // probes abilities-columns and data: a balance set's own text (Ubertip:custom,V1) is the key's on that set; a field of
    // another ability reads the column the ability keeps under its own field there, matched by its id or code
    [Fact]
    public void An_ability_keeps_a_column_under_its_own_field()
    {
        var data = new GameData(Standard());
        var (hb, sb, nsb) = (data.Object(ObjectKind.Ability, Ids.Of("AHhb"))!, data.Object(ObjectKind.Ability, Ids.Of("AHtb"))!, data.Object(ObjectKind.Ability, Ids.Of("ANsb"))!);

        Assert.Equal(Ids.Of("Hhb1"), data.Own(hb, Ids.Of("Htb1")));
        Assert.Equal(Ids.Of("Htb1"), data.Own(sb, Ids.Of("Hhb1")));
        Assert.Equal(Ids.Of("Htb1"), data.Own(nsb, Ids.Of("Def1")));
        Assert.Equal(Ids.Of("acdn"), data.Own(hb, Ids.Of("acdn")));
    }
}
