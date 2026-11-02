"""Main UI layout and components."""

import customtkinter as ctk
from app.monitor import NetworkMonitor, InterfaceStats
from app.charts import SpeedChart, MiniSpeedChart, COLORS
from app.utils import format_bytes, format_speed, format_speed_value_and_unit, format_uptime


class StatusDot(ctk.CTkFrame):
    """Animated status indicator dot."""

    def __init__(self, master, **kwargs):
        super().__init__(master, width=10, height=10, corner_radius=5, **kwargs)
        self.set_status(False)

    def set_status(self, is_up: bool):
        if is_up:
            self.configure(fg_color="#00E676")  # Green
        else:
            self.configure(fg_color="#FF5252")  # Red


class SpeedGauge(ctk.CTkFrame):
    """Large speed display widget with value and unit."""

    def __init__(self, master, label: str, color: str, **kwargs):
        super().__init__(master, fg_color=COLORS["card_bg"], corner_radius=14, **kwargs)

        self.grid_columnconfigure(0, weight=1)

        # Icon + label row
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=16, pady=(14, 0))

        icon_text = "↓" if "down" in label.lower() else "↑"
        ctk.CTkLabel(
            header, text=icon_text, font=ctk.CTkFont(size=18, weight="bold"),
            text_color=color
        ).pack(side="left")

        ctk.CTkLabel(
            header, text=label, font=ctk.CTkFont(size=12),
            text_color=COLORS["text"]
        ).pack(side="left", padx=(8, 0))

        # Speed value
        self.speed_label = ctk.CTkLabel(
            self, text="0", font=ctk.CTkFont(size=36, weight="bold"),
            text_color="#FFFFFF"
        )
        self.speed_label.pack(padx=16, pady=(4, 0))

        # Speed unit
        self.unit_label = ctk.CTkLabel(
            self, text="B/s", font=ctk.CTkFont(size=13),
            text_color=COLORS["text"]
        )
        self.unit_label.pack(padx=16, pady=(0, 14))

    def update_speed(self, bytes_per_sec: float):
        value, unit = format_speed_value_and_unit(bytes_per_sec)
        self.speed_label.configure(text=value)
        self.unit_label.configure(text=unit)


class TotalDataCard(ctk.CTkFrame):
    """Card showing total data usage."""

    def __init__(self, master, **kwargs):
        super().__init__(master, fg_color=COLORS["card_bg"], corner_radius=14, **kwargs)

        # Title
        ctk.CTkLabel(
            self, text="Session Data Usage",
            font=ctk.CTkFont(size=14, weight="bold"),
            text_color="#FFFFFF"
        ).pack(padx=16, pady=(14, 8), anchor="w")

        # Data grid
        grid = ctk.CTkFrame(self, fg_color="transparent")
        grid.pack(fill="x", padx=16, pady=(0, 14))
        grid.grid_columnconfigure((0, 1), weight=1)

        # Downloaded
        dl_frame = ctk.CTkFrame(grid, fg_color="#1A2332", corner_radius=10)
        dl_frame.grid(row=0, column=0, sticky="ew", padx=(0, 4), pady=2)

        ctk.CTkLabel(
            dl_frame, text="↓ Downloaded", font=ctk.CTkFont(size=11),
            text_color=COLORS["download"]
        ).pack(padx=12, pady=(8, 2), anchor="w")

        self.dl_total = ctk.CTkLabel(
            dl_frame, text="0 B", font=ctk.CTkFont(size=18, weight="bold"),
            text_color="#FFFFFF"
        )
        self.dl_total.pack(padx=12, pady=(0, 8), anchor="w")

        # Uploaded
        ul_frame = ctk.CTkFrame(grid, fg_color="#2A1A1A", corner_radius=10)
        ul_frame.grid(row=0, column=1, sticky="ew", padx=(4, 0), pady=2)

        ctk.CTkLabel(
            ul_frame, text="↑ Uploaded", font=ctk.CTkFont(size=11),
            text_color=COLORS["upload"]
        ).pack(padx=12, pady=(8, 2), anchor="w")

        self.ul_total = ctk.CTkLabel(
            ul_frame, text="0 B", font=ctk.CTkFont(size=18, weight="bold"),
            text_color="#FFFFFF"
        )
        self.ul_total.pack(padx=12, pady=(0, 8), anchor="w")

        # Total + uptime row
        bottom = ctk.CTkFrame(self, fg_color="transparent")
        bottom.pack(fill="x", padx=16, pady=(0, 14))
        bottom.grid_columnconfigure((0, 1), weight=1)

        total_frame = ctk.CTkFrame(bottom, fg_color="transparent")
        total_frame.grid(row=0, column=0, sticky="w")
        ctk.CTkLabel(total_frame, text="Total", font=ctk.CTkFont(size=11),
                      text_color=COLORS["text"]).pack(anchor="w")
        self.total_label = ctk.CTkLabel(
            total_frame, text="0 B", font=ctk.CTkFont(size=14, weight="bold"),
            text_color="#FFFFFF"
        )
        self.total_label.pack(anchor="w")

        uptime_frame = ctk.CTkFrame(bottom, fg_color="transparent")
        uptime_frame.grid(row=0, column=1, sticky="e")
        ctk.CTkLabel(uptime_frame, text="Session Time", font=ctk.CTkFont(size=11),
                      text_color=COLORS["text"]).pack(anchor="e")
        self.uptime_label = ctk.CTkLabel(
            uptime_frame, text="0s", font=ctk.CTkFont(size=14, weight="bold"),
            text_color="#FFFFFF"
        )
        self.uptime_label.pack(anchor="e")

    def update_data(self, downloaded: float, uploaded: float, uptime: int):
        self.dl_total.configure(text=format_bytes(downloaded))
        self.ul_total.configure(text=format_bytes(uploaded))
        self.total_label.configure(text=format_bytes(downloaded + uploaded))
        self.uptime_label.configure(text=format_uptime(uptime))


