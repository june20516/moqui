# dotnet test -> Unity EditMode -> Unity PlayMode 순서로 실행한다 (tech/verification.md §4).
# Core가 실패하면 Unity 단계는 건너뛴다. 결과: Logs/test-results-*.xml
param([switch]$SkipPlayMode)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\unity-path.ps1"

& "$PSScriptRoot\run-core-tests.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Output 'Core tests failed. Skipping Unity tests.'
    exit $LASTEXITCODE
}

$logDir = Join-Path $ProjectRoot 'Logs'
New-Item -ItemType Directory -Force $logDir | Out-Null

$platforms = @('EditMode')
if (-not $SkipPlayMode) { $platforms += 'PlayMode' }

$failed = $false
foreach ($platform in $platforms) {
    $results = Join-Path $logDir "test-results-$($platform.ToLower()).xml"
    $log = Join-Path $logDir "unity-test-$($platform.ToLower()).log"
    if (Test-Path $results) { Remove-Item $results }

    # testables로 들어온 패키지 자체 테스트(Input System 등)는 제외한다.
    $assembly = "Moqui.Unity.Tests.$platform"
    $code = Invoke-UnityBatch -LogFile $log -Arguments @('-runTests', '-testPlatform', $platform, '-assemblyNames', $assembly, '-testResults', $results)

    if (-not (Test-Path $results)) {
        Write-Output "$platform : no results (unity exit=$code). See $log"
        $failed = $true
        continue
    }

    [xml]$xml = Get-Content $results -Raw
    $run = $xml.'test-run'
    Write-Output "$platform : result=$($run.result) total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped)"
    foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
        Write-Output "  FAILED $($case.fullname): $($case.failure.message.InnerText)"
    }
    if ($code -ne 0 -or [int]$run.failed -gt 0) { $failed = $true }

    # 경고 0 규약 (tech/conventions.md §3): 이번 실행에서 다시 컴파일된 스크립트의 경고·에러도 실패로 본다.
    $compileIssues = @(Select-String -Path $log -Pattern 'warning CS\d+|error CS\d+' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique)
    foreach ($issue in $compileIssues) { Write-Output "  COMPILE $issue" }
    if ($compileIssues.Count -gt 0) { $failed = $true }
}

if ($failed) { exit 1 }
exit 0
