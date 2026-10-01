# Unity 에디터 경로 (plan/decisions.md D-019).
$UnityVersion = '6000.6.3f1'
$UnityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Assert-UnityAvailable {
    if (-not (Test-Path $UnityExe)) { throw "Unity $UnityVersion not found at $UnityExe" }
    $lockFile = Join-Path $ProjectRoot 'Temp\UnityLockfile'
    if (Test-Path $lockFile) {
        try { [IO.File]::Open($lockFile, 'Open', 'ReadWrite', 'None').Close() }
        catch { throw 'Project is open in another Unity Editor. Close it before running batch mode.' }
    }
}

# 배치 모드로 Unity를 실행하고 종료 코드를 돌려준다.
function Invoke-UnityBatch {
    param([string[]]$Arguments, [string]$LogFile, [switch]$WithGraphics)
    Assert-UnityAvailable
    $allArgs = @('-batchmode', '-projectPath', $ProjectRoot, '-logFile', $LogFile)
    if (-not $WithGraphics) { $allArgs += '-nographics' }
    $allArgs += $Arguments
    $process = Start-Process -FilePath $UnityExe -ArgumentList $allArgs -NoNewWindow -Wait -PassThru
    return $process.ExitCode
}
