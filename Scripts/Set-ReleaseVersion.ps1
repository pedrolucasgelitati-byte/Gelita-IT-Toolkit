[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\Gelita-IT-Toolkit.csproj'),
    [string]$ChangelogPath = (Join-Path $PSScriptRoot '..\CHANGELOG.md'),
    [string]$PreviousTag,
    [string]$ReleaseNotesPath = (Join-Path $PSScriptRoot '..\release-notes.md')
)

$ErrorActionPreference = 'Stop'
[xml]$project = Get-Content -LiteralPath $ProjectPath -Raw -Encoding utf8
$propertyGroup = @($project.Project.PropertyGroup) |
    Where-Object { $_.Version } |
    Select-Object -First 1
if ($null -eq $propertyGroup) { throw 'PropertyGroup with Version was not found.' }

$propertyGroup.Version = $Version
$propertyGroup.AssemblyVersion = "$Version.0"
$propertyGroup.FileVersion = "$Version.0"
$settings = [Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$settings.Encoding = [Text.UTF8Encoding]::new($false)
$writer = [Xml.XmlWriter]::Create((Resolve-Path $ProjectPath), $settings)
try { $project.Save($writer) } finally { $writer.Dispose() }

$range = if ([string]::IsNullOrWhiteSpace($PreviousTag)) { 'HEAD' } else { "$PreviousTag..HEAD" }
$subjects = @(git log $range --no-merges --pretty=format:'%s') |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
if ($LASTEXITCODE -ne 0) { throw 'Could not generate notes from Git history.' }
if ($subjects.Count -eq 0) { $subjects = @('Maintenance and distribution update.') }

$date = Get-Date -Format 'yyyy-MM-dd'
$notes = @("## $Version - $date", '') + @($subjects | ForEach-Object { "- $_" })
$notesText = ($notes -join [Environment]::NewLine) + [Environment]::NewLine
[IO.File]::WriteAllText($ReleaseNotesPath, $notesText, [Text.UTF8Encoding]::new($false))

$changelog = Get-Content -LiteralPath $ChangelogPath -Raw -Encoding utf8
$header = '# Hist' + [char]0x00F3 + 'rico de vers' + [char]0x00F5 + 'es'
if (-not $changelog.StartsWith($header, [StringComparison]::Ordinal)) {
    throw 'Expected CHANGELOG header was not found.'
}
$updated = $header + [Environment]::NewLine + [Environment]::NewLine + $notesText +
    $changelog.Substring($header.Length).TrimStart("`r", "`n")
[IO.File]::WriteAllText($ChangelogPath, $updated, [Text.UTF8Encoding]::new($false))

Write-Host "Project and CHANGELOG updated to $Version."
