[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $CertificateThumbprint,
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\publish-signed'),
    [string] $TimestampUrl = 'http://timestamp.digicert.com'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$certificate = Get-Item "Cert:\CurrentUser\My\$CertificateThumbprint" -ErrorAction Stop
if (-not $certificate.HasPrivateKey) { throw 'The selected certificate has no private key.' }
if (-not ($certificate.EnhancedKeyUsageList.ObjectId.Value -contains '1.3.6.1.5.5.7.3.3')) { throw 'Certificate is not valid for code signing.' }
if ($certificate.NotAfter -le (Get-Date)) { throw 'The selected certificate has expired.' }
$signTool = Get-Command signtool.exe -ErrorAction Stop
dotnet publish (Join-Path $projectRoot 'Gelita-IT-Toolkit.csproj') -c Release -r win-x64 --self-contained false -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
Get-ChildItem -LiteralPath $OutputDirectory -File | Where-Object Extension -in '.exe', '.dll' | ForEach-Object {
    & $signTool.Source sign /sha1 $certificate.Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $_.FullName
    if ($LASTEXITCODE -ne 0) { throw "Signing failed: $($_.FullName)" }
}
Get-AuthenticodeSignature (Join-Path $OutputDirectory 'Gelita-IT-Toolkit.exe') | Format-List Path, Status, SignerCertificate
