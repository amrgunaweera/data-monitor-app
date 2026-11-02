"""Utility functions for formatting and conversions."""


def format_bytes(byte_count: float) -> str:
    """Format byte count into a human-readable string."""
    if byte_count < 0:
        byte_count = 0
    units = [("B", 1), ("KB", 1024), ("MB", 1024**2), ("GB", 1024**3), ("TB", 1024**4)]
    for unit_name, unit_size in reversed(units):
        if byte_count >= unit_size:
            value = byte_count / unit_size
            if value >= 100:
                return f"{value:.0f} {unit_name}"
            elif value >= 10:
                return f"{value:.1f} {unit_name}"
            else:
                return f"{value:.2f} {unit_name}"
    return "0 B"


def format_speed(bytes_per_sec: float) -> str:
    """Format speed in bytes/sec to human-readable string."""
    if bytes_per_sec < 0:
        bytes_per_sec = 0
    return f"{format_bytes(bytes_per_sec)}/s"


def format_speed_value_and_unit(bytes_per_sec: float) -> tuple[str, str]:
    """Return (value, unit) tuple for display in large speed widgets."""
    if bytes_per_sec < 0:
        bytes_per_sec = 0
    units = [
        ("B/s", 1),
        ("KB/s", 1024),
        ("MB/s", 1024**2),
        ("GB/s", 1024**3),
    ]
    for unit_name, unit_size in reversed(units):
        if bytes_per_sec >= unit_size:
            value = bytes_per_sec / unit_size
            if value >= 100:
                return f"{value:.0f}", unit_name
            elif value >= 10:
                return f"{value:.1f}", unit_name
            else:
                return f"{value:.2f}", unit_name
    return "0", "B/s"


def format_uptime(seconds: int) -> str:
    """Format seconds into a human-readable uptime string."""
    if seconds < 60:
        return f"{seconds}s"
    elif seconds < 3600:
        m, s = divmod(seconds, 60)
        return f"{m}m {s}s"
    elif seconds < 86400:
        h, remainder = divmod(seconds, 3600)
        m, _ = divmod(remainder, 60)
        return f"{h}h {m}m"
    else:
        d, remainder = divmod(seconds, 86400)
        h, _ = divmod(remainder, 3600)
        return f"{d}d {h}h"

