// Rawcodes in a map's JASS, as the game's JASS machine reads them: four ASCII letters, one letter, and four bytes of
// which two are one UTF-8 letter, the letter first, in the middle and last (decisions D102's question).
function probe takes nothing returns nothing
    call out("ascii " + I2S('hfoo') + " " + I2S('A0z~') + " " + I2S('a'))
    call out("high " + I2S('éab') + " " + I2S('aéb') + " " + I2S('abé'))
    call done()
endfunction
