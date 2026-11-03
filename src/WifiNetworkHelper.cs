using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NetPulse {
    public static class WifiNetworkHelper {
        private static string _cachedSsid;
        private static DateTime _lastLookupTime = DateTime.MinValue;
        private static readonly object _lookupLock = new object();

        // ── Native WiFi API (wlanapi.dll) ──
        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern int WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern int WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern int WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern int WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid, int OpCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, IntPtr pWlanOpcodeValueType);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        private const int wlan_intf_opcode_current_connection = 7;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_INTERFACE_INFO {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public int isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_INTERFACE_INFO_LIST {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_INTERFACE_INFO InterfaceInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DOT11_SSID {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ucSSID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_CONNECTION_ATTRIBUTES {
            public int isState;
            public int wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public DOT11_SSID dot11Ssid;
        }

        public static string GetCurrentWifiSsid() {
            lock (_lookupLock) {
                if ((DateTime.Now - _lastLookupTime).TotalSeconds < 3 && _cachedSsid != null) {
                    return _cachedSsid;
                }

                string ssid = QueryWlanApiSsid();
                if (string.IsNullOrEmpty(ssid)) {
                    ssid = QueryNetshSsid();
                }

                if (string.IsNullOrEmpty(ssid)) {
                    ssid = "Wi-Fi Network";
                }

                _cachedSsid = ssid;
                _lastLookupTime = DateTime.Now;
                return _cachedSsid;
            }
        }

        private static string QueryWlanApiSsid() {
            IntPtr clientHandle = IntPtr.Zero;
            uint negotiatedVersion;
            try {
                int res = WlanOpenHandle(2, IntPtr.Zero, out negotiatedVersion, out clientHandle);
                if (res != 0) return null;

                IntPtr pIfList = IntPtr.Zero;
                res = WlanEnumInterfaces(clientHandle, IntPtr.Zero, out pIfList);
                if (res != 0 || pIfList == IntPtr.Zero) return null;

                try {
                    WLAN_INTERFACE_INFO_LIST ifList = (WLAN_INTERFACE_INFO_LIST)Marshal.PtrToStructure(pIfList, typeof(WLAN_INTERFACE_INFO_LIST));
                    long pItemList = pIfList.ToInt64() + Marshal.OffsetOf(typeof(WLAN_INTERFACE_INFO_LIST), "InterfaceInfo").ToInt64();

                    for (int i = 0; i < ifList.dwNumberOfItems; i++) {
                        IntPtr pInfo = new IntPtr(pItemList + (i * Marshal.SizeOf(typeof(WLAN_INTERFACE_INFO))));
                        WLAN_INTERFACE_INFO info = (WLAN_INTERFACE_INFO)Marshal.PtrToStructure(pInfo, typeof(WLAN_INTERFACE_INFO));

                        IntPtr pConnData = IntPtr.Zero;
                        uint dataSize;
                        res = WlanQueryInterface(clientHandle, ref info.InterfaceGuid, wlan_intf_opcode_current_connection, IntPtr.Zero, out dataSize, out pConnData, IntPtr.Zero);
                        if (res == 0 && pConnData != IntPtr.Zero) {
                            try {
                                WLAN_CONNECTION_ATTRIBUTES conn = (WLAN_CONNECTION_ATTRIBUTES)Marshal.PtrToStructure(pConnData, typeof(WLAN_CONNECTION_ATTRIBUTES));
                                if (conn.dot11Ssid.uSSIDLength > 0 && conn.dot11Ssid.ucSSID != null) {
                                    int len = (int)Math.Min(conn.dot11Ssid.uSSIDLength, 32);
                                    string ssid = Encoding.UTF8.GetString(conn.dot11Ssid.ucSSID, 0, len);
                                    if (!string.IsNullOrEmpty(ssid) && !ssid.StartsWith("\0")) {
                                        return ssid.Trim();
                                    }
                                }
                                if (!string.IsNullOrEmpty(conn.strProfileName)) {
                                    return conn.strProfileName.Trim();
                                }
                            } finally {
                                WlanFreeMemory(pConnData);
                            }
                        }
                    }
                } finally {
                    WlanFreeMemory(pIfList);
                }
            } catch {
            } finally {
                if (clientHandle != IntPtr.Zero) {
                    WlanCloseHandle(clientHandle, IntPtr.Zero);
                }
            }
            return null;
        }

        private static string QueryNetshSsid() {
            try {
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "netsh.exe",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process proc = Process.Start(psi)) {
                    if (proc != null) {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(1000);

                        using (StringReader reader = new StringReader(output)) {
                            string line;
                            while ((line = reader.ReadLine()) != null) {
                                string trimmed = line.Trim();
                                if (trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) &&
                                    !trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase)) {
                                    int colonIdx = trimmed.IndexOf(':');
                                    if (colonIdx >= 0 && colonIdx < trimmed.Length - 1) {
                                        string ssid = trimmed.Substring(colonIdx + 1).Trim();
                                        if (!string.IsNullOrEmpty(ssid)) {
                                            return ssid;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch {}
            return null;
        }
    }
}

