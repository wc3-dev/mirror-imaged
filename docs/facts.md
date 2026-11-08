# Facts

Game behaviour measured for MirrorImaged, in the style of the workspace's `docs/game-facts.md`.

## Lua numbers

The values below the workspace's "Lua runtime" facts summarise, read again from the two probes' own output: the numbers
probe after Moonwell's 0.3 gate (10-27, `CustomMapData\moonwell-0.3-numbers.txt`, values with their float bits) and the
maths probe (11-01, `moonwell-maths-numbers.txt`, printed with `%.9g`). 3.0.1.24371, single player. The tests in
`tests/MirrorImaged.Tests/Lua` use them.

- Adding a number of k float steps to 16384, 16384 plus one step, -16384, 1, 1 plus one step and 1000.1, and
  subtracting it from 16386, -16384, 1.5 and 1000.1, moves the result by 0, 2, 3, 4, 5, 6, 7 and 8 steps for k = 1 to 8.
  `1 - 2^-25`, `1 - 3 * 2^-25`, `1 + 2^-24` and `1 + 3 * 2^-24` are 1; `-(2^24) - 3` is -2^24.
- 7200.017 (`45e10023`) plus 0.0002, 0.000244, 0.00025, 0.0003 or 0.000488; 20000.75 (`469c4180`) plus 0.0009, 0.00097,
  0.00098, 0.001, 0.0015, 0.0019 or 0.00195; and 33000.01 (`4700e803`) plus 0.001, 0.0019, 0.002 or 0.0039 stay where
  they were, as with the larger additions in the facts.
- `1 - 0.9` is 0.100000024 and `(1000 + 0.1) - 1000` 0.0999755859. `1 - cos(0)` is 5.96046448e-08 (2^-24): Moonwell's
  `Ease.InSine(0)`.
- Products: `0.1 * 0.1` is `3c23d70a`, `20000.75 * 1000` `4b9897f7`, `33000.01 * 1000` `4bfbc525`, `100 * 0.07` 7,
  `100 * 0.29` 28.9999981, `100 * 0.57` 56.9999962, `1000 * 0.7` 699.999939, `3 * 0.1 * 10` cast 2. The square of
  each root below: 5.00006676 gives `41c8015e`, 10.0001335 `42c8015e`, 100.001854 `461c417b`, 200.003708 `471c417b`,
  1.41421354 `3fffffff`, 1.73205435 `40400033`, 1000.01501 `497425e0`, 0.010000092 `38d1b814`; `120^2 + 160^2` is
  40000.
- Quotients: `2/3` is `3f2aaaab`, `7/7` 1, `10000/100` 100, `(1/3) * 3` 1, `-1/0` -1.70141183e+38, `1e38 * 10`
  -5.5173927e-39 exactly, `1e-38 / 1000` and `1e-30 * 1e-30` 0, `-1e30 * 1e30` +0, `(1e20 * 1e20) / 1e20` 0,
  `float.MaxValue + 1` is MaxValue. `7.0 // 2` is the float 3. `(3, 4)` normalised has x 0.600000024.
- Square roots with their bits: 25 `40a0008c`, 100 `4120008c`, 10000 `42c800f3`, 40000 `434800f3`, 2 `3fb504f3`, 3
  `3fddb3f5`, 1000000 `447a00f6`, 0.0001 `3c23d76d`; and 9 is 3.0000577, 36 6.00011539, 49 7.00011635, 81 9.00006676,
  144 12.0002308, 625 25.0004635, 2500 50.000927, 0.00999999978 0.100000285; 1, 4, 16, 64, 1024, 0.25 and 2^24 are
  exact. `x ^ 0.5` gives 4 `3ffffffd`, 25 `409ffff6`, 100 `411ffff4`, 10000 `42c7fff1`, 40000 `4347fff0`, 2^24
  `457fffec`, 0.25 `3f000002`, 2 `3fb504f3`, 3 `3fddb3d1`, 1000000 `4479ff75`, 0.0001 `3c23d708`.
- One correction step `(s + x / s) / 2` after `math.sqrt` gives 2 1.41421366, 3 1.7320509, 625 25.0000019, 2500
  50.0000038, 10000 100.000008, 40000 200.000015, 1000000 1000.00006, 0.00999999978 0.100000001, 0.0001 0.00999999978,
  and the exact root for 1, 4, 9, 16, 25, 36, 49, 64, 81, 100, 144, 1024, 2^24 and 0.25.
- By octave, `math.sqrt`'s worst is 1.92854142 parts in 100,000 high and 0.0773249492 low in the even octaves (2^0, 2^2,
  ...) and 1.92324328 high and 0.087634936 low in the odd ones, the same in every octave from 2^-6 to 2^26.
- Sums of a float added to 0 again and again: 0.001 10,000 times is 9.99838257, 100,000 times 99.9489441, 1,000,000
  times 981.466797; 0.01: 99.971489, 996.854736, 9799.05859; 0.03: 299.988312, 2989.46997, 29441.9434; 0.1: 999.804871,
  9973.49512, 97000.0703; 0.37: 3699.40674, 36906.2461, 357990.594; 1.7: 16995.1406, 169506.906, 1664565. 0.03 added
  120,000 times is 3585.1731. Adding stops after 8,383,586 additions of 0.001 (at 8192), 7,546,324 of 0.01, 5,557,738 of
  0.1 and 8,388,608 of 1.
- Loops `t < limit` and `t <= limit` from 0: by 0.01 to 1 run 101 and 101, by 0.1 to 2 21 and 21, by 0.3 to 3 11 and
  11, by 0.1 to 100 1001 and 1001, by 0.25 to 1 4 and 5, by 0.03125 to 5 160 and 161.
- `(int)(n * (k / 100))` for n 1-1000 and k 1-99 is wrong 140 times, all among the 4,200 whole results, and never with
  0.001 added first. `(int)(n / d)` for n 1-2000 and d 2-100 is wrong 268 times of 198,000, and `Ceiling` 893.
- C#'s `5.5 % 2` is 1.5 and `-5.5 % 2` -1.5: CSharp.lua's `System.modf`, Lua's `%` with the sign put right, as is the
  facts' `7.2 % 0.1` = 0. `math.floor(3e9)` is the float `4f32d05e`.
- `Log(10)` is 2.30258489, `Asin(0.5)` 0.523598731, `Acos(0.5)` 1.04719746, `Atan(1)` 0.785398245, `Pow(2, 10)`
  1024, `Pow(10, 2)` 100, `Pow(10, 3)` 1000, `Pow(1.5, 2)` 2.25, `Pow(2, 0.5)` 1.41421354, `Sin(PI)` and `Cos(PI / 2)`
  4.65661287e-10 (2^-31).
- `float.Epsilon` reads 1.40129846e-45: a subnormal literal is read and printed as it is.
