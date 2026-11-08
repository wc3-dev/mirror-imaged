#!/usr/bin/env pwsh
# Builds the Lua library (Lua 5.3.6 with the game's numbers) for every platform with zig 0.17.0, into
# src/MirrorImaged.Lua/runtimes/<rid>/native. The zig to use: $env:ZIG, or zig on the path.
$ErrorActionPreference = 'Stop'
$zig = $env:ZIG ? $env:ZIG : 'zig'
$version = & $zig version
if ($version -ne '0.17.0') { throw "zig 0.17.0 is needed, found $version" }

$targets = @(
    @{ Rid = 'win-x64'; Target = 'x86_64-windows-gnu'; File = 'mirrorlua.dll'; Flags = @('-DLUA_BUILD_AS_DLL') }
    # LUA_USE_POSIX only makes the time functions thread-safe (localtime_r); Windows's are already
    @{ Rid = 'linux-x64'; Target = 'x86_64-linux-gnu.2.17'; File = 'libmirrorlua.so'; Flags = @('-DLUA_USE_POSIX', '-lm') }
    @{ Rid = 'osx-arm64'; Target = 'aarch64-macos'; File = 'libmirrorlua.dylib'
       Flags = @('-DLUA_USE_POSIX', '-Wl,-install_name,@rpath/libmirrorlua.dylib') }
)
# -DLUA_USE_C89: every platform gets the configuration a Windows build of Lua has, as the game is one.
# -ffp-contract=off: no fused multiply-add, which arm64 would use and x64 not. -s: without it the DLL holds a hash of
# the path it was built in.
$flags = @('-shared', '-O2', '-g0', '-s', '-DLUA_USE_C89', '-ffp-contract=off', '-fno-math-errno', '-fno-sanitize=all',
    '-Wno-string-plus-int', '-I.', '-Ilua')

Push-Location $PSScriptRoot
try {
    $sources = @(Get-ChildItem lua/*.c | Sort-Object Name | ForEach-Object { "lua/$($_.Name)" }) + 'arena.c', 'mifloat.c', 'sandbox.c'
    foreach ($t in $targets) {
        $out = "../src/MirrorImaged.Lua/runtimes/$($t.Rid)/native"
        $tmp = Join-Path ([IO.Path]::GetTempPath()) "mirrorlua-$($t.Rid)-$PID"
        New-Item -ItemType Directory -Force $out, $tmp | Out-Null
        & $zig cc -target $t.Target @flags @($t.Flags) -o "$tmp/$($t.File)" @sources
        if ($LASTEXITCODE) { throw "zig cc failed for $($t.Rid)" }
        Copy-Item "$tmp/$($t.File)" "$out/$($t.File)" -Force
        Remove-Item -Recurse -Force $tmp
    }
}
finally { Pop-Location }
