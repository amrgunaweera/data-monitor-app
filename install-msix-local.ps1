# Installs the built MSIX package locally on Windows for testing
param(
    [string]$PackagePath = ".\artifacts\msix\NetPulseMonitor-2.1.0.0-x64.msix"
)

$PackagePath = [System.IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path $PackagePath)) {
    Write-Warning "MSIX package not found at $PackagePath. Building it now..."
    & (Join-Path $PSScriptRoot "build-msix.ps1")
}

if (-not (Test-Path $PackagePath)) {
    Write-Error "Could not find or build MSIX at $PackagePath"
    exit 1
}

$publisher = "CN=86D97629-174B-46ED-8787-814914FDD587"
$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -eq $publisher } | Select-Object -First 1

if (-not $cert) {
    Write-Host "Creating local developer signing certificate for publisher '$publisher'..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $publisher -CertStoreLocation "Cert:\CurrentUser\My" -FriendlyName "NetPulse MSIX Dev" -NotAfter (Get-Date).AddYears(5)
}

if ($cert) {
    try {
        # Trust in CurrentUser\TrustedPeople for Windows App Installer
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPeople", "CurrentUser")
        $store.Open("ReadWrite")
        $store.Add($cert)
        $store.Close()

        # Sign the package
        Set-AuthenticodeSignature -FilePath $PackagePath -Certificate $cert | Out-Null
        Write-Host "[SIGNED] MSIX package digitally signed with local developer certificate." -ForegroundColor Green
    } catch {
        Write-Warning "Could not sign MSIX: $($_.Exception.Message)"
    }
}

try {
    Write-Host "Installing NetPulse Monitor package via Windows App Installer..." -ForegroundColor Cyan
    Add-AppxPackage -Path $PackagePath
    Write-Host "[SUCCESS] NetPulse Monitor MSIX installed successfully! You can launch it from your Start Menu." -ForegroundColor Green
} catch {
    Write-Host "`nTo install manually:" -ForegroundColor Yellow
    Write-Host "1. Double-click the MSIX at: $PackagePath"
    Write-Host "2. Or upload it to Microsoft Partner Center for Store distribution."
}

