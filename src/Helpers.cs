using System;
using System.Globalization;
using System.Windows.Media;

namespace NetPulse {
    public static class Helpers {
        public static SolidColorBrush BrushFromHex(string hex) {
            Color color = (Color)ColorConverter.ConvertFromString(hex);
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static Color ColorFromHex(string hex) {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        public static string FormatBytes(double bytes) {
            if (bytes < 0) bytes = 0;
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB" };
            int unitIndex = 0;
            double value = bytes;

            while (value >= 1024 && unitIndex < units.Length - 1) {
                value /= 1024.0;
                unitIndex++;
            }

            if (unitIndex == 0) {
                return string.Format("{0:0} B", value);
            } else if (value >= 100) {
                return string.Format("{0:0} {1}", value, units[unitIndex]);
            } else if (value >= 10) {
                return string.Format("{0:0.0} {1}", value, units[unitIndex]);
            } else {
                return string.Format("{0:0.00} {1}", value, units[unitIndex]);
            }
        }

        public static string FormatSpeed(double bytesPerSec) {
            return FormatBytes(bytesPerSec) + "/s";
        }

        public static void FormatSpeedParts(double bytesPerSec, out string valueStr, out string unitStr) {
            if (bytesPerSec < 0) bytesPerSec = 0;
            string[] units = new string[] { "B/s", "KB/s", "MB/s", "GB/s" };
            int unitIndex = 0;
            double value = bytesPerSec;

            while (value >= 1024 && unitIndex < units.Length - 1) {
                value /= 1024.0;
                unitIndex++;
            }

            if (unitIndex == 0) {
                valueStr = string.Format("{0:0}", value);
            } else if (value >= 100) {
                valueStr = string.Format("{0:0}", value);
            } else if (value >= 10) {
                valueStr = string.Format("{0:0.0}", value);
            } else {
                valueStr = string.Format("{0:0.00}", value);
            }

            unitStr = units[unitIndex];
        }

        public static string FormatDuration(TimeSpan ts) {
            if (ts.TotalHours >= 1) {
                return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            }
            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }

        public static string FormatLinkSpeed(long speedBitsPerSec) {
            if (speedBitsPerSec <= 0) return "Unknown";
            double mbps = speedBitsPerSec / 1000000.0;
            if (mbps >= 1000) {
                return string.Format("{0:0.#} Gbps", mbps / 1000.0);
            }
            return string.Format("{0:0} Mbps", mbps);
        }
    }
}

