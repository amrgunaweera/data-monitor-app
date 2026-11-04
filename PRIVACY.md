# Privacy Policy for NetPulse Monitor

**Last updated:** November 4, 2026

**NetPulse Monitor** ("we", "our", or "the Application") is committed to respecting and protecting your privacy. This Privacy Policy explains what the Windows desktop application accesses, stores, and transmits, including when distributed as an MSIX package through the Microsoft Store.

---

### 1. Summary
* **No app telemetry:** NetPulse does not send network statistics, Wi-Fi names, settings, diagnostic logs, or crash reports to the developer or to analytics or advertising services.
* **Local processing:** Network information and usage history are processed and stored on your device. Wi-Fi names and other network identifiers may be sensitive, so the app does not transmit them.
* **No packet inspection:** NetPulse reads Windows network-interface counters and connection details. It does not inspect packet contents, visited URLs, or communications.
* **No app-operated server:** NetPulse does not connect to a developer-operated service for analytics, advertising, account management, or cloud storage.

---

### 2. Information Handled Locally

The Application accesses and stores the following information locally on your device:

1. **Network Interface Information:**
   * Adapter names (e.g., *Wi-Fi*, *Ethernet*).
   * Interface identifiers, hardware MAC addresses, and local IPv4 addresses (for interface identification or display).
   * Connection link speed and operational status (Connected / Disconnected).
2. **Wi-Fi Network Identifiers (SSID):**
   * The name of the currently connected Wi-Fi network (SSID), detected using the Windows WLAN API or, if needed, the built-in `netsh` command. SSIDs are associated with local usage records to show data consumption by network. They can reveal information about a person's home, workplace, or other location.
3. **Bandwidth & Usage Statistics:**
   * Byte counters (bytes downloaded and uploaded) aggregated periodically.
   * Daily usage summaries and peak transfer speeds.
4. **Application Settings:**
   * Your chosen UI preferences (e.g., Theme preference, polling intervals, speed unit format, monthly data quota).
5. **Startup diagnostic log:**
   * If an unhandled application exception occurs, NetPulse may append its timestamp, exception message, and stack trace to a local `startup_error.log` file. This log is not sent to the developer.

---

### 3. Data Storage and Security

* **Storage Location:** Usage history and settings are saved as plain-text JSON files in your Windows user profile folder. The optional startup diagnostic log is stored in the same folder:
  ```
  %LOCALAPPDATA%\NetPulse\
  ```
  *(e.g., `C:\Users\<YourUsername>\AppData\Local\NetPulse\`)*
   The files are `usage_history.json`, `settings.json`, and, only if an unhandled exception is logged, `startup_error.log`.
* **Data Protection:** The files are protected by your Windows user account and filesystem permissions. NetPulse does not upload them or transmit them over the internet.
* **Optional startup behavior:** In the standalone desktop build, enabling "Run on Windows Startup" creates a per-user Windows startup entry and shortcut containing the app's local executable path. The Microsoft Store MSIX build disables this option and does not create those entries.

---

### 4. User Rights and Data Control

You have full control over all data stored by the Application:
* **View Data:** You can inspect the local files by clicking **"Open Storage Folder"** in Settings.
* **Export Data:** You can export usage history and Wi-Fi SSID breakdowns to a `.csv` file at a location you choose. Anyone with access to that file can read its contents.
* **Delete History:** **"Clear All Data"** deletes persistent usage history. It does not delete application settings or a startup diagnostic log. To delete all locally stored files, close NetPulse and delete the `%LOCALAPPDATA%\NetPulse\` folder.
* **Uninstallation:** Uninstalling removes the application package and its installed files. The `%LOCALAPPDATA%\NetPulse\` data folder is outside the MSIX package and may remain after uninstall; delete it separately to remove usage history, settings, and any diagnostic log.

---

### 5. Third-Party Services and Analytics

NetPulse does **not** integrate third-party analytics, tracking, advertising, or cloud-storage services. When installed through Microsoft Store, Microsoft may process account, download, purchase, or diagnostic information under its own privacy statement; NetPulse does not receive that information through the application. See the [Microsoft Privacy Statement](https://privacy.microsoft.com/privacystatement).

---

### 6. Children's Privacy

NetPulse is not designed to collect information from children. The application processes network identifiers locally, and users or guardians should consider whether saving Wi-Fi names or exporting usage reports is appropriate for the device user.

---

### 7. Changes to This Privacy Policy

Because NetPulse does not collect or transmit personal information, changes to this policy will be minimal. If we update this document, the revised version will be published in this repository with an updated revision date.

---

### 8. Contact Information

If you have questions, feedback, or concerns regarding this Privacy Policy or NetPulse Monitor, please open an issue in the project's repository or contact the developer.

