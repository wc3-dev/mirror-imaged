// A rawcode holding a double quote, which War3Net would write into a Lua string as it is (decisions D102).
function probe takes nothing returns nothing
    call out("quote " + I2S('ab"c'))
    call done()
endfunction
