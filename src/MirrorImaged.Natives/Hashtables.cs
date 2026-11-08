namespace MirrorImaged.Natives;

public partial class hashtable
{
    // Under each parent key, each child key holds an integer, a real, a boolean, a string and a handle apart.
    internal readonly Dictionary<int, Dictionary<(int Child, Slot Slot), object>> Parents = [];
}

enum Slot { Integer, Real, Boolean, String, Handle }

// The hashtable natives as the probes hashtables and strings-2 measured them (docs/facts.md, "Strings, numbers and
// hashtables"): what a key holds of one kind leaves the others alone; a missing value loads as 0, 0.0, false, "" or
// null; at most 256 hashtables at once, and FlushParentHashtable destroys one, which gives its place back.
public sealed partial class GameNatives
{
    const string TablesFact = "docs/facts.md, Strings, numbers and hashtables (probes hashtables, strings-2)";
    int _hashtables;

    /// <summary>A new hashtable; null when 256 are in use.</summary>
    [Native(Level.Exact, Fact = TablesFact)]
    public hashtable? InitHashtable()
    {
        if (_hashtables == 256) return null;
        _hashtables++;
        return Make(new hashtable());
    }

    bool Alive(handle h) => _game.Handle(h.Key) == h;

    void Save(hashtable? table, int parentKey, int childKey, Slot slot, object value)
    {
        if (table is null || !Alive(table)) return;
        if (!table.Parents.TryGetValue(parentKey, out var children)) table.Parents.Add(parentKey, children = []);
        children[(childKey, slot)] = value;
    }

    object? Load(hashtable? table, int parentKey, int childKey, Slot slot) =>
        table is not null && table.Parents.TryGetValue(parentKey, out var children) ? children.GetValueOrDefault((childKey, slot)) : null;

    bool Have(hashtable? table, int parentKey, int childKey, Slot slot) =>
        table is not null && table.Parents.TryGetValue(parentKey, out var children) && children.ContainsKey((childKey, slot));

    void Remove(hashtable? table, int parentKey, int childKey, Slot slot)
    {
        if (table is not null && table.Parents.TryGetValue(parentKey, out var children)) children.Remove((childKey, slot));
    }

    // A null or destroyed table or handle saves nothing and gives false; a Convert native's value is saved and gives
    // false too.
    bool SaveHandle(hashtable? table, int parentKey, int childKey, handle? value)
    {
        if (table is null || !Alive(table) || value is null || !Alive(value)) return false;
        Save(table, parentKey, childKey, Slot.Handle, value);
        return !(_converted.TryGetValue((value.GetType(), value.Id), out var converted) && converted == value);
    }

    // A handle of another type, or one destroyed since, loads as null.
    T? LoadHandle<T>(hashtable? table, int parentKey, int childKey) where T : handle =>
        Load(table, parentKey, childKey, Slot.Handle) is T h && Alive(h) ? h : null;

    [Native(Level.Exact, Fact = TablesFact)]
    public void SaveInteger(hashtable? table, int parentKey, int childKey, int value) => Save(table, parentKey, childKey, Slot.Integer, value);

    [Native(Level.Exact, Fact = TablesFact)]
    public void SaveReal(hashtable? table, int parentKey, int childKey, float value) => Save(table, parentKey, childKey, Slot.Real, value);

    [Native(Level.Exact, Fact = TablesFact)]
    public void SaveBoolean(hashtable? table, int parentKey, int childKey, bool value) => Save(table, parentKey, childKey, Slot.Boolean, value);

    // A null string saved, which JASS can give and which loads as null (probe jass-threads); Lua refuses a nil one
    static readonly object NullString = new();

    /// <summary>Saves the string, null too, and gives true; false, saving nothing, with no table or a destroyed one.</summary>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool SaveStr(hashtable? table, int parentKey, int childKey, string? value)
    {
        if (table is null || !Alive(table)) return false;
        Save(table, parentKey, childKey, Slot.String, value ?? NullString);
        return true;
    }

    [Native(Level.Exact, Fact = TablesFact)]
    public int LoadInteger(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.Integer) as int? ?? 0;

    [Native(Level.Exact, Fact = TablesFact)]
    public float LoadReal(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.Real) as float? ?? 0;

    [Native(Level.Exact, Fact = TablesFact)]
    public bool LoadBoolean(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.Boolean) as bool? ?? false;

    [Native(Level.Exact, Fact = TablesFact)]
    public string? LoadStr(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.String) switch
    {
        string s => s,
        null => "",
        _ => null,
    };

    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedInteger(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Integer);

    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedReal(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Real);

    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedBoolean(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Boolean);

    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedString(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.String);

    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedHandle(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Handle);

