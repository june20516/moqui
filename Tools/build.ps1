# Windows Standalone 빌드 (Moqui.Unity.Editor.BuildScript.BuildStandalone). 산출물: Builds/Windows/
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\unity-path.ps1"

$logDir = Join-Path $ProjectRoot 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir 'unity-build.log'

$code = Invoke-UnityBatch -LogFile $log -Arguments @('-quit', '-buildTarget', 'Win64', '-executeMethod', 'Moqui.Unity.Editor.BuildScript.BuildStandalone')
Select-String -Path $log -Pattern '\[BuildScript\]|BuildFailedException|error CS\d+' | ForEach-Object { Write-Output $_.Line.Trim() }
Write-Output "build exit=$code, log=$log"
exit $code
