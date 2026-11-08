namespace MirrorImaged.Natives;

// The natives every widget takes: units (W2-2, probe units) and destructables (W2-4, probe destructables). An item's
// wait for W2-3 and stop the run as Missing, so these are Close until then, though Exact on what they take (D252).
public sealed partial class GameNatives
{
    /// <summary>A unit's or a destructable's life; 0 for nil.</summary>
    [Native(Level.Close)]
    public float GetWidgetLife(widget? whichWidget) => whichWidget switch
    {
        unit u => u.Life,
        destructable d => d.Life,
        _ => Unless(whichWidget, 0f),
    };

    /// <summary>
    /// A unit's as its life is set (<c>SetLife</c>: below 0.405 it dies; facts, Units: SetWidgetLife clamps and 0.3, 0
    /// and -5 kill), a destructable's as SetDestructableLife (probe destructables).
    /// </summary>
    [Native(Level.Close)]
    public void SetWidgetLife(widget? whichWidget, float newLife)
    {
        if (whichWidget is unit u) SetLife(u, newLife);
        else if (whichWidget is destructable d) SetDestructableLife(d, newLife);
        else Unless(whichWidget, 0);
    }

    [Native(Level.Close)]
    public float GetWidgetX(widget? whichWidget) => whichWidget switch
    {
        unit u => u.X,
        destructable d => d.X,
        _ => Unless(whichWidget, 0f),
    };

    [Native(Level.Close)]
    public float GetWidgetY(widget? whichWidget) => whichWidget switch
    {
        unit u => u.Y,
        destructable d => d.Y,
        _ => Unless(whichWidget, 0f),
    };

    /// <summary>
    /// Fires inside the widget's death: a unit's before its player-unit and unit events (probe units); a destructable's
    /// inside KillDestructable, a set of its life below 0.405 and RemoveDestructable (probe destructables).
    /// </summary>
    [Native(Level.Close)]
    public @event? TriggerRegisterDeathEvent(trigger? whichTrigger, widget? whichWidget) =>
        RegisterUnit(whichTrigger, Unless(whichWidget, whichWidget), null, Convert<widgetevent>(WidgetDeath));

    /// <summary>
    /// The widget of a widget death event, the unit or destructable seen as a widget (Lua's == holds with it); nil in a
    /// unit's other events and outside one.
    /// </summary>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + Destructables)]
    public widget? GetTriggerWidget() => (widget?)Current.Destructable ?? Response(WidgetDeath)?.Unit;

    // An item, the one widget left, stops the run as the native being Missing until W2-3 (D252)
    static T Unless<T>(widget? w, T value, [System.Runtime.CompilerServices.CallerMemberName] string native = "") =>
        w is null or unit or destructable ? value : throw new MissingNativeException(native);
}
