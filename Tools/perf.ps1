# 성능 측정 (M11): 빌드한 실행 파일을 -moquiPerf로 실행해 다섯 스테이지 프레임 타임 보고서를 만든다.
# 먼저 Tools/build.ps1로 빌드한다. 산출물: Logs/perf-report.md
param(
    [int]$Width = 1920,
    [int]$Height = 1080
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $ProjectRoot 'Builds\Windows\Moqui.exe'
if (-not (Test-Path $exe)) {
    throw "Build not found: $exe (run Tools/build.ps1 first)"
}

$logDir = Join-Path $ProjectRoot 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
$report = Join-Path $logDir 'perf-report.md'
$playerLog = Join-Path $logDir 'perf-player.log'
if (Test-Path $report) { Remove-Item $report }

$arguments = @('-moquiPerf', $report, '-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', '0', '-logFile', $playerLog)
$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -Wait
if (-not (Test-Path $report)) {
    throw "Perf report was not written (exit=$($process.ExitCode), log=$playerLog)"
}

Get-Content $report -Encoding UTF8
Write-Output "perf exit=$($process.ExitCode), report=$report, log=$playerLog"
