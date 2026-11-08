// JASS's real comparison and returns, after jass-numbers: where == stops calling two reals equal (1 for equal, 0 not,
// one digit a pair), whether != and the order comparisons are exact, an integer compared with a real, and a function
// returning real that returns an integer literal, sum or variable (jass-numbers saw a variable's bits come back as they
// are), each in a thread of its own.
function B takes boolean b returns string
    if b then
        return "1"
    endif
    return "0"
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

function Ladder takes real base returns string
    local real b = base
    return B(b == base + 0.0009) + B(b == base + 0.001) + B(b == base + 0.00101) + B(b == base + 0.0011) + B(b == base + 0.0012) + B(b == base + 0.0015) + B(b == base + 0.002) + B(b == base + 0.005) + B(b == base - 0.0009) + B(b == base - 0.001) + B(b == base - 0.0011) + B(b == base - 0.002)
endfunction

function ReturnsLiteral takes nothing returns real
    return 16777219
endfunction

function ReturnsOne takes nothing returns real
    return 1
endfunction

function ReturnsSum takes integer i returns real
    return i + 1
endfunction

function ReturnsReal takes integer i returns real
    return i + 0.5
endfunction

function OutLiteral takes nothing returns nothing
    call out("returns literal start")
    call out("returns literal " + Bits(ReturnsLiteral()))
endfunction

function OutOne takes nothing returns nothing
    call out("returns one start")
    call out("returns one " + Bits(ReturnsOne()))
endfunction

function OutSum takes nothing returns nothing
    call out("returns sum start")
    call out("returns sum " + Bits(ReturnsSum(16777219)))
endfunction

function OutReal takes nothing returns nothing
    call out("returns real start")
    call out("returns real " + Bits(ReturnsReal(16777219)))
endfunction

function probe takes nothing returns nothing
    local integer one = 1
    local real x = 1.0
    call out("ladder 0 " + Ladder(0.0))
    call out("ladder 1 " + Ladder(1.0))
    call out("ladder -1 " + Ladder(-1.0))
    call out("ladder 100 " + Ladder(100.0))
    call out("ladder 1000 " + Ladder(1000.0))
    call out("ladder 0.5 " + Ladder(0.5))
    call out("far " + B(1000000.0 == 1000000.0625) + B(0.0001 == 0.0002) + B(x == 1.0) + B(x != 1.0) + B(x != 1.0000001))
    call out("integer " + B(one == 1.0005) + B(one == 1.002) + B(one != 1.0005) + B(1.0005 == one))
    call out("order " + B(x < 1.0005) + B(x > 0.9995) + B(x <= 0.9995) + B(x >= 1.0005))
    call ExecuteFunc("OutLiteral")
    call ExecuteFunc("OutOne")
    call ExecuteFunc("OutSum")
    call ExecuteFunc("OutReal")
    call done()
endfunction
