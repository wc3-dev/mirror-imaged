// ExecuteFunc calling itself in a JASS map, guarded at 200 levels (W2-4's review, decisions D250): the deepest level
// reached and how many levels went on after their call. A Lua map's threads stop at about 98 levels, Lua's 200 C calls.
globals
    integer depth = 0
    integer deepest = 0
    integer returned = 0
endglobals

function nest takes nothing returns nothing
    set depth = depth + 1
    if depth > deepest then
        set deepest = depth
    endif
    if depth < 200 then
        call ExecuteFunc("nest")
    endif
    set returned = returned + 1
    set depth = depth - 1
endfunction

function probe takes nothing returns nothing
    call ExecuteFunc("nest")
    call out("deepest " + I2S(deepest) + " returned " + I2S(returned))
    call done()
endfunction
