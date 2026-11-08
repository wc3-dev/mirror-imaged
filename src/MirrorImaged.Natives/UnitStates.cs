namespace MirrorImaged.Natives;

// A unit's life, mana, place, facing, height, owner and flags, as the units probe and the facts' "Units" measure them.
public sealed partial class GameNatives
{
    const int LifeState = 0, MaxLifeState = 1, ManaState = 2, MaxManaState = 3; // UNIT_STATE_*

    /// <summary>A unit's life; 0 for nil.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetWidgetLife(widget? whichWidget) => whichWidget is unit u ? u.Life : 0;

    /// <summary>
    /// Kept up to the most life, as the most life less the damage that leaves, in floats (0.41 reads 0.4100037 on a
    /// footman of 420, 0.4099731 on a Paladin of 650); below 0.405 the unit dies, the death event inside the call
    /// reading life 0 (facts, Units; probe units). A dead unit stays dead, its life kept from 0 to its most.
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: SetWidgetLife clamps and 0.3, 0 and -5 kill (10-21); " + UnitsProbe)]
    public void SetWidgetLife(widget? whichWidget, float newLife)
    {
        if (whichWidget is unit u) SetLife(u, newLife);
    }

    void SetLife(unit u, float life)
    {
        var (max, old) = (u.MaxLife, u.Life);
        var now = max - (max - Math.Min(life, max));
        if (!u.Dead && now < Dying)
        {
            Kill(u);
            return;
        }
        u.Life = u.Dead ? Math.Max(now, 0) : now;
        StateChanged(u, LifeState, old, u.Life);
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetWidgetX(widget? whichWidget) => whichWidget is unit u ? u.X : 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetWidgetY(widget? whichWidget) => whichWidget is unit u ? u.Y : 0;

    /// <summary>Life, most life, mana and most mana; 0 for any other state.</summary>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitState(unit? whichUnit, unitstate? whichUnitState) => whichUnit is null ? 0 : whichUnitState?.Id switch
    {
        LifeState => whichUnit.Life,
        MaxLifeState => whichUnit.MaxLife,
        ManaState => whichUnit.Mana,
        MaxManaState => whichUnit.MaxMana,
        _ => 0,
    };

    /// <summary>
    /// Life as SetWidgetLife, mana kept from 0 up to the most mana; the most life and mana don't change this way
    /// (facts, Units: BlzSetUnitMaxHP and BlzSetUnitMaxMana do).
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: SetUnitState of the maxima changes nothing; mana clamps (10-21); " + UnitsProbe)]
    public void SetUnitState(unit? whichUnit, unitstate? whichUnitState, float newVal)
    {
        if (whichUnit is null) return;
        if (whichUnitState?.Id == LifeState) SetLife(whichUnit, newVal);
        else if (whichUnitState?.Id == ManaState) SetMana(whichUnit, newVal);
    }

    void SetMana(unit u, float mana)
    {
        var old = u.Mana;
        u.Mana = Math.Max(Math.Min(mana, u.MaxMana), 0);
        StateChanged(u, ManaState, old, u.Mana);
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int BlzGetUnitMaxHP(unit? whichUnit) => (int)(whichUnit?.MaxLife ?? 0);

    /// <summary>Keeps the life, cut to the new most life when it is lower (facts, Units).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: BlzSetUnitMaxHP keeps life and clamps it (10-21); " + UnitsProbe)]
    public void BlzSetUnitMaxHP(unit? whichUnit, int hp)
    {
        if (whichUnit is null) return;
        whichUnit.MaxLife = hp;
        if (whichUnit.Life > hp) SetLife(whichUnit, hp);
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public int BlzGetUnitMaxMana(unit? whichUnit) => (int)(whichUnit?.MaxMana ?? 0);

    [Native(Level.Exact, Fact = UnitsProbe)]
    public void BlzSetUnitMaxMana(unit? whichUnit, int mana)
    {
        if (whichUnit is null) return;
        whichUnit.MaxMana = mana;
        SetMana(whichUnit, whichUnit.Mana);
    }

    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitX(unit? whichUnit) => whichUnit?.X ?? 0;

    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitY(unit? whichUnit) => whichUnit?.Y ?? 0;

    /// <summary>Moves the unit, ignoring pathing and the map's bounds (facts, Units: x 5096 past an edge of 4096 stays).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: SetUnitX/Y ignore pathing and bounds (10-21); " + UnitsProbe)]
    public void SetUnitX(unit? whichUnit, float newX)
    {
        if (whichUnit is not null) whichUnit.X = newX;
    }

    [Native(Level.Exact, Fact = "game-facts.md, Units: SetUnitX/Y ignore pathing and bounds (10-21); " + UnitsProbe)]
    public void SetUnitY(unit? whichUnit, float newY)
    {
        if (whichUnit is not null) whichUnit.Y = newY;
    }

    /// <summary>The ground under the unit, not its height above it (facts, Units: 0 for a lifted footman on flat ground).</summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: BlzGetUnitZ reads the ground under the unit (10-27); " + UnitsProbe)]
    public float BlzGetUnitZ(unit? whichUnit) => whichUnit is null ? 0 : Data.Terrain?.Height(whichUnit.X, whichUnit.Y) ?? 0;

    /// <summary>As BlzGetUnitZ; Close: not measured, and the game may draw the unit elsewhere.</summary>
    [Native(Level.Close)]
    public float BlzGetLocalUnitZ(unit? whichUnit) => BlzGetUnitZ(whichUnit);
}
