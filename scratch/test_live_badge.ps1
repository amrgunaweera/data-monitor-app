Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$exePath = (Resolve-Path ".\DataMonitor.exe").Path
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)

$mainWindowType = $asm.GetType("NetPulse.MainWindow")
$win = [Activator]::CreateInstance($mainWindowType)

$flags = [System.Reflection.BindingFlags]"NonPublic,Instance"
$liveBadge = $mainWindowType.GetField("_liveBadge", $flags).GetValue($win)
$switchTabMethod = $mainWindowType.GetMethod("SwitchTab", $flags)

Write-Host "Initial Live Badge Visibility on load (Tab 0): $($liveBadge.Visibility)"
if ($liveBadge.Visibility -ne [System.Windows.Visibility]::Visible) {
    throw "Live badge should be Visible on Tab 0"
}

# Switch to Tab 1 (History)
Write-Host "Switching to Tab 1 (History)..."
$switchTabMethod.Invoke($win, @(1))
Write-Host "Live Badge Visibility on Tab 1: $($liveBadge.Visibility)"
if ($liveBadge.Visibility -ne [System.Windows.Visibility]::Collapsed) {
    throw "Live badge should be Collapsed on Tab 1 (History)"
}

# Switch to Tab 2 (Settings)
Write-Host "Switching to Tab 2 (Settings)..."
$switchTabMethod.Invoke($win, @(2))
Write-Host "Live Badge Visibility on Tab 2: $($liveBadge.Visibility)"
if ($liveBadge.Visibility -ne [System.Windows.Visibility]::Collapsed) {
    throw "Live badge should be Collapsed on Tab 2 (Settings)"
}

# Switch back to Tab 0 (Dashboard)
Write-Host "Switching back to Tab 0 (Live Dashboard)..."
$switchTabMethod.Invoke($win, @(0))
Write-Host "Live Badge Visibility on Tab 0: $($liveBadge.Visibility)"
if ($liveBadge.Visibility -ne [System.Windows.Visibility]::Visible) {
    throw "Live badge should be Visible again on Tab 0 (Live Dashboard)"
}

Write-Host "TEST PASSED: Live badge is only shown for the Dashboard!"

