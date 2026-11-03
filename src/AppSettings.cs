using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace NetPulse {
    public class SettingsData {
        public bool RunOnStartup { get; set; }
        public bool StartMinimized { get; set; }
        public bool CloseToTray { get; set; }
        public bool AlwaysOnTop { get; set; }

        public double PollingIntervalSeconds { get; set; }
        public string SpeedUnitMode { get; set; } // "Auto", "Bits", "BytesOnly"
        public bool HideDisconnectedAdapters { get; set; }

        public bool EnableMonthlyQuota { get; set; }
        public double MonthlyQuotaGB { get; set; }
        public int QuotaResetDay { get; set; }

        public string ThemePreference { get; set; } // "System", "Dark", "Light"

        public SettingsData() {
            RunOnStartup = true;
            StartMinimized = false;
            CloseToTray = true;
            AlwaysOnTop = false;

            PollingIntervalSeconds = 1.0;
            SpeedUnitMode = "Auto";
            HideDisconnectedAdapters = false;

            EnableMonthlyQuota = false;
            MonthlyQuotaGB = 100.0;
            QuotaResetDay = 1;

            ThemePreference = "System";
        }
    }

    public class AppSettings {
        private static AppSettings _instance;
        private static readonly object _lock = new object();

        public static AppSettings Instance {
            get {
                if (_instance == null) {
                    lock (_lock) {
                        if (_instance == null) {
                            AppSettings s = new AppSettings();
                            s.Initialize();
                            _instance = s;
                        }
                    }
                }
                return _instance;
            }
        }

        public event Action SettingsChanged;

        private SettingsData _data;
        private string _settingsFilePath;

        public bool RunOnStartup {
            get { return _data.RunOnStartup; }
            set { _data.RunOnStartup = value; }
        }

        public bool StartMinimized {
            get { return _data.StartMinimized; }
            set { _data.StartMinimized = value; }
        }

        public bool CloseToTray {
            get { return _data.CloseToTray; }
            set { _data.CloseToTray = value; }
        }

        public bool AlwaysOnTop {
            get { return _data.AlwaysOnTop; }
            set { _data.AlwaysOnTop = value; }
        }

        public double PollingIntervalSeconds {
            get { return _data.PollingIntervalSeconds; }
            set { _data.PollingIntervalSeconds = value; }
        }

        public string SpeedUnitMode {
            get { return _data.SpeedUnitMode; }
            set { _data.SpeedUnitMode = value; }
        }

        public bool HideDisconnectedAdapters {
            get { return _data.HideDisconnectedAdapters; }
            set { _data.HideDisconnectedAdapters = value; }
        }

        public bool EnableMonthlyQuota {
            get { return _data.EnableMonthlyQuota; }
            set { _data.EnableMonthlyQuota = value; }
        }

        public double MonthlyQuotaGB {
            get { return _data.MonthlyQuotaGB; }
            set { _data.MonthlyQuotaGB = value; }
        }

        public int QuotaResetDay {
            get { return _data.QuotaResetDay; }
            set { _data.QuotaResetDay = value; }
        }

        public string ThemePreference {
            get { return _data.ThemePreference; }
            set { _data.ThemePreference = value; }
        }

        public string SettingsFilePath {
            get { return _settingsFilePath; }
        }

        public AppSettings() {
            _data = new SettingsData();
        }

        public void Initialize() {
            string appDataDir;
            try {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                appDataDir = Path.Combine(localAppData, "NetPulse");
                if (!Directory.Exists(appDataDir)) {
                    Directory.CreateDirectory(appDataDir);
                }
            } catch {
                appDataDir = AppDomain.CurrentDomain.BaseDirectory;
            }

            _settingsFilePath = Path.Combine(appDataDir, "settings.json");
            Load();
            SyncStartupRegistry();
        }

        public void Load() {
            lock (_lock) {
                if (string.IsNullOrEmpty(_settingsFilePath) || !File.Exists(_settingsFilePath)) return;

                try {
                    string json = File.ReadAllText(_settingsFilePath, Encoding.UTF8);
                    if (string.IsNullOrEmpty(json)) return;

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    SettingsData loaded = serializer.Deserialize<SettingsData>(json);
                    if (loaded != null) {
                        _data = loaded;

                        if (_data.PollingIntervalSeconds < 0.2 || _data.PollingIntervalSeconds > 10.0) {
                            _data.PollingIntervalSeconds = 1.0;
                        }
                        if (string.IsNullOrEmpty(_data.SpeedUnitMode)) {
                            _data.SpeedUnitMode = "Auto";
                        }
                        if (_data.MonthlyQuotaGB <= 0) {
                            _data.MonthlyQuotaGB = 100.0;
                        }
                        if (_data.QuotaResetDay < 1 || _data.QuotaResetDay > 31) {
                            _data.QuotaResetDay = 1;
                        }
                        if (string.IsNullOrEmpty(_data.ThemePreference)) {
                            _data.ThemePreference = "System";
                        }
                    }
                } catch {}
            }
        }

        public void Save() {
            lock (_lock) {
                if (string.IsNullOrEmpty(_settingsFilePath)) return;

                try {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(_data);

                    string tmp = _settingsFilePath + ".tmp";
                    File.WriteAllText(tmp, json, Encoding.UTF8);

                    try {
                        File.Copy(tmp, _settingsFilePath, true);
                        File.Delete(tmp);
                    } catch {
                        File.WriteAllText(_settingsFilePath, json, Encoding.UTF8);
                    }
                } catch {}
            }

            SyncStartupRegistry();

            if (SettingsChanged != null) {
                SettingsChanged();
            }
        }

        public void SyncStartupRegistry() {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)) {
                    if (key != null) {
                        string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (RunOnStartup) {
                            key.SetValue("NetPulseMonitor", "\"" + appPath + "\"");
                        } else {
                            key.DeleteValue("NetPulseMonitor", false);
                        }
                    }
                }
            } catch {}
        }
    }
}
