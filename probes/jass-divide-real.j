// A real divided by zero, and zero by zero, in the game's JASS machine (jass-divide closed the game; one kind a probe).
globals
    integer jd_zero = 0
    real jd_r = 0.0
endglobals

function Divide takes nothing returns nothing
    call out("start")
    set jd_r = 7.0 / jd_zero
    call out("after " + R2S(jd_r))
    set jd_r = 0.0 / (jd_zero * 1.0)
    call out("zero " + R2S(jd_r))
endfunction

function probe takes nothing returns nothing
    call ExecuteFunc("Divide")
    call out("caller")
    call done()
endfunction
