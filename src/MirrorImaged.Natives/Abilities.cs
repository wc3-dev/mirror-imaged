using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class ability
{
    internal GameObject Type = null!;
    internal unit Unit = null!;
    internal int Level = 1, Levels; // Levels is the level count ABILITY_IF_LEVELS gives this one, its type's at first
    internal bool Permanent, Hidden, Disabled; // what a hidden or disabled ability holds back is wave 3's and 4's
    internal readonly Dictionary<(int Field, int Level), object> Sets = []; // the field natives' sets, by the field keeping the column (GameData.Own); level -1 for a field without levels
    internal readonly Dictionary<int, int> Researched = []; // the upgrade levels its level counts, by upgrade
    internal double CooldownEnd, Cooldown; // a cooldown started without a cast: when it ends, and how long it is
}

public partial class unit
{
    /// <summary>The unit's abilities in the order BlzGetUnitAbilityByIndex gives them: one added goes first.</summary>
    internal readonly List<ability> Abilities = [];
}

public partial class player
{
    internal readonly HashSet<int> Unavailable = []; // SetPlayerAbilityAvailable's
}

// A unit's abilities without casting (probes abilities and abilities-lists, docs/facts.md "Abilities"; game-facts, "Spells
// and abilities"): adding and removing, levels, the ability handles, hiding, disabling and permanence, Avul and an
// upgrade raising an ability's level. The cast flow is wave 4's.
public sealed partial class GameNatives
{
    const string AbilitiesProbe = "facts.md, Abilities: probe abilities (11-08)";
    static readonly int Invulnerable = Code("Avul"), AbilityList = Code("uabi"), UpgradesUsed = Code("upgr"), AbilityCode = Code("code");

    // The abilities an upgrade raises may change level when the owner's research does (probe abilities-upgrades)
    readonly List<ability> _raised = [];

    ability? Find(unit? u, int id) => u?.Abilities.Find(a => a.Type.Id == id);

    // An ability takes its handle id when a native first gives it out, and keeps it (probe abilities)
    ability Out(ability a) => a.Id != 0 ? a : Make(a);

    ability Give(unit u, GameObject type, int at)
    {
        var a = new ability { Type = type, Unit = u, Levels = type.Levels };
        u.Abilities.Insert(at, a);
        if (Raises(u.Type).Any(raise => raise.Ability == type.Id))
        {
            _raised.Add(a);
            Research(a, false);
        }
        return a;
    }

    void Take(ability a)
    {
        a.Unit.Abilities.Remove(a);
        _raised.Remove(a);
        if (a.Id != 0) _game.Remove(a.Key);
    }

    // The abilities a unit starts with, in the order BlzGetUnitAbilityByIndex gives them (probe abilities-lists, 60 kinds of
    // unit): those the game gives it for what it is around its type's list, which it holds last first, a neutral or
    // allied building's selling (Anei) after it. Each must be in the game's data.
    void GiveAbilities(unit u)
    {
        var t = u.Type;
        bool Lists(string field) => t.Text(Code(field)) is { Length: > 0 } list && list != "_";
        var building = t.Integer(IsBuilding) != 0;
        var race = t.Text(Race)?.ToLowerInvariant();
        var ids = new List<string>(); // the game's own have no data: a type's list's must be in the data
        if (t.Integer(Code("usle")) != 0) ids.Add("ACsp");
        if (building) ids.Add("Aalr");
        if ("fly".Equals(t.Text(MoveType), StringComparison.OrdinalIgnoreCase)) ids.Add("Abdt");
        if (building || (Flags(t.Text(Classification), Categories) & 8) != 0)
            ids.Add(race switch { "orc" => "Afio", "undead" => "Afiu", "nightelf" => "Afin", _ => "Afih" });
        if (Lists("utra")) ids.Add("ARal");
        foreach (var id in (t.Text(AbilityList) ?? "").Split(',', StringSplitOptions.TrimEntries).Reverse())
        {
            ids.Add(id);
            if (id is "Aneu" or "Aall") ids.Add("Anei");
        }
        if (IsHero(t.Id)) ids.Add("AHer");
        if (t.Integer(Code("uaen")) > 0) ids.Add("Aatk");
        if (t.Integer(MoveSpeed) > 0) ids.Add("Amov");
        if (Lists("uupt")) ids.Add("Aupg");
        if (Lists("utra") || Lists("ures")) ids.Add("Aque");
        if (Lists("ubui")) ids.Add(race switch { "human" => "AHbu", "orc" => "AObu", "undead" => "AUbu", "nightelf" => "AEbu", "naga" => "AGbu", _ => "ANbu" });
        if (race == "undead" && !building) ids.Add("Ablr");
        if (Lists("umki")) ids.Add("Amai");
        if (Lists("usei")) ids.Add("Asei");
        if (Lists("useu")) ids.Add("Asel");
        var listed = (t.Text(AbilityList) ?? "").Split(',', StringSplitOptions.TrimEntries).ToHashSet();
        foreach (var id in ids)
            if (id.Length == 4 && Find(u, Code(id)) is null && (listed.Contains(id) ? Data.Object(ObjectKind.Ability, Code(id)) : Data.Bare(ObjectKind.Ability, Code(id))) is { } type)
                Give(u, type, u.Abilities.Count);
    }

