[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9.-]{0,49}$')]
    [string]$IdentityName,

    [Parameter(Mandatory = $true)]
    [string]$IdentityPublisher,

    [string]$DisplayName = "Net Plus Monitor",

    [Parameter(Mandatory = $true)]
    [string]$PublisherDisplayName,

    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version = "2.1.0.0",

    [string]$OutputDirectory = ".\artifacts\msix"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$outputRoot = [System.IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
$stageDirectory = Join-Path $outputRoot "stage"
$assetDirectory = Join-Path $stageDirectory "Assets"
$manifestTemplate = Join-Path $root "msix\AppxManifest.xml"
$manifestPath = Join-Path $stageDirectory "AppxManifest.xml"
$iconPath = Join-Path $root "app.ico"
$packagePath = Join-Path $outputRoot "NetPulseMonitor-$Version-x64.msix"

if ([string]::IsNullOrWhiteSpace($IdentityPublisher) -or [string]::IsNullOrWhiteSpace($PublisherDisplayName)) {
    throw "Supply the exact Publisher identity and publisher display name from Partner Center."
}

$makeAppx = $env:MAKEAPPX_PATH
if ([string]::IsNullOrWhiteSpace($makeAppx)) {
    $command = Get-Command MakeAppx.exe -ErrorAction SilentlyContinue
    if ($command) {
        $makeAppx = $command.Source
    } else {
        $sdkBin = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
        if (Test-Path $sdkBin) {
            $makeAppx = Get-ChildItem $sdkBin -Directory | Sort-Object Name -Descending | ForEach-Object {
                Join-Path $_.FullName "x64\MakeAppx.exe"
            } | Where-Object { Test-Path $_ } | Select-Object -First 1
        }
    }
}

if ([string]::IsNullOrWhiteSpace($makeAppx) -or -not (Test-Path $makeAppx)) {
    throw "MakeAppx.exe was not found. Install the Windows SDK or set MAKEAPPX_PATH to MakeAppx.exe."
}

New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null

Push-Location $root
try {
    & (Join-Path $root "build.ps1") -OutputDirectory $stageDirectory -MsixPackage
    if ($LASTEXITCODE -ne 0) {
        throw "The MSIX application build failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}

Remove-Item (Join-Path $stageDirectory "DataMonitor.exe") -Force -ErrorAction SilentlyContinue
Copy-Item $iconPath (Join-Path $stageDirectory "app.ico") -Force

$manifest = New-Object System.Xml.XmlDocument
$manifest.PreserveWhitespace = $true
$manifest.Load($manifestTemplate)
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$namespaceManager.AddNamespace("appx", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
$namespaceManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10")
$identityNode = $manifest.SelectSingleNode("/appx:Package/appx:Identity", $namespaceManager)
$propertiesNode = $manifest.SelectSingleNode("/appx:Package/appx:Properties", $namespaceManager)
$visualElementsNode = $manifest.SelectSingleNode("/appx:Package/appx:Applications/appx:Application/uap:VisualElements", $namespaceManager)
$identityNode.SetAttribute("Name", $IdentityName)
$identityNode.SetAttribute("Publisher", $IdentityPublisher)
$identityNode.SetAttribute("Version", $Version)
$propertiesNode.SelectSingleNode("appx:DisplayName", $namespaceManager).InnerText = $DisplayName
$publisherNode = $propertiesNode.SelectSingleNode("appx:PublisherDisplayName", $namespaceManager)
$publisherNode.InnerText = $PublisherDisplayName
$visualElementsNode.SetAttribute("DisplayName", $DisplayName)
$manifest.Save($manifestPath)

Add-Type -AssemblyName System.Drawing
$icon = New-Object System.Drawing.Icon($iconPath)
$iconBitmap = $icon.ToBitmap()
try {
    $assetSizes = @{
        "StoreLogo.png" = @(50, 50)
        "Square44x44Logo.png" = @(44, 44)
        "Square150x150Logo.png" = @(150, 150)
        "Wide310x150Logo.png" = @(310, 150)
        "SplashScreen.png" = @(620, 300)
    }

    foreach ($asset in $assetSizes.GetEnumerator()) {
        $width = [int]$asset.Value[0]
        $height = [int]$asset.Value[1]
        $bitmap = New-Object System.Drawing.Bitmap($width, $height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::White)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $scale = [Math]::Min(($width - 8.0) / $iconBitmap.Width, ($height - 8.0) / $iconBitmap.Height)
            $drawWidth = [int][Math]::Max(1, [Math]::Round($iconBitmap.Width * $scale))
            $drawHeight = [int][Math]::Max(1, [Math]::Round($iconBitmap.Height * $scale))
            $drawX = [int](($width - $drawWidth) / 2)
            $drawY = [int](($height - $drawHeight) / 2)
            $graphics.DrawImage($iconBitmap, $drawX, $drawY, $drawWidth, $drawHeight)
            $bitmap.Save((Join-Path $assetDirectory $asset.Key), [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
} finally {
    $iconBitmap.Dispose()
    $icon.Dispose()
}

if (Test-Path $packagePath) {
    Remove-Item $packagePath -Force
}
& $makeAppx pack /d $stageDirectory /p $packagePath /o
if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx failed to package the MSIX with exit code $LASTEXITCODE."
}
$validationDirectory = Join-Path $env:TEMP ("NetPulseMsixValidation-" + [guid]::NewGuid().ToString("N"))
& $makeAppx unpack /p $packagePath /d $validationDirectory /o
if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx package round-trip validation failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path (Join-Path $validationDirectory "AppxManifest.xml"))) {
    throw "MakeAppx did not extract a valid package manifest."
}
Remove-Item $validationDirectory -Recurse -Force

Write-Host "Unsigned Store MSIX created at $packagePath" -ForegroundColor Green
Write-Host "Upload this package to Partner Center so the Store can sign it." -ForegroundColor Yellow
