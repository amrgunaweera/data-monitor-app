#pragma warning disable 618
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NetPulse.Controls {
    public class HistoricalBarChart : FrameworkElement {
        private PeriodUsageSummary _summary;
        private int _hoveredIndex = -1;
        private Point _mousePos;

        private Brush _bgBrush;
        private Pen _gridPen;
        private Pen _hoverPen;
        private Brush _hoverBgBrush;
        private Brush _textBrush;
        private Brush _textMutedBrush;
        private Brush _dlBrush;
        private Brush _ulBrush;
        private Brush _tooltipBgBrush;
        private Pen _tooltipBorderPen;
        private readonly Typeface _typeface;
        private readonly Typeface _typefaceBold;

        public HistoricalBarChart() {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            _typeface = new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _typefaceBold = new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            ClipToBounds = true;

            UpdateThemeBrushes();
            ThemeManager.ThemeChanged += OnThemeChanged;

            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;
        }

        private void OnThemeChanged() {
            UpdateThemeBrushes();
            InvalidateVisual();
        }

        private void UpdateThemeBrushes() {
            bool dark = ThemeManager.IsDark;

            _bgBrush = Helpers.BrushFromHex(ThemeManager.ChartBg);
            _gridPen = new Pen(Helpers.BrushFromHex(ThemeManager.ChartGrid), 1.0);
            _gridPen.Freeze();

            _textBrush = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _textMutedBrush = Helpers.BrushFromHex(ThemeManager.ChartText);

            _dlBrush = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _ulBrush = Helpers.BrushFromHex(ThemeManager.UploadColor);

            _hoverBgBrush = Helpers.BrushFromHex(dark ? "#1FFFFFFF" : "#0D000000");
            _hoverPen = new Pen(Helpers.BrushFromHex(ThemeManager.TotalColor), 1.5);
            _hoverPen.Freeze();

            _tooltipBgBrush = Helpers.BrushFromHex(dark ? "#1E2538" : "#FFFFFF");
            _tooltipBorderPen = new Pen(Helpers.BrushFromHex(dark ? "#3A466A" : "#CBD5E1"), 1.0);
            _tooltipBorderPen.Freeze();
        }

        public void SetData(PeriodUsageSummary summary) {
            _summary = summary;
            _hoveredIndex = -1;
            InvalidateVisual();
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            if (_summary == null || _summary.DailyRecords == null || _summary.DailyRecords.Count == 0) return;

            Point pos = e.GetPosition(this);
            _mousePos = pos;

            double width = ActualWidth;
            double height = ActualHeight;
            double leftMargin = 65;
            double rightMargin = 20;
            double plotWidth = width - leftMargin - rightMargin;

            int count = _summary.DailyRecords.Count;
            if (pos.X >= leftMargin && pos.X <= leftMargin + plotWidth && count > 0) {
                double slotWidth = plotWidth / count;
                int idx = (int)((pos.X - leftMargin) / slotWidth);
                if (idx >= 0 && idx < count) {
                    if (_hoveredIndex != idx) {
                        _hoveredIndex = idx;
                        InvalidateVisual();
                    }
                    return;
                }
            }

            if (_hoveredIndex != -1) {
                _hoveredIndex = -1;
                InvalidateVisual();
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e) {
            if (_hoveredIndex != -1) {
                _hoveredIndex = -1;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;
            if (width < 30 || height < 30) return;

            double leftMargin = 65;
            double rightMargin = 20;
            double topMargin = 20;
            double bottomMargin = 35;

            double plotWidth = width - leftMargin - rightMargin;
            double plotHeight = height - topMargin - bottomMargin;
            if (plotWidth <= 0 || plotHeight <= 0) return;

            // Chart area background
            Rect bgRect = new Rect(leftMargin, topMargin, plotWidth, plotHeight);
            dc.DrawRoundedRectangle(_bgBrush, null, bgRect, 8, 8);

            if (_summary == null || _summary.DailyRecords == null || _summary.DailyRecords.Count == 0) {
                DrawCenteredText(dc, "No historical data available", leftMargin + plotWidth / 2.0, topMargin + plotHeight / 2.0);
                return;
            }

            List<DayUsageRecord> records = _summary.DailyRecords;
            int count = records.Count;

            // Calculate peak day bytes for scaling
            long maxBytes = 100 * 1024 * 1024; // Default scale: 100 MB minimum
            for (int i = 0; i < count; i++) {
                if (records[i].TotalBytes > maxBytes) {
                    maxBytes = records[i].TotalBytes;
                }
            }
            maxBytes = (long)(maxBytes * 1.15); // 15% headroom

            // Draw Y-axis gridlines & labels
            int gridSteps = 4;
            for (int s = 0; s <= gridSteps; s++) {
                double fraction = (double)s / gridSteps;
                double y = topMargin + plotHeight * (1.0 - fraction);

                dc.DrawLine(_gridPen, new Point(leftMargin, y), new Point(leftMargin + plotWidth, y));

                double labelValue = maxBytes * fraction;
                string labelText = Helpers.FormatBytes(labelValue);

                FormattedText ft = new FormattedText(
                    labelText,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    10,
                    _textMutedBrush
                );
                dc.DrawText(ft, new Point(leftMargin - ft.Width - 8, y - ft.Height / 2.0));
            }

            // Calculate bar geometry
            double slotWidth = plotWidth / count;
            double barWidth = Math.Max(4, Math.Min(36, slotWidth * 0.65));

            DayUsageRecord hoveredRecord = null;
            Rect hoveredBarRect = Rect.Empty;

            for (int i = 0; i < count; i++) {
                DayUsageRecord day = records[i];
                double slotCenter = leftMargin + (i * slotWidth) + (slotWidth / 2.0);
                double barLeft = slotCenter - (barWidth / 2.0);

                double dlRatio = (double)day.TotalDownloaded / maxBytes;
                double ulRatio = (double)day.TotalUploaded / maxBytes;

                if (dlRatio < 0) dlRatio = 0;
                if (ulRatio < 0) ulRatio = 0;
                if (dlRatio + ulRatio > 1.0) {
                    double scale = 1.0 / (dlRatio + ulRatio);
                    dlRatio *= scale;
                    ulRatio *= scale;
                }

                double dlH = plotHeight * dlRatio;
                double ulH = plotHeight * ulRatio;
                double totalH = dlH + ulH;

                double baseY = topMargin + plotHeight;

                // Slot hover background
                if (i == _hoveredIndex) {
                    Rect hoverSlotRect = new Rect(leftMargin + (i * slotWidth), topMargin, slotWidth, plotHeight);
                    dc.DrawRoundedRectangle(_hoverBgBrush, null, hoverSlotRect, 4, 4);
                    hoveredRecord = day;
                }

                if (totalH > 0) {
                    // Download bar (bottom)
                    if (dlH > 0) {
                        Rect dlRect = new Rect(barLeft, baseY - dlH, barWidth, dlH);
                        dc.DrawRectangle(_dlBrush, null, dlRect);
                    }

                    // Upload bar (stacked on top)
                    if (ulH > 0) {
                        Rect ulRect = new Rect(barLeft, baseY - totalH, barWidth, ulH);
                        dc.DrawRectangle(_ulBrush, null, ulRect);
                    }

                    if (i == _hoveredIndex) {
                        hoveredBarRect = new Rect(barLeft - 1, baseY - totalH - 1, barWidth + 2, totalH + 2);
                        dc.DrawRectangle(null, _hoverPen, hoveredBarRect);
                    }
                } else {
                    // Subtle baseline pill for empty day
                    Rect emptyRect = new Rect(barLeft, baseY - 3, barWidth, 3);
                    dc.DrawRectangle(_gridPen.Brush, null, emptyRect);
                }

                // X-axis date labels
                bool showLabel = ShouldShowDateLabel(count, i);
                if (showLabel) {
                    string dateText = FormatDateForXAxis(day.Date, count);
                    FormattedText ft = new FormattedText(
                        dateText,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        (i == _hoveredIndex) ? _typefaceBold : _typeface,
                        9.5,
                        (i == _hoveredIndex) ? _textBrush : _textMutedBrush
                    );
                    dc.DrawText(ft, new Point(slotCenter - ft.Width / 2.0, height - bottomMargin + 8));
                }
            }

            // Draw interactive tooltip when hovering
            if (hoveredRecord != null) {
                DrawTooltip(dc, hoveredRecord, leftMargin, topMargin, plotWidth, plotHeight);
            }
        }

        private bool ShouldShowDateLabel(int totalCount, int index) {
            if (totalCount <= 7) return true; // Show every day
            if (totalCount <= 14) return (index % 2 == 0) || (index == totalCount - 1);
            if (totalCount <= 31) {
                // Show 1st, 5th, 10th, 15th, 20th, 25th, and last
                return (index == 0) || ((index + 1) % 5 == 0) || (index == totalCount - 1);
            }
            return (index % 7 == 0) || (index == totalCount - 1);
        }

        private string FormatDateForXAxis(string dateStr, int totalCount) {
            DateTime dt;
            if (DateTime.TryParse(dateStr, out dt)) {
                if (totalCount <= 7) {
                    return dt.ToString("ddd dd"); // e.g. "Mon 26"
                }
                return dt.ToString("MM/dd");
            }
            return dateStr;
        }

        private void DrawCenteredText(DrawingContext dc, string text, double cx, double cy) {
            FormattedText ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _typeface,
                13,
                _textMutedBrush
            );
            dc.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - ft.Height / 2.0));
        }

        private void DrawTooltip(DrawingContext dc, DayUsageRecord day, double left, double top, double w, double h) {
            DateTime dt;
            string dateHeader = DateTime.TryParse(day.Date, out dt) ? dt.ToString("dddd, MMM dd, yyyy") : day.Date;

            string totalStr = "Total: " + Helpers.FormatBytes(day.TotalBytes);
            string dlStr = "↓ Recv: " + Helpers.FormatBytes(day.TotalDownloaded);
            string ulStr = "↑ Sent: " + Helpers.FormatBytes(day.TotalUploaded);

            FormattedText ftHeader = new FormattedText(dateHeader, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typefaceBold, 11, _textBrush);
            FormattedText ftTotal = new FormattedText(totalStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typefaceBold, 12, Helpers.BrushFromHex(ThemeManager.TotalColor));
            FormattedText ftDl = new FormattedText(dlStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, 11, _dlBrush);
            FormattedText ftUl = new FormattedText(ulStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, 11, _ulBrush);

            double tipW = Math.Max(ftHeader.Width, Math.Max(ftTotal.Width, Math.Max(ftDl.Width, ftUl.Width))) + 24;
            double tipH = 78;

            double tipX = _mousePos.X + 14;
            double tipY = _mousePos.Y - 30;

            if (tipX + tipW > left + w) {
                tipX = _mousePos.X - tipW - 14;
            }
            if (tipY < top) tipY = top + 4;
            if (tipY + tipH > top + h) tipY = top + h - tipH - 4;

            Rect tipRect = new Rect(tipX, tipY, tipW, tipH);
            dc.DrawRoundedRectangle(_tooltipBgBrush, _tooltipBorderPen, tipRect, 8, 8);

            dc.DrawText(ftHeader, new Point(tipX + 12, tipY + 8));
            dc.DrawText(ftTotal, new Point(tipX + 12, tipY + 25));
            dc.DrawText(ftDl, new Point(tipX + 12, tipY + 44));
            dc.DrawText(ftUl, new Point(tipX + 12, tipY + 60));
        }
    }
}

