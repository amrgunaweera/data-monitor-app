$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Error "C# compiler not found at $csc"
    exit 1
}

Write-Host "Compiling DataMonitor.exe with embedded icon, High-DPI manifest, and ThemeManager..." -ForegroundColor Cyan

$sources = @(
    "src\Helpers.cs",
    "src\ThemeManager.cs",
    "src\Models.cs",
    "src\NetworkMonitor.cs",
    "src\Controls\RealtimeChart.cs",
    "src\Controls\InterfaceCardControl.cs",
    "src\MainWindow.cs",
    "src\Program.cs"
)

$args = @(
    "/target:winexe",
    "/optimize+",
    "/win32icon:app.ico",
    "/win32manifest:app.manifest",
    "/out:DataMonitor.exe",
    "/lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF,C:\Windows\Microsoft.NET\Framework64\v4.0.30319",
    "/r:PresentationFramework.dll,PresentationCore.dll,WindowsBase.dll,System.Xaml.dll,System.dll,System.Core.dll,System.Drawing.dll"
) + $sources

& $csc $args

if ($LASTEXITCODE -eq 0) {
    Copy-Item DataMonitor.exe NetPulseMonitor.exe -Force
    Write-Host "[SUCCESS] DataMonitor.exe and NetPulseMonitor.exe compiled successfully!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Compilation failed with exit code $LASTEXITCODE" -ForegroundColor Red
}
