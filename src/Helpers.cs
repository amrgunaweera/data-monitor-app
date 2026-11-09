using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
            string val, unit;
            FormatSpeedParts(bytesPerSec, out val, out unit);
            return val + " " + unit;
        }

        public static void FormatSpeedParts(double bytesPerSec, out string valueStr, out string unitStr) {
            if (bytesPerSec < 0) bytesPerSec = 0;

            string mode = "Auto";
            try {
                if (AppSettings.Instance != null && !string.IsNullOrEmpty(AppSettings.Instance.SpeedUnitMode)) {
                    mode = AppSettings.Instance.SpeedUnitMode;
                }
            } catch {}

            if (mode == "Bits") {
                string[] units = new string[] { "bps", "Kbps", "Mbps", "Gbps" };
                int unitIndex = 0;
                double value = bytesPerSec * 8.0;

                while (value >= 1000 && unitIndex < units.Length - 1) {
                    value /= 1000.0;
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
                return;
            }

            if (mode == "BytesOnly") {
                double mb = bytesPerSec / (1024.0 * 1024.0);
                valueStr = string.Format("{0:0.00}", mb);
                unitStr = "MB/s";
                return;
            }

            // Standard Auto B/s, KB/s, MB/s, GB/s
            string[] byteUnits = new string[] { "B/s", "KB/s", "MB/s", "GB/s" };
            int bUnitIndex = 0;
            double bValue = bytesPerSec;

            while (bValue >= 1024 && bUnitIndex < byteUnits.Length - 1) {
                bValue /= 1024.0;
                bUnitIndex++;
            }

            if (bUnitIndex == 0) {
                valueStr = string.Format("{0:0}", bValue);
            } else if (bValue >= 100) {
                valueStr = string.Format("{0:0}", bValue);
            } else if (bValue >= 10) {
                valueStr = string.Format("{0:0.0}", bValue);
            } else {
                valueStr = string.Format("{0:0.00}", bValue);
            }

            unitStr = byteUnits[bUnitIndex];
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

        public static ControlTemplate CreateButtonTemplate(double cornerRadius) {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "btnBorder";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            cp.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            border.AppendChild(cp);

            template.VisualTree = border;

            Trigger mouseOver = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            mouseOver.Setters.Add(new Setter(Button.OpacityProperty, 0.86));
            template.Triggers.Add(mouseOver);

            Trigger pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(Button.OpacityProperty, 0.70));
            template.Triggers.Add(pressed);

            return template;
        }

        public static void StyleButton(Button btn, double cornerRadius) {
            if (btn == null) return;
            btn.Template = CreateButtonTemplate(cornerRadius);
            btn.Cursor = Cursors.Hand;
            btn.FocusVisualStyle = null;
        }
    }
}

