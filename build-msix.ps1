[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9.-]{0,49}$')]
    [string]$IdentityName = "amrg-projects.NetPlusMonitor",

    [string]$IdentityPublisher = "CN=86D97629-174B-46ED-8787-814914FDD587",

    [string]$DisplayName = "Net Plus Monitor",

    [string]$PublisherDisplayName = "amrg-projects",

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
        if ([string]::IsNullOrWhiteSpace($makeAppx)) {
            $tempBuildTools = Join-Path $env:TEMP "NetPulseSdkBuildTools*\bin\*\x64\makeappx.exe"
            $found = Get-ChildItem -Path $tempBuildTools -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($found) {
                $makeAppx = $found.FullName
            }
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
$cfg = Join-Path $root "DataMonitor.exe.config"
if (Test-Path $cfg) {
    Copy-Item $cfg (Join-Path $stageDirectory "NetPulseMonitor.exe.config") -Force -ErrorAction SilentlyContinue
}

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

# Sign the MSIX package for local installation
$signtool = $null
$tempSignTools = Join-Path $env:TEMP "NetPulseSdkBuildTools*\bin\*\x64\signtool.exe"
$foundSt = Get-ChildItem -Path $tempSignTools -ErrorAction SilentlyContinue | Select-Object -First 1
if ($foundSt) {
    $signtool = $foundSt.FullName
} else {
    $st = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($st) { $signtool = $st.Source }
}

$pfxPath = Join-Path $outputRoot "NetPulseDev.pfx"
$cerPath = Join-Path $outputRoot "NetPulseDev.cer"

if ($signtool) {
    if (-not (Test-Path $pfxPath)) {
        $rsa = [System.Security.Cryptography.RSA]::Create(2048)
        $req = New-Object System.Security.Cryptography.X509Certificates.CertificateRequest(
            $IdentityPublisher,
            $rsa,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256,
            [System.Security.Cryptography.RSASignaturePadding]::Pkcs1
        )
        $oids = New-Object System.Security.Cryptography.OidCollection
        $null = $oids.Add((New-Object System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.3")))
        $eku = New-Object System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension($oids, $false)
        $req.CertificateExtensions.Add($eku)

        $cert = $req.CreateSelfSigned((Get-Date).AddDays(-1), (Get-Date).AddYears(5))
        [System.IO.File]::WriteAllBytes($pfxPath, $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, "NetPulse123!"))
        [System.IO.File]::WriteAllBytes($cerPath, $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    }

    & $signtool sign /fd SHA256 /a /f $pfxPath /p "NetPulse123!" $packagePath | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[SIGNED] MSIX package signed with developer certificate." -ForegroundColor Green
        & certutil.exe -f -addstore -user TrustedPeople $cerPath | Out-Null
        & certutil.exe -f -addstore -user Root $cerPath | Out-Null
        Write-Host "[TRUSTED] Certificate added to TrustedPeople & Root stores for 1-click install." -ForegroundColor Green
    }
}

Write-Host "Store-ready & locally testable MSIX created at $packagePath" -ForegroundColor Green
Write-Host "You can now double-click the MSIX to install it, or upload to Partner Center for Store distribution." -ForegroundColor Yellow
