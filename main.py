"""
NetFlow Monitor — Real-time Network Data Usage Monitor for Windows.

Monitors WiFi and Ethernet interfaces, showing live speeds, session totals,
and real-time charts in a modern dark-themed desktop application.

Usage:
    python main.py
"""

import sys
import os

# Ensure the project root is on the path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from app.ui import DataMonitorApp


def main():
    app = DataMonitorApp()
    app.mainloop()


if __name__ == "__main__":
    main()

