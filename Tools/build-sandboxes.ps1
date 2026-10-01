# 테스트용 Sandbox 씬을 코드로 다시 생성한다 (Moqui.Unity.Editor.SandboxSceneBuilder).
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\unity-path.ps1"

$logDir = Join-Path $ProjectRoot 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir 'unity-sandboxes.log'

$code = Invoke-UnityBatch -LogFile $log -Arguments @('-quit', '-executeMethod', 'Moqui.Unity.Editor.SandboxSceneBuilder.BuildAll')
Select-String -Path $log -Pattern '\[SandboxSceneBuilder\]|Exception|error CS\d+' | ForEach-Object { Write-Output $_.Line.Trim() }
Write-Output "sandboxes exit=$code, log=$log"
exit $code
