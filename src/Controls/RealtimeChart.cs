#pragma warning disable 618
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace NetPulse.Controls {
    public class RealtimeChart : FrameworkElement {
        public static readonly DependencyProperty IsMiniProperty =
            DependencyProperty.Register("IsMini", typeof(bool), typeof(RealtimeChart),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsMini {
            get { return (bool)GetValue(IsMiniProperty); }
            set { SetValue(IsMiniProperty, value); }
        }

        private double[] _downloadHistory;
        private double[] _uploadHistory;

        private Pen _gridPen;
        private Pen _dlPen;
        private Pen _ulPen;
        private Brush _dlFillBrush;
        private Brush _ulFillBrush;
        private Brush _dlDotBrush;
        private Brush _ulDotBrush;
        private Brush _textBrush;
        private Brush _bgBrush;
        private readonly Typeface _typeface;

        public RealtimeChart() {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            _typeface = new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            ClipToBounds = true;

            UpdateThemeBrushes();
            ThemeManager.ThemeChanged += OnThemeChanged;
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

            _textBrush = Helpers.BrushFromHex(ThemeManager.ChartText);

            string dlHex = ThemeManager.DownloadColor;
            string ulHex = ThemeManager.UploadColor;

            _dlPen = new Pen(Helpers.BrushFromHex(dlHex), 2.0);
            _dlPen.Freeze();

            _ulPen = new Pen(Helpers.BrushFromHex(ulHex), 2.0);
            _ulPen.Freeze();

            Color dlColor = Helpers.ColorFromHex(dlHex);
            LinearGradientBrush dlGrad = new LinearGradientBrush();
            dlGrad.StartPoint = new Point(0, 0);
            dlGrad.EndPoint = new Point(0, 1);
            byte dlAlpha = (byte)(dark ? 90 : 60);
            dlGrad.GradientStops.Add(new GradientStop(Color.FromArgb(dlAlpha, dlColor.R, dlColor.G, dlColor.B), 0.0));
            dlGrad.GradientStops.Add(new GradientStop(Color.FromArgb(0, dlColor.R, dlColor.G, dlColor.B), 1.0));
            dlGrad.Freeze();
            _dlFillBrush = dlGrad;

            Color ulColor = Helpers.ColorFromHex(ulHex);
            LinearGradientBrush ulGrad = new LinearGradientBrush();
            ulGrad.StartPoint = new Point(0, 0);
            ulGrad.EndPoint = new Point(0, 1);
            byte ulAlpha = (byte)(dark ? 80 : 50);
            ulGrad.GradientStops.Add(new GradientStop(Color.FromArgb(ulAlpha, ulColor.R, ulColor.G, ulColor.B), 0.0));
            ulGrad.GradientStops.Add(new GradientStop(Color.FromArgb(0, ulColor.R, ulColor.G, ulColor.B), 1.0));
            ulGrad.Freeze();
            _ulFillBrush = ulGrad;

            _dlDotBrush = Helpers.BrushFromHex(dlHex);
            _ulDotBrush = Helpers.BrushFromHex(ulHex);
        }

        public void SetData(double[] dlHistory, double[] ulHistory) {
            _downloadHistory = dlHistory;
            _uploadHistory = ulHistory;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;
            if (width < 20 || height < 20) return;

            double leftMargin = IsMini ? 0 : 58;
            double rightMargin = IsMini ? 0 : 12;
            double topMargin = IsMini ? 4 : 14;
            double bottomMargin = IsMini ? 4 : 24;

            double plotWidth = width - leftMargin - rightMargin;
            double plotHeight = height - topMargin - bottomMargin;
            if (plotWidth <= 0 || plotHeight <= 0) return;

            // Background for non-mini
            if (!IsMini) {
                Rect bgRect = new Rect(leftMargin, topMargin, plotWidth, plotHeight);
                dc.DrawRoundedRectangle(_bgBrush, null, bgRect, 8, 8);
            }

            int count = (_downloadHistory != null) ? _downloadHistory.Length : 60;

            // Find peak value
            double maxSpeed = 1024 * 10; // Minimum scale: 10 KB/s
            if (_downloadHistory != null) {
                for (int i = 0; i < _downloadHistory.Length; i++) {
                    if (_downloadHistory[i] > maxSpeed) maxSpeed = _downloadHistory[i];
                }
            }
            if (_uploadHistory != null) {
                for (int i = 0; i < _uploadHistory.Length; i++) {
                    if (_uploadHistory[i] > maxSpeed) maxSpeed = _uploadHistory[i];
                }
            }
            maxSpeed *= 1.15; // 15% headroom

            // Draw grid & Y-axis labels
            if (!IsMini) {
                int gridSteps = 4;
                for (int s = 0; s <= gridSteps; s++) {
                    double fraction = (double)s / gridSteps;
                    double y = topMargin + plotHeight * (1.0 - fraction);

                    dc.DrawLine(_gridPen, new Point(leftMargin, y), new Point(leftMargin + plotWidth, y));

                    double labelValue = maxSpeed * fraction;
                    string labelText = Helpers.FormatSpeed(labelValue);

                    FormattedText ft = new FormattedText(
                        labelText,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        _typeface,
                        10,
                        _textBrush
                    );
                    dc.DrawText(ft, new Point(leftMargin - ft.Width - 8, y - ft.Height / 2.0));
                }

                // X-axis time marks
                string[] xLabels = new string[] { "-60s", "-45s", "-30s", "-15s", "Now" };
                for (int i = 0; i < xLabels.Length; i++) {
                    double fraction = (double)i / (xLabels.Length - 1);
                    double x = leftMargin + plotWidth * fraction;

                    FormattedText ft = new FormattedText(
                        xLabels[i],
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        _typeface,
                        9.5,
                        _textBrush
                    );
                    double tx = (i == xLabels.Length - 1) ? (x - ft.Width) : (i == 0 ? x : x - ft.Width / 2.0);
                    dc.DrawText(ft, new Point(tx, height - bottomMargin + 6));
                }
            }

            // Draw series
            if (_downloadHistory != null && _downloadHistory.Length > 1) {
                DrawSeries(dc, _downloadHistory, maxSpeed, leftMargin, topMargin, plotWidth, plotHeight, _dlFillBrush, _dlPen, _dlDotBrush);
            }
            if (_uploadHistory != null && _uploadHistory.Length > 1) {
                DrawSeries(dc, _uploadHistory, maxSpeed, leftMargin, topMargin, plotWidth, plotHeight, _ulFillBrush, _ulPen, _ulDotBrush);
            }
        }

        private void DrawSeries(DrawingContext dc, double[] values, double maxSpeed,
            double left, double top, double w, double h,
            Brush fillBrush, Pen strokePen, Brush dotBrush) {

            int n = values.Length;
            Point[] points = new Point[n];

            for (int i = 0; i < n; i++) {
                double x = left + (w * i / (n - 1));
                double valNorm = Math.Max(0, Math.Min(1.0, values[i] / maxSpeed));
                double y = top + h * (1.0 - valNorm);
                points[i] = new Point(x, y);
            }

            // Area path
            StreamGeometry areaGeom = new StreamGeometry();
            using (StreamGeometryContext ctx = areaGeom.Open()) {
                ctx.BeginFigure(new Point(points[0].X, top + h), true, true);
                ctx.LineTo(points[0], true, true);
                for (int i = 1; i < n; i++) {
                    ctx.LineTo(points[i], true, true);
                }
                ctx.LineTo(new Point(points[n - 1].X, top + h), true, true);
            }
            areaGeom.Freeze();
            dc.DrawGeometry(fillBrush, null, areaGeom);

            // Line path
            StreamGeometry lineGeom = new StreamGeometry();
            using (StreamGeometryContext ctx = lineGeom.Open()) {
                ctx.BeginFigure(points[0], false, false);
                for (int i = 1; i < n; i++) {
                    ctx.LineTo(points[i], true, true);
                }
            }
            lineGeom.Freeze();
            dc.DrawGeometry(null, strokePen, lineGeom);

            // Latest point glowing dot
            if (!IsMini) {
                Point last = points[n - 1];
                dc.DrawEllipse(dotBrush, null, last, 4.0, 4.0);
            }
        }
    }
}
