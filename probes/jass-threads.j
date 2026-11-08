// JASS threads in the game's JASS machine: the operation limit (how many times a loop of each shape runs before its
// thread stops, whether a thread ExecuteFunc starts and a wait each start afresh), reading a variable never set, and
// null strings given to natives and joined. Each case that may stop its thread runs in a thread of its own
// (ExecuteFunc) and writes a line before and after; a missing "after" line means the thread stopped there.
globals
    integer jt_n = 0
    integer jt_target = 0
    integer jt_limit = 0
    integer jt_unset
    timer jt_unset_timer
    integer array jt_ints
    string array jt_strings
    timer array jt_timers
    hashtable jt_table = InitHashtable()
endglobals

function B takes boolean b returns string
    if b then
        return "true"
    endif
    return "false"
endfunction

function Nothing takes nothing returns nothing
endfunction

function Spin takes nothing returns nothing
    loop
        set jt_n = jt_n + 1
    endloop
endfunction

function SpinCalling takes nothing returns nothing
    loop
        set jt_n = jt_n + 1
        call Nothing()
    endloop
endfunction

function SpinNative takes nothing returns nothing
    loop
        set jt_n = jt_n + 1
        call GetHandleId(null)
    endloop
endfunction

function SpinLocal takes nothing returns nothing
    local integer k = 0
    loop
        set k = k + 1
        set jt_n = k
    endloop
endfunction

function SpinExit takes nothing returns nothing
    loop
        exitwhen jt_n > jt_target
        set jt_n = jt_n + 1
    endloop
endfunction

function Count takes string name returns integer
    set jt_n = 0
    set jt_target = 2000000000
    call ExecuteFunc(name)
    call out("limit " + name + " " + I2S(jt_n))
    return jt_n
endfunction

// six tenths of the limit, in SpinExit's shape
function Part takes integer tenths returns nothing
    set jt_n = 0
    set jt_target = jt_limit * tenths / 10
    loop
        exitwhen jt_n > jt_target
        set jt_n = jt_n + 1
    endloop
endfunction

function Inner takes nothing returns nothing
    call Part(6)
    call out("inner done")
endfunction

function Outer takes nothing returns nothing
    call Part(6)
    call out("outer first")
    call ExecuteFunc("Inner")
    call Part(3)
    call out("outer second")
endfunction

function Waits takes nothing returns nothing
    call Part(6)
    call out("before wait")
    call TriggerSleepAction(0.0)
    call Part(6)
    call out("after wait")
endfunction

function UninitInteger takes nothing returns nothing
    local integer x
    call out("uninit integer start")
    set jt_n = x + 1
    call out("uninit integer after")
endfunction

function UninitIntegerNative takes nothing returns nothing
    local integer x
    call out("uninit integer native start")
    call I2S(x)
    call out("uninit integer native after")
endfunction

function UninitReal takes nothing returns nothing
    local real x
    local real y
    call out("uninit real start")
    set y = x + 1.0
    call out("uninit real after")
endfunction

function UninitString takes nothing returns nothing
    local string x
    local string y
    call out("uninit string start")
    set y = x + "a"
    call out("uninit string after")
endfunction

function UninitStringNative takes nothing returns nothing
    local string x
    call out("uninit string native start")
    call StringLength(x)
    call out("uninit string native after")
endfunction

function UninitBoolean takes nothing returns nothing
    local boolean x
    call out("uninit boolean start")
    if x then
        call out("uninit boolean true")
    endif
    call out("uninit boolean after")
endfunction

function UninitHandle takes nothing returns nothing
    local timer x
    call out("uninit handle start")
    if x == null then
        call out("uninit handle null")
    endif
    call out("uninit handle after")
endfunction

function UninitHandleNative takes nothing returns nothing
    local timer x
    call out("uninit handle native start")
    call GetHandleId(x)
    call out("uninit handle native after")
endfunction

function UninitCopy takes nothing returns nothing
    local integer x
    local integer y
    call out("uninit copy start")
    set y = x
    call out("uninit copy after")
