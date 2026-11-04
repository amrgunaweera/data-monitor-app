$ErrorActionPreference = "Stop"

$publisher = $env:NETPULSE_STORE_PUBLISHER
if ([string]::IsNullOrWhiteSpace($publisher)) {
    throw "Set NETPULSE_STORE_PUBLISHER to the exact publisher name shown in Partner Center."
}

$certificateThumbprint = $env:NETPULSE_SIGN_CERT_SHA1 -replace '\s', ''
if ([string]::IsNullOrWhiteSpace($certificateThumbprint)) {
    throw "Set NETPULSE_SIGN_CERT_SHA1 to the thumbprint of your code-signing certificate in the Current User certificate store."
}

$iscc = $env:INNO_SETUP_COMPILER
if ([string]::IsNullOrWhiteSpace($iscc)) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        $iscc = $command.Source
    } else {
        $candidates = @(
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
        )
        $iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    }
}

if ([string]::IsNullOrWhiteSpace($iscc) -or -not (Test-Path $iscc)) {
    throw "Inno Setup 6 compiler not found. Install Inno Setup 6 or set INNO_SETUP_COMPILER to ISCC.exe."
}

$signTool = $env:NETPULSE_SIGNTOOL
if ([string]::IsNullOrWhiteSpace($signTool)) {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command) {
        $signTool = $command.Source
    } else {
        $sdkBin = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
        if (Test-Path $sdkBin) {
            $signTool = Get-ChildItem $sdkBin -Directory | Sort-Object Name -Descending | ForEach-Object {
                Join-Path $_.FullName "x64\signtool.exe"
            } | Where-Object { Test-Path $_ } | Select-Object -First 1
        }
    }
}

if ([string]::IsNullOrWhiteSpace($signTool) -or -not (Test-Path $signTool)) {
    throw "SignTool not found. Install the Windows SDK or set NETPULSE_SIGNTOOL to signtool.exe."
}

$root = $PSScriptRoot
$payloadDirectory = Join-Path $root "artifacts\store\payload"
$outputDirectory = Join-Path $root "artifacts\store"
New-Item -ItemType Directory -Path $payloadDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$appExecutable = Join-Path $payloadDirectory "NetPulseMonitor.exe"
$setupExecutable = Join-Path $outputDirectory "NetPulseMonitor-Setup-x64.exe"

function Sign-StoreBinary([string]$Path) {
    & $signTool sign /sha1 $certificateThumbprint /s My /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 $Path
    if ($LASTEXITCODE -ne 0) {
        throw "SignTool failed to sign $Path with exit code $LASTEXITCODE."
    }

    & $signTool verify /pa $Path
    if ($LASTEXITCODE -ne 0) {
        throw "SignTool could not verify the signature on $Path."
    }
}

Push-Location $root
try {
    & (Join-Path $root "build.ps1") -OutputDirectory $payloadDirectory
    if (-not (Test-Path $appExecutable)) {
        throw "The application build did not produce NetPulseMonitor.exe."
    }
    Sign-StoreBinary $appExecutable

    & $iscc "store-installer.iss"
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup failed with exit code $LASTEXITCODE."
    }
    if (-not (Test-Path $setupExecutable)) {
        throw "Inno Setup did not produce the expected installer."
    }
    Sign-StoreBinary $setupExecutable
} finally {
    Pop-Location
}

Write-Host "Signed Store installer created at $setupExecutable" -ForegroundColor Green
