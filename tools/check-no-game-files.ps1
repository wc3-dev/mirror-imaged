#!/usr/bin/env pwsh
# Fails when a file in git's index looks like it came from the game (plan, A15): a game or sound file extension, a game
# script's name, a game archive, texture or model signature at its start, a Git LFS pointer (git doesn't hold its
# content), or an entry git can't give the content of, such as a submodule. The content is read from git, not the disk,
# so a file changed or deleted on disk is checked as it would be committed. Untracked files are not checked.
param([string]$Path = (Split-Path $PSScriptRoot))

$ErrorActionPreference = 'Stop'
# .NET resolves a relative path against its own current directory, not PowerShell's location.
$Path = Convert-Path -LiteralPath $Path

$extensions = '.w3x', '.w3m', '.w3n', '.mpq', '.mdx', '.mdl', '.blp', '.dds', '.slk', '.w3e', '.doo',
    '.wav', '.mp3', '.flac', '.ogg'
$names = 'common.j', 'blizzard.j', 'common.ai'
# HM3W is the header a map puts before its MPQ archive.
$signatures = "MPQ`u{1A}", "MPQ`u{1B}", 'HM3W', 'BLP0', 'BLP1', 'BLP2', 'MDLX'
$lfsPointer = 'version https://git-lfs.github.com/spec/'

function Start-Git {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    foreach ($argument in @('-C', $Path) + $args) { $start.ArgumentList.Add($argument) }
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    [Diagnostics.Process]::Start($start)
}

# The first bytes of a blob as git stores it, or $null when git has no blob for the id.
function Read-Start([string]$id) {
    $catFile.StandardInput.Write("$id`n")
    $catFile.StandardInput.Flush()
    $header = [Collections.Generic.List[byte]]::new()
    while (($byte = $blobs.ReadByte()) -ne 10) {
        if ($byte -lt 0) { throw 'git cat-file ended early' }
        $header.Add($byte)
    }
    # "<id> missing" has no content; "<id> <type> <size>" is followed by the content, read even when it isn't a blob's.
    $fields = [Text.Encoding]::ASCII.GetString($header.ToArray()) -split ' '
    if ($fields.Count -lt 3) { return $null }

    $start = [IO.MemoryStream]::new()
    $remaining = [long]$fields[2] + 1 # git writes a newline after the content
    $keep = [Math]::Min([long]$fields[2], 64)
    while ($remaining -gt 0) {
        $count = $blobs.Read($buffer, 0, [Math]::Min($remaining, $buffer.Length))
        if ($count -le 0) { throw 'git cat-file ended early' }
        $start.Write($buffer, 0, [Math]::Min($count, $keep - $start.Length))
        $remaining -= $count
    }
    if ($fields[1] -ne 'blob') { return $null }
    [Text.Encoding]::Latin1.GetString($start.ToArray())
}

$list = Start-Git 'ls-files' '--stage' '-z'
$list.StandardInput.Close()
$entries = $list.StandardOutput.ReadToEnd() -split "`0" | Where-Object { $_ }
$list.WaitForExit()
if ($list.ExitCode -ne 0) { throw "git ls-files failed in $Path" }

$catFile = Start-Git 'cat-file' '--batch'
$blobs = $catFile.StandardOutput.BaseStream
$buffer = [byte[]]::new(65536)

# Each entry is "<mode> <id> <stage>`t<path>".
$found = foreach ($entry in $entries) {
    $info, $file = $entry -split "`t", 2
    $name = $file.Substring($file.LastIndexOf([char]'/') + 1)
    if ([IO.Path]::GetExtension($name) -in $extensions) { "$file (game or sound file extension)"; continue }
    if ($name -in $names) { "$file (game script)"; continue }

    $start = Read-Start ($info -split ' ')[1]
    if ($null -eq $start) { "$file (git can't give its content)"; continue }
    if ($start.StartsWith($lfsPointer, [StringComparison]::Ordinal)) { "$file (Git LFS pointer)"; continue }
    if ($start.Substring(0, [Math]::Min(4, $start.Length)) -cin $signatures) { "$file (game file signature)" }
}
$catFile.StandardInput.Close()
$catFile.WaitForExit()

if ($found) {
    Write-Output "Game files are tracked; remove them from git:"
    $found | ForEach-Object { Write-Output "  $_" }
    exit 1
}
Write-Output "No game files in $($entries.Count) tracked files."
