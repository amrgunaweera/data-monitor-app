# ⚡ NetPulse — Windows Network Data Monitor & History

A sleek, modern, real-time network data, bandwidth monitor, persistent usage history tracker, and configurable management suite for Windows. Built natively to track **Wi-Fi** and **Ethernet** network interfaces with high precision, beautiful cyber-dark & light visuals, live interactive speed charts, comprehensive daily, weekly, and monthly data analytics, and rich user preferences.

![Platform](https://img.shields.io/badge/Platform-Windows_10_%2F_11-0078D4?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/.NET_Framework-4.8_WPF-512BD4?style=for-the-badge&logo=dotnet)
![Dependencies](https://img.shields.io/badge/Dependencies-Zero_(Self--Contained)-success?style=for-the-badge)

---

## ✨ Features

### ⚙️ Dedicated Settings Page (`AppSettings.cs`)
- **System & Windows Integration**:
  - **Run on Windows Startup**: Optionally auto-launch NetPulse on Windows login via native user registry entry.
  - **Start Minimized**: Launch silently in the background on startup.
  - **Minimize to System Tray on Close**: Closing the window docks NetPulse into the Windows Notification Area / System Tray so background tracking is never interrupted.
  - **Keep Always on Top (Float)**: Ensure the window stays visible over games and fullscreen media.
- **Monitoring & Units**:
  - **Configurable Polling Rate**: Switch between `0.5s (High Precision)`, `1.0s (Normal / Recommended)`, or `2.0s (Eco Mode)`.
  - **Speed Unit Display**: Format network throughput in `Auto (B/s, KB/s, MB/s, GB/s)`, `Bits (bps, Kbps, Mbps, Gbps)`, or strict `MB/s Only`.
  - **Hide Inactive Adapters**: Filter out disconnected adapters from the live dashboard for a clutter-free view.
- **Monthly Data Quota & Allowance Tracking**:
  - Set a monthly bandwidth cap (in GB) and your billing cycle reset day (1st – 31st).
  - Live color-coded progress bar with remaining data and usage percentage warnings.
- **Appearance & Theming**:
  - Choose between Windows System Preference (Auto sync), Always Dark, or Always Light mode.
- **Data Management & Privacy**:
  - View the persistent file location (`%LocalAppData%\NetPulse\usage_history.json`).
  - One-click `📂 Open Storage Folder` in Windows Explorer.
  - One-click `📁 Export All Data (CSV)` and safe `🗑 Clear Data` with confirmation.

---

### 📊 Persistent Data Storage & Usage History
- **Automatic Background Data Storage**:
  - Automatically records all received and sent network bytes grouped by day and adapter to persistent local storage (`%LocalAppData%\NetPulse\usage_history.json`).
  - Never loses your records across app restarts, system reboots, or updates.
  - Safe atomic disk writes prevent data corruption.
- **Dedicated Time Range Presets**:
  - **Today**: Current day's live usage.
  - **Yesterday**: Full 24-hour retrospective of yesterday's usage.
  - **Last 7 Days**: Rolling 7-day trailing total and daily trend.
  - **Last Week**: Previous calendar week (Monday to Sunday) summary.
  - **This Month**: Month-to-date data consumption.
  - **Last Month**: Previous full calendar month usage (e.g. Oct 1 – Oct 31).
  - **All Time**: Entire recorded history since first run.
- **Interactive Daily Stacked Bar Chart**:
  - Custom vector bar chart visualizing daily usage with stacked Download (Teal) and Upload (Coral) metrics.
  - Hover over any day's bar to see a rich interactive tooltip with exact download, upload, and total usage.
  - Auto-scaling Y-axis gridlines (`GB`, `MB`).
- **Adapter & Wi-Fi Network Breakdown**:
  - Detailed split between **Wi-Fi** and **Ethernet** usage with percentage shares and visual progress bars.
  - **📶 Wi-Fi Breakdown by Networks (SSID)**: Automatically queries native Windows WLAN API (`wlanapi.dll` & `netsh wlan`) to detect connected network SSIDs (e.g., `Home-Fiber-5G`, `Office-Corporate-WPA3`, `Mobile-Hotspot`).
  - Displays each Wi-Fi network used during the selected period with total bytes, download/upload split, relative percentage of Wi-Fi traffic, and progress bars.
  - Highlights currently active connected Wi-Fi network with an `Active` badge.
  - Includes full Wi-Fi network breakdown in exported CSV reports.
- **Daily Usage Data Table**:
  - Clean chronological table listing every day's date, download volume, upload volume, and combined total.
- **One-Click CSV Export**:
  - Export your complete daily history and summary reports to standard `.csv` spreadsheet files anytime.
- **Instant Demo Mode**:
  - Built-in `🎲 Sample Data` generator to preview past 45 days of realistic network activity.

---

### ⚡ Real-Time Monitoring & Controls
- **Triple Tab Navigation**:
  - Seamlessly switch between **⚡ Live Dashboard**, **📊 Usage History**, and **⚙️ Settings** with one click.
- **System Tray Notification Area Icon**:
  - Right-click tray menu to show window, reset session, or exit cleanly.
- **Automatic Windows Theme Detection & Instant Toggle**:
  - Automatically detects whether Windows is in **Dark** or **Light** mode (via `AppsUseLightTheme` registry setting) and defaults to it.
  - Dynamically synchronizes if the Windows theme changes in Settings (`WM_SETTINGCHANGE`).
  - Top header toggle (**☀️ Light / 🌙 Dark**) with smooth palette transitions.
  - Immersive Windows title bar integration via Desktop Window Manager (`DwmSetWindowAttribute`).
- **Zero-Dependency Native Windows App (`NetPulseMonitor.exe`)**:
  - Double-click and run directly on Windows 10 & 11 — zero external runtimes or NuGet packages required.
- **Dedicated Wi-Fi & Ethernet Tracking**:
  - Automatically identifies physical wireless (802.11) and Ethernet adapters.
  - Filters out virtual interfaces, VPNs, loops, and Bluetooth noise.
- **Live Speed Gauges & Peak Indicators**:
  - Live Download speed (Teal/Emerald `#00E5A3`) and Upload speed (Rose/Coral `#FF5B79`) with dynamic B/s, KB/s, MB/s, GB/s scaling.
  - Peak download and upload speed indicators.
- **Real-Time Vector Area Chart**:
  - Custom hardware-accelerated 60-second scrolling traffic graph with dual-gradient fills, auto-scaling gridlines, and glowing data points.
- **Per-Interface Cards**:
  - Individual cards for each adapter with connection status (Connected / Disconnected), IPv4 address, MAC address, link speed, current download/upload rates, session totals, and a mini live sparkline chart.
- **Pin on Top / Float (in Settings)**:
  - Keep the monitor floating while gaming, streaming, or downloading.

---

## 🚀 How to Run

### Option 1: Run Precompiled Executable (Instant)

Simply double-click:
```
NetPulseMonitor.exe
```
*(or `DataMonitor.exe`)*. It runs instantly with zero prerequisites.

### Option 2: Rebuild from Source

You can recompile the app at any time with one click:
- Double-click **`build.bat`**  
  *or*
- In PowerShell:
  ```powershell
  .\build.ps1
  ```

---

## 📁 Project Structure

```
data-monitor-app/
│
├── NetPulseMonitor.exe       # Standalone native Windows executable
├── DataMonitor.exe           # Direct compiler binary
├── build.bat                 # 1-click batch build script
├── build.ps1                 # PowerShell build script
│
├── src/                      # C# WPF Native Application
│   ├── Program.cs            # Application entry point
│   ├── MainWindow.cs         # Main dashboard layout, tabs, settings & DWM title bar
│   ├── NetworkMonitor.cs     # System.Net.NetworkInformation polling engine
│   ├── AppSettings.cs        # Persistent user settings & Windows startup manager
│   ├── Models.cs             # Interface snapshots & 60-second history buffers
│   ├── HistoryModels.cs      # Daily usage records, presets, and period models
│   ├── UsageStorage.cs       # Persistent JSON storage engine & CSV exporter
│   ├── Helpers.cs            # Formatting, colors, and unit conversions
│   ├── ThemeManager.cs       # Dark/Light theme manager & palette definitions
│   └── Controls/
│       ├── RealtimeChart.cs      # Hardware-accelerated 60-second vector speed chart
│       ├── HistoricalBarChart.cs # Interactive daily stacked bar chart with tooltips
│       └── InterfaceCardControl.cs # Dedicated Wi-Fi & Ethernet adapter cards
│
├── app/                      # Python / CustomTkinter alternative
│   ├── monitor.py
│   ├── charts.py
│   ├── ui.py
│   └── utils.py
│
├── main.py                   # Python entry point
├── requirements.txt          # Python dependencies (if Python is installed)
└── README.md
```

---

## 📄 License

MIT License. Open source and free to use.