endfunction

function UninitGlobal takes nothing returns nothing
    call out("uninit global start")
    set jt_n = jt_unset + 1
    call out("uninit global after")
endfunction

function UninitGlobalHandle takes nothing returns nothing
    call out("uninit global handle start")
    if jt_unset_timer == null then
        call out("uninit global handle null")
    endif
    call out("uninit global handle after")
endfunction

function UninitThenSet takes nothing returns nothing
    local integer x
    call out("uninit set start")
    set x = 5
    call out("uninit set after " + I2S(x))
endfunction

function UninitArrays takes nothing returns nothing
    local integer array la
    call out("arrays start")
    call out("arrays integer " + I2S(jt_ints[5]) + " " + I2S(la[3]))
    call out("arrays string " + B(jt_strings[5] == null) + " " + B(jt_strings[5] == "") + " " + I2S(StringLength(jt_strings[5])))
    call out("arrays timer " + B(jt_timers[5] == null))
    call out("arrays after")
endfunction

function NullLength takes nothing returns nothing
    local string s = null
    call out("null length " + I2S(StringLength(s)))
endfunction

function NullJoin takes nothing returns nothing
    local string s = null
    call out("null join [" + ("a" + s) + "] [" + (s + "b") + "]")
endfunction

function NullJoinBoth takes nothing returns nothing
    local string s = null
    call out("null join both " + B(s + s == null) + " " + B(s + s == ""))
endfunction

function NullEqual takes nothing returns nothing
    local string s = null
    local string e = ""
    call out("null equal " + B(s == null) + " " + B(s == "") + " " + B(e == null))
endfunction

function NullCase takes nothing returns nothing
    local string s = null
    call out("null case " + B(StringCase(s, true) == null) + " " + B(StringCase(s, true) == ""))
endfunction

function NullSub takes nothing returns nothing
    local string s = null
    call out("null sub " + B(SubString(s, 0, 1) == null) + " " + B(SubString(s, 0, 1) == ""))
endfunction

function NullNumbers takes nothing returns nothing
    local string s = null
    call out("null numbers " + I2S(S2I(s)) + " " + R2S(S2R(s)) + " " + I2S(StringHash(s)))
endfunction

function NullSaved takes nothing returns nothing
    local string s = null
    call out("null saved " + B(SaveStr(jt_table, 1, 2, s)) + " " + B(HaveSavedString(jt_table, 1, 2)) + " " + B(LoadStr(jt_table, 1, 2) == null) + " " + B(LoadStr(jt_table, 1, 2) == ""))
endfunction

function probe takes nothing returns nothing
    local trigger t = CreateTrigger()
    call Count("Spin")
    call Count("SpinCalling")
    call Count("SpinNative")
    call Count("SpinLocal")
    set jt_limit = Count("SpinExit")
    call ExecuteFunc("Outer")
    call ExecuteFunc("UninitInteger")
    call ExecuteFunc("UninitIntegerNative")
    call ExecuteFunc("UninitReal")
    call ExecuteFunc("UninitString")
    call ExecuteFunc("UninitStringNative")
    call ExecuteFunc("UninitBoolean")
    call ExecuteFunc("UninitHandle")
    call ExecuteFunc("UninitHandleNative")
    call ExecuteFunc("UninitCopy")
    call ExecuteFunc("UninitGlobal")
    call ExecuteFunc("UninitGlobalHandle")
    call ExecuteFunc("UninitThenSet")
    call ExecuteFunc("UninitArrays")
    call ExecuteFunc("NullLength")
    call ExecuteFunc("NullJoin")
    call ExecuteFunc("NullJoinBoth")
    call ExecuteFunc("NullEqual")
    call ExecuteFunc("NullCase")
    call ExecuteFunc("NullSub")
    call ExecuteFunc("NullNumbers")
    call ExecuteFunc("NullSaved")
    call TriggerAddAction(t, function Waits)
    call TriggerExecute(t)
    call TimerStart(CreateTimer(), 1.0, false, function done)
endfunction
