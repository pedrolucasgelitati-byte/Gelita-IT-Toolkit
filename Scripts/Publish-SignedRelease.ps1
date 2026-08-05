[CmdletBinding(DefaultParameterSetName = 'Store')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Store')]
    [ValidatePattern('^[A-Fa-f0-9]{40,64}$')]
    [string]$CertificateThumbprint,
    [Parameter(Mandatory, ParameterSetName = 'Pfx')]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$PfxPath,
    [Parameter(Mandatory, ParameterSetName = 'Pfx')]
    [Security.SecureString]$PfxPassword,
    [string]$OutputDirectory = '.\dist',
    [string]$TimestampServer = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $projectRoot 'Gelita-IT-Toolkit.csproj'
[xml]$project = Get-Content -LiteralPath $projectPath -Raw -Encoding utf8
$version = $project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) { throw 'Versão não encontrada no projeto.' }
$publishPath = Join-Path $projectRoot 'publish-signed'
$destination = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
New-Item -ItemType Directory -Path $publishPath,$destination -Force | Out-Null

$importedCertificate = $null
try {
    if ($PSCmdlet.ParameterSetName -eq 'Pfx') {
        $importedCertificate = Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation Cert:\CurrentUser\My -Password $PfxPassword
        $CertificateThumbprint = $importedCertificate.Thumbprint
    }
    $CertificateThumbprint = $CertificateThumbprint.Replace(' ', '').ToUpperInvariant()
    $certificate = Get-ChildItem "Cert:\CurrentUser\My\$CertificateThumbprint","Cert:\LocalMachine\My\$CertificateThumbprint" -ErrorAction SilentlyContinue | Where-Object HasPrivateKey | Select-Object -First 1
    if (-not $certificate) { throw "Certificado com chave privada não encontrado: $CertificateThumbprint" }
    if ($certificate.NotAfter -le (Get-Date)) { throw 'O certificado de assinatura está expirado.' }

    dotnet test (Join-Path $projectRoot 'Tests\Gelita-IT-Toolkit.SmokeTests.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Os testes falharam; publicação bloqueada.' }
    dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $publishPath
    if ($LASTEXITCODE -ne 0) { throw 'A publicação falhou.' }

    $executable = Join-Path $publishPath 'Gelita-IT-Toolkit.exe'
    $signature = Set-AuthenticodeSignature -LiteralPath $executable -Certificate $certificate -TimestampServer $TimestampServer -HashAlgorithm SHA256
    if ($signature.Status -ne 'Valid') { throw "Assinatura inválida: $($signature.StatusMessage)" }
    if ($signature.SignerCertificate.Thumbprint -ne $CertificateThumbprint) { throw 'O assinante não corresponde ao thumbprint autorizado.' }

    "GELITA_TOOLKIT_SIGNER_THUMBPRINT=$CertificateThumbprint" | Set-Content -LiteralPath (Join-Path $publishPath '.env') -Encoding ascii
    @{ version = $version; tag = "v$version" } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publishPath 'version.json') -Encoding utf8
    $baseName = "Gelita-IT-Toolkit-v$version-win-x64"
    $zipPath = Join-Path $destination "$baseName.zip"
    $hashPath = "$zipPath.sha256"
    Compress-Archive -Path (Join-Path $publishPath '*'),(Join-Path $publishPath '.env') -DestinationPath $zipPath -CompressionLevel Optimal -Force
    & (Join-Path $PSScriptRoot 'Test-ReleaseVersion.ps1') `
        -ExpectedVersion $version `
        -ExpectedTag "v$version" `
        -ExecutablePath $executable `
        -ZipPath $zipPath `
        -ProjectPath $projectPath
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash
    "$hash  $baseName.zip" | Set-Content -LiteralPath $hashPath -Encoding ascii
    Write-Host "Release assinada: $zipPath"
    Write-Host "Thumbprint: $CertificateThumbprint"
}
finally {
    if ($importedCertificate) {
        Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($importedCertificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
    }
}
