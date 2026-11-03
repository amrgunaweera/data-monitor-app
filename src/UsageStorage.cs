using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace NetPulse {
    public class UsageStorage {
        private static UsageStorage _instance;
        private static readonly object _instanceLock = new object();

        public static UsageStorage Instance {
            get {
                if (_instance == null) {
                    lock (_instanceLock) {
                        if (_instance == null) {
                            _instance = new UsageStorage();
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly object _lock = new object();
        private readonly Dictionary<string, DayUsageRecord> _recordsByDate;
        private readonly string _storageFilePath;
        private bool _isDirty;
        private DateTime _lastSavedTime;

        public string StorageFilePath {
            get { return _storageFilePath; }
        }

        public UsageStorage() {
            _recordsByDate = new Dictionary<string, DayUsageRecord>(StringComparer.OrdinalIgnoreCase);
            _lastSavedTime = DateTime.Now;

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

            _storageFilePath = Path.Combine(appDataDir, "usage_history.json");
            LoadFromDisk();
        }

        private void LoadFromDisk() {
            lock (_lock) {
                if (!File.Exists(_storageFilePath)) return;

                try {
                    string json = File.ReadAllText(_storageFilePath, Encoding.UTF8);
                    if (string.IsNullOrEmpty(json)) return;

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = int.MaxValue;
                    UsageDatabase db = serializer.Deserialize<UsageDatabase>(json);

                    if (db != null && db.Days != null) {
                        _recordsByDate.Clear();
                        for (int i = 0; i < db.Days.Count; i++) {
                            DayUsageRecord day = db.Days[i];
                            if (day != null && !string.IsNullOrEmpty(day.Date)) {
                                _recordsByDate[day.Date] = day;
                            }
                        }
                    }
                } catch {
                    // If file is corrupted or unreadable, preserve old file with .bak extension
                    try {
                        string bak = _storageFilePath + ".corrupt.bak";
                        if (!File.Exists(bak)) {
                            File.Copy(_storageFilePath, bak);
                        }
                    } catch {}
                }
            }
        }

        public void SaveToDisk(bool force) {
            lock (_lock) {
                if (!_isDirty && !force) return;

                try {
                    UsageDatabase db = new UsageDatabase();
                    db.LastUpdatedDate = DateTime.Now.ToString("o");
                    db.Days = new List<DayUsageRecord>(_recordsByDate.Values);
                    db.Days.Sort(delegate(DayUsageRecord a, DayUsageRecord b) {
                        return string.Compare(a.Date, b.Date, StringComparison.Ordinal);
                    });

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = int.MaxValue;
                    string json = serializer.Serialize(db);

                    string tmpPath = _storageFilePath + ".tmp";
                    File.WriteAllText(tmpPath, json, Encoding.UTF8);

                    try {
                        File.Copy(tmpPath, _storageFilePath, true);
                        File.Delete(tmpPath);
                    } catch {
                        File.WriteAllText(_storageFilePath, json, Encoding.UTF8);
                    }

                    _isDirty = false;
                    _lastSavedTime = DateTime.Now;
                } catch {}
            }
        }

        public void RecordTraffic(string ifaceId, string ifaceName, string ifaceType, string wifiSsid, long dlBytes, long ulBytes, double dlSpeed, double ulSpeed) {
            if (dlBytes <= 0 && ulBytes <= 0) return;

            string todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            lock (_lock) {
                DayUsageRecord today;
                if (!_recordsByDate.TryGetValue(todayKey, out today)) {
                    today = new DayUsageRecord { Date = todayKey };
                    _recordsByDate[todayKey] = today;
                }

                today.AddTraffic(ifaceId, ifaceName, ifaceType, wifiSsid, dlBytes, ulBytes, dlSpeed, ulSpeed);
                _isDirty = true;
            }

            // Periodic auto-save every 20 seconds
            if ((DateTime.Now - _lastSavedTime).TotalSeconds >= 20) {
                SaveToDisk(false);
            }
        }

        public void RecordTraffic(string ifaceId, string ifaceName, string ifaceType, long dlBytes, long ulBytes, double dlSpeed, double ulSpeed) {
            RecordTraffic(ifaceId, ifaceName, ifaceType, null, dlBytes, ulBytes, dlSpeed, ulSpeed);
        }

        public DayUsageRecord GetTodayRecord() {
            string todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            lock (_lock) {
                DayUsageRecord rec;
                if (_recordsByDate.TryGetValue(todayKey, out rec)) {
                    return rec;
                }
                return new DayUsageRecord { Date = todayKey };
            }
        }

        public PeriodUsageSummary GetSummary(TimeRangePreset preset) {
            DateTime start, end;
            string rangeLabel;
            GetDateRange(preset, out start, out end, out rangeLabel);

            PeriodUsageSummary summary = new PeriodUsageSummary {
                Preset = preset,
                StartDate = start,
                EndDate = end,
                DateRangeLabel = rangeLabel
            };

            Dictionary<string, InterfaceUsageRecord> aggregatedInterfaces = new Dictionary<string, InterfaceUsageRecord>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, WifiNetworkUsageRecord> aggregatedWifiNetworks = new Dictionary<string, WifiNetworkUsageRecord>(StringComparer.OrdinalIgnoreCase);

            lock (_lock) {
                DateTime cur = start;
                while (cur <= end) {
                    string dateKey = cur.ToString("yyyy-MM-dd");
                    DayUsageRecord day;
                    if (_recordsByDate.TryGetValue(dateKey, out day)) {
                        summary.DailyRecords.Add(day);
                        summary.TotalDownloaded += day.TotalDownloaded;
                        summary.TotalUploaded += day.TotalUploaded;

                        if (day.TotalBytes > summary.PeakDayBytes) {
                            summary.PeakDayBytes = day.TotalBytes;
                            summary.PeakDayDate = day.Date;
                        }

                        if (day.Interfaces != null) {
                            for (int i = 0; i < day.Interfaces.Count; i++) {
                                InterfaceUsageRecord iface = day.Interfaces[i];
                                if (iface == null) continue;

                                string ifaceKey = iface.InterfaceId ?? iface.InterfaceName ?? "unknown";
                                InterfaceUsageRecord agg;
                                if (!aggregatedInterfaces.TryGetValue(ifaceKey, out agg)) {
                                    agg = new InterfaceUsageRecord {
                                        InterfaceId = iface.InterfaceId,
                                        InterfaceName = iface.InterfaceName,
                                        InterfaceType = iface.InterfaceType
                                    };
                                    aggregatedInterfaces[ifaceKey] = agg;
                                }
                                agg.BytesDownloaded += iface.BytesDownloaded;
                                agg.BytesUploaded += iface.BytesUploaded;
                                if (iface.PeakDownloadSpeed > agg.PeakDownloadSpeed) agg.PeakDownloadSpeed = iface.PeakDownloadSpeed;
                                if (iface.PeakUploadSpeed > agg.PeakUploadSpeed) agg.PeakUploadSpeed = iface.PeakUploadSpeed;

                                if (iface.InterfaceType == "Wi-Fi") {
                                    summary.WifiDownloaded += iface.BytesDownloaded;
                                    summary.WifiUploaded += iface.BytesUploaded;
                                } else if (iface.InterfaceType == "Ethernet") {
                                    summary.EthernetDownloaded += iface.BytesDownloaded;
                                    summary.EthernetUploaded += iface.BytesUploaded;
                                }
                            }
                        }

                        if (day.WifiNetworks != null) {
                            for (int w = 0; w < day.WifiNetworks.Count; w++) {
                                WifiNetworkUsageRecord net = day.WifiNetworks[w];
                                if (net == null || string.IsNullOrEmpty(net.Ssid)) continue;

                                WifiNetworkUsageRecord aggNet;
                                if (!aggregatedWifiNetworks.TryGetValue(net.Ssid, out aggNet)) {
                                    aggNet = new WifiNetworkUsageRecord {
                                        Ssid = net.Ssid
                                    };
                                    aggregatedWifiNetworks[net.Ssid] = aggNet;
                                }
                                aggNet.BytesDownloaded += net.BytesDownloaded;
                                aggNet.BytesUploaded += net.BytesUploaded;
                                if (net.PeakDownloadSpeed > aggNet.PeakDownloadSpeed) aggNet.PeakDownloadSpeed = net.PeakDownloadSpeed;
                                if (net.PeakUploadSpeed > aggNet.PeakUploadSpeed) aggNet.PeakUploadSpeed = net.PeakUploadSpeed;
                            }
                        }
                    } else {
                        // Empty record for continuous timeline representation
                        summary.DailyRecords.Add(new DayUsageRecord { Date = dateKey });
                    }
                    cur = cur.AddDays(1);
                }
            }

            int daysCount = (int)(end - start).TotalDays + 1;
            if (daysCount < 1) daysCount = 1;
            summary.DailyAverageBytes = (double)summary.TotalBytes / daysCount;

            summary.InterfaceRecords = new List<InterfaceUsageRecord>(aggregatedInterfaces.Values);
            summary.InterfaceRecords.Sort(delegate(InterfaceUsageRecord a, InterfaceUsageRecord b) {
                return b.TotalBytes.CompareTo(a.TotalBytes);
            });

            summary.WifiNetworkBreakdown = new List<WifiNetworkUsageRecord>(aggregatedWifiNetworks.Values);
            summary.WifiNetworkBreakdown.Sort(delegate(WifiNetworkUsageRecord a, WifiNetworkUsageRecord b) {
                return b.TotalBytes.CompareTo(a.TotalBytes);
            });

            return summary;
        }

        public void GetDateRange(TimeRangePreset preset, out DateTime start, out DateTime end, out string label) {
            DateTime today = DateTime.Today;

            switch (preset) {
                case TimeRangePreset.Today:
                    start = today;
                    end = today;
                    label = today.ToString("MMM dd, yyyy");
                    break;

                case TimeRangePreset.Yesterday:
                    start = today.AddDays(-1);
                    end = today.AddDays(-1);
                    label = start.ToString("MMM dd, yyyy");
                    break;

                case TimeRangePreset.Last7Days:
                    start = today.AddDays(-6);
                    end = today;
                    label = string.Format("{0} — {1}", start.ToString("MMM dd"), end.ToString("MMM dd, yyyy"));
                    break;

                case TimeRangePreset.LastWeek:
                    // Previous full calendar week (Monday to Sunday)
                    int diff = ((int)today.DayOfWeek + 6) % 7; // Monday = 0
                    DateTime thisMonday = today.AddDays(-diff);
                    start = thisMonday.AddDays(-7);
                    end = thisMonday.AddDays(-1);
                    label = string.Format("{0} — {1}", start.ToString("MMM dd"), end.ToString("MMM dd, yyyy"));
                    break;

                case TimeRangePreset.ThisMonth:
                    start = new DateTime(today.Year, today.Month, 1);
                    end = today;
                    label = string.Format("{0} — {1}", start.ToString("MMM 01"), end.ToString("MMM dd, yyyy"));
                    break;

                case TimeRangePreset.LastMonth:
                    DateTime firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
                    end = firstOfThisMonth.AddDays(-1);
                    start = new DateTime(end.Year, end.Month, 1);
                    label = string.Format("{0} — {1}", start.ToString("MMM 01"), end.ToString("MMM dd, yyyy"));
                    break;

                case TimeRangePreset.AllTime:
                default:
                    start = today;
                    lock (_lock) {
                        foreach (string k in _recordsByDate.Keys) {
                            DateTime d;
                            if (DateTime.TryParse(k, out d)) {
                                if (d < start) start = d;
                            }
                        }
                    }
                    end = today;
                    if (start == today) {
                        label = today.ToString("MMM dd, yyyy");
                    } else {
                        label = string.Format("{0} — {1}", start.ToString("MMM dd, yyyy"), end.ToString("MMM dd, yyyy"));
                    }
                    break;
            }
        }

        public void GenerateSampleData() {
            lock (_lock) {
                Random rand = new Random();
                DateTime today = DateTime.Today;

                // Generate records for the past 45 days
                for (int i = 45; i >= 0; i--) {
                    DateTime dt = today.AddDays(-i);
                    string dateKey = dt.ToString("yyyy-MM-dd");

                    DayUsageRecord day;
                    if (!_recordsByDate.TryGetValue(dateKey, out day)) {
                        day = new DayUsageRecord { Date = dateKey };
                        _recordsByDate[dateKey] = day;
                    }

                    // Realistic day volume between 1.2 GB and 8.5 GB
                    long wifiDl = (long)(rand.Next(800, 5500) * 1024L * 1024L);
                    long wifiUl = (long)(rand.Next(150, 1100) * 1024L * 1024L);

                    long ethDl = (long)(rand.Next(300, 2500) * 1024L * 1024L);
                    long ethUl = (long)(rand.Next(80, 700) * 1024L * 1024L);

                    double wifiPeakDl = rand.Next(15, 60) * 1024 * 1024;
                    double wifiPeakUl = rand.Next(5, 20) * 1024 * 1024;
                    double ethPeakDl = rand.Next(25, 95) * 1024 * 1024;
                    double ethPeakUl = rand.Next(10, 35) * 1024 * 1024;

                    // Distribute Wi-Fi traffic across realistic SSIDs
                    bool isWeekend = dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
                    if (isWeekend) {
                        // Weekends: Mostly Home network, some mobile hotspot
                        long homeDl = (long)(wifiDl * (rand.Next(78, 88) / 100.0));
                        long homeUl = (long)(wifiUl * (rand.Next(78, 88) / 100.0));
                        day.AddTraffic("wifi_adapter_sample", "Wi-Fi (Wireless Network)", "Wi-Fi", "Home-Fiber-5G", homeDl, homeUl, wifiPeakDl, wifiPeakUl);

                        long hotspotDl = wifiDl - homeDl;
                        long hotspotUl = wifiUl - homeUl;
                        if (hotspotDl > 0 || hotspotUl > 0) {
                            day.AddTraffic("wifi_adapter_sample", "Wi-Fi (Wireless Network)", "Wi-Fi", "Pixel-Hotspot-5G", hotspotDl, hotspotUl, wifiPeakDl * 0.45, wifiPeakUl * 0.45);
                        }
                    } else {
                        // Weekdays: Split between Office, Home, and occasional Cafe
                        long officeDl = (long)(wifiDl * (rand.Next(50, 62) / 100.0));
                        long officeUl = (long)(wifiUl * (rand.Next(50, 62) / 100.0));
                        day.AddTraffic("wifi_adapter_sample", "Wi-Fi (Wireless Network)", "Wi-Fi", "Office-Corporate-WPA3", officeDl, officeUl, wifiPeakDl, wifiPeakUl);

                        long remainingDl = wifiDl - officeDl;
                        long remainingUl = wifiUl - officeUl;
                        long homeDl = (long)(remainingDl * 0.85);
                        long homeUl = (long)(remainingUl * 0.85);
                        day.AddTraffic("wifi_adapter_sample", "Wi-Fi (Wireless Network)", "Wi-Fi", "Home-Fiber-5G", homeDl, homeUl, wifiPeakDl * 0.8, wifiPeakUl * 0.8);

                        long cafeDl = remainingDl - homeDl;
                        long cafeUl = remainingUl - homeUl;
                        if (cafeDl > 0 || cafeUl > 0) {
                            day.AddTraffic("wifi_adapter_sample", "Wi-Fi (Wireless Network)", "Wi-Fi", "Cafe-Free-WiFi", cafeDl, cafeUl, wifiPeakDl * 0.3, wifiPeakUl * 0.3);
                        }
                    }

                    day.AddTraffic("eth_adapter_sample", "Ethernet (Gigabit LAN)", "Ethernet", null, ethDl, ethUl, ethPeakDl, ethPeakUl);
                }

                _isDirty = true;
            }
            SaveToDisk(true);
        }

        public void ClearHistory() {
            lock (_lock) {
                _recordsByDate.Clear();
                _isDirty = true;
            }
            SaveToDisk(true);
        }

        public bool ExportToCsv(string filePath, TimeRangePreset preset) {
            try {
                PeriodUsageSummary summary = GetSummary(preset);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Date,Downloaded (Bytes),Uploaded (Bytes),Total (Bytes),Downloaded Formatted,Uploaded Formatted,Total Formatted");

                for (int i = 0; i < summary.DailyRecords.Count; i++) {
                    DayUsageRecord day = summary.DailyRecords[i];
                    sb.AppendFormat("{0},{1},{2},{3},\"{4}\",\"{5}\",\"{6}\"\r\n",
                        day.Date,
                        day.TotalDownloaded,
                        day.TotalUploaded,
                        day.TotalBytes,
                        Helpers.FormatBytes(day.TotalDownloaded),
                        Helpers.FormatBytes(day.TotalUploaded),
                        Helpers.FormatBytes(day.TotalBytes));
                }

                sb.AppendLine();
                sb.AppendFormat("SUMMARY,{0},{1},{2},\"{3}\",\"{4}\",\"{5}\"\r\n",
                    summary.TotalDownloaded,
                    summary.TotalUploaded,
                    summary.TotalBytes,
                    Helpers.FormatBytes(summary.TotalDownloaded),
                    Helpers.FormatBytes(summary.TotalUploaded),
                    Helpers.FormatBytes(summary.TotalBytes));

                if (summary.WifiNetworkBreakdown != null && summary.WifiNetworkBreakdown.Count > 0) {
                    sb.AppendLine();
                    sb.AppendLine("WI-FI NETWORKS BREAKDOWN");
                    sb.AppendLine("SSID,Downloaded (Bytes),Uploaded (Bytes),Total (Bytes),Downloaded Formatted,Uploaded Formatted,Total Formatted,Share of Wi-Fi");
                    for (int i = 0; i < summary.WifiNetworkBreakdown.Count; i++) {
                        WifiNetworkUsageRecord net = summary.WifiNetworkBreakdown[i];
                        double share = summary.WifiTotal > 0 ? ((double)net.TotalBytes / summary.WifiTotal * 100.0) : 0;
                        sb.AppendFormat("\"{0}\",{1},{2},{3},\"{4}\",\"{5}\",\"{6}\",\"{7:0.#}%\"\r\n",
                            (net.Ssid ?? "Unknown").Replace("\"", "\"\""),
                            net.BytesDownloaded,
                            net.BytesUploaded,
                            net.TotalBytes,
                            Helpers.FormatBytes(net.BytesDownloaded),
                            Helpers.FormatBytes(net.BytesUploaded),
                            Helpers.FormatBytes(net.TotalBytes),
                            share);
                    }
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            } catch {
                return false;
            }
        }
    }
}
