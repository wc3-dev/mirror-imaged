// A debug statement in a map's JASS: the JASS manual says the game never runs one (a claim; decisions D65).
function probe takes nothing returns nothing
    local integer n = 1
    call out("before")
    debug call out("debug ran")
    debug set n = 2
    call out("after " + I2S(n))
    call done()
endfunction
