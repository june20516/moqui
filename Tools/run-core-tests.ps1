# Core 테스트(dotnet test)만 실행한다 (tech/verification.md §4).
param([string]$Filter)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\dotnet-path.ps1"

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$dotnet = Get-MoquiDotnet
$solution = Join-Path $PSScriptRoot '..\dotnet\Moqui.sln'

$testArgs = @('test', $solution, '--nologo')
if ($Filter) { $testArgs += @('--filter', $Filter) }

& $dotnet @testArgs
exit $LASTEXITCODE
