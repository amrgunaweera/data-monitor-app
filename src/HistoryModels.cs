using System;
using System.Collections.Generic;

namespace NetPulse {
    public enum TimeRangePreset {
        Today,
        Yesterday,
        Last7Days,
        LastWeek,
        ThisMonth,
        LastMonth,
        AllTime
    }

    public class InterfaceUsageRecord {
        public string InterfaceId { get; set; }
        public string InterfaceName { get; set; }
        public string InterfaceType { get; set; } // "Wi-Fi" or "Ethernet"
        public long BytesDownloaded { get; set; }
        public long BytesUploaded { get; set; }
        public double PeakDownloadSpeed { get; set; }
        public double PeakUploadSpeed { get; set; }

        public InterfaceUsageRecord() {
            InterfaceId = "";
            InterfaceName = "";
            InterfaceType = "Unknown";
        }

        public long TotalBytes {
            get { return BytesDownloaded + BytesUploaded; }
        }
    }

    public class WifiNetworkUsageRecord {
        public string Ssid { get; set; } // Network SSID name
        public long BytesDownloaded { get; set; }
        public long BytesUploaded { get; set; }
        public double PeakDownloadSpeed { get; set; }
        public double PeakUploadSpeed { get; set; }

        public WifiNetworkUsageRecord() {
            Ssid = "Wi-Fi Network";
        }

        public long TotalBytes {
            get { return BytesDownloaded + BytesUploaded; }
        }
    }

    public class VisitedSiteRecord {
        public string Url { get; set; }
        public string Title { get; set; }
        public string Domain { get; set; }
        public string Browser { get; set; } // "Microsoft Edge", "Google Chrome", "Mozilla Firefox", etc.
        public int VisitCount { get; set; }
        public DateTime LastVisitTime { get; set; }
        public string LastVisitTimeFormatted { get; set; }
        public string Date { get; set; } // "yyyy-MM-dd"

        public VisitedSiteRecord() {
            Url = "";
            Title = "";
            Domain = "";
            Browser = "Web Browser";
            VisitCount = 1;
            LastVisitTime = DateTime.Now;
            LastVisitTimeFormatted = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Date = DateTime.Today.ToString("yyyy-MM-dd");
        }
    }

    public class DomainUsageRecord {
        public string Domain { get; set; }
        public int TotalVisits { get; set; }
        public int UniqueUrlsCount { get; set; }
        public DateTime LastVisited { get; set; }
        public double Percentage { get; set; }
        public string PrimaryBrowser { get; set; }

        public DomainUsageRecord() {
            Domain = "";
            PrimaryBrowser = "";
        }
    }

    public class DayUsageRecord {
        public string Date { get; set; } // Format: "yyyy-MM-dd"
        public long TotalDownloaded { get; set; }
        public long TotalUploaded { get; set; }
        public double PeakDownloadSpeed { get; set; }
        public double PeakUploadSpeed { get; set; }
        public List<InterfaceUsageRecord> Interfaces { get; set; }
        public List<WifiNetworkUsageRecord> WifiNetworks { get; set; }
        public List<VisitedSiteRecord> VisitedSites { get; set; }

        public DayUsageRecord() {
            Date = DateTime.Today.ToString("yyyy-MM-dd");
            Interfaces = new List<InterfaceUsageRecord>();
            WifiNetworks = new List<WifiNetworkUsageRecord>();
            VisitedSites = new List<VisitedSiteRecord>();
        }

        public long TotalBytes {
            get { return TotalDownloaded + TotalUploaded; }
        }

        public InterfaceUsageRecord GetOrCreateInterface(string id, string name, string type) {
            if (Interfaces == null) {
                Interfaces = new List<InterfaceUsageRecord>();
            }
            for (int i = 0; i < Interfaces.Count; i++) {
                if (Interfaces[i].InterfaceId == id) {
                    if (!string.IsNullOrEmpty(name)) Interfaces[i].InterfaceName = name;
                    if (!string.IsNullOrEmpty(type)) Interfaces[i].InterfaceType = type;
                    return Interfaces[i];
                }
            }
            InterfaceUsageRecord rec = new InterfaceUsageRecord {
                InterfaceId = id,
                InterfaceName = name ?? id,
                InterfaceType = type ?? "Other"
            };
            Interfaces.Add(rec);
            return rec;
        }

        public WifiNetworkUsageRecord GetOrCreateWifiNetwork(string ssid) {
            if (string.IsNullOrEmpty(ssid)) ssid = "Wi-Fi Network";
            if (WifiNetworks == null) {
                WifiNetworks = new List<WifiNetworkUsageRecord>();
            }
            for (int i = 0; i < WifiNetworks.Count; i++) {
                if (string.Equals(WifiNetworks[i].Ssid, ssid, StringComparison.OrdinalIgnoreCase)) {
                    return WifiNetworks[i];
                }
            }
            WifiNetworkUsageRecord rec = new WifiNetworkUsageRecord {
                Ssid = ssid
            };
            WifiNetworks.Add(rec);
            return rec;
        }