    [Native(Level.Exact, Fact = TablesFact)]
    public void RemoveSavedInteger(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Integer);

    [Native(Level.Close)]
    public void RemoveSavedReal(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Real);

    [Native(Level.Close)]
    public void RemoveSavedBoolean(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Boolean);

    [Native(Level.Close)]
    public void RemoveSavedString(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.String);

    [Native(Level.Exact, Fact = TablesFact)]
    public void RemoveSavedHandle(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Handle);

    [Native(Level.Exact, Fact = TablesFact)]
    public void FlushChildHashtable(hashtable? table, int parentKey) => table?.Parents.Remove(parentKey);

    /// <summary>Empties the hashtable and destroys it, so it holds nothing more and another can be made in its place.</summary>
    [Native(Level.Exact, Fact = TablesFact)]
    public void FlushParentHashtable(hashtable? table)
    {
        if (table is null || !Alive(table)) return;
        table.Parents.Clear();
        _game.Remove(table.Key);
        _hashtables--;
    }

    // One handle a child key, whatever its type. Exact for the types the probes saved and loaded that a test can make here;
    // the probes' location and text tag saves gave true, but Location and CreateTextTag are Missing still.
    [Native(Level.Close)] public bool SaveAbilityHandle(hashtable? table, int parentKey, int childKey, ability? whichAbility) => SaveHandle(table, parentKey, childKey, whichAbility);
    [Native(Level.Exact, Fact = TablesFact)] public bool SaveAgentHandle(hashtable? table, int parentKey, int childKey, agent? whichAgent) => SaveHandle(table, parentKey, childKey, whichAgent);
    [Native(Level.Close)] public bool SaveBooleanExprHandle(hashtable? table, int parentKey, int childKey, boolexpr? whichBoolexpr) => SaveHandle(table, parentKey, childKey, whichBoolexpr);
    [Native(Level.Close)] public bool SaveButtonHandle(hashtable? table, int parentKey, int childKey, button? whichButton) => SaveHandle(table, parentKey, childKey, whichButton);
    [Native(Level.Close)] public bool SaveDefeatConditionHandle(hashtable? table, int parentKey, int childKey, defeatcondition? whichDefeatcondition) => SaveHandle(table, parentKey, childKey, whichDefeatcondition);
    [Native(Level.Close)] public bool SaveDestructableHandle(hashtable? table, int parentKey, int childKey, destructable? whichDestructable) => SaveHandle(table, parentKey, childKey, whichDestructable);
    [Native(Level.Close)] public bool SaveDialogHandle(hashtable? table, int parentKey, int childKey, dialog? whichDialog) => SaveHandle(table, parentKey, childKey, whichDialog);
    [Native(Level.Close)] public bool SaveEffectHandle(hashtable? table, int parentKey, int childKey, effect? whichEffect) => SaveHandle(table, parentKey, childKey, whichEffect);
    [Native(Level.Close)] public bool SaveFogModifierHandle(hashtable? table, int parentKey, int childKey, fogmodifier? whichFogModifier) => SaveHandle(table, parentKey, childKey, whichFogModifier);
    [Native(Level.Exact, Fact = TablesFact)] public bool SaveFogStateHandle(hashtable? table, int parentKey, int childKey, fogstate? whichFogState) => SaveHandle(table, parentKey, childKey, whichFogState);
    [Native(Level.Close)] public bool SaveForceHandle(hashtable? table, int parentKey, int childKey, force? whichForce) => SaveHandle(table, parentKey, childKey, whichForce);
    [Native(Level.Close)] public bool SaveFrameHandle(hashtable? table, int parentKey, int childKey, framehandle? whichFrameHandle) => SaveHandle(table, parentKey, childKey, whichFrameHandle);
    [Native(Level.Exact, Fact = TablesFact)] public bool SaveGroupHandle(hashtable? table, int parentKey, int childKey, group? whichGroup) => SaveHandle(table, parentKey, childKey, whichGroup);
    [Native(Level.Close)] public bool SaveHashtableHandle(hashtable? table, int parentKey, int childKey, hashtable? whichHashtable) => SaveHandle(table, parentKey, childKey, whichHashtable);
    [Native(Level.Close)] public bool SaveImageHandle(hashtable? table, int parentKey, int childKey, image? whichImage) => SaveHandle(table, parentKey, childKey, whichImage);
    [Native(Level.Close)] public bool SaveItemHandle(hashtable? table, int parentKey, int childKey, item? whichItem) => SaveHandle(table, parentKey, childKey, whichItem);
    [Native(Level.Close)] public bool SaveItemPoolHandle(hashtable? table, int parentKey, int childKey, itempool? whichItempool) => SaveHandle(table, parentKey, childKey, whichItempool);
    [Native(Level.Close)] public bool SaveLeaderboardHandle(hashtable? table, int parentKey, int childKey, leaderboard? whichLeaderboard) => SaveHandle(table, parentKey, childKey, whichLeaderboard);
    [Native(Level.Close)] public bool SaveLightningHandle(hashtable? table, int parentKey, int childKey, lightning? whichLightning) => SaveHandle(table, parentKey, childKey, whichLightning);
    [Native(Level.Close)] public bool SaveLocationHandle(hashtable? table, int parentKey, int childKey, location? whichLocation) => SaveHandle(table, parentKey, childKey, whichLocation);
    [Native(Level.Close)] public bool SaveMultiboardHandle(hashtable? table, int parentKey, int childKey, multiboard? whichMultiboard) => SaveHandle(table, parentKey, childKey, whichMultiboard);
    [Native(Level.Close)] public bool SaveMultiboardItemHandle(hashtable? table, int parentKey, int childKey, multiboarditem? whichMultiboarditem) => SaveHandle(table, parentKey, childKey, whichMultiboarditem);
    [Native(Level.Exact, Fact = TablesFact)] public bool SavePlayerHandle(hashtable? table, int parentKey, int childKey, player? whichPlayer) => SaveHandle(table, parentKey, childKey, whichPlayer);
    [Native(Level.Close)] public bool SaveQuestHandle(hashtable? table, int parentKey, int childKey, quest? whichQuest) => SaveHandle(table, parentKey, childKey, whichQuest);
    [Native(Level.Close)] public bool SaveQuestItemHandle(hashtable? table, int parentKey, int childKey, questitem? whichQuestitem) => SaveHandle(table, parentKey, childKey, whichQuestitem);
    [Native(Level.Close)] public bool SaveRectHandle(hashtable? table, int parentKey, int childKey, rect? whichRect) => SaveHandle(table, parentKey, childKey, whichRect);
    [Native(Level.Close)] public bool SaveRegionHandle(hashtable? table, int parentKey, int childKey, region? whichRegion) => SaveHandle(table, parentKey, childKey, whichRegion);
    [Native(Level.Close)] public bool SaveSoundHandle(hashtable? table, int parentKey, int childKey, sound? whichSound) => SaveHandle(table, parentKey, childKey, whichSound);
    [Native(Level.Close)] public bool SaveTextTagHandle(hashtable? table, int parentKey, int childKey, texttag? whichTexttag) => SaveHandle(table, parentKey, childKey, whichTexttag);
    [Native(Level.Close)] public bool SaveTimerDialogHandle(hashtable? table, int parentKey, int childKey, timerdialog? whichTimerdialog) => SaveHandle(table, parentKey, childKey, whichTimerdialog);
    [Native(Level.Exact, Fact = TablesFact)] public bool SaveTimerHandle(hashtable? table, int parentKey, int childKey, timer? whichTimer) => SaveHandle(table, parentKey, childKey, whichTimer);
    [Native(Level.Close)] public bool SaveTrackableHandle(hashtable? table, int parentKey, int childKey, trackable? whichTrackable) => SaveHandle(table, parentKey, childKey, whichTrackable);
    [Native(Level.Close)] public bool SaveTriggerActionHandle(hashtable? table, int parentKey, int childKey, triggeraction? whichTriggeraction) => SaveHandle(table, parentKey, childKey, whichTriggeraction);
    [Native(Level.Close)] public bool SaveTriggerConditionHandle(hashtable? table, int parentKey, int childKey, triggercondition? whichTriggercondition) => SaveHandle(table, parentKey, childKey, whichTriggercondition);
    [Native(Level.Close)] public bool SaveTriggerEventHandle(hashtable? table, int parentKey, int childKey, @event? whichEvent) => SaveHandle(table, parentKey, childKey, whichEvent);
    [Native(Level.Close)] public bool SaveTriggerHandle(hashtable? table, int parentKey, int childKey, trigger? whichTrigger) => SaveHandle(table, parentKey, childKey, whichTrigger);
    [Native(Level.Close)] public bool SaveUbersplatHandle(hashtable? table, int parentKey, int childKey, ubersplat? whichUbersplat) => SaveHandle(table, parentKey, childKey, whichUbersplat);
    [Native(Level.Close)] public bool SaveUnitHandle(hashtable? table, int parentKey, int childKey, unit? whichUnit) => SaveHandle(table, parentKey, childKey, whichUnit);
    [Native(Level.Close)] public bool SaveUnitPoolHandle(hashtable? table, int parentKey, int childKey, unitpool? whichUnitpool) => SaveHandle(table, parentKey, childKey, whichUnitpool);
    [Native(Level.Close)] public bool SaveWidgetHandle(hashtable? table, int parentKey, int childKey, widget? whichWidget) => SaveHandle(table, parentKey, childKey, whichWidget);

