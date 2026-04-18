# Run once per machine after the emulator/device is connected (before F5 on Bobeta.Mobile).
# Forwards the emulator's localhost:5163 to this PC's localhost:5163 so the app can use ApiBaseUrl http://127.0.0.1:5163
# (avoids 10.0.2.2 + Windows Firewall issues for Bobeta.API on http://0.0.0.0:5163).

$ErrorActionPreference = "Stop"
$adb = Get-Command adb -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
if (-not $adb) {
    $candidate = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
    if (Test-Path $candidate) { $adb = $candidate }
}
if (-not $adb -or -not (Test-Path $adb)) {
    Write-Error "adb not found. Add Android SDK platform-tools to PATH or install the SDK."
}
& $adb reverse tcp:5163 tcp:5163
Write-Host "OK: adb reverse tcp:5163 tcp:5163 (Bobeta.API must be listening on http://localhost:5163)"
