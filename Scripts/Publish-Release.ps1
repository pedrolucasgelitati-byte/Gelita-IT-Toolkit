[CmdletBinding()]
param(
    [switch] $IncludeLocalConfiguration,
    [switch] $IncludeLocalInstallers
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
[xml] $project = Get-Content -LiteralPath (Join-Path $projectRoot 'Gelita-IT-Toolkit.csproj')
$version = [string]$project.Project.PropertyGroup.Version
$internalPackage = $IncludeLocalConfiguration -or $IncludeLocalInstallers
$kind = if ($internalPackage) { 'internal' } else { 'public' }
$destination = Join-Path $projectRoot ("dist\{0}-{1}-{2}" -f $version, $kind, (Get-Date -Format 'yyyyMMddHHmmss'))
$payload = Join-Path $destination 'Gelita-IT-Toolkit'
New-Item -ItemType Directory -Path $payload -Force | Out-Null

dotnet publish (Join-Path $projectRoot 'Gelita-IT-Toolkit.csproj') `
    --configuration Release --runtime win-x64 --self-contained true `
    --output $payload -p:DebugType=None -p:DebugSymbols=false -p:TreatWarningsAsErrors=false
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# Public packages start with an empty inventory. Local settings are opt-in only.
foreach ($name in @('printers', 'scanners', 'units')) {
    $source = Join-Path $projectRoot "Config\$name.example.json"
    $local = Join-Path $projectRoot "Config\$name.json"
    if ($IncludeLocalConfiguration -and (Test-Path -LiteralPath $local)) { $source = $local }
    Copy-Item -LiteralPath $source -Destination (Join-Path $payload "Config\$name.json") -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot '.env.example') -Destination $payload
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\INSTALACAO.md') -Destination (Join-Path $payload 'LEIA-ME.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\GITHUB-INVENTORY.md') -Destination (Join-Path $payload 'INVENTARIO-GITHUB.md')
if ($IncludeLocalConfiguration -and (Test-Path -LiteralPath (Join-Path $projectRoot '.env'))) {
    Copy-Item -LiteralPath (Join-Path $projectRoot '.env') -Destination $payload
}
if ($IncludeLocalInstallers) {
    foreach ($relative in @('EpsonScan2\WFC5810_C5890_EScan2_67810_AM.exe', 'EpsonScan2\WFM5899_EScan2_67810_AM.exe', 'NAPS\naps2-8.3.2-win-x64.exe')) {
        $source = Join-Path $projectRoot "Assets\$relative"
        if (!(Test-Path -LiteralPath $source)) { throw "Local installer missing: $relative" }
        $target = Join-Path $payload "Assets\$relative"
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $target
    }
}
@'
@echo off
cd /d "%~dp0"
start "" "%~dp0Gelita-IT-Toolkit.exe"
'@ | Set-Content -LiteralPath (Join-Path $payload 'Abrir-Gelita-IT-Toolkit.cmd') -Encoding ascii

$unexpected = Get-ChildItem -LiteralPath $payload -File -Recurse | Where-Object {
    $_.Name -match '(?i)token.*\.txt$|\.(pfx|p12|key)$' -or $_.FullName -match '[\\/]SentinelOne[\\/]'
}
if ($unexpected) { throw 'Package contains prohibited credential or SentinelOne files.' }
if (!$internalPackage -and (Test-Path -LiteralPath (Join-Path $payload '.env'))) { throw 'Public package contains a local .env.' }

Get-ChildItem -LiteralPath $payload -File -Recurse | Sort-Object FullName | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.FullName.Substring($payload.Length + 1).Replace('\', '/')
} | Set-Content -LiteralPath (Join-Path $payload 'SHA256SUMS.txt') -Encoding ascii
$fileSuffix = if ($internalPackage) { "-internal" } else { "" }
$zip = Join-Path $destination "Gelita-IT-Toolkit-$version-win-x64$fileSuffix.zip"
Compress-Archive -LiteralPath $payload -DestinationPath $zip
'{0}  {1}' -f (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $zip -Leaf) |
    Set-Content -LiteralPath ($zip + '.sha256') -Encoding ascii
Write-Output "Package: $zip"
Write-Output "Kind: $kind. Internal packages must never be uploaded to the public repository."