    [Native(Level.Close)] public ability? LoadAbilityHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<ability>(table, parentKey, childKey);
    [Native(Level.Close)] public boolexpr? LoadBooleanExprHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<boolexpr>(table, parentKey, childKey);
    [Native(Level.Close)] public button? LoadButtonHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<button>(table, parentKey, childKey);
    [Native(Level.Close)] public defeatcondition? LoadDefeatConditionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<defeatcondition>(table, parentKey, childKey);
    [Native(Level.Close)] public destructable? LoadDestructableHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<destructable>(table, parentKey, childKey);
    [Native(Level.Close)] public dialog? LoadDialogHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<dialog>(table, parentKey, childKey);
    [Native(Level.Close)] public effect? LoadEffectHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<effect>(table, parentKey, childKey);
    [Native(Level.Close)] public fogmodifier? LoadFogModifierHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<fogmodifier>(table, parentKey, childKey);
    [Native(Level.Exact, Fact = TablesFact)] public fogstate? LoadFogStateHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<fogstate>(table, parentKey, childKey);
    [Native(Level.Close)] public force? LoadForceHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<force>(table, parentKey, childKey);
    [Native(Level.Close)] public framehandle? LoadFrameHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<framehandle>(table, parentKey, childKey);
    [Native(Level.Exact, Fact = TablesFact)] public group? LoadGroupHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<group>(table, parentKey, childKey);
    [Native(Level.Close)] public hashtable? LoadHashtableHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<hashtable>(table, parentKey, childKey);
    [Native(Level.Close)] public image? LoadImageHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<image>(table, parentKey, childKey);
    [Native(Level.Close)] public item? LoadItemHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<item>(table, parentKey, childKey);
    [Native(Level.Close)] public itempool? LoadItemPoolHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<itempool>(table, parentKey, childKey);
    [Native(Level.Close)] public leaderboard? LoadLeaderboardHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<leaderboard>(table, parentKey, childKey);
    [Native(Level.Close)] public lightning? LoadLightningHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<lightning>(table, parentKey, childKey);
    [Native(Level.Close)] public location? LoadLocationHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<location>(table, parentKey, childKey);
    [Native(Level.Close)] public multiboard? LoadMultiboardHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<multiboard>(table, parentKey, childKey);
    [Native(Level.Close)] public multiboarditem? LoadMultiboardItemHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<multiboarditem>(table, parentKey, childKey);
    [Native(Level.Exact, Fact = TablesFact)] public player? LoadPlayerHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<player>(table, parentKey, childKey);
    [Native(Level.Close)] public quest? LoadQuestHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<quest>(table, parentKey, childKey);
    [Native(Level.Close)] public questitem? LoadQuestItemHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<questitem>(table, parentKey, childKey);
    [Native(Level.Close)] public rect? LoadRectHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<rect>(table, parentKey, childKey);
    [Native(Level.Close)] public region? LoadRegionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<region>(table, parentKey, childKey);
    [Native(Level.Close)] public sound? LoadSoundHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<sound>(table, parentKey, childKey);
    [Native(Level.Close)] public texttag? LoadTextTagHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<texttag>(table, parentKey, childKey);
    [Native(Level.Close)] public timerdialog? LoadTimerDialogHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<timerdialog>(table, parentKey, childKey);
    [Native(Level.Exact, Fact = TablesFact)] public timer? LoadTimerHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<timer>(table, parentKey, childKey);
    [Native(Level.Close)] public trackable? LoadTrackableHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<trackable>(table, parentKey, childKey);
    [Native(Level.Close)] public triggeraction? LoadTriggerActionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<triggeraction>(table, parentKey, childKey);
    [Native(Level.Close)] public triggercondition? LoadTriggerConditionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<triggercondition>(table, parentKey, childKey);
    [Native(Level.Close)] public @event? LoadTriggerEventHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<@event>(table, parentKey, childKey);
    [Native(Level.Close)] public trigger? LoadTriggerHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<trigger>(table, parentKey, childKey);
    [Native(Level.Close)] public ubersplat? LoadUbersplatHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<ubersplat>(table, parentKey, childKey);
    [Native(Level.Close)] public unit? LoadUnitHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<unit>(table, parentKey, childKey);
    [Native(Level.Close)] public unitpool? LoadUnitPoolHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<unitpool>(table, parentKey, childKey);
    [Native(Level.Exact, Fact = TablesFact)] public widget? LoadWidgetHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<widget>(table, parentKey, childKey);
}
