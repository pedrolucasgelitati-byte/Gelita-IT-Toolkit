[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $CertificateThumbprint,
    [string] $Runtime = 'win-x64',
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\dist\signed'),
    [string] $TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$certificate = Get-Item "Cert:\CurrentUser\My\$CertificateThumbprint" -ErrorAction Stop

if (-not $certificate.HasPrivateKey) { throw 'The selected certificate has no private key.' }
if (-not ($certificate.EnhancedKeyUsageList.ObjectId.Value -contains '1.3.6.1.5.5.7.3.3')) {
    throw 'The selected certificate is not valid for code signing.'
}
if ($certificate.NotAfter -le (Get-Date)) { throw 'The selected certificate has expired.' }

$signTool = Get-Command signtool.exe -ErrorAction Stop
dotnet publish (Join-Path $projectRoot 'Gelita-IT-Toolkit.csproj') `
    --configuration Release --runtime $Runtime --self-contained false `
    --output $OutputDirectory /p:DebugType=None /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$filesToSign = Get-ChildItem -LiteralPath $OutputDirectory -File |
    Where-Object { $_.Extension -in '.exe', '.dll' }
foreach ($file in $filesToSign) {
    & $signTool.Source sign /sha1 $certificate.Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $file.FullName
    if ($LASTEXITCODE -ne 0) { throw "Signing failed: $($file.FullName)" }
}

Get-AuthenticodeSignature (Join-Path $OutputDirectory 'Gelita-IT-Toolkit.exe') |
    Format-List Path, Status, StatusMessage, SignerCertificate