    // A unit gone: its abilities' handles go with it (probe abilities: the id reads 0 after the step)
    void TakeAbilities(unit u)
    {
        foreach (var a in u.Abilities.ToList()) Take(a);
    }

    // What the upgrades a unit's type uses raise (effect rlev): the ability and its bonus at an upgrade level, base + mod
    // for each level after the first, the upgrade's last level's above it (probe abilities-upgrades: Skeletal Mastery
    // set to 2, past its 1, raised Raise Dead by 1)
    IEnumerable<(int Upgrade, int Ability, Func<int, int> Bonus)> Raises(GameObject unitType)
    {
        foreach (var name in (unitType.Text(UpgradesUsed) ?? "").Split(',', StringSplitOptions.TrimEntries))
            if (name.Length == 4 && Data.Object(ObjectKind.Upgrade, Code(name)) is { } upgrade)
                for (var effect = 1; effect <= 4; effect++)
                    if (upgrade.Text(Code($"gef{effect}")) == "rlev" && upgrade.Text(Code($"gco{effect}")) is { Length: 4 } raised)
                    {
                        float start = upgrade.Real(Code($"gba{effect}")), step = upgrade.Real(Code($"gmo{effect}"));
                        var last = Math.Max(upgrade.Levels, 1);
                        yield return (upgrade.Id, Code(raised), level => level <= 0 ? 0 : (int)(start + step * (Math.Min(level, last) - 1)));
                    }
    }

    // Brings an ability's level to its owner's research: by the bonus's change since it last counted, so a level set
    // in between moves with it (probe abilities-upgrades: set to 1 under research, 0 once unresearched). Given to a new
    // owner it only gains: what the old owner had researched stays.
    void Research(ability a, bool gainOnly)
    {
        foreach (var (upgrade, raised, bonus) in Raises(a.Unit.Type))
        {
            if (raised != a.Type.Id) continue;
            var (now, before) = (a.Unit.Owner.TechLevels.GetValueOrDefault(upgrade), a.Researched.GetValueOrDefault(upgrade));
            if (gainOnly && now <= before) continue;
            a.Level += bonus(now) - bonus(before);
            a.Researched[upgrade] = now;
        }
    }

    // The research of a player changed: its units' abilities follow
    void Research(player p)
    {
        foreach (var a in _raised.ToList())
            if (a.Unit.Owner == p) Research(a, false);
    }

    /// <summary>
    /// Adds the ability at level 1, first in the unit's list, and answers true, also on a dead unit; false for an
    /// ability the unit has (its level stays), an id that is no ability's (a buff's, an item's, a unit's, an upgrade's,
    /// 0) and one of no levels, Attack and Move (probe abilities). An upgrade the unit's type uses raises its level as for
    /// the type's own (probe abilities-upgrades). A hero's skill added fires no skill event and takes no point (game-facts).
    /// </summary>
    [Native(Level.Exact, Fact = AbilitiesProbe + "; facts.md, Abilities: probe abilities-upgrades; game-facts.md, Spells and abilities: UnitAddAbility (11-02)")]
    public bool UnitAddAbility(unit? whichUnit, int abilityId)
    {
        if (whichUnit is null || Find(whichUnit, abilityId) is not null || Data.Object(ObjectKind.Ability, abilityId) is not { Levels: > 0 } type) return false;
        Give(whichUnit, type, 0);
        return true;
    }

    /// <summary>
    /// Takes the ability off and answers true, its handle gone with it; false when the unit lacks it. An ability of the
    /// unit's type goes too (Defend, Attack, Move), and one made permanent (probe abilities; game-facts).
    /// </summary>
    [Native(Level.Exact, Fact = AbilitiesProbe + "; game-facts.md, Spells and abilities: UnitRemoveAbility (11-02)")]
    public bool UnitRemoveAbility(unit? whichUnit, int abilityId)
    {
        if (Find(whichUnit, abilityId) is not { } a) return false;
        Take(a);
        return true;
    }

    /// <summary>
    /// The ability's level on the unit, else that of one whose code it is (a footman's AInv is its Aihn's); 0 when it
    /// lacks both. A unit's own abilities are at level 1, Attack and Move too; a hero's skills it hasn't learned it lacks
    /// (probe abilities).
    /// </summary>
    [Native(Level.Exact, Fact = AbilitiesProbe)]
    public int GetUnitAbilityLevel(unit? whichUnit, int abilcode) =>
        (Find(whichUnit, abilcode) ?? whichUnit?.Abilities.Find(a => a.Type.Text(AbilityCode) is { Length: 4 } code && Code(code) == abilcode))?.Level ?? 0;

