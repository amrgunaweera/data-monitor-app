# ⚡ NetPulse — Windows Network Data Monitor

A sleek, modern, real-time network data and bandwidth monitor for Windows. Built natively to track **Wi-Fi** and **Ethernet** network interfaces with high precision, beautiful cyber-dark visuals, and live interactive speed charts.

![Platform](https://img.shields.io/badge/Platform-Windows_10_%2F_11-0078D4?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/.NET_Framework-4.8_WPF-512BD4?style=for-the-badge&logo=dotnet)
![Dependencies](https://img.shields.io/badge/Dependencies-Zero_(Self--Contained)-success?style=for-the-badge)

---

## ✨ Features

- **Automatic Windows Theme Detection & Instant Toggle**:
  - Automatically detects whether Windows is set to **Dark** or **Light** mode (via `AppsUseLightTheme` registry setting) and defaults to it.
  - Dynamically synchronizes if the Windows theme is toggled in Windows Settings (`WM_SETTINGCHANGE`).
  - Interactive **☀️ Light / 🌙 Dark** switch in the top header to manually toggle themes anytime with instant smooth transition.
  - Dynamically changes the Windows title bar via Desktop Window Manager (`DwmSetWindowAttribute`).
- **Zero-Dependency Native Windows App (`DataMonitor.exe`)**:
  - Double-click and run directly on Windows 10 & 11 — no Python, Node, or heavy runtimes required.
- **Dedicated Wi-Fi & Ethernet Tracking**:
  - Automatically discovers and categorizes physical wireless (802.11) and Ethernet adapters.
  - Automatically filters out virtual interfaces, VPNs, loops, and Bluetooth noise.
- **Real-Time Speed & Bandwidth Gauges**:
  - Live Download speed (Teal/Emerald `#00E5A3`) and Upload speed (Rose/Coral `#FF5B79`) with dynamic B/s, KB/s, MB/s, GB/s scaling.
  - Peak download and upload speed indicators.
- **Session Data Consumption**:
  - Tracks total data received, sent, and combined since startup or last reset.
- **Real-Time Vector Area Chart**:
  - Custom hardware-accelerated 60-second scrolling traffic graph with dual-gradient fills, auto-scaling gridlines, and glowing data points.
- **Per-Interface Cards**:
  - Individual cards for each adapter with connection status (Connected / Disconnected), IPv4 address, MAC address, link speed, current download/upload rates, session totals, and a mini live sparkline chart.
- **Modern Dark UI Design**:
  - Immersive Windows dark title bar integration via DWM API.
  - Acrylic/slate glass aesthetics with glowing accents and clean Segoe UI typography.
  - **Pin on Top / Float** button to monitor network usage while gaming, streaming, or downloading.
  - **Reset Session** button to measure specific downloads or gaming sessions.

---

## 🚀 How to Run

### Option 1: Run Precompiled Executable (Instant)

Simply double-click:
```
NetPulseMonitor.exe
```
That's it! It runs instantly with zero prerequisites.

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
├── build.bat                 # 1-click batch build script
├── build.ps1                 # PowerShell build script
│
├── src/                      # C# WPF Native Application
│   ├── Program.cs            # Application entry point
│   ├── MainWindow.cs         # Main dashboard layout & DWM dark title bar
│   ├── NetworkMonitor.cs     # System.Net.NetworkInformation polling engine
│   ├── Models.cs             # Interface snapshots & 60-second history buffers
│   ├── Helpers.cs            # Formatting, colors, and unit conversions
│   └── Controls/
│       ├── RealtimeChart.cs  # Hardware-accelerated vector speed chart
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

## 🎨 UI Overview

- **Top Header Bar**: App branding, Live status badge, Active adapter count, Session uptime counter, "Float (Always on Top)" toggle, and "Reset Session" button.
- **Top Metric Cards**:
  - **Download Speed**: Prominent numerical readout, dynamic unit, and peak speed.
  - **Upload Speed**: Prominent numerical readout, dynamic unit, and peak speed.
  - **Session Data**: Combined data transferred, separate received and sent totals.
- **Real-Time Activity Graph**: Full-width scrolling vector canvas displaying the last 60 seconds of traffic with gridlines and live speed labels.
- **Network Adapters**: Scrollable container rendering cards for every physical Wi-Fi and Ethernet adapter with real-time stats and sparklines.

---

## 📄 License

MIT License. Open source and free to use.
