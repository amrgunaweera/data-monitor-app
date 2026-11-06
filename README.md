# ⚡ NetPulse Monitor — Full Technical Specification & Documentation

> **A native, zero-dependency Windows desktop network monitor, real-time bandwidth analyzer, persistent historical traffic tracker, and customizable system utility.**

[![Platform](https://img.shields.io/badge/Platform-Windows_10_%2F_11_x64-0078D4?style=for-the-badge&logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET_Framework-4.8_WPF-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com)
[![Dependencies](https://img.shields.io/badge/Dependencies-Zero_(Self--Contained)-success?style=for-the-badge)](#technical-architecture--specifications)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)
[![Privacy](https://img.shields.io/badge/Privacy-Local_Data_(No_App_Telemetry)-brightgreen?style=for-the-badge)](PRIVACY.md)

---

## 📑 Table of Contents
1. [Executive Summary](#-executive-summary)
2. [Technical Architecture & Specifications](#-technical-architecture--specifications)
3. [Core Feature Specifications](#-core-feature-specifications)
   - [Live Dashboard View](#1-live-dashboard-view)
   - [Usage History & Analytics](#2-usage-history--analytics)
   - [Wi-Fi Network (SSID) Breakdown Engine](#3-wi-fi-network-ssid-breakdown-engine)
   - [Visited Site URL List Usage History Engine](#4-visited-site-url-list-usage-history-engine)
   - [Settings & Preferences Management](#5-settings--preferences-management)
   - [System Tray & Windows Integration](#6-system-tray--windows-integration)
4. [Data Storage & Schema Specifications](#-data-storage--schema-specifications)
   - [Usage History Schema (`usage_history.json`)](#usage-history-schema)
   - [Configuration Schema (`settings.json`)](#configuration-schema)
5. [Microsoft Store Publication Specification](#-microsoft-store-publication-specification)
6. [Compilation & Build Guide](#-compilation--build-guide)
7. [Repository File Hierarchy](#-repository-file-hierarchy)
8. [Privacy & Security](#-privacy--security)
9. [License](#-license)

---

## 🔍 Executive Summary

**NetPulse Monitor** is an enterprise-grade, high-performance Windows desktop application designed to provide comprehensive observability over local network interfaces (**Wi-Fi** and **Ethernet**). 

Unlike typical modern utilities that rely on heavyweight web runtimes (such as Electron or Chromium Embedded Framework), NetPulse is engineered with **native .NET Framework 4.8 and Windows Presentation Foundation (WPF)**. The regular build produces a standalone executable (`NetPulseMonitor.exe` / `DataMonitor.exe`); the Microsoft Store build packages the x64 app as an MSIX for Store signing and distribution.

### Why NetPulse?
* **Sub-Millisecond Speed & Efficiency:** Ultra-low resource footprint (< 0.5% CPU during high-speed transfers; ~40–60 MB RAM).
* **Private Local Processing:** No app telemetry, analytics, advertising, or transmitted crash reports. Network identifiers, usage history, and diagnostic logs remain on the device unless the user exports a report.
* **Persistent History:** Tracks daily, weekly, and monthly data consumption permanently across system reboots.
* **SSID-Level Wi-Fi Attribution:** Differentiates Wi-Fi usage between distinct wireless networks (e.g., Home vs. Office vs. Mobile Hotspot).
* **Hardware-Accelerated UI:** Custom Direct3D/WPF vector graphics supporting high-DPI displays (100% to 400% scaling).

---

## 🛠 Technical Architecture & Specifications

### System Architecture Overview

```
                                  ┌───────────────────────────────┐
                                  │   Desktop Window Manager      │
                                  │   (DwmSetWindowAttribute P/Inv)│
                                  └───────────────┬───────────────┘
                                                  │
┌─────────────────────────────────────────────────▼─────────────────────────────────────────────────┐
│                                    NetPulse Main UI (WPF)                                         │
│  ┌─────────────────────────┐  ┌───────────────────────────────┐  ┌─────────────────────────────┐  │
│  │    Live Dashboard Tab   │  │       Usage History Tab       │  │        Settings Tab         │  │
│  │ ┌─────────────────────┐ │  │ ┌───────────────────────────┐ │  │ ┌─────────────────────────┐ │  │
│  │ │ RealtimeChart.cs    │ │  │ │ HistoricalBarChart.cs     │ │  │ │ AppSettings & Registry   │ │  │
│  │ ├─────────────────────┤ │  │ ├───────────────────────────┤ │  │ ├─────────────────────────┤ │  │
│  │ │ InterfaceCardCtrl   │ │  │ │ Wi-Fi Breakdown (SSID)    │ │  │ │ Monthly Quota Tracker    │ │  │
│  │ └─────────────────────┘ │  │ └───────────────────────────┘ │  │ └─────────────────────────┘ │  │
│  └─────────────────────────┘  └───────────────────────────────┘  └─────────────────────────────┘  │
└───────────────────▲─────────────────────────────▲─────────────────────────────▲───────────────────┘
                    │                             │                             │
    ┌───────────────┴───────────────┐             │             ┌───────────────┴───────────────┐
    │     Polling Engine (Timer)    │             │             │   ThemeManager & Color Engine │
    │    System.Net.NetworkInfo     │             │             │  (Light / Dark / Auto-Detect) │
    └───────────────┬───────────────┘             │             └───────────────────────────────┘
                    │                             │
    ┌───────────────▼───────────────┐ ┌───────────▼───────────┐
    │    WlanApi.dll Native WLAN    │ │   UsageStorage Engine │
    │     SSID Query (P/Invoke)     │ │ (Atomic JSON Storage) │
    └───────────────────────────────┘ └───────────────────────┘
```

### Technical Specification Matrix

| Component | Specification |
| :--- | :--- |
| **Target Operating System** | Windows 10 version 1809 or later and Windows 11 (x64 MSIX) |
| **Target Framework** | .NET Framework 4.8 (C# 5 Language Specification) |
| **Graphics Subsystem** | WPF with Direct3D hardware rendering pipeline |
| **DPI Awareness** | Per-Monitor V2 High-DPI Aware (declared in `app.manifest`) |
| **Network Telemetry Source** | `System.Net.NetworkInformation.IPInterfaceStatistics` |
| **WLAN API Integration** | Native Win32 P/Invoke to `wlanapi.dll` (`WlanOpenHandle`, `WlanEnumInterfaces`, `WlanQueryInterface`) with fallback to `netsh wlan show interfaces` |
| **Window Theming Integration** | Win32 DWM API P/Invoke (`DwmSetWindowAttribute`, attribute `DWMWA_USE_IMMERSIVE_DARK_MODE = 20`) |
| **Shell Integration** | `System.Windows.Forms.NotifyIcon` for System Notification Area (Tray) |
| **Serialization Engine** | `System.Web.Script.Serialization.JavaScriptSerializer` (native framework, zero JSON library dependencies) |
| **Persistent Storage Path** | `%LOCALAPPDATA%\NetPulse\usage_history.json`, `settings.json`, and optional `startup_error.log` |
| **Binary Output** | Standalone Windows executable or x64 MSIX (`NetPulseMonitor-2.1.0.0-x64.msix`) |

---

## 🌟 Core Feature Specifications

### 1. Live Dashboard View

The Live Dashboard delivers real-time connection telemetry updated on a configurable timer interval (default 1.0s):

* **Real-Time Bandwidth Gauges (KPI Cards):**
  * **Download Rate:** Large format display scaled dynamically (`B/s`, `KB/s`, `MB/s`, `GB/s` or `bps`, `Kbps`, `Mbps`, `Gbps`). Emphasized in Emerald Teal (`#00E5A3` Dark / `#059669` Light).
  * **Upload Rate:** Large format display emphasized in Rose Coral (`#FF5B79` Dark / `#DC2626` Light).
  * **Session Data Accumulator:** Running total of bytes transferred during the active monitoring session, alongside live download/upload split and a quick historical teaser (`Today: X GB • Last 7d: Y GB • This Month: Z GB`).
* **Hardware-Accelerated 60-Second Real-Time Chart (`RealtimeChart.cs`):**
  * Vector geometry rendered using `DrawingVisual` and `PathGeometry` with dual vertical gradient brushes.
  * Real-time dynamic Y-axis ceiling auto-scaling (e.g., auto-adjusts from 500 KB/s to 100 MB/s seamlessly).
  * Smooth 60-second rolling circular buffer tracking both incoming and outgoing channels simultaneously.
* **Per-Interface Live Adapter Cards (`InterfaceCardControl.cs`):**
  * **Dedicated Wi-Fi Adapter Card:**
    * **Connected SSID Pill:** Vibrant header pill displaying the active wireless network name (e.g., `[ 📶 Home-Fiber-5G ]`) with green active-link indicator.
    * **Details Grid:** 4-column layout presenting `CONNECTED SSID`, `TODAY ON THIS SSID`, `IPv4 ADDRESS`, and `LINK SPEED`.
    * **Live Mini-Sparkline:** Embedded 60-second mini visual graph dedicated to the interface.
    * **Embedded Wi-Fi Networks Breakdown (Today):** Live list of all networks used today with individual transfer totals, percentage shares, visual progress bars, and a 1-click shortcut to full history analytics.
  * **Dedicated Ethernet Adapter Card:**
    * Displays connection status, IPv4 address, hardware MAC address, negotiated link speed (e.g., 1 Gbps), today's total data, live speed metrics, and mini sparkline.

---

### 2. Usage History & Analytics

The Usage History view provides retrospective analytics over recorded data:

* **7 Time Range Presets:**
  * **Today:** Data recorded since 00:00:00 of the current calendar day.
  * **Yesterday:** Full 24-hour retrospective of yesterday's usage.
  * **Last 7 Days:** Rolling 7-day trailing total and daily distribution.
  * **Last Week:** Previous calendar week (Monday through Sunday) summary.
  * **This Month:** Month-to-date cumulative consumption.
  * **Last Month:** Previous full calendar month (e.g., Oct 01 to Oct 31).
  * **All Time:** Complete historical aggregation since first installation.
* **Summary KPI Cards:**
  * Displays **Total Usage**, **Downloaded Volume** (with percentage share), **Uploaded Volume** (with percentage share), and **Daily Average** (with peak day identifier).
* **Interactive Daily Stacked Bar Chart (`HistoricalBarChart.cs`):**
  * Vector-rendered stacked bar chart displaying daily download and upload ratios.
  * Interactive mouse hover tooltips displaying date, download bytes, upload bytes, and total bytes.
  * Horizontal gridlines with automatic unit scaling (`MB`, `GB`).
* **Daily Data Usage Log Table:**
  * Scrollable chronological table displaying every day's date, formatted download, upload, and combined total data.
* **One-Click CSV Export:**
  * Generates clean, standard `.csv` spreadsheet files with structured sections for daily logs, overall summaries, and Wi-Fi network breakdowns.

---

### 3. Wi-Fi Network (SSID) Breakdown Engine

NetPulse features an integrated Wi-Fi attribution engine:

```
                              ┌─────────────────────────────────────────┐
                              │           NetworkMonitor.cs             │
                              │       Periodic Polling Trigger          │
                              └────────────────────┬────────────────────┘
                                                   │
                        ┌──────────────────────────┴──────────────────────────┐
                        │                                                     │
            [Non-Wireless / Ethernet]                                [Wireless 802.11 Interface]
                        │                                                     │
            Usage recorded under                                  Query WifiNetworkHelper.cs
             Adapter GUID only                                                │
                                                   ┌──────────────────────────┴──────────────────────────┐
                                                   │ P/Invoke wlanapi.dll (WlanQueryInterface opcode 7)  │
                                                   │ Fallback: netsh wlan show interfaces parser         │
                                                   │ Result cached for 3,000ms to eliminate CPU load     │
                                                   └──────────────────────────┬──────────────────────────┘
                                                                              │
                                                                 Extract Connected SSID Name
                                                               (e.g. "Home-Fiber-5G", "Office-Corp")
                                                                              │
                                                            Record traffic in DayUsageRecord with SSID
                                                                              │
                                                   ┌──────────────────────────┴──────────────────────────┐
                                                   │                                                     │
                                        Live Page Wi-Fi Card:                               History Tab:
                                        - Header SSID Badge                                 - "WI-FI NETWORKS (BY SSID)" Panel
                                        - Today on SSID Counter                             - Sorted descending by Total Bytes
                                        - Today Networks Breakdown                          - % Share of Wi-Fi traffic
                                                                                            - Full inclusion in CSV export
```

* **SSID Detection Pipeline (`WifiNetworkHelper.cs`):**
  * Queries `wlanapi.dll` via P/Invoke (`WlanOpenHandle`, `WlanEnumInterfaces`, `WlanQueryInterface` with opcode `wlan_intf_opcode_current_connection = 7`).
  * Extracts the raw `DOT11_SSID` byte array and decodes it using UTF-8 / ASCII.
  * If the WLAN service is unavailable (e.g., in sandboxed containers), safely falls back to executing and parsing `netsh wlan show interfaces`.
  * Implements a 3.0-second caching mechanism so repeated polling cycles incur zero operating system overhead.
* **Data Attribution & Interactive SSID Inspection:**
  * Every byte transferred on a Wi-Fi interface is simultaneously attributed to both the physical adapter and the connected SSID.
  * If the device transitions from `Office-WiFi` to `Mobile-Hotspot`, traffic is partitioned accurately without data overlap.
  * **Interactive Network Selection & Deep Inspection:**
    * Users can click any Wi-Fi network card in the **WI-FI NETWORKS (BY SSID)** list.
    * The clicked network is highlighted with an accent border, tinted background, `Selected ✓` badge, and an inline quick-drawer showing recorded peak download/upload speeds.
    * A **Network Details Inspector** banner appears above the daily log displaying the network name, connection status (Active vs. Saved), total usage, recv/sent breakdown, recorded peak speeds, and percentage share of Wi-Fi traffic.
    * The daily log table, historical chart, and KPI summary automatically filter to show data exclusive to that SSID.
    * A 1-click `[ ✕ Show All ]` / `[ ✕ Show All Networks ]` button (or clicking the card again) instantly clears the filter and restores the multi-network view.
  * **Delete Unnecessary Networks:**
    * Users can cleanly delete any unwanted or temporary network (e.g., public hotspots, one-time guest networks) directly from history:
      * **Right-Click Context Menu:** Right-click any Wi-Fi network card and select `🗑 Delete "<SSID>" from History`.
    * A standard confirmation dialog protects against accidental deletion. Upon confirmation, the SSID and its attributed data are cleanly removed from storage across all historical days, with totals, Wi-Fi interface summaries, and charts recalculated and saved immediately.

---

### 4. Visited Site URL List Usage History Engine

NetPulse provides deep visibility into web browsing activity alongside network bandwidth consumption:

```
                               ┌──────────────────────────────────────────┐
                               │           VisitedSitesHelper.cs          │
                               │        Native Windows SQLite Engine      │
                               └────────────────────┬─────────────────────┘
                                                    │
                         ┌──────────────────────────┴──────────────────────────┐
                         │                                                     │
             [Chromium-Based Browsers]                                 [Mozilla Firefox]
             Edge, Chrome, Brave, Opera, Vivaldi                       Places SQLite Architecture
                         │                                                     │
             Extracts from History database                            Extracts from places.sqlite
             (Safe snapshot with WAL support)                          (Safe snapshot with WAL support)
                         │                                                     │
                         └──────────────────────────┬──────────────────────────┘
                                                    │
                                   Filter by Time Range Preset
                       (Today, Yesterday, Last 7 Days, This Month, All Time)
                                                    │
                                     Deduplicate & Group by URL
                                     Extract Domain (e.g. "github.com")
                                     Calculate Total Visits & Last Visit Time
                                                    │
                         ┌──────────────────────────┴──────────────────────────┐
                         │                                                     │
                                                                 Right History Panel:
                                                                 - Switchable Sub-View:
             - Top domains ranked by visits                        - [📊 Daily Data Log]
             - Visit share percentage & visual bars                - [🌐 Visited Sites & URLs (<count>)]
                                                                 - Real-time search by URL, Title, Domain
                                                                   - Browser filter dropdown (Edge, Chrome, FF...)
                                                                   - 1-click Open Site ↗ & Copy URL 📋
                                                                   - Dedicated CSV Export (📁 Export URLs)
```

* **Zero External Dependencies:**
  * Uses native Windows P/Invoke to `winsqlite3.dll` (shipped with Windows 10 & 11) to read local browser databases. No external NuGet packages or SQLite runtime files required.
* **Multi-Browser Discovery:**
  * Automatically discovers profiles across **Microsoft Edge**, **Google Chrome**, **Mozilla Firefox**, **Brave Browser**, **Opera**, **Opera GX**, and **Vivaldi**.
* **Lock-Free Safe File Copying:**
  * Uses `FileStream` with `FileShare.ReadWrite | FileShare.Delete` and temporary shadow snapshots to safely query browser databases even while browsers are actively running.
* **100% Offline & Private:**
  * Browsing history never leaves the local machine. NetPulse contains zero telemetry, zero analytics, and zero external network transmission.

---

### 5. Settings & Preferences Management

The Settings tab (`BuildSettingsView`) provides deep control over the application's behavior and appearance:

| Setting Category | Control | Description |
| :--- | :--- | :--- |
| **System & Startup** | **Run on Windows Startup** | The standalone build can register a per-user Windows startup entry and shortcut. This option is disabled in the Microsoft Store MSIX build. |
| | **Start Minimized** | Launches NetPulse hidden in the notification area on system boot. |
| | **Minimize to System Tray on Close** | Prevents window close (`WM_CLOSE`) from terminating the process; hides the window and docks into the taskbar tray to keep background recording active. |
| | **Always on Top (Floating Window)** | Toggles the `Topmost` window property so NetPulse remains visible over games, IDEs, and fullscreen browsers. |
| **Monitoring & Units** | **Bandwidth Polling Rate** | Select between `0.5s (High Precision)`, `1.0s (Normal / Recommended)`, or `2.0s (Eco Mode / Battery Saver)`. |
| | **Speed Units Display** | Formats rates as `Auto (B/s, KB/s, MB/s, GB/s)`, `Bits (bps, Kbps, Mbps, Gbps)`, or strict `Bytes (B/s, KB/s, MB/s)`. |
| | **Hide Inactive Adapters** | Toggles filtering of disconnected/dormant adapters from the Live Dashboard. |
| **Appearance & Theme** | **Theme Mode** | Three-way segmented selection: `🖥 System (Auto)`, `🌙 Dark Mode`, or `☀️ Light Mode`. |
| **Visited Sites History** | **Track Visited Site URLs** | Enables extracting and analyzing visited site URLs and titles from local web browser databases. |
| | **Include in CSV Exports** | Toggles whether visited site URLs and visit counts are appended to exported usage CSV reports. |
| | **Refresh Cache Now** | Button to immediately reload and recalculate browser history from local databases. |
| **Monthly Quota** | **Monthly Quota (GB)** | Configurable data cap (e.g., 100 GB) with billing cycle reset day (1st – 31st). Includes live visual progress gauge with warning thresholds (Green < 75%, Amber 75–90%, Red > 90%). |
| **Storage & Privacy** | **Data Management** | Displays real-time storage file size; provides 1-click buttons to `Open Folder`, `Export All Data (CSV)`, and `Clear All Data`. |

---

### 6. System Tray & Windows Integration

* **Notification Area Icon (`NotifyIcon`):**
  * NetPulse docks cleanly into the Windows Notification Area with a high-resolution embedded application icon (`app.ico`).
  * Right-click Context Menu:
    * `⚡ Show NetPulse`: Restores and focuses the main window.
    * `↻ Reset Session`: Clears current session counters while preserving historical logs.
    * `✕ Exit`: Fully terminates the application and flushes pending data to disk.
* **Desktop Window Manager (DWM) Integration:**
  * Uses Win32 P/Invoke (`DwmSetWindowAttribute`) to enforce native Windows 10/11 dark title bars when Dark Mode is active, preventing bright white title bars from breaking the theme.
* **Windows Theme Change Listener:**
  * Hooks into the Win32 window message pipeline (`HwndSource`) to listen for `WM_SETTINGCHANGE`. When the user toggles Windows between Dark and Light mode in Windows Settings, NetPulse adapts dynamically in real time.

---

## 💾 Data Storage & Schema Specifications

NetPulse stores all persistent data locally in the standard Windows local application directory:
```
%LOCALAPPDATA%\NetPulse\
```

### Safe Atomic Write Pattern
To prevent file corruption during sudden system shutdowns, power outages, or application crashes:
1. Data is serialized into a temporary file (`usage_history.json.tmp`).
2. The file is flushed to disk via `File.WriteAllText`.
3. `File.Copy` overwrites the destination target atomically.
4. The temporary file is safely cleaned up.

---

### Usage History Schema

**File Path:** `%LOCALAPPDATA%\NetPulse\usage_history.json`

```json
{
  "Version": 1,
  "CreatedDate": "2026-11-03T12:00:00.0000000+05:30",
  "LastUpdatedDate": "2026-11-03T18:25:00.0000000+05:30",
  "Days": [
    {
      "Date": "2026-11-03",
      "TotalDownloaded": 4294967296,
      "TotalUploaded": 536870912,
      "PeakDownloadSpeed": 25165824.0,
      "PeakUploadSpeed": 4194304.0,
      "Interfaces": [
        {
          "InterfaceId": "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}",
          "InterfaceName": "Wi-Fi (Wireless Network)",
          "InterfaceType": "Wi-Fi",
          "BytesDownloaded": 3221225472,
          "BytesUploaded": 322122547,
          "PeakDownloadSpeed": 20971520.0,
          "PeakUploadSpeed": 3145728.0
        }
      ],
      "WifiNetworks": [
        {
          "Ssid": "Home-Fiber-5G",
          "BytesDownloaded": 2684354560,
          "BytesUploaded": 268435456,
          "PeakDownloadSpeed": 20971520.0,
          "PeakUploadSpeed": 3145728.0
        },
        {
          "Ssid": "Pixel-Hotspot-5G",
          "BytesDownloaded": 536870912,
          "BytesUploaded": 53687091,
          "PeakDownloadSpeed": 8388608.0,
          "PeakUploadSpeed": 1048576.0
        }
      ]
    }
  ]
}
```

---

### Configuration Schema

**File Path:** `%LOCALAPPDATA%\NetPulse\settings.json`

```json
{
  "RunOnStartup": true,
  "StartMinimized": false,
  "CloseToTray": true,
  "AlwaysOnTop": false,
  "PollingIntervalSeconds": 1.0,
  "SpeedUnitMode": "Auto",
  "HideDisconnectedAdapters": false,
  "ThemePreference": "System",
  "EnableMonthlyQuota": true,
  "MonthlyQuotaGB": 100.0,
  "QuotaResetDay": 1
}
```

---

## 🏪 Microsoft Store Publication Specification

When submitting **NetPulse Monitor** to the **Microsoft Store via Windows Partner Center**, utilize the following configuration:

| Partner Center Property | Recommended Value |
| :--- | :--- |
| **Product Type** | MSIX (Store-signed on submission) or trusted-certificate-signed Win32 installer (EXE/MSI) |
| **Primary Category** | **Utilities & tools** |
| **Subcategory** | **System & performance** *(or **Networking & connectivity**)* |
| **Secondary Category** | **Developer tools** (Subcategory: *Networking*) — *or* **Productivity** |
| **Content Rating** | All Ages (No age-restricted content; network identifiers are processed locally) |
| **Pricing & Availability** | Free / Open Source |
| **Search Keywords** | `network monitor`, `bandwidth monitor`, `data usage tracker`, `wifi monitor`, `internet speed meter`, `traffic monitor`, `ssid tracker`, `daily data usage` |
| **Privacy Policy URL** | Link to [`PRIVACY.md`](PRIVACY.md) in your public repository |

The standalone `NetPulseMonitor.exe` produced by the regular build is not an installer and does not pass Partner Center's silent-install or Add/Remove Programs checks. For a Win32 submission, build and upload the installer described below. It registers the app name and publisher, supports quiet install and uninstall, and installs per user without requiring elevation. Set the publisher to the exact publisher identity shown in Partner Center. Both the app executable and installer are signed with a trusted code-signing certificate before submission.

---

## 🚀 Compilation & Build Guide

NetPulse is designed to build directly using the native C# compiler (`csc.exe`) bundled with the Windows operating system. No Visual Studio installation or third-party package managers are needed.

### Prerequisites
* Windows 10 or Windows 11.
* Microsoft .NET Framework 4.8 runtime (built into all modern Windows systems).
* Windows SDK (including MakeAppx) for MSIX packaging.

### Microsoft Store Win32 Installer
Install Inno Setup 6 and the Windows SDK SignTool. Install your code-signing certificate in the Current User `Personal` certificate store, then set the exact Partner Center publisher name and certificate thumbprint:
```powershell
$env:NETPULSE_STORE_PUBLISHER = "Exact Store publisher name"
$env:NETPULSE_SIGN_CERT_SHA1 = "Certificate thumbprint"
.\build-store.ps1
```
The script signs both the app executable and the installer and writes the setup to `artifacts\store\NetPulseMonitor-Setup-x64.exe`. If SignTool or Inno Setup is outside the standard install location, set `NETPULSE_SIGNTOOL` or `INNO_SETUP_COMPILER` to its path. Configure Partner Center's silent install command as:
```text
/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
```
The Inno Setup uninstaller supports quiet removal with `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`.

### Microsoft Store MSIX
For a certificate-free Store submission, install the Windows SDK and use the exact identity values reserved in Partner Center. The Store signs the package during submission; do not self-sign it.
```powershell
powershell -ExecutionPolicy Bypass -File .\build-msix.ps1 `
  -IdentityName "amrg-projects.NetPlusMonitor" `
  -IdentityPublisher "CN=86D97629-174B-46ED-8787-814914FDD587" `
  -DisplayName "Net Plus Monitor" `
  -PublisherDisplayName "amrg-projects"
```
The unsigned package is written to `artifacts\msix\NetPulseMonitor-2.1.0.0-x64.msix`. Partner Center's Package Identity Name, Identity Publisher, and publisher display name must match exactly. The MSIX build disables the unsupported registry-based startup option; regular EXE builds retain it.

In Partner Center, target the Windows Desktop device family only; this MSIX does not support Xbox. The `runFullTrust` restricted capability is required for the WPF desktop app, so request its approval in the submission rather than removing it from the manifest.

### Option 1: 1-Click Batch Build
Double-click `build.bat` in the repository root:
```cmd
build.bat
```

### Option 2: PowerShell Automated Build
Execute `build.ps1` from PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

### Direct CLI Compiler Command
If building manually via Command Prompt:
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /win32icon:app.ico /win32manifest:app.manifest /out:DataMonitor.exe /lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF","C:\Windows\Microsoft.NET\Framework64\v4.0.30319" /r:PresentationFramework.dll,PresentationCore.dll,WindowsBase.dll,System.Xaml.dll,System.dll,System.Core.dll,System.Drawing.dll,System.Web.Extensions.dll,System.Windows.Forms.dll src\Helpers.cs src\AppSettings.cs src\WifiNetworkHelper.cs src\VisitedSitesHelper.cs src\ThemeManager.cs src\Models.cs src\HistoryModels.cs src\UsageStorage.cs src\NetworkMonitor.cs src\Controls\RealtimeChart.cs src\Controls\HistoricalBarChart.cs src\Controls\InterfaceCardControl.cs src\MainWindow.cs src\Program.cs
```

---

## 📁 Repository File Hierarchy

```
data-monitor-app/
├── NetPulseMonitor.exe          # Production standalone 64-bit Windows binary (~150 KB)
├── DataMonitor.exe              # Direct compiler target executable
├── app.ico                      # High-resolution application and taskbar tray icon
├── app.manifest                 # Win32 Application Manifest (Per-Monitor V2 High-DPI & OS targeting)
├── build.bat                    # 1-click Windows Batch build script
├── build.ps1                    # Automated PowerShell compilation script
├── build-store.ps1              # Signed Win32 installer build
├── build-msix.ps1               # Unsigned MSIX build for Store signing
├── store-installer.iss          # Inno Setup Win32 installer definition
├── msix/AppxManifest.xml        # MSIX manifest template
├── PRIVACY.md                   # Complete Privacy Policy for Microsoft Store submission
├── README.md                    # Full technical specification & documentation
│
└── src/                         # Complete C# WPF Application Source
    ├── Program.cs               # Application entry point & single-instance enforcement
    ├── MainWindow.cs            # Root UI layout, triple-tab coordinator & DWM title bar styling
    ├── NetworkMonitor.cs        # Periodic network polling engine & interface tracker
    ├── WifiNetworkHelper.cs     # Native Win32 WLAN API (wlanapi.dll) P/Invoke & SSID detection
    ├── VisitedSitesHelper.cs    # Native Windows SQLite (winsqlite3.dll) multi-browser history extractor
    ├── UsageStorage.cs          # Thread-safe persistent JSON storage engine & CSV exporter
    ├── HistoryModels.cs         # Historical daily records, SSID usage records & period summaries
    ├── AppSettings.cs           # Persistent settings manager & Windows startup registry handler
    ├── Models.cs                # Live interface snapshots, circular speed buffers & global metrics
    ├── ThemeManager.cs          # Cyber-dark & clean-light color palettes & theme coordinator
    ├── Helpers.cs               # Byte formatting, speed conversions, duration helpers & color parsers
    └── Controls/
        ├── RealtimeChart.cs     # Hardware-accelerated 60-second vector speed chart
        ├── HistoricalBarChart.cs# Interactive daily stacked bar chart with tooltips
        └── InterfaceCardControl.cs # Live adapter cards with Wi-Fi SSID breakdown & sparklines
```

---

## 🔒 Privacy & Security

* **No Packet Capture / Inspection:** NetPulse reads high-level byte counters and connection identifiers from Windows. It never inspects packet payloads, visited URLs, or communications.
* **Local Data Only:** SSIDs, usage history, settings, and startup exception details are stored locally. NetPulse does not send this information to developer or analytics servers.
* **User Control:** Users can view the storage location, export usage history and SSID breakdowns to CSV, clear usage history, or delete the local data folder. Uninstalling the MSIX may leave this external data folder in place.
* For the full legal text, review [`PRIVACY.md`](PRIVACY.md).

---

## 📄 License

Licensed under the [MIT License](LICENSE). Open-source, transparent, and free for personal and commercial use.
