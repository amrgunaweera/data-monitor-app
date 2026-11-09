# Unblocks and starts NetPulse DataMonitor
$exe = Join-Path $PSScriptRoot "DataMonitor.exe"
if (Test-Path $exe) {
    Unblock-File $exe -ErrorAction SilentlyContinue
    Start-Process -FilePath $exe -ArgumentList $args
} else {
    Write-Warning "DataMonitor.exe not found. Running build.ps1 first..."
    & (Join-Path $PSScriptRoot "build.ps1")
    Start-Process -FilePath $exe -ArgumentList $args
}

