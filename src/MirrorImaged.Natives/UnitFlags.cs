using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// What a unit shows and is flagged with: pause, hide, invulnerable, its own data, colour, scale and tint.
public sealed partial class GameNatives
{
    /// <summary>
    /// A flag, not a count: true twice and false once leaves it unpaused (facts, Units; probe units). Close: the game
    /// fires the stunned order (851973) and undefend on a pause and gives the unit its earlier move back on unpausing
    /// (game-facts), which come with orders (W2-10, wave 3).
    /// </summary>
    [Native(Level.Close)]
    public void PauseUnit(unit? whichUnit, bool flag)
    {
        if (whichUnit is not null) whichUnit.Paused = flag;
    }

    /// <summary>Whether PauseUnit paused the unit; BlzPauseUnitEx's pauses don't count here (probe units).</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool IsUnitPaused(unit? whichHero) => whichHero?.Paused ?? false;

    /// <summary>
    /// A count of its own, up for true and down for false, which IsUnitPaused doesn't read (probe units). Close: what
    /// such a pause holds back comes with orders and movement (wave 3).
    /// </summary>
    [Native(Level.Close)]
    public void BlzPauseUnitEx(unit? whichUnit, bool flag)
    {
        if (whichUnit is not null) whichUnit.Pauses += flag ? 1 : -1;
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public void ShowUnit(unit? whichUnit, bool show)
    {
        if (whichUnit is not null) whichUnit.Hidden = !show;
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool IsUnitHidden(unit? whichUnit) => whichUnit?.Hidden ?? false;

    /// <summary>
    /// Invulnerable is having Avul: true adds it first in the unit's list, false takes it off, also one UnitAddAbility
    /// added (probe abilities).
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + AbilitiesProbe)]
    public void SetUnitInvulnerable(unit? whichUnit, bool flag)
    {
        if (flag && whichUnit is not null && Find(whichUnit, Invulnerable) is null && Data.Object(ObjectKind.Ability, Invulnerable) is { } avul) Give(whichUnit, avul, 0);
        else if (!flag && Find(whichUnit, Invulnerable) is { } a) Take(a);
    }

    /// <summary>Whether the unit has Avul, from SetUnitInvulnerable or UnitAddAbility (probe abilities).</summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + AbilitiesProbe)]
    public bool BlzIsUnitInvulnerable(unit? whichUnit) => Find(whichUnit, Invulnerable) is not null;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public void SetUnitUserData(unit? whichUnit, int data)
    {
        if (whichUnit is not null) whichUnit.UserData = data;
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitUserData(unit? whichUnit) => whichUnit?.UserData ?? 0;
}