class InterfaceCard(ctk.CTkFrame):
    """Card for an individual network interface."""

    def __init__(self, master, stats: InterfaceStats, **kwargs):
        super().__init__(master, fg_color=COLORS["card_bg"], corner_radius=14, **kwargs)

        self.stats = stats

        # Header
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=16, pady=(14, 0))

        # Icon based on type
        icon = "📶" if stats.interface_type == "WiFi" else "🔌"
        ctk.CTkLabel(header, text=icon, font=ctk.CTkFont(size=18)).pack(side="left")

        title_frame = ctk.CTkFrame(header, fg_color="transparent")
        title_frame.pack(side="left", padx=(8, 0), fill="x", expand=True)

        ctk.CTkLabel(
            title_frame, text=stats.interface_type,
            font=ctk.CTkFont(size=14, weight="bold"), text_color="#FFFFFF"
        ).pack(anchor="w")

        self.name_label = ctk.CTkLabel(
            title_frame, text=stats.name,
            font=ctk.CTkFont(size=10), text_color=COLORS["text"]
        )
        self.name_label.pack(anchor="w")

        # Status dot
        self.status_dot = StatusDot(header)
        self.status_dot.pack(side="right")

        self.status_label = ctk.CTkLabel(
            header, text="Connected", font=ctk.CTkFont(size=11),
            text_color="#00E676"
        )
        self.status_label.pack(side="right", padx=(0, 8))

        # Speed displays
        speeds = ctk.CTkFrame(self, fg_color="transparent")
        speeds.pack(fill="x", padx=16, pady=(10, 0))
        speeds.grid_columnconfigure((0, 1), weight=1)

        # Download speed
        dl_frame = ctk.CTkFrame(speeds, fg_color="transparent")
        dl_frame.grid(row=0, column=0, sticky="w")
        ctk.CTkLabel(dl_frame, text="↓", font=ctk.CTkFont(size=12, weight="bold"),
                      text_color=COLORS["download"]).pack(side="left")
        self.dl_speed = ctk.CTkLabel(
            dl_frame, text="0 B/s", font=ctk.CTkFont(size=13, weight="bold"),
            text_color="#FFFFFF"
        )
        self.dl_speed.pack(side="left", padx=(4, 0))

        # Upload speed
        ul_frame = ctk.CTkFrame(speeds, fg_color="transparent")
        ul_frame.grid(row=0, column=1, sticky="e")
        ctk.CTkLabel(ul_frame, text="↑", font=ctk.CTkFont(size=12, weight="bold"),
                      text_color=COLORS["upload"]).pack(side="left")
        self.ul_speed = ctk.CTkLabel(
            ul_frame, text="0 B/s", font=ctk.CTkFont(size=13, weight="bold"),
            text_color="#FFFFFF"
        )
        self.ul_speed.pack(side="left", padx=(4, 0))

        # Mini chart
        self.mini_chart = MiniSpeedChart(self, height=90)
        self.mini_chart.pack(fill="x", padx=12, pady=(4, 4))

        # Data totals
        totals = ctk.CTkFrame(self, fg_color="transparent")
        totals.pack(fill="x", padx=16, pady=(0, 4))
        totals.grid_columnconfigure((0, 1), weight=1)

        dl_total_frame = ctk.CTkFrame(totals, fg_color="transparent")
        dl_total_frame.grid(row=0, column=0, sticky="w")
        ctk.CTkLabel(dl_total_frame, text="Total ↓", font=ctk.CTkFont(size=10),
                      text_color=COLORS["text"]).pack(anchor="w")
        self.dl_total = ctk.CTkLabel(
            dl_total_frame, text="0 B", font=ctk.CTkFont(size=12, weight="bold"),
            text_color="#FFFFFF"
        )
        self.dl_total.pack(anchor="w")

        ul_total_frame = ctk.CTkFrame(totals, fg_color="transparent")
        ul_total_frame.grid(row=0, column=1, sticky="e")
        ctk.CTkLabel(ul_total_frame, text="Total ↑", font=ctk.CTkFont(size=10),
                      text_color=COLORS["text"]).pack(anchor="e")
        self.ul_total = ctk.CTkLabel(
            ul_total_frame, text="0 B", font=ctk.CTkFont(size=12, weight="bold"),
            text_color="#FFFFFF"
        )
        self.ul_total.pack(anchor="e")

        # IP Address
        info = ctk.CTkFrame(self, fg_color="transparent")
        info.pack(fill="x", padx=16, pady=(2, 14))

        ip_text = stats.ipv4_address if stats.ipv4_address else "No IP"
        self.ip_label = ctk.CTkLabel(
            info, text=f"IP: {ip_text}", font=ctk.CTkFont(size=10),
            text_color=COLORS["text"]
        )
        self.ip_label.pack(anchor="w")

    def update_stats(self, stats: InterfaceStats):
        self.stats = stats

        # Status
        self.status_dot.set_status(stats.is_up)
        status_text = "Connected" if stats.is_up else "Disconnected"
        status_color = "#00E676" if stats.is_up else "#FF5252"
        self.status_label.configure(text=status_text, text_color=status_color)

        # Speeds
        self.dl_speed.configure(text=format_speed(stats.download_speed))
        self.ul_speed.configure(text=format_speed(stats.upload_speed))

        # Totals
        self.dl_total.configure(text=format_bytes(stats.session_downloaded))
        self.ul_total.configure(text=format_bytes(stats.session_uploaded))

        # IP
        ip_text = stats.ipv4_address if stats.ipv4_address else "No IP"
        self.ip_label.configure(text=f"IP: {ip_text}")

        # Mini chart
        self.mini_chart.update_data(
            list(stats.download_history),
            list(stats.upload_history)
        )


