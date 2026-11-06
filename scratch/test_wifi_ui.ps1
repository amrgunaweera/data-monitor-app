Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$exePath = (Resolve-Path ".\DataMonitor.exe").Path
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)

$mainWindowType = $asm.GetType("NetPulse.MainWindow")
$win = [Activator]::CreateInstance($mainWindowType)

$flags = [System.Reflection.BindingFlags]"NonPublic,Instance"
$selectedField = $mainWindowType.GetField("_selectedWifiSsid", $flags)
$refreshMethod = $mainWindowType.GetMethod("RefreshHistoryView", $flags)
$inspectorCard = $mainWindowType.GetField("_selectedNetworkInspectorCard", $flags).GetValue($win)
$wifiList = $mainWindowType.GetField("_wifiNetworksListPanel", $flags).GetValue($win)

# Populate test data first so there are items in the list
$storageType = $asm.GetType("NetPulse.UsageStorage")
$storageInstance = $storageType.GetProperty("Instance", [System.Reflection.BindingFlags]"Public,Static").GetValue($null, $null)
$recordMethod = $storageType.GetMethod("RecordTraffic", [Type[]]@([string], [string], [string], [string], [long], [long], [double], [double]))
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "SLT_FIBER_ZD4sV", [long]33930241638, [long]5562085785, [double]4500000, [double]850000))
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "Wavenet International", [long]2394541260, [long]249561088, [double]2100000, [double]450000))

# Refresh to generate cards
$refreshMethod.Invoke($win, $null)
Write-Host "Wi-Fi cards count: $($wifiList.Children.Count) (Expected 2)"
if ($wifiList.Children.Count -lt 2) {
    throw "Expected at least 2 cards in Wi-Fi networks list"
}

$firstCard = $wifiList.Children[0]
Write-Host "Clicking first card (SLT_FIBER_ZD4sV)..."
$mouseArgs = New-Object System.Windows.Input.MouseButtonEventArgs([System.Windows.Input.Mouse]::PrimaryDevice, 0, [System.Windows.Input.MouseButton]::Left)
$mouseArgs.RoutedEvent = [System.Windows.UIElement]::MouseLeftButtonUpEvent
$firstCard.RaiseEvent($mouseArgs)

$currentSsid = $selectedField.GetValue($win)
Write-Host "Current SSID after click: '$currentSsid'"
if ($currentSsid -ne "SLT_FIBER_ZD4sV") {
    throw "Click failed to select SLT_FIBER_ZD4sV"
}

if ($inspectorCard.Visibility -ne [System.Windows.Visibility]::Visible) {
    throw "Inspector card should be Visible after click"
}
$totalText = $mainWindowType.GetField("_inspectorTotalText", $flags).GetValue($win).Text
Write-Host "Inspector Total Data for SLT_FIBER_ZD4sV: $totalText"

# Click again on first card to toggle off
Write-Host "Clicking first card again to toggle off..."
$firstCard.RaiseEvent($mouseArgs)
$currentSsidAfterToggle = $selectedField.GetValue($win)
Write-Host "Current SSID after toggle: '$currentSsidAfterToggle' (Expected null)"
if ($currentSsidAfterToggle -ne $null) {
    throw "Click failed to toggle off SSID selection"
}
if ($inspectorCard.Visibility -ne [System.Windows.Visibility]::Collapsed) {
    throw "Inspector card should be Collapsed after toggle off"
}

# Click second card (Wavenet International)
$secondCard = $wifiList.Children[1]
Write-Host "Clicking second card (Wavenet International)..."
$secondCard.RaiseEvent($mouseArgs)
$currentSsidSecond = $selectedField.GetValue($win)
Write-Host "Current SSID after second card click: '$currentSsidSecond'"
if ($currentSsidSecond -ne "Wavenet International") {
    throw "Click failed to select Wavenet International"
}
if ($inspectorCard.Visibility -ne [System.Windows.Visibility]::Visible) {
    throw "Inspector card should be Visible after second click"
}

# Click Clear Button
Write-Host "Clicking Inspector Clear Button..."
$clearBtn = $mainWindowType.GetField("_inspectorClearBtn", $flags).GetValue($win)
$clickArgs = New-Object System.Windows.RoutedEventArgs([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent)
$clearBtn.RaiseEvent($clickArgs)

$currentSsidCleared = $selectedField.GetValue($win)
Write-Host "Current SSID after clear button: '$currentSsidCleared' (Expected null)"
if ($currentSsidCleared -ne $null) {
    throw "Clear button failed to reset SSID"
}
if ($inspectorCard.Visibility -ne [System.Windows.Visibility]::Collapsed) {
    throw "Inspector card should be Collapsed after clear button"
}

Write-Host "ALL CLICK, TOGGLE, AND CLEAR INTERACTIONS TESTED AND PASSED PERFECTLY!"
