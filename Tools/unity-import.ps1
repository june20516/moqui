# 프로젝트를 배치 모드로 열어 임포트·컴파일만 하고 종료한다. 컴파일 에러/경고 수를 출력한다.
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\unity-path.ps1"

$logDir = Join-Path $ProjectRoot 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir 'unity-import.log'

$code = Invoke-UnityBatch -Arguments @('-quit') -LogFile $log
$issues = Select-String -Path $log -Pattern 'error CS\d+|warning CS\d+' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
$issues | ForEach-Object { Write-Output $_ }
Write-Output "unity exit=$code, compile issues=$(@($issues).Count), log=$log"
if ($code -ne 0 -or @($issues).Count -gt 0) { exit 1 }
