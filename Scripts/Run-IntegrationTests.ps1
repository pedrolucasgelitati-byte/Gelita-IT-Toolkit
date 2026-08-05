[CmdletBinding()]
param(
    [switch]$IncludeAdministrative,
    [switch]$IncludeDestructive,
    [string]$CheckpointName
)

$ErrorActionPreference = 'Stop'
$computer = Get-CimInstance -ClassName Win32_ComputerSystem
$signature = "$($computer.Manufacturer) $($computer.Model)"
$virtualPatterns = 'Virtual|VMware|VirtualBox|KVM|QEMU|Xen|Parallels|Hyper-V'
if ($signature -notmatch $virtualPatterns) {
    throw "Execução bloqueada: '$signature' não foi identificado como máquina virtual."
}

if ($IncludeAdministrative -or $IncludeDestructive) {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Abra o PowerShell como administrador para os testes solicitados.'
    }
}

if ($IncludeDestructive -and [string]::IsNullOrWhiteSpace($CheckpointName)) {
    throw 'Informe -CheckpointName depois de criar e verificar um checkpoint da VM.'
}

$env:GELITA_INTEGRATION_TESTS = '1'
$env:GELITA_DESTRUCTIVE_INTEGRATION_TESTS = if ($IncludeDestructive) { '1' } else { '0' }
$project = Join-Path $PSScriptRoot '..\IntegrationTests\Gelita-IT-Toolkit.IntegrationTests.csproj'
$filter = if ($IncludeDestructive) {
    'TestCategory=Integration'
} elseif ($IncludeAdministrative) {
    'TestCategory=Integration&TestCategory!=Destructive'
} else {
    'TestCategory=Integration&TestCategory!=Administrative&TestCategory!=Destructive'
}

Write-Host "VM confirmada: $signature"
if ($IncludeDestructive) { Write-Host "Checkpoint informado: $CheckpointName" }
dotnet test $project -c Release --filter $filter --logger 'trx;LogFileName=integration-tests.trx'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
