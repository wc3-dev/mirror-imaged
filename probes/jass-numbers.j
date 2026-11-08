// JASS's numbers in the game's JASS machine: integer overflow, integer division, integers given to reals, real
// arithmetic and real comparison. A real is written as its IEEE bits in hex (as string.pack('<f') reads the Lua
// probes' floats), worked out with exact steps (halving, doubling, subtracting 1 in [1, 2), R2I of a whole number).
globals
    integer jn_n = 0
endglobals

function B takes boolean b returns string
    if b then
        return "true"
    endif
    return "false"
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

function Same takes real x returns real
    return x
endfunction

function AsReal takes integer x returns real
    return x
endfunction

function Modulo takes integer dividend, integer divisor returns integer
    local integer modulus = dividend - (dividend / divisor) * divisor
    if (modulus < 0) then
        set modulus = modulus + divisor
    endif
    return modulus
endfunction

function Conversions takes integer i returns nothing
    local real r = i
    call out("i2r " + I2S(i) + " set " + Bits(r) + " native " + Bits(I2R(i)) + " argument " + Bits(Same(i)) + " return " + Bits(AsReal(i)) + " plus " + Bits(i + 0.0))
endfunction

function probe takes nothing returns nothing
    local integer big = 2147483647
    local integer low = -2147483647 - 1
    local integer a = 65536
    local integer b = 46341
    local integer seven = 7
    local integer two = 2
    local integer three = 3
    local real r
    local real tenth = 0.1
    local real fifth = 0.2
    local real one = 1.0
    call out("add " + I2S(big + 1) + " " + I2S(low - 1) + " " + I2S(big + big) + " " + I2S(-low))
    call out("mul " + I2S(big * 2) + " " + I2S(a * a) + " " + I2S(b * b) + " " + I2S(low * -1) + " " + I2S(big * big))
    call out("div " + I2S(seven / two) + " " + I2S(-seven / two) + " " + I2S(seven / -two) + " " + I2S(-seven / -two) + " " + I2S(1 / three) + " " + I2S(-1 / three) + " " + I2S(low / two) + " " + I2S(big / -1))
    call out("modulo " + I2S(Modulo(7, -3)) + " " + I2S(Modulo(-7, 3)) + " " + I2S(Modulo(7, 3)) + " " + I2S(Modulo(-7, -3)))
    call out("divide real " + Bits(seven / two) + " " + Bits(7.0 / two) + " " + Bits(seven / 2.0))
    call Conversions(16777219)
    call Conversions(-16777219)
    call Conversions(2147483647)
    call Conversions(33554435)
    set r = 16777219
    call out("i2r literal " + Bits(r) + " " + Bits(Same(16777219)) + " " + Bits(-16777219))
    call out("sum " + Bits(tenth + fifth) + " " + Bits(3.0 * tenth) + " " + Bits(one / 3.0) + " " + Bits(10.0 * 0.7) + " " + Bits(2.0 / 3.0) + " " + Bits(one - 0.9))
    call out("literal " + Bits(0.1) + " " + Bits(0.3) + " " + Bits(16777217.0) + " " + Bits(340000000000000000000000000000000000000.0) + " " + Bits(0.001))
    call out("equal " + B(tenth + fifth == 0.3) + " " + B(one == 1.0001) + " " + B(one == 1.001) + " " + B(one == 1.01) + " " + B(one != 1.0001) + " " + B(0.0 == 0.0001) + " " + B(1000.0 == 1000.01) + " " + B(1000.0 == 1000.1))
    call out("order " + B(one < 1.0001) + " " + B(one >= 1.0001) + " " + B(one <= 0.9999) + " " + B(not (one == 1.0001)))
    call out("r2i " + I2S(R2I(2.9)) + " " + I2S(R2I(-2.9)))
    call done()
endfunction
