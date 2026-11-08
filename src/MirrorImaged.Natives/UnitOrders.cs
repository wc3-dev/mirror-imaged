using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

// The order events the unit natives give without orders, which are wave 3's (probes units, units-more, units-more-2 and
// units-bones; game-facts, "Orders and events"): SetUnitOwner's stop, PauseUnit's stunned order, and the undefend a
// unit's Defend gives it as it dies, goes, is paused, unpaused or given away.
public sealed partial class GameNatives
{
    const int Stop = 851972, Stunned = 851973, Undefending = 852056;
    const double Frame = 0.01; // when PauseUnit(false)'s order comes back: 0.01 s later, twice (probes units-more, units-more-2)

    static readonly int AbilityList = Code("uabi"), Defend = Code("Adef");

    // How many Defends the unit has: its abilities made from Adef, a map's copies too (game-facts)
    int Defends(unit u) => Abilities(u).Count(a => a?.BaseId == Defend);

    // shortcut: the unit's type's abilities (uabi), not the unit's own; W2-7's UnitAddAbility and UnitRemoveAbility change those.
    IEnumerable<GameObject?> Abilities(unit u) =>
        (u.Type.Text(AbilityList) ?? "").Split(',', StringSplitOptions.TrimEntries).Where(id => id.Length == 4).Select(id => Data.Object(ObjectKind.Ability, Code(id)));

    // undefend's order events, on the owner's registrations and the unit's
    void Undefend(unit u, int times)
    {
        for (var i = 0; i < times; i++) FireUnit(new(u, Order: Undefending), UnitIssuedOrder, PlayerUnitIssuedOrder);
    }

    /// <summary>
    /// A flag, not a count: true twice and false once leaves it unpaused (facts, Units; probe units). A living unit
    /// paused gets the stunned order (851973) as a point order, then undefend (852056) for each Defend; unpaused, a
    /// frame later (0.01 s) undefend for each Defend, or the stop it missed if it changed hands while paused; paused
    /// again before that, that order comes at once, with no stunned order. A dead unit's pause does nothing (probes
    /// units-more and units-more-2). Close: the game also gives back the order the unit had, which comes with orders
    /// (wave 3; game-facts: the move, 0.016 s later).
    /// </summary>
    [Native(Level.Close)]
    public void PauseUnit(unit? whichUnit, bool flag)
    {
        if (whichUnit is not { Dead: false } u || u.Paused == flag) return;
        u.Paused = flag;
        if (flag)
        {
            if (u.Unpausing is { } back)
            {
                u.Unpausing = null;
                back();
                return;
            }
            FireUnit(new(u, Order: Stunned), UnitIssuedPointOrder, PlayerUnitIssuedPointOrder);
            Undefend(u, Defends(u));
            return;
        }
        var given = u.GivenWhilePaused;
        u.GivenWhilePaused = false;
        Action giveBack = () =>
        {
            if (given) FireUnit(new(u, Order: Stop), UnitIssuedOrder, PlayerUnitIssuedOrder);
            else Undefend(u, Defends(u));
        };
        u.Unpausing = giveBack;
        _game.At(Game.Steps(Seconds + Frame), () =>
        {
            if (u.Unpausing != giveBack) return;
            u.Unpausing = null;
            giveBack();
        });
    }
}
