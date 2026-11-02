"""Real-time chart widgets using matplotlib embedded in CustomTkinter."""

import matplotlib
matplotlib.use("TkAgg")

import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure
import numpy as np
import customtkinter as ctk


# Color palette
COLORS = {
    "download": "#00D4AA",       # Teal/cyan
    "upload": "#FF6B6B",         # Coral red
    "download_fill": "#00D4AA",
    "upload_fill": "#FF6B6B",
    "grid": "#2A2D3E",
    "bg": "#1A1B2E",
    "text": "#8892B0",
    "card_bg": "#1E2033",
}


class SpeedChart(ctk.CTkFrame):
    """Real-time area chart showing download/upload speeds."""

    def __init__(self, master, title: str = "Network Speed", height: int = 200, **kwargs):
        super().__init__(master, fg_color=COLORS["card_bg"], corner_radius=16, **kwargs)

        self.title = title

        # Title label
        title_frame = ctk.CTkFrame(self, fg_color="transparent")
        title_frame.pack(fill="x", padx=20, pady=(16, 0))

        ctk.CTkLabel(
            title_frame, text=title,
            font=ctk.CTkFont(size=14, weight="bold"),
            text_color="#FFFFFF"
        ).pack(side="left")

        # Legend
        legend_frame = ctk.CTkFrame(title_frame, fg_color="transparent")
        legend_frame.pack(side="right")

        # Download legend dot
        dl_canvas_frame = ctk.CTkFrame(legend_frame, fg_color="transparent")
        dl_canvas_frame.pack(side="left", padx=(0, 12))
        ctk.CTkLabel(dl_canvas_frame, text="●", text_color=COLORS["download"],
                      font=ctk.CTkFont(size=10)).pack(side="left", padx=(0, 4))
        ctk.CTkLabel(dl_canvas_frame, text="Download", text_color=COLORS["text"],
                      font=ctk.CTkFont(size=11)).pack(side="left")

        # Upload legend dot
        ul_canvas_frame = ctk.CTkFrame(legend_frame, fg_color="transparent")
        ul_canvas_frame.pack(side="left")
        ctk.CTkLabel(ul_canvas_frame, text="●", text_color=COLORS["upload"],
                      font=ctk.CTkFont(size=10)).pack(side="left", padx=(0, 4))
        ctk.CTkLabel(ul_canvas_frame, text="Upload", text_color=COLORS["text"],
                      font=ctk.CTkFont(size=11)).pack(side="left")

        # Create matplotlib figure
        self.fig = Figure(figsize=(6, height / 100), dpi=100)
        self.fig.patch.set_facecolor(COLORS["card_bg"])

        self.ax = self.fig.add_subplot(111)
        self._style_axes()

        self.canvas = FigureCanvasTkAgg(self.fig, master=self)
        self.canvas.get_tk_widget().configure(bg=COLORS["card_bg"], highlightthickness=0)
        self.canvas.get_tk_widget().pack(fill="both", expand=True, padx=12, pady=(4, 12))

        # Initialize empty data
        self.x_data = np.arange(60)
        self.dl_data = np.zeros(60)
        self.ul_data = np.zeros(60)

    def _style_axes(self):
        """Apply dark theme styling to axes."""
        self.ax.set_facecolor(COLORS["bg"])
        self.ax.tick_params(colors=COLORS["text"], labelsize=8)
        self.ax.spines["top"].set_visible(False)
        self.ax.spines["right"].set_visible(False)
        self.ax.spines["left"].set_color(COLORS["grid"])
        self.ax.spines["bottom"].set_color(COLORS["grid"])
        self.ax.grid(True, axis="y", color=COLORS["grid"], linewidth=0.5, alpha=0.5)
        self.ax.set_xlim(0, 59)
        self.ax.set_xticks([])

    def update_data(self, download_history: list[float], upload_history: list[float]):
        """Update the chart with new data."""
        self.dl_data = np.array(list(download_history))
        self.ul_data = np.array(list(upload_history))

        self.ax.clear()
        self._style_axes()

        # Convert to KB/s for display
        dl_kb = self.dl_data / 1024
        ul_kb = self.ul_data / 1024

        # Determine best unit
        max_val = max(dl_kb.max(), ul_kb.max(), 0.1)
        if max_val >= 1024:
            dl_display = dl_kb / 1024
            ul_display = ul_kb / 1024
            unit = "MB/s"
        else:
            dl_display = dl_kb
            ul_display = ul_kb
            unit = "KB/s"

        # Plot area charts
        self.ax.fill_between(
            self.x_data, dl_display, alpha=0.15, color=COLORS["download_fill"]
        )
        self.ax.plot(
            self.x_data, dl_display, color=COLORS["download"],
            linewidth=1.5, alpha=0.9
        )

        self.ax.fill_between(
            self.x_data, ul_display, alpha=0.15, color=COLORS["upload_fill"]
        )
        self.ax.plot(
            self.x_data, ul_display, color=COLORS["upload"],
            linewidth=1.5, alpha=0.9
        )

        self.ax.set_ylabel(unit, color=COLORS["text"], fontsize=9)
        y_max = max(dl_display.max(), ul_display.max(), 0.1) * 1.2
        self.ax.set_ylim(0, y_max)

        self.fig.tight_layout(pad=0.5)

        try:
            self.canvas.draw_idle()
        except Exception:
            pass


class MiniSpeedChart(ctk.CTkFrame):
    """Compact speed chart for individual interface cards."""

    def __init__(self, master, height: int = 120, **kwargs):
        super().__init__(master, fg_color="transparent", **kwargs)

        self.fig = Figure(figsize=(3.5, height / 100), dpi=100)
        self.fig.patch.set_alpha(0.0)

        self.ax = self.fig.add_subplot(111)
        self._style_axes()

        self.canvas = FigureCanvasTkAgg(self.fig, master=self)
        self.canvas.get_tk_widget().configure(bg=COLORS["card_bg"], highlightthickness=0)
        self.canvas.get_tk_widget().pack(fill="both", expand=True)

        self.x_data = np.arange(60)

    def _style_axes(self):
        self.ax.set_facecolor("none")
        self.ax.tick_params(left=False, bottom=False, labelleft=False, labelbottom=False)
        for spine in self.ax.spines.values():
            spine.set_visible(False)
        self.ax.set_xlim(0, 59)

    def update_data(self, download_history: list[float], upload_history: list[float]):
        self.ax.clear()
        self._style_axes()

        dl = np.array(list(download_history)) / 1024
        ul = np.array(list(upload_history)) / 1024

        self.ax.fill_between(self.x_data, dl, alpha=0.2, color=COLORS["download_fill"])
        self.ax.plot(self.x_data, dl, color=COLORS["download"], linewidth=1.2, alpha=0.8)
        self.ax.fill_between(self.x_data, ul, alpha=0.2, color=COLORS["upload_fill"])
        self.ax.plot(self.x_data, ul, color=COLORS["upload"], linewidth=1.2, alpha=0.8)

        y_max = max(dl.max(), ul.max(), 0.1) * 1.3
        self.ax.set_ylim(0, y_max)

        self.fig.tight_layout(pad=0)

        try:
            self.canvas.draw_idle()
        except Exception:
            pass