class DataMonitorApp(ctk.CTk):
    """Main application window."""

    UPDATE_INTERVAL_MS = 1000  # 1 second

    def __init__(self):
        super().__init__()

        # Window setup
        self.title("NetFlow Monitor")
        self.geometry("1100x780")
        self.minsize(900, 650)

        # Dark theme
        ctk.set_appearance_mode("dark")
        ctk.set_default_color_theme("dark-blue")
        self.configure(fg_color="#0F1020")

        # Initialize monitor
        self.monitor = NetworkMonitor()

        # Build UI
        self._build_ui()

        # Start update loop
        self._update_tick()

    def _build_ui(self):
        """Build the complete UI layout."""
        # Main scrollable container
        main_container = ctk.CTkFrame(self, fg_color="transparent")
        main_container.pack(fill="both", expand=True, padx=0, pady=0)

        # ── Header ──
        header = ctk.CTkFrame(main_container, fg_color="#0F1020", height=60)
        header.pack(fill="x", padx=24, pady=(18, 0))
        header.pack_propagate(False)

        title_frame = ctk.CTkFrame(header, fg_color="transparent")
        title_frame.pack(side="left", fill="y")

        ctk.CTkLabel(
            title_frame, text="⚡ NetFlow Monitor",
            font=ctk.CTkFont(size=22, weight="bold"), text_color="#FFFFFF"
        ).pack(anchor="w")

        ctk.CTkLabel(
            title_frame, text="Real-time network data usage monitor",
            font=ctk.CTkFont(size=12), text_color=COLORS["text"]
        ).pack(anchor="w")

        # Active interfaces count badge
        self.active_badge = ctk.CTkLabel(
            header, text="0 active",
            font=ctk.CTkFont(size=12), text_color=COLORS["text"],
            fg_color="#1E2033", corner_radius=12,
            padx=12, pady=4
        )
        self.active_badge.pack(side="right", padx=(0, 8))

        ctk.CTkLabel(
            header, text="Interfaces:", font=ctk.CTkFont(size=12),
            text_color=COLORS["text"]
        ).pack(side="right")

        # ── Top Row: Speed Gauges + Data Card ──
        top_row = ctk.CTkFrame(main_container, fg_color="transparent")
        top_row.pack(fill="x", padx=24, pady=(16, 0))
        top_row.grid_columnconfigure((0, 1, 2), weight=1)

        self.dl_gauge = SpeedGauge(top_row, "Download", COLORS["download"])
        self.dl_gauge.grid(row=0, column=0, sticky="nsew", padx=(0, 6))

        self.ul_gauge = SpeedGauge(top_row, "Upload", COLORS["upload"])
        self.ul_gauge.grid(row=0, column=1, sticky="nsew", padx=6)

        self.data_card = TotalDataCard(top_row)
        self.data_card.grid(row=0, column=2, sticky="nsew", padx=(6, 0))

        # ── Middle: Combined Speed Chart ──
        self.speed_chart = SpeedChart(main_container, title="Combined Network Speed", height=180)
        self.speed_chart.pack(fill="x", padx=24, pady=(12, 0))

        # ── Bottom: Interface Cards ──
        iface_header = ctk.CTkFrame(main_container, fg_color="transparent")
        iface_header.pack(fill="x", padx=24, pady=(16, 0))

        ctk.CTkLabel(
            iface_header, text="Network Interfaces",
            font=ctk.CTkFont(size=16, weight="bold"), text_color="#FFFFFF"
        ).pack(side="left")

        # Interface cards container
        self.iface_container = ctk.CTkFrame(main_container, fg_color="transparent")
        self.iface_container.pack(fill="both", expand=True, padx=24, pady=(8, 18))

        self.interface_cards: dict[str, InterfaceCard] = {}
        self._build_interface_cards()

    def _build_interface_cards(self):
        """Create cards for each discovered interface."""
        # Clear existing cards
        for card in self.interface_cards.values():
            card.destroy()
        self.interface_cards.clear()

        interfaces = list(self.monitor.interfaces.values())

        if not interfaces:
            no_iface = ctk.CTkLabel(
                self.iface_container,
                text="No WiFi or Ethernet interfaces detected",
                font=ctk.CTkFont(size=14),
                text_color=COLORS["text"]
            )
            no_iface.pack(pady=20)
            return

        # Configure columns
        col_count = min(len(interfaces), 3)
        for i in range(col_count):
            self.iface_container.grid_columnconfigure(i, weight=1)

        for idx, stats in enumerate(interfaces):
            card = InterfaceCard(self.iface_container, stats)
            card.grid(
                row=idx // col_count,
                column=idx % col_count,
                sticky="nsew",
                padx=6,
                pady=6
            )
            self.interface_cards[stats.name] = card

    def _update_tick(self):
        """Periodic update of all data and UI."""
        self.monitor.update()

        # Update speed gauges
        self.dl_gauge.update_speed(self.monitor.get_total_download_speed())
        self.ul_gauge.update_speed(self.monitor.get_total_upload_speed())

        # Update data card
        self.data_card.update_data(
            self.monitor.get_total_downloaded(),
            self.monitor.get_total_uploaded(),
            self.monitor.get_session_uptime()
        )

        # Update active badge
        active = sum(1 for s in self.monitor.interfaces.values() if s.is_up)
        total = len(self.monitor.interfaces)
        self.active_badge.configure(text=f"{active}/{total} active")

        # Aggregate speed history for combined chart
        all_dl = [0.0] * 60
        all_ul = [0.0] * 60
        for stats in self.monitor.interfaces.values():
            dl_hist = list(stats.download_history)
            ul_hist = list(stats.upload_history)
            for i in range(min(60, len(dl_hist))):
                all_dl[i] += dl_hist[i]
                all_ul[i] += ul_hist[i]
        self.speed_chart.update_data(all_dl, all_ul)

        # Update individual interface cards
        for name, stats in self.monitor.interfaces.items():
            if name in self.interface_cards:
                self.interface_cards[name].update_stats(stats)

        # Schedule next update
        self.after(self.UPDATE_INTERVAL_MS, self._update_tick)

