// An integer divided by zero in the game's JASS machine (jass-divide closed the game; one case a probe).
globals
    integer jd_zero = 0
    integer jd_n = 0
endglobals

function Divide takes nothing returns nothing
    call out("start")
    set jd_n = 7 / jd_zero
    call out("after " + I2S(jd_n))
endfunction

function probe takes nothing returns nothing
    call ExecuteFunc("Divide")
    call out("caller")
    call done()
endfunction
