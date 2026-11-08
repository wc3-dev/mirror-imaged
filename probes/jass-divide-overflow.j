// The lowest integer divided by -1 in the game's JASS machine (jass-divide closed the game; one case a probe).
globals
    integer jd_zero = 0
    integer jd_n = 0
    integer jd_low = -2147483647 - 1
endglobals

function Divide takes nothing returns nothing
    call out("start")
    set jd_n = jd_low / (jd_zero - 1)
    call out("after " + I2S(jd_n))
endfunction

function probe takes nothing returns nothing
    call ExecuteFunc("Divide")
    call out("caller")
    call done()
endfunction