    /// <summary>The level set, kept from 1 to the ability's level count; 0, adding nothing, when the unit lacks it (probe abilities).</summary>
    [Native(Level.Exact, Fact = AbilitiesProbe + "; facts.md, Abilities: probe abilities-sets")]
    public int SetUnitAbilityLevel(unit? whichUnit, int abilcode, int level)
    {
        if (Find(whichUnit, abilcode) is not { } a) return 0;
        a.Level = Math.Clamp(level, 1, Math.Max(a.Levels, 1));
        return a.Level;
    }

    /// <summary>One level up, to one past the last at most (3 to 4 on Holy Light, not 5); the level; 0 when the unit lacks it (probe abilities).</summary>
    [Native(Level.Exact, Fact = AbilitiesProbe + "; facts.md, Abilities: probe abilities-sets")]
    public int IncUnitAbilityLevel(unit? whichUnit, int abilcode)
    {
        if (Find(whichUnit, abilcode) is not { } a) return 0;
        if (a.Level < a.Levels + 1) a.Level++;
        return a.Level;
    }

    /// <summary>One level down, not below 1; the level; 0 when the unit lacks it (probe abilities).</summary>
    [Native(Level.Exact, Fact = AbilitiesProbe)]
    public int DecUnitAbilityLevel(unit? whichUnit, int abilcode)
    {
        if (Find(whichUnit, abilcode) is not { } a) return 0;
        if (a.Level > 1) a.Level--;
        return a.Level;
    }

    /// <summary>
    /// The unit's ability, which takes a handle id the first time it is given out and is the same handle after; nil when
    /// the unit lacks it (probe abilities).
    /// </summary>
    [Native(Level.Exact, Fact = AbilitiesProbe)]
    public ability? BlzGetUnitAbility(unit? whichUnit, int abilId) => Find(whichUnit, abilId) is { } a ? Out(a) : null;

    /// <summary>
    /// The unit's ability at the index from 0: the one added last first, then the type's own list after the abilities the
    /// game gives it for what it is (probes abilities and abilities-lists); nil outside the list.
    /// </summary>
    [Native(Level.Exact, Fact = AbilitiesProbe + "; facts.md, Abilities: probe abilities-lists")]
    public ability? BlzGetUnitAbilityByIndex(unit? whichUnit, int index) =>
        whichUnit is not null && index >= 0 && index < whichUnit.Abilities.Count ? Out(whichUnit.Abilities[index]) : null;

    /// <summary>The ability's id; 0 for nil, and for one taken off its unit or gone with it (probe abilities).</summary>
    [Native(Level.Exact, Fact = AbilitiesProbe)]
    public int BlzGetAbilityId(ability? whichAbility) => whichAbility?.Type.Id ?? 0;

    /// <summary>
    /// Hides the ability's button; its level, handle and place stay, and one the unit lacks is added as usual later (probe
    /// abilities). Close: what a hidden button holds back is input's and orders' (waves 3 and 6).
    /// </summary>
    [Native(Level.Close)]
    public void BlzUnitHideAbility(unit? whichUnit, int abilId, bool flag)
    {
        if (Find(whichUnit, abilId) is { } a) a.Hidden = flag;
    }

    /// <summary>
    /// Disables the ability, its button hidden as asked; its level, handle and place stay (probe abilities). Close: what
    /// a disabled ability holds back is the cast's (wave 4).
    /// </summary>
    [Native(Level.Close)]
    public void BlzUnitDisableAbility(unit? whichUnit, int abilId, bool flag, bool hideUI)
    {
        if (Find(whichUnit, abilId) is not { } a) return;
        a.Disabled = flag;
        a.Hidden = hideUI;
    }

    /// <summary>
    /// True for an ability the unit has, whichever way, and false for one it lacks, adding nothing (probe abilities);
    /// UnitRemoveAbility still takes it off. Close: what it keeps through a morph is wave 4's (game-facts).
    /// </summary>
    [Native(Level.Close)]
    public bool UnitMakeAbilityPermanent(unit? whichUnit, bool permanent, int abilityId)
    {
        if (Find(whichUnit, abilityId) is not { } a) return false;
        a.Permanent = permanent;
        return true;
    }

    /// <summary>
    /// Kept for the player: its units' level, handle and place of the ability stay, a new unit has it, and it can still be
    /// added and levelled (probe abilities). Close: it takes the button off the command card (game-facts), which input
    /// and orders read (waves 3 and 6).
    /// </summary>
    [Native(Level.Close)]
    public void SetPlayerAbilityAvailable(player? whichPlayer, int abilid, bool avail)
    {
        if (whichPlayer is null) return;
        if (avail) whichPlayer.Unavailable.Remove(abilid);
        else whichPlayer.Unavailable.Add(abilid);
    }
}
