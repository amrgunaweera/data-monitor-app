"""Network monitoring engine using psutil."""

import time
from dataclasses import dataclass, field
from collections import deque

import psutil


@dataclass
class InterfaceStats:
    """Statistics for a single network interface."""
    name: str
    interface_type: str  # "WiFi" or "Ethernet"
    is_up: bool = False
    ipv4_address: str = ""
    mac_address: str = ""

    # Current speed (bytes/sec)
    download_speed: float = 0.0
    upload_speed: float = 0.0

    # Session totals (bytes)
    session_downloaded: float = 0.0
    session_uploaded: float = 0.0

    # Speed history for charts (last 60 data points)
    download_history: deque = field(default_factory=lambda: deque(maxlen=60))
    upload_history: deque = field(default_factory=lambda: deque(maxlen=60))

    # Internal tracking
    _last_bytes_sent: float = 0.0
    _last_bytes_recv: float = 0.0
    _last_time: float = 0.0
    _initialized: bool = False

    @property
    def total_session_data(self) -> float:
        return self.session_downloaded + self.session_uploaded


class NetworkMonitor:
    """Monitors network interfaces and tracks data usage."""

    # Common name patterns for identifying interface types
    WIFI_KEYWORDS = ["wi-fi", "wifi", "wlan", "wireless", "802.11"]
    ETHERNET_KEYWORDS = ["ethernet", "eth", "local area connection", "realtek", "intel"]

    def __init__(self):
        self.interfaces: dict[str, InterfaceStats] = {}
        self._start_time = time.time()
        self._discover_interfaces()

    def _classify_interface(self, name: str) -> str | None:
        """Classify an interface as WiFi, Ethernet, or None (skip)."""
        name_lower = name.lower()

        # Skip virtual/loopback interfaces
        skip_keywords = [
            "loopback", "vmware", "virtualbox", "vethernet", "docker",
            "veth", "br-", "virbr", "hyper-v", "pseudo", "bluetooth",
            "isatap", "teredo", "6to4",
        ]
        for skip in skip_keywords:
            if skip in name_lower:
                return None

        for keyword in self.WIFI_KEYWORDS:
            if keyword in name_lower:
                return "WiFi"

        for keyword in self.ETHERNET_KEYWORDS:
            if keyword in name_lower:
                return "Ethernet"

        return None

    def _discover_interfaces(self):
        """Discover and classify all network interfaces."""
        net_if_addrs = psutil.net_if_addrs()
        net_if_stats = psutil.net_if_stats()
        net_io = psutil.net_io_counters(pernic=True)

        for name in net_if_addrs:
            iface_type = self._classify_interface(name)
            if iface_type is None:
                continue

            # Get addresses
            ipv4 = ""
            mac = ""
            for addr in net_if_addrs[name]:
                if addr.family.name == "AF_INET":
                    ipv4 = addr.address
                elif addr.family.name == "AF_LINK" or str(addr.family) == "AddressFamily.AF_PACKET":
                    mac = addr.address
                # On Windows, MAC address family is AF_LINK (-1)
                elif addr.family.value == -1:
                    mac = addr.address

            is_up = net_if_stats.get(name, None)
            is_up = is_up.isup if is_up else False

            stats = InterfaceStats(
                name=name,
                interface_type=iface_type,
                is_up=is_up,
                ipv4_address=ipv4,
                mac_address=mac,
            )

            # Initialize byte counters
            if name in net_io:
                stats._last_bytes_recv = net_io[name].bytes_recv
                stats._last_bytes_sent = net_io[name].bytes_sent
                stats._last_time = time.time()
                stats._initialized = True

            # Pre-fill history with zeros
            for _ in range(60):
                stats.download_history.append(0.0)
                stats.upload_history.append(0.0)

            self.interfaces[name] = stats

    def update(self):
        """Update all interface statistics. Call this periodically."""
        net_io = psutil.net_io_counters(pernic=True)
        net_if_stats = psutil.net_if_stats()
        current_time = time.time()

        for name, stats in self.interfaces.items():
            # Update link status
            if name in net_if_stats:
                stats.is_up = net_if_stats[name].isup

            if name not in net_io:
                stats.download_speed = 0.0
                stats.upload_speed = 0.0
                stats.download_history.append(0.0)
                stats.upload_history.append(0.0)
                continue

            counters = net_io[name]

            if stats._initialized:
                elapsed = current_time - stats._last_time
                if elapsed > 0:
                    bytes_recv_diff = max(0, counters.bytes_recv - stats._last_bytes_recv)
                    bytes_sent_diff = max(0, counters.bytes_sent - stats._last_bytes_sent)

                    stats.download_speed = bytes_recv_diff / elapsed
                    stats.upload_speed = bytes_sent_diff / elapsed

                    stats.session_downloaded += bytes_recv_diff
                    stats.session_uploaded += bytes_sent_diff

                    stats.download_history.append(stats.download_speed)
                    stats.upload_history.append(stats.upload_speed)
            else:
                stats._initialized = True
                stats.download_history.append(0.0)
                stats.upload_history.append(0.0)

            stats._last_bytes_recv = counters.bytes_recv
            stats._last_bytes_sent = counters.bytes_sent
            stats._last_time = current_time

    def get_wifi_interfaces(self) -> list[InterfaceStats]:
        """Get all WiFi interfaces."""
        return [s for s in self.interfaces.values() if s.interface_type == "WiFi"]

    def get_ethernet_interfaces(self) -> list[InterfaceStats]:
        """Get all Ethernet interfaces."""
        return [s for s in self.interfaces.values() if s.interface_type == "Ethernet"]

    def get_total_download_speed(self) -> float:
        return sum(s.download_speed for s in self.interfaces.values())

    def get_total_upload_speed(self) -> float:
        return sum(s.upload_speed for s in self.interfaces.values())

    def get_total_downloaded(self) -> float:
        return sum(s.session_downloaded for s in self.interfaces.values())

    def get_total_uploaded(self) -> float:
        return sum(s.session_uploaded for s in self.interfaces.values())

    def get_session_uptime(self) -> int:
        return int(time.time() - self._start_time)

    def refresh_interfaces(self):
        """Re-discover interfaces (e.g., if a new adapter is plugged in)."""
        current_names = set(self.interfaces.keys())
        self._discover_interfaces()
        # Preserve session data for existing interfaces
        for name in current_names:
            if name in self.interfaces:
                pass  # Already preserved by _discover_interfaces overwrite logic

