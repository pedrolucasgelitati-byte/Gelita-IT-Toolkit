[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$ExpectedVersion,
    [Parameter(Mandatory)][string]$ExpectedTag,
    [Parameter(Mandatory)][string]$ExecutablePath,
    [Parameter(Mandatory)][string]$ZipPath,
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\Gelita-IT-Toolkit.csproj')
)

$ErrorActionPreference = 'Stop'
if ($ExpectedTag -ne "v$ExpectedVersion") { throw 'Tag does not match expected version.' }
if (-not (Test-Path -LiteralPath $ExecutablePath)) { throw 'Executable was not found.' }
if (-not (Test-Path -LiteralPath $ZipPath)) { throw 'ZIP was not found.' }

[xml]$project = Get-Content -LiteralPath $ProjectPath -Raw -Encoding utf8
$projectVersion = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if ($projectVersion -ne $ExpectedVersion) { throw "Project version mismatch: $projectVersion." }

$fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath).FileVersion
$normalizedFileVersion = ([Version]$fileVersion).ToString(3)
if ($normalizedFileVersion -ne $ExpectedVersion) { throw "Executable version mismatch: $fileVersion." }

$expectedZipName = "Gelita-IT-Toolkit-v$ExpectedVersion-win-x64.zip"
if ([IO.Path]::GetFileName($ZipPath) -ne $expectedZipName) { throw 'ZIP name does not match version.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $ZipPath))
try {
    $entry = $archive.Entries | Where-Object FullName -eq 'version.json' | Select-Object -First 1
    if ($null -eq $entry) { throw 'version.json was not found inside ZIP.' }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { $manifest = ($reader.ReadToEnd() | ConvertFrom-Json) } finally { $reader.Dispose() }
    if ($manifest.version -ne $ExpectedVersion -or $manifest.tag -ne $ExpectedTag) {
        throw 'ZIP manifest does not match expected version and tag.'
    }
}
finally { $archive.Dispose() }

Write-Host "Versions match across project, executable, ZIP and tag: $ExpectedVersion."