        public void AddTraffic(string ifaceId, string ifaceName, string ifaceType, string wifiSsid, long dl, long ul, double dlSpeed, double ulSpeed) {
            TotalDownloaded += dl;
            TotalUploaded += ul;
            if (dlSpeed > PeakDownloadSpeed) PeakDownloadSpeed = dlSpeed;
            if (ulSpeed > PeakUploadSpeed) PeakUploadSpeed = ulSpeed;

            InterfaceUsageRecord iface = GetOrCreateInterface(ifaceId, ifaceName, ifaceType);
            iface.BytesDownloaded += dl;
            iface.BytesUploaded += ul;
            if (dlSpeed > iface.PeakDownloadSpeed) iface.PeakDownloadSpeed = dlSpeed;
            if (ulSpeed > iface.PeakUploadSpeed) iface.PeakUploadSpeed = ulSpeed;

            if (ifaceType == "Wi-Fi" && !string.IsNullOrEmpty(wifiSsid)) {
                WifiNetworkUsageRecord wifiNet = GetOrCreateWifiNetwork(wifiSsid);
                wifiNet.BytesDownloaded += dl;
                wifiNet.BytesUploaded += ul;
                if (dlSpeed > wifiNet.PeakDownloadSpeed) wifiNet.PeakDownloadSpeed = dlSpeed;
                if (ulSpeed > wifiNet.PeakUploadSpeed) wifiNet.PeakUploadSpeed = ulSpeed;
            }
        }

        public void AddVisitedSite(VisitedSiteRecord site) {
            if (site == null || string.IsNullOrEmpty(site.Url)) return;
            if (VisitedSites == null) {
                VisitedSites = new List<VisitedSiteRecord>();
            }
            for (int i = 0; i < VisitedSites.Count; i++) {
                if (string.Equals(VisitedSites[i].Url, site.Url, StringComparison.OrdinalIgnoreCase)) {
                    VisitedSites[i].VisitCount += site.VisitCount;
                    if (site.LastVisitTime > VisitedSites[i].LastVisitTime) {
                        VisitedSites[i].LastVisitTime = site.LastVisitTime;
                        VisitedSites[i].LastVisitTimeFormatted = site.LastVisitTimeFormatted;
                    }
                    return;
                }
            }
            VisitedSites.Add(site);
        }
    }

    public class UsageDatabase {
        public int Version { get; set; }
        public string CreatedDate { get; set; }
        public string LastUpdatedDate { get; set; }
        public List<DayUsageRecord> Days { get; set; }

        public UsageDatabase() {
            Version = 1;
            CreatedDate = DateTime.Now.ToString("o");
            LastUpdatedDate = DateTime.Now.ToString("o");
            Days = new List<DayUsageRecord>();
        }
    }

    public class PeriodUsageSummary {
        public TimeRangePreset Preset { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DateRangeLabel { get; set; }

        public long TotalDownloaded { get; set; }
        public long TotalUploaded { get; set; }
        public long TotalBytes {
            get { return TotalDownloaded + TotalUploaded; }
        }

        public double DailyAverageBytes { get; set; }
        public long PeakDayBytes { get; set; }
        public string PeakDayDate { get; set; }

        public long WifiDownloaded { get; set; }
        public long WifiUploaded { get; set; }
        public long WifiTotal {
            get { return WifiDownloaded + WifiUploaded; }
        }

        public long EthernetDownloaded { get; set; }
        public long EthernetUploaded { get; set; }
        public long EthernetTotal {
            get { return EthernetDownloaded + EthernetUploaded; }
        }

        public List<DayUsageRecord> DailyRecords { get; set; }
        public List<InterfaceUsageRecord> InterfaceRecords { get; set; }
        public List<WifiNetworkUsageRecord> WifiNetworkBreakdown { get; set; }
        public List<VisitedSiteRecord> VisitedSites { get; set; }
        public List<DomainUsageRecord> TopDomains { get; set; }
        public int TotalSiteVisits { get; set; }
        public int UniqueDomainsCount { get; set; }

        public PeriodUsageSummary() {
            DailyRecords = new List<DayUsageRecord>();
            InterfaceRecords = new List<InterfaceUsageRecord>();
            WifiNetworkBreakdown = new List<WifiNetworkUsageRecord>();
            VisitedSites = new List<VisitedSiteRecord>();
            TopDomains = new List<DomainUsageRecord>();
        }
    }
}
