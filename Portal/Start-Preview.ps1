[CmdletBinding()]
param([int]$Port = 5085)

$ErrorActionPreference = 'Stop'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$url = "http://127.0.0.1:$Port/preview"
Start-Process $url
dotnet run --project $PSScriptRoot --urls "http://127.0.0.1:$Port"
