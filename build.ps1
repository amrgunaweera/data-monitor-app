param(
    [string]$OutputDirectory = ".",
    [switch]$MsixPackage
)

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Error "C# compiler not found at $csc"
    exit 1
}

Write-Host "Compiling DataMonitor.exe with embedded icon, High-DPI manifest, and ThemeManager..." -ForegroundColor Cyan

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$dataMonitorPath = Join-Path $OutputDirectory "DataMonitor.exe"
$netPulseMonitorPath = Join-Path $OutputDirectory "NetPulseMonitor.exe"

$sources = @(
    "src\Helpers.cs",
    "src\AppSettings.cs",
    "src\WifiNetworkHelper.cs",
    "src\VisitedSitesHelper.cs",
    "src\ThemeManager.cs",
    "src\Models.cs",
    "src\HistoryModels.cs",
    "src\UsageStorage.cs",
    "src\NetworkMonitor.cs",
    "src\Controls\RealtimeChart.cs",
    "src\Controls\HistoricalBarChart.cs",
    "src\Controls\InterfaceCardControl.cs",
    "src\MainWindow.cs",
    "src\Program.cs"
)

$args = @(
    "/target:winexe",
    "/optimize+",
    "/win32icon:app.ico",
    "/win32manifest:app.manifest",
    "/out:$dataMonitorPath",
    "/lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF,C:\Windows\Microsoft.NET\Framework64\v4.0.30319",
    "/r:PresentationFramework.dll,PresentationCore.dll,WindowsBase.dll,System.Xaml.dll,System.dll,System.Core.dll,System.Drawing.dll,System.Web.Extensions.dll,System.Windows.Forms.dll"
) + $sources

if ($MsixPackage) {
    $args += "/define:MSIX_PACKAGE"
}

# Cleanup any leftover temp binaries from previous locked runs
Get-ChildItem -Path $OutputDirectory -Filter "*_old_*.tmp" -ErrorAction SilentlyContinue | ForEach-Object {
    try { Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue } catch {}
}

# If target is locked by running instance, move to temporary name first
if (Test-Path $dataMonitorPath) {
    try {
        $stream = [System.IO.File]::Open($dataMonitorPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $stream.Close()
    } catch {
        $tmpOld = Join-Path $OutputDirectory ("DataMonitor_old_" + [System.Guid]::NewGuid().ToString("N").Substring(0, 8) + ".tmp")
        Move-Item $dataMonitorPath $tmpOld -Force -ErrorAction SilentlyContinue
    }
}

& $csc $args

if ($LASTEXITCODE -eq 0) {
    try {
        Copy-Item $dataMonitorPath $netPulseMonitorPath -Force -ErrorAction Stop
    } catch {
        try {
            $tmpOldNet = Join-Path $OutputDirectory ("NetPulseMonitor_old_" + [System.Guid]::NewGuid().ToString("N").Substring(0, 8) + ".tmp")
            Move-Item $netPulseMonitorPath $tmpOldNet -Force -ErrorAction SilentlyContinue
            Copy-Item $dataMonitorPath $netPulseMonitorPath -Force -ErrorAction Stop
        } catch {
            Write-Warning "Could not overwrite NetPulseMonitor.exe (file may be in use by a running process). DataMonitor.exe was compiled and updated successfully."
        }
    }
    Write-Host "[SUCCESS] DataMonitor.exe and NetPulseMonitor.exe compiled successfully in $OutputDirectory" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Compilation failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
