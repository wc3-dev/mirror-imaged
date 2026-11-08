namespace MirrorImaged.Natives;

public partial class unit
{
    internal bool CanSleep, Asleep, SleptAtNight; // SleptAtNight: whether it was night when its sleep was last settled
    internal double? WokenAt;
}

// Creeps asleep at night, as the probes units-more and units-more-2 measure them (docs/facts.md, "Units"). Whether a unit
// sleeps is settled as it is made, woken or allowed to sleep, and again when read after the day has turned, so nothing
// runs for it in between; a change to what that reads (death, pause, owner, the owner's no creep sleep) settles it first.
public sealed partial class GameNatives
{
    const int NeutralAggressive = 24, NoCreepSleep = 25; // the slot, PLAYER_STATE_NO_CREEP_SLEEP
    const double Wakeful = 2; // a woken creep sleeps again just over 2 s later (probe units-more-2: by 2.05 s, not at 2.0)
    static readonly int Sleeps = Code("usle");
    readonly HashSet<unit> _sleepers = []; // the units that can sleep or sleep, to settle when their owner's no creep sleep changes

    // Whether the unit would sleep now: a creep that can sleep, at night, its owner not set to keep creeps awake
    // (inferred: setting that woke none, probe units-more-2), dead or alive
    bool Sleepy(unit u) => u.CanSleep && u.Owner.Number == NeutralAggressive && !IsDay && u.Owner.States[NoCreepSleep] == 0;

    void FallAsleep(unit u)
    {
        u.CanSleep = u.Type.Integer(Sleeps) != 0;
        if (!u.CanSleep) return;
        _sleepers.Add(u);
        u.Asleep = Sleepy(u);
        u.SleptAtNight = !IsDay;
    }

    // Before the player's no creep sleep changes
    void SettleSleep(player p)
    {
        foreach (var u in _sleepers)
            if (u.Owner == p) Asleep(u);
    }

    // At dusk the creeps that can sleep fall asleep, at dawn the living unpaused ones wake; a woken one sleeps again
    // after a while (probes units-more and units-more-2)
    bool Asleep(unit u)
    {
        if (!u.CanSleep && !u.Asleep) return false;
        var night = !IsDay;
        if (night != u.SleptAtNight)
        {
            u.SleptAtNight = night;
            u.WokenAt = null;
            u.Asleep = night ? Sleepy(u) : u.Asleep && (u.Dead || u.Paused);
        }
        else if (!u.Asleep && u.WokenAt is { } woken && Seconds - woken > Wakeful && Sleepy(u))
        {
            u.WokenAt = null;
            u.Asleep = true;
        }
        return u.Asleep;
    }

    /// <summary>
    /// Whether the unit sleeps: a neutral hostile unit whose type can sleep (murlocs, not footmen) made at night sleeps
    /// at once, dead too; setting the time to day wakes the living unpaused ones at once and to night puts them to
    /// sleep, as do dawn and dusk; a woken one sleeps again just over 2 s later; no other player's units sleep (probes
    /// units-more and units-more-2). Close: the game wakes and sleeps them 0.2 to 0.25 s after dawn and dusk, here at
    /// the hour.
    /// </summary>
    [Native(Level.Close)]
    public bool UnitIsSleeping(unit? whichUnit) => whichUnit is not null && Asleep(whichUnit);

    /// <summary>Wakes it at once; it sleeps again just over 2 s later at night (probe units-more-2).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public void UnitWakeUp(unit? whichUnit)
    {
        if (whichUnit is null) return;
        Asleep(whichUnit);
        whichUnit.Asleep = false;
        whichUnit.WokenAt = Seconds;
    }

    /// <summary>Its type's canSleep, or what UnitAddSleep set, whoever owns it (probe units-more).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool UnitCanSleep(unit? whichUnit) => whichUnit?.CanSleep ?? false;

    /// <summary>Lets it sleep or not: false wakes it at once, true at night puts it to sleep at once (probes units-more and units-more-2).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public void UnitAddSleep(unit? whichUnit, bool add)
    {
        if (whichUnit is null) return;
        Asleep(whichUnit);
        whichUnit.CanSleep = add;
        whichUnit.Asleep = add && Sleepy(whichUnit);
        whichUnit.SleptAtNight = !IsDay;
        whichUnit.WokenAt = null;
        if (add) _sleepers.Add(whichUnit);
        else _sleepers.Remove(whichUnit);
    }

    /// <summary>False, also after UnitAddSleepPerm true (probe units-more).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool UnitCanSleepPerm(unit? whichUnit) => false;

    /// <summary>Changes nothing UnitCanSleepPerm or the unit's sleep reads, true or false (probe units-more).</summary>
    [Native(Level.Exact, Fact = MoreProbe)]
    public void UnitAddSleepPerm(unit? whichUnit, bool add)
    {
    }
}
