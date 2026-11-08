// A rawcode holding a backslash, four characters as written: is it an escape to the game (decisions D102)?
function probe takes nothing returns nothing
    call out("backslash " + I2S('a\bc'))
    call done()
endfunction
