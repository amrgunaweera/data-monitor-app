# Privacy Policy for NetPulse Monitor

**Last updated:** November 3, 2026

**NetPulse Monitor** ("we", "our", or "the Application") is committed to respecting and protecting your privacy. This Privacy Policy explains our practices regarding the collection, storage, and processing of data when you use the NetPulse Windows desktop application.

---

### 1. Summary (Privacy by Design)
* **Zero Telemetry & Tracking:** NetPulse does **not** collect, track, or transmit any personal information, telemetry, usage statistics, or crash reports.
* **100% Local Operation:** All bandwidth metrics, historical usage logs, and configuration settings are stored strictly on your local computer.
* **No Cloud / Remote Servers:** NetPulse has no remote servers, external APIs, advertising networks, or third-party SDKs. It operates entirely offline.
* **No Packet Inspection:** NetPulse does **not** inspect, read, or record the contents of your network packets, visited URLs, web traffic, or communications. It only reads high-level byte counters provided by the Windows operating system.

---

### 2. Information Handled Locally

The Application reads and stores the following information strictly on your local device:

1. **Network Interface Information:**
   * Adapter names (e.g., *Wi-Fi*, *Ethernet*).
   * Hardware MAC addresses and local IPv4 addresses (for display purposes only).
   * Connection link speed and operational status (Connected / Disconnected).
2. **Wi-Fi Network Identifiers (SSID):**
   * The name of currently and previously connected Wi-Fi networks (SSIDs) detected via native Windows WLAN APIs, used solely to break down data consumption by network on your local machine.
3. **Bandwidth & Usage Statistics:**
   * Byte counters (bytes downloaded and uploaded) aggregated periodically.
   * Daily usage summaries and peak transfer speeds.
4. **Application Settings:**
   * Your chosen UI preferences (e.g., Theme preference, polling intervals, speed unit format, monthly data quota).

---

### 3. Data Storage and Security

* **Storage Location:** All data is saved in plain text (JSON format) directly in your Windows user profile folder:
  ```
  %LOCALAPPDATA%\NetPulse\
  ```
  *(e.g., `C:\Users\<YourUsername>\AppData\Local\NetPulse\`)*
* **Data Protection:** Your data is protected by your Windows user account security and filesystem permissions. No data is ever uploaded to the cloud or transmitted over the internet.

---

### 4. User Rights and Data Control

You have full control over all data stored by the Application:
* **View Data:** You can inspect your usage file (`usage_history.json`) at any time by clicking **"📂 Open Storage Folder"** in the Settings tab.
* **Export Data:** You can export your complete daily usage logs and Wi-Fi breakdowns to standard `.csv` spreadsheet files at any time via the **"Export CSV"** button.
* **Delete Data:** You can permanently delete all recorded history at any time by clicking **"🗑 Clear All Data"** in the Settings tab, or by deleting the `%LOCALAPPDATA%\NetPulse\` folder.
* **Uninstallation:** Uninstalling the application removes the executable. Deleting the `%LOCALAPPDATA%\NetPulse\` folder removes all remaining traces of stored data.

---

### 5. Third-Party Services and Analytics

NetPulse does **not** integrate with any third-party analytics services, tracking tools, advertising frameworks, or cloud platforms. No third parties have access to your data through our software.

---

### 6. Children's Privacy

NetPulse does not collect personal data from anyone, including children under the age of 13.

---

### 7. Changes to This Privacy Policy

Because NetPulse does not collect or transmit personal information, changes to this policy will be minimal. If we update this document, the revised version will be published in this repository with an updated revision date.

---

### 8. Contact Information

If you have questions, feedback, or concerns regarding this Privacy Policy or NetPulse Monitor, please open an issue in the project's repository or contact the developer.

