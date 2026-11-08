// Where JASS's == on reals stops calling two reals equal (jass-reals saw 1.0 == 1.001 hold and 1.0 == 0.999 not). For
// each base a, the largest k for which a == a + k * u holds, u a power of two no smaller than a's float step there,
// so every value is exact; found by halving the interval, which assumes == holds up to a point and then never. Then
// the same with the operands swapped, and below a (a == a - k * u). Then for a from 2^n to 1.875 * 2^n in eighths, the
// largest k above and below with u the float step there (below 2^n itself, half of it): whether a margin that grows
// with a grows smoothly or by octaves.
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

function Largest takes real a, real u, boolean swapped returns integer
    local integer lo = 0
    local integer hi = 16777216
    local integer mid
    local boolean same
    loop
        exitwhen hi - lo <= 1
        set mid = (lo + hi) / 2
        if swapped then
            set same = a + mid * u == a
        else
            set same = a == a + mid * u
        endif
        if same then
            set lo = mid
        else
            set hi = mid
        endif
    endloop
    return lo
endfunction

function Base takes string name, real a, integer up, integer down returns nothing
    call out(name + " above " + I2S(Largest(a, Pow2(up), false)) + " " + I2S(Largest(a, Pow2(up), true)) + " below " + I2S(Largest(a, -Pow2(down), false)) + " " + I2S(Largest(a, -Pow2(down), true)) + " steps 2^" + I2S(up) + " 2^" + I2S(down))
endfunction

function Sweep takes integer octave returns nothing
    local integer j = 0
    local string above = ""
    local string below = ""
    local real a
    loop
        exitwhen j == 8
        set a = Pow2(octave) * (1.0 + j * 0.125)
        set above = above + " " + I2S(Largest(a, Pow2(octave - 23), false))
        if j == 0 then
            set below = below + " " + I2S(Largest(a, -Pow2(octave - 24), false))
        else
            set below = below + " " + I2S(Largest(a, -Pow2(octave - 23), false))
        endif
        set j = j + 1
    endloop
    call out("2^" + I2S(octave) + " times 1 to 1.875 above" + above + " below" + below)
endfunction

function probe takes nothing returns nothing
    call Base("0", 0.0, -30, -30)
    call Base("1", 1.0, -23, -24)
    call Base("0.25", 0.25, -25, -26)
    call Base("4", 4.0, -21, -22)
    call Base("1000", 1000.0, -14, -14)
    call Base("2^20", Pow2(20), -3, -4)
    call Base("-1", -1.0, -24, -23)
    call Base("-1000", -1000.0, -14, -14)
    call Sweep(12)
    call Sweep(20)
    call Sweep(23)
    call done()
endfunction
