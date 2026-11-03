using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetPulse {
    public class NetworkMonitor {
        private readonly Dictionary<string, InterfaceInfo> _interfaces;
        private readonly GlobalStats _globalStats;
        private DateTime _sessionStartTime;

        public GlobalStats GlobalStats {
            get { return _globalStats; }
        }

        public TimeSpan SessionUptime {
            get { return DateTime.Now - _sessionStartTime; }
        }

        public IEnumerable<InterfaceInfo> Interfaces {
            get { return _interfaces.Values; }
        }

        public NetworkMonitor() {
            _interfaces = new Dictionary<string, InterfaceInfo>();
            _globalStats = new GlobalStats();
            _sessionStartTime = DateTime.Now;
            RefreshInterfaces();
        }

        private static bool IsIgnoredInterface(NetworkInterface nic) {
            string name = (nic.Name ?? "").ToLowerInvariant();
            string desc = (nic.Description ?? "").ToLowerInvariant();

            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                nic.NetworkInterfaceType == NetworkInterfaceType.Ppp) {
                return true;
            }

            string[] ignoreKeywords = new string[] {
                "loopback", "virtual", "pseudo", "bluetooth",
                "vethernet", "hyper-v", "vmware", "virtualbox",
                "docker", "veth", "teredo", "isatap", "6to4",
                "wan miniport", "npcap", "wi-fi direct"
            };

            for (int i = 0; i < ignoreKeywords.Length; i++) {
                if (name.Contains(ignoreKeywords[i]) || desc.Contains(ignoreKeywords[i])) {
                    return true;
                }
            }

            return false;
        }

        private static string ClassifyType(NetworkInterface nic) {
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) {
                return "Wi-Fi";
            }
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT) {
                return "Ethernet";
            }
            string desc = (nic.Description ?? "").ToLowerInvariant();
            if (desc.Contains("wi-fi") || desc.Contains("wireless") || desc.Contains("wlan") || desc.Contains("802.11")) {
                return "Wi-Fi";
            }
            if (desc.Contains("ethernet") || desc.Contains("gigabit") || desc.Contains("realtek") || desc.Contains("intel") || desc.Contains("lan")) {
                return "Ethernet";
            }
            return "Other";
        }

        private static string FormatMacAddress(PhysicalAddress physicalAddress) {
            if (physicalAddress == null) return "Unknown";
            byte[] bytes = physicalAddress.GetAddressBytes();
            if (bytes == null || bytes.Length == 0) return "Unknown";
            string[] parts = new string[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) {
                parts[i] = bytes[i].ToString("X2");
            }
            return string.Join(":", parts);
        }

        public void RefreshInterfaces() {
            NetworkInterface[] nics;
            try {
                nics = NetworkInterface.GetAllNetworkInterfaces();
            } catch {
                return;
            }

            for (int i = 0; i < nics.Length; i++) {
                NetworkInterface nic = nics[i];
                if (IsIgnoredInterface(nic)) continue;

                string type = ClassifyType(nic);
                if (type != "Wi-Fi" && type != "Ethernet") continue;

                InterfaceInfo info;
                if (!_interfaces.TryGetValue(nic.Id, out info)) {
                    info = new InterfaceInfo {
                        Id = nic.Id,
                        Name = nic.Name,
                        Description = nic.Description,
                        InterfaceType = type
                    };
                    _interfaces[nic.Id] = info;
                }

                // Update status and link info
                info.IsConnected = (nic.OperationalStatus == OperationalStatus.Up);
                info.LinkSpeed = Helpers.FormatLinkSpeed(nic.Speed);
                info.MacAddress = FormatMacAddress(nic.GetPhysicalAddress());

                // Fetch IPv4
                string ipv4 = "";
                try {
                    IPInterfaceProperties ipProps = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation u in ipProps.UnicastAddresses) {
                        if (u.Address.AddressFamily == AddressFamily.InterNetwork) {
                            ipv4 = u.Address.ToString();
                            break;
                        }
                    }
                } catch {}
                info.IPv4Address = string.IsNullOrEmpty(ipv4) ? "Not Assigned" : ipv4;

                if (info.InterfaceType == "Wi-Fi" && info.IsConnected) {
                    info.ConnectedNetworkName = WifiNetworkHelper.GetCurrentWifiSsid();
                } else if (info.InterfaceType == "Wi-Fi" && !info.IsConnected) {
                    info.ConnectedNetworkName = null;
                }
            }
        }

        public void Update() {
            DateTime now = DateTime.Now;
            NetworkInterface[] nics;
            try {
                nics = NetworkInterface.GetAllNetworkInterfaces();
            } catch {
                return;
            }

            double totalDlSpeed = 0;
            double totalUlSpeed = 0;

            for (int i = 0; i < nics.Length; i++) {
                NetworkInterface nic = nics[i];
                InterfaceInfo info;
                if (!_interfaces.TryGetValue(nic.Id, out info)) {
                    continue;
                }

                info.IsConnected = (nic.OperationalStatus == OperationalStatus.Up);
                info.LinkSpeed = Helpers.FormatLinkSpeed(nic.Speed);

                IPInterfaceStatistics stats = null;
                try {
                    stats = nic.GetIPStatistics();
                } catch {
                    continue;
                }

                long curRecv = stats.BytesReceived;
                long curSent = stats.BytesSent;

                if (info.HasBaseline) {
                    double elapsed = (now - info.LastUpdated).TotalSeconds;
                    if (elapsed > 0.1) {
                        long diffRecv = curRecv - info.LastBytesReceived;
                        long diffSent = curSent - info.LastBytesSent;

                        // Account for potential reset/overflow
                        if (diffRecv < 0) diffRecv = 0;
                        if (diffSent < 0) diffSent = 0;

                        double dlSpeed = diffRecv / elapsed;
                        double ulSpeed = diffSent / elapsed;

                        info.PushSpeed(dlSpeed, ulSpeed);
                        info.SessionDownloaded += diffRecv;
                        info.SessionUploaded += diffSent;

                        totalDlSpeed += dlSpeed;
                        totalUlSpeed += ulSpeed;

                        _globalStats.TotalSessionDownloaded += diffRecv;
                        _globalStats.TotalSessionUploaded += diffSent;

                        // Persist historical daily usage with Wi-Fi network SSID
                        string wifiSsid = null;
                        if (info.InterfaceType == "Wi-Fi" && info.IsConnected) {
                            wifiSsid = WifiNetworkHelper.GetCurrentWifiSsid();
                            info.ConnectedNetworkName = wifiSsid;
                        } else if (info.InterfaceType == "Wi-Fi" && !info.IsConnected) {
                            info.ConnectedNetworkName = null;
                        }

                        UsageStorage.Instance.RecordTraffic(
                            info.Id,
                            info.Name,
                            info.InterfaceType,
                            wifiSsid,
                            diffRecv,
                            diffSent,
                            dlSpeed,
                            ulSpeed
                        );
                    }
                } else {
                    info.HasBaseline = true;
                    info.PushSpeed(0, 0);
                }

                info.LastBytesReceived = curRecv;
                info.LastBytesSent = curSent;
                info.LastUpdated = now;
            }

            _globalStats.PushSpeed(totalDlSpeed, totalUlSpeed);
        }

        public void ResetSession() {
            _sessionStartTime = DateTime.Now;
            _globalStats.ResetSession();
            foreach (var iface in _interfaces.Values) {
                iface.ResetSession();
            }
        }
    }
}

