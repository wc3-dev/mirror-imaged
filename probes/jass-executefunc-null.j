// ExecuteFunc given null, and a name no function has, each in a thread of its own (W1-7's review).
function Null takes nothing returns nothing
    call out("null start")
    call ExecuteFunc(null)
    call out("null after")
endfunction

function Missing takes nothing returns nothing
    call out("missing start")
    call ExecuteFunc("NoSuchFunction")
    call out("missing after")
endfunction

function probe takes nothing returns nothing
    call ExecuteFunc("Null")
    call ExecuteFunc("Missing")
    call out("caller")
    call done()
endfunction
