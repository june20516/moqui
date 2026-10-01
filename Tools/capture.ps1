# 캡처 (Moqui.Unity.Editor.CaptureTool.CaptureAll). 그래픽 장치가 필요하다. 산출물: Captures/<날짜>/
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\unity-path.ps1"

$logDir = Join-Path $ProjectRoot 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir 'unity-capture.log'

$code = Invoke-UnityBatch -WithGraphics -LogFile $log -Arguments @('-quit', '-executeMethod', 'Moqui.Unity.Editor.CaptureTool.CaptureAll')
Select-String -Path $log -Pattern '\[CaptureTool\]|Exception' | ForEach-Object { Write-Output $_.Line.Trim() }
Write-Output "capture exit=$code, log=$log"
exit $code
