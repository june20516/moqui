# PATH의 dotnet에 SDK가 있으면 그것을, 없으면 Unity 번들 SDK를 쓴다 (plan/decisions.md D-021).
$ErrorActionPreference = 'Stop'

$UnityVersion = '6000.6.3f1'
$BundledDotnet = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Data\DotNetSdk\dotnet.exe"

function Get-MoquiDotnet {
    $systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -ne $systemDotnet) {
        $sdks = & $systemDotnet.Source --list-sdks
        if ($sdks) { return $systemDotnet.Source }
    }
    if (Test-Path $BundledDotnet) { return $BundledDotnet }
    throw "No .NET SDK found. Install one or Unity $UnityVersion."
}
