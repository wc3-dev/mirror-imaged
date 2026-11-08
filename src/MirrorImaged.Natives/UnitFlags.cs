namespace MirrorImaged.Natives;

// What a unit shows and is flagged with: pause, hide, invulnerable, its own data, colour, scale and tint.
public sealed partial class GameNatives
{
    /// <summary>A flag, not a count: true twice and false once leaves it unpaused (facts, Units).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: PauseUnit is a flag, not a count (10-21); " + UnitsProbe)]
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

    [Native(Level.Exact, Fact = UnitsProbe)]
    public void SetUnitInvulnerable(unit? whichUnit, bool flag)
    {
        if (whichUnit is not null) whichUnit.Invulnerable = flag;
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool BlzIsUnitInvulnerable(unit? whichUnit) => whichUnit?.Invulnerable ?? false;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public void SetUnitUserData(unit? whichUnit, int data)
    {
        if (whichUnit is not null) whichUnit.UserData = data;
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitUserData(unit? whichUnit) => whichUnit?.UserData ?? 0;
}
