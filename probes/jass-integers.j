// Decimal integer literals past 32 bits, as the game's JASS reads them: wrapped, held at the ends, or refused (W1-7's
// review: written as they are, Lua reads them as floats).
function probe takes nothing returns nothing
    call out("big " + I2S(4294967295) + " " + I2S(2147483648) + " " + I2S(-2147483648) + " " + I2S(4294967296) + " " + I2S(99999999999))
    call done()
endfunction
