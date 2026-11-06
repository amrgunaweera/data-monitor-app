Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$exePath = (Resolve-Path ".\DataMonitor.exe").Path
Write-Host "Loading assembly from $exePath..."
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)

$storageType = $asm.GetType("NetPulse.UsageStorage")
$instanceProp = $storageType.GetProperty("Instance", [System.Reflection.BindingFlags]"Public,Static")
$storageInstance = $instanceProp.GetValue($null, $null)

$timePresetType = $asm.GetType("NetPulse.TimeRangePreset")
$allTimePreset = [Enum]::Parse($timePresetType, "AllTime")

# RecordTraffic(string ifaceId, string ifaceName, string ifaceType, string wifiSsid, long dlBytes, long ulBytes, double dlSpeed, double ulSpeed)
$recordMethod = $storageType.GetMethod("RecordTraffic", [Type[]]@([string], [string], [string], [string], [long], [long], [double], [double]))

Write-Host "Recording mock Wi-Fi data for SLT_FIBER_ZD4sV, Wavenet International, Galaxy S10e1e06..."
# SLT_FIBER_ZD4sV: 31.6 GB dl, 5.18 GB ul
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "SLT_FIBER_ZD4sV", [long]33930241638, [long]5562085785, [double]4500000, [double]850000))
# Wavenet International: 2.23 GB dl, 238 MB ul
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "Wavenet International", [long]2394541260, [long]249561088, [double]2100000, [double]450000))
# Galaxy S10e1e06: 1.14 GB dl, 384 MB ul
$recordMethod.Invoke($storageInstance, @("wlan0", "Wi-Fi", "Wi-Fi", "Galaxy S10e1e06", [long]1224065024, [long]402653184, [double]1500000, [double]320000))

$summaryMethod = $storageType.GetMethod("GetSummary", [Type[]]@($timePresetType))
$globalSummary = $summaryMethod.Invoke($storageInstance, @($allTimePreset))
Write-Host "Total Bytes: $($globalSummary.TotalBytes) | Wi-Fi Total: $($globalSummary.WifiTotal)"
Write-Host "Networks found in breakdown: $($globalSummary.WifiNetworkBreakdown.Count)"

$getSsidSummaryMethod = $storageType.GetMethod("GetSummaryForSsid", [Type[]]@($timePresetType, [string]))

foreach ($net in $globalSummary.WifiNetworkBreakdown) {
    Write-Host "`n-- Testing SSID: '$($net.Ssid)' --"
    Write-Host "  Breakdown Record: Total=$($net.TotalBytes) DL=$($net.BytesDownloaded) UL=$($net.BytesUploaded) PeakDL=$($net.PeakDownloadSpeed) PeakUL=$($net.PeakUploadSpeed)"
    
    $ssidSummary = $getSsidSummaryMethod.Invoke($storageInstance, @($allTimePreset, $net.Ssid))
    Write-Host "  Filtered Summary: Total=$($ssidSummary.TotalBytes) DL=$($ssidSummary.TotalDownloaded) UL=$($ssidSummary.TotalUploaded)"
    
    if ($ssidSummary.TotalBytes -ne $net.TotalBytes) {
        Write-Error "Mismatch in total bytes for SSID $($net.Ssid): expected $($net.TotalBytes), got $($ssidSummary.TotalBytes)"
        exit 1
    }
    if ($ssidSummary.TotalDownloaded -ne $net.BytesDownloaded) {
        Write-Error "Mismatch in downloaded bytes for SSID $($net.Ssid)"
        exit 1
    }
    if ($ssidSummary.TotalUploaded -ne $net.BytesUploaded) {
        Write-Error "Mismatch in uploaded bytes for SSID $($net.Ssid)"
        exit 1
    }
    Write-Host "  [PASS] Filtered metrics exactly match breakdown"
}

Write-Host "`nALL WI-FI SELECTION AND FILTERING TESTS PASSED!"
