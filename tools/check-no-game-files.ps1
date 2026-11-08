#!/usr/bin/env pwsh
# Fails when a tracked file looks like it came from the game (plan, A15): a game file extension, a game script's name,
# or a game archive, texture or model signature at its start. Untracked files are not checked.
param([string]$Path = (Split-Path $PSScriptRoot))

$ErrorActionPreference = 'Stop'

$extensions = '.w3x', '.w3m', '.w3n', '.mpq', '.mdx', '.mdl', '.blp', '.dds', '.slk', '.w3e', '.doo'
$names = 'common.j', 'blizzard.j', 'common.ai'
# HM3W is the header a map puts before its MPQ archive.
$signatures = "MPQ`u{1A}", "MPQ`u{1B}", 'HM3W', 'BLP0', 'BLP1', 'BLP2', 'MDLX'

# git writes paths in UTF-8; read in the console's code page, a non-ASCII path would not be found on disk.
$consoleEncoding = [Console]::OutputEncoding
try {
    [Console]::OutputEncoding = [Text.Encoding]::UTF8
    $files = (git -C $Path ls-files -z) -split "`0" | Where-Object { $_ }
} finally { [Console]::OutputEncoding = $consoleEncoding }
if ($LASTEXITCODE -ne 0) { throw "git ls-files failed in $Path" }

$found = foreach ($file in $files) {
    $name = Split-Path $file -Leaf
    if ([IO.Path]::GetExtension($name) -in $extensions) { "$file (game file extension)"; continue }
    if ($name -in $names) { "$file (game script)"; continue }

    $full = Join-Path $Path $file
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { continue }
    $stream = [IO.File]::OpenRead($full)
    try {
        $head = [byte[]]::new(4)
        $read = $stream.Read($head, 0, 4)
    } finally { $stream.Dispose() }
    $start = [Text.Encoding]::Latin1.GetString($head, 0, $read)
    if ($start -cin $signatures) { "$file (game file signature)" }
}

if ($found) {
    Write-Output "Game files are tracked; remove them from git:"
    $found | ForEach-Object { Write-Output "  $_" }
    exit 1
}
Write-Output "No game files in $($files.Count) tracked files."
