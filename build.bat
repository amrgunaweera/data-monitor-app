@echo off
setlocal
echo ========================================================
echo   NetPulse Data Monitor - Building Standalone Windows App
echo ========================================================

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist %CSC% (
    echo [ERROR] C# Compiler not found at %CSC%
    pause
    exit /b 1
)

echo Compiling DataMonitor.exe with embedded icon, High-DPI manifest, and ThemeManager...
%CSC% /target:winexe /optimize+ /win32icon:app.ico /win32manifest:app.manifest /out:DataMonitor.exe /lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF","C:\Windows\Microsoft.NET\Framework64\v4.0.30319" /r:PresentationFramework.dll,PresentationCore.dll,WindowsBase.dll,System.Xaml.dll,System.dll,System.Core.dll,System.Drawing.dll,System.Web.Extensions.dll,System.Windows.Forms.dll src\Helpers.cs src\AppSettings.cs src\WifiNetworkHelper.cs src\ThemeManager.cs src\Models.cs src\HistoryModels.cs src\UsageStorage.cs src\NetworkMonitor.cs src\Controls\RealtimeChart.cs src\Controls\HistoricalBarChart.cs src\Controls\InterfaceCardControl.cs src\MainWindow.cs src\Program.cs

if %ERRORLEVEL% EQU 0 (
    copy /y DataMonitor.exe NetPulseMonitor.exe >nul
    echo [SUCCESS] DataMonitor.exe and NetPulseMonitor.exe generated successfully!
    echo.
    echo Launching DataMonitor.exe...
    start "" DataMonitor.exe
) else (
    echo [ERROR] Compilation failed.
    pause
)
