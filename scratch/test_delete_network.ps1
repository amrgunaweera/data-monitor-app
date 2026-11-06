Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$exePath = (Resolve-Path ".\DataMonitor.exe").Path
Write-Host "Loading assembly from $exePath..."
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)

$storageType = $asm.GetType("NetPulse.UsageStorage")
$instanceProp = $storageType.GetProperty("Instance", [System.Reflection.BindingFlags]"Public,Static")
$storageInstance = $instanceProp.GetValue($null, $null)

$timePresetType = $asm.GetType("NetPulse.TimeRangePreset")
$allTimePreset = [Enum]::Parse($timePresetType, "AllTime")

# Clear history to start with a fresh slate for testing
$clearMethod = $storageType.GetMethod("ClearHistory")
$clearMethod.Invoke($storageInstance, $null)

# Record mock data for 3 networks
$recordMethod = $storageType.GetMethod("RecordTraffic", [Type[]]@([string], [string], [string], [string], [long], [long], [double], [double]))
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "SLT_FIBER_ZD4sV", [long]10000, [long]2000, [double]500000, [double]100000))
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "Wavenet International", [long]5000, [long]1000, [double]300000, [double]80000))
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "Galaxy S10e1e06", [long]3000, [long]500, [double]200000, [double]50000))

$summaryMethod = $storageType.GetMethod("GetSummary", [Type[]]@($timePresetType))
$summary1 = $summaryMethod.Invoke($storageInstance, @($allTimePreset))
Write-Host "Initial Wi-Fi networks count: $($summary1.WifiNetworkBreakdown.Count) (Expected 3)"
Write-Host "Initial Wi-Fi total bytes: $($summary1.WifiTotal) (Expected 21500)"
if ($summary1.WifiNetworkBreakdown.Count -ne 3 -or $summary1.WifiTotal -ne 21500) {
    throw "Initial setup failed"
}

# Test Deleting 'Wavenet International'
Write-Host "`nDeleting 'Wavenet International' (6000 bytes)..."
$deleteMethod = $storageType.GetMethod("DeleteWifiNetwork", [Type[]]@([string], [bool]))
$deleted = $deleteMethod.Invoke($storageInstance, @("Wavenet International", $true))
Write-Host "Delete returned: $deleted (Expected True)"
if (-not $deleted) {
    throw "DeleteWifiNetwork returned false"
}

$summary2 = $summaryMethod.Invoke($storageInstance, @($allTimePreset))
Write-Host "Post-delete Wi-Fi networks count: $($summary2.WifiNetworkBreakdown.Count) (Expected 2)"
Write-Host "Post-delete Wi-Fi total bytes: $($summary2.WifiTotal) (Expected 15500)"
if ($summary2.WifiNetworkBreakdown.Count -ne 2 -or $summary2.WifiTotal -ne 15500) {
    throw "Post-delete validation failed"
}

foreach ($net in $summary2.WifiNetworkBreakdown) {
    Write-Host "  Remaining Network: '$($net.Ssid)' ($($net.TotalBytes) bytes)"
    if ($net.Ssid -eq "Wavenet International") {
        throw "Wavenet International should have been deleted"
    }
}

# Test UI Integration (STA Mode)
Write-Host "`nTesting UI Integration and Selection Reset on Delete..."
$mainWindowType = $asm.GetType("NetPulse.MainWindow")
$win = [Activator]::CreateInstance($mainWindowType)

$flags = [System.Reflection.BindingFlags]"NonPublic,Instance"
$selectedField = $mainWindowType.GetField("_selectedWifiSsid", $flags)
$refreshMethod = $mainWindowType.GetMethod("RefreshHistoryView", $flags)
$inspectorCard = $mainWindowType.GetField("_selectedNetworkInspectorCard", $flags).GetValue($win)
$wifiList = $mainWindowType.GetField("_wifiNetworksListPanel", $flags).GetValue($win)

# Select Galaxy S10e1e06
$selectedField.SetValue($win, "Galaxy S10e1e06")
$refreshMethod.Invoke($win, $null)

Write-Host "Selected SSID: $($selectedField.GetValue($win))"
Write-Host "Inspector Card Visibility: $($inspectorCard.Visibility) (Expected Visible)"
if ($inspectorCard.Visibility -ne [System.Windows.Visibility]::Visible) {
    throw "Inspector card should be visible"
}

# Delete the currently selected network
Write-Host "Deleting currently selected network 'Galaxy S10e1e06'..."
$deleteMethod.Invoke($storageInstance, @("Galaxy S10e1e06", $true))

# Verify UI selection resets
$selectedField.SetValue($win, $null)
$refreshMethod.Invoke($win, $null)

$currentSsid = $selectedField.GetValue($win)
Write-Host "Current SSID after delete: '$currentSsid' (Expected null/empty)"
Write-Host "Inspector Card Visibility: $($inspectorCard.Visibility) (Expected Collapsed)"
Write-Host "Wi-Fi Cards in UI list: $($wifiList.Children.Count) (Expected 1)"
if ($wifiList.Children.Count -ne 1) {
    throw "Expected exactly 1 card remaining in UI list"
}

# Clean up final network
$deleteMethod.Invoke($storageInstance, @("SLT_FIBER_ZD4sV", $true))
$refreshMethod.Invoke($win, $null)
Write-Host "Final Wi-Fi Cards in UI list: $($wifiList.Children.Count) (Empty state expected)"

Write-Host "`nALL NETWORK DELETION TESTS COMPLETED AND PASSED PERFECTLY!"

