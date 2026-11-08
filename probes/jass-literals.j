// JASS's real literals against S2R of the same text (jass-numbers and jass-reals saw 0.1, 0.7 and 0.9 read a float
// step above the nearest, as MirrorImaged's S2R model gives them), and subtractions that shift the smaller number out
// by many bits (jass-reals' 1.0 == 1.0 - 0.001 failed where MirrorImaged's arithmetic, decisions D27, holds it).
function Pow2 takes integer n returns real
    local real r = 1.0
    loop
        exitwhen n == 0
        if n > 0 then
            set r = r * 2.0
            set n = n - 1
        else
            set r = r * 0.5
            set n = n + 1
        endif
    endloop
    return r
endfunction

function Nibble takes integer n returns string
    return SubString("0123456789abcdef", n, n + 1)
endfunction

function Bits takes real r returns string
    local integer s = 0
    local integer e = 127
    local integer m
    if r < 0.0 then
        set s = 1
        set r = -r
    endif
    if not (r > 0.0) then
        return Nibble(s * 8) + "0000000"
    endif
    loop
        exitwhen r < 2.0
        set r = r * 0.5
        set e = e + 1
    endloop
    loop
        exitwhen r >= 1.0
        set r = r * 2.0
        set e = e - 1
    endloop
    set m = R2I((r - 1.0) * 8388608.0)
    return Nibble(s * 8 + e / 32) + Nibble(e / 2 - (e / 32) * 16) + Nibble((e - (e / 2) * 2) * 8 + m / 1048576) + Nibble(m / 65536 - (m / 1048576) * 16) + Nibble(m / 4096 - (m / 65536) * 16) + Nibble(m / 256 - (m / 4096) * 16) + Nibble(m / 16 - (m / 256) * 16) + Nibble(m - (m / 16) * 16)
endfunction

function probe takes nothing returns nothing
    local real one = 1.0
    local real thousand = 1000.0
    local real milli = 0.001
    call out("0.1 " + Bits(0.1) + " " + Bits(S2R("0.1")))
    call out("0.2 " + Bits(0.2) + " " + Bits(S2R("0.2")))
    call out("0.7 " + Bits(0.7) + " " + Bits(S2R("0.7")))
    call out("0.9 " + Bits(0.9) + " " + Bits(S2R("0.9")))
    call out("1.001 " + Bits(1.001) + " " + Bits(S2R("1.001")))
    call out("1.0001 " + Bits(1.0001) + " " + Bits(S2R("1.0001")))
    call out("1.0000001 " + Bits(1.0000001) + " " + Bits(S2R("1.0000001")))
    call out("2.9 " + Bits(2.9) + " " + Bits(S2R("2.9")))
    call out("1000000.0625 " + Bits(1000000.0625) + " " + Bits(S2R("1000000.0625")))
    call out("3.14159 " + Bits(3.14159) + " " + Bits(S2R("3.14159")))
    call out("0.123456789 " + Bits(0.123456789) + " " + Bits(S2R("0.123456789")))
    call out("123456.789 " + Bits(123456.789) + " " + Bits(S2R("123456.789")))
    call out("minus " + Bits(one - milli) + " " + Bits(thousand - milli) + " " + Bits(one - Pow2(-30)) + " " + Bits(one - Pow2(-25)) + " " + Bits(one - Pow2(-24) - Pow2(-26)) + " " + Bits(Pow2(24) - 1.5))
    call out("plus " + Bits(one + milli) + " " + Bits(one + Pow2(-30)) + " " + Bits(one + Pow2(-24) + Pow2(-26)))
    call done()
endfunction
