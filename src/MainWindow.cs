using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NetPulse.Controls;

namespace NetPulse {
    public class MainWindow : Window {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int WM_SETTINGCHANGE = 0x001A;

        private readonly NetworkMonitor _monitor;
        private readonly DispatcherTimer _timer;
        private readonly Dictionary<string, InterfaceCardControl> _cardControls;

        // UI references for theming
        private TextBlock _titleText;
        private TextBlock _subtitleText;
        private Border _versionBadge;
        private TextBlock _versionBadgeText;

        private Border _liveBadge;
        private Border _liveDot;
        private TextBlock _liveText;

        private Border _activePill;
        private TextBlock _activeCountText;

        private Border _uptimePill;
        private TextBlock _uptimeText;

        private Button _topmostBtn;
        private Button _themeBtn;
        private Button _resetBtn;

        // KPI UI elements
        private Border _dlCard;
        private TextBlock _dlCardTitle;
        private Border _dlIconBorder;
        private TextBlock _dlIconText;
        private TextBlock _dlSpeedValueText;
        private TextBlock _dlSpeedUnitText;
        private TextBlock _dlPeakText;

        private Border _ulCard;
        private TextBlock _ulCardTitle;
        private Border _ulIconBorder;
        private TextBlock _ulIconText;
        private TextBlock _ulSpeedValueText;
        private TextBlock _ulSpeedUnitText;
        private TextBlock _ulPeakText;

        private Border _totalCard;
        private TextBlock _totalCardTitle;
        private TextBlock _sessionDlText;
        private TextBlock _sessionUlText;
        private TextBlock _sessionTotalText;

        // Chart elements
        private Border _chartCard;
        private TextBlock _chartTitle;
        private RealtimeChart _mainChart;
        private TextBlock _chartDlSpeedLegend;
        private TextBlock _chartUlSpeedLegend;

        // Container for interface cards
        private TextBlock _ifaceSectionTitle;
        private StackPanel _interfaceCardsPanel;

        public MainWindow() {
            Title = "⚡ NetPulse — Windows Network Data Monitor";
            Width = 1120;
            Height = 820;
            MinWidth = 920;
            MinHeight = 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            if (System.IO.File.Exists("app.ico")) {
                try {
                    Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(System.IO.Path.GetFullPath("app.ico")));
                } catch {}
            }

            _monitor = new NetworkMonitor();
            _cardControls = new Dictionary<string, InterfaceCardControl>();

            Loaded += OnWindowLoaded;

            BuildUI();
            ApplyTheme();

            ThemeManager.ThemeChanged += OnThemeChanged;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            UpdateTitleBar();

            // Hook Windows setting change messages (auto theme sync)
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null) {
                source.AddHook(WndProc);
            }

            OnTimerTick(null, null);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == WM_SETTINGCHANGE) {
                string area = Marshal.PtrToStringAuto(lParam);
                if (area == "ImmersiveColorSet" || area == null) {
                    ThemeManager.SyncWithWindows();
                }
            }
            return IntPtr.Zero;
        }

        private void OnThemeChanged() {
            UpdateTitleBar();
            ApplyTheme();
        }

        private void UpdateTitleBar() {
            try {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int darkMode = ThemeManager.IsDark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0) {
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                }
            } catch {}
        }

        private void ApplyTheme() {
            Background = Helpers.BrushFromHex(ThemeManager.WindowBg);

            _titleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _subtitleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            _versionBadge.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
            _versionBadge.BorderBrush = Helpers.BrushFromHex(ThemeManager.BadgeBorder);
            _versionBadgeText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);

            _liveBadge.Background = Helpers.BrushFromHex(ThemeManager.DownloadBg);
            _liveBadge.BorderBrush = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _liveDot.Background = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _liveText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);

            _activePill.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
            _activePill.BorderBrush = Helpers.BrushFromHex(ThemeManager.BadgeBorder);
            _activeCountText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);

            _uptimePill.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
            _uptimePill.BorderBrush = Helpers.BrushFromHex(ThemeManager.BadgeBorder);
            _uptimeText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            // Theme toggle button
            _themeBtn.Content = ThemeManager.IsDark ? "☀️ Light" : "🌙 Dark";
            _themeBtn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
            _themeBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ButtonFg);
            _themeBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ButtonBorder);

            // Topmost button
            _topmostBtn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
            _topmostBtn.Foreground = Topmost ? Helpers.BrushFromHex(ThemeManager.DownloadColor) : Helpers.BrushFromHex(ThemeManager.ButtonFg);
            _topmostBtn.BorderBrush = Topmost ? Helpers.BrushFromHex(ThemeManager.DownloadColor) : Helpers.BrushFromHex(ThemeManager.ButtonBorder);

            // Reset button
            _resetBtn.Background = Helpers.BrushFromHex(ThemeManager.ResetBtnBg);
            _resetBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ResetBtnFg);
            _resetBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ResetBtnBorder);

            // KPI Download Card
            _dlCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _dlCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _dlCardTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _dlIconBorder.Background = Helpers.BrushFromHex(ThemeManager.DownloadBg);
            _dlIconText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _dlSpeedValueText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _dlSpeedUnitText.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _dlPeakText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            // KPI Upload Card
            _ulCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _ulCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _ulCardTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _ulIconBorder.Background = Helpers.BrushFromHex(ThemeManager.UploadBg);
            _ulIconText.Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor);
            _ulSpeedValueText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _ulSpeedUnitText.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _ulPeakText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            // KPI Total Card
            _totalCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _totalCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _totalCardTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _sessionTotalText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);
            _sessionDlText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _sessionUlText.Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor);

            // Chart Card
            _chartCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _chartCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _chartTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _chartDlSpeedLegend.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _chartUlSpeedLegend.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);

            // Interfaces Section
            _ifaceSectionTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
        }

        private void BuildUI() {
            Grid mainRoot = new Grid();
            mainRoot.Margin = new Thickness(24);
            mainRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // KPI Row
            mainRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(220) }); // Main Chart
            mainRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Interfaces

            // ════════════════════════════════════════════════
            // ── ROW 0: Header ──
            // ════════════════════════════════════════════════
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: Title + subtitle
            StackPanel titlePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            StackPanel logoRow = new StackPanel { Orientation = Orientation.Horizontal };

            TextBlock logoIcon = new TextBlock {
                Text = "⚡",
                FontSize = 24,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _titleText = new TextBlock {
                Text = "NetPulse Monitor",
                FontWeight = FontWeights.Bold,
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center
            };
            _versionBadge = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _versionBadgeText = new TextBlock {
                Text = "Windows Real-Time",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            _versionBadge.Child = _versionBadgeText;

            logoRow.Children.Add(logoIcon);
            logoRow.Children.Add(_titleText);
            logoRow.Children.Add(_versionBadge);

            _subtitleText = new TextBlock {
                Text = "High-precision live bandwidth & data consumption tracking for Wi-Fi and Ethernet",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            titlePanel.Children.Add(logoRow);
            titlePanel.Children.Add(_subtitleText);
            Grid.SetColumn(titlePanel, 0);
            headerGrid.Children.Add(titlePanel);

            // Right: Actions & indicators
            StackPanel actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Live status badge
            _liveBadge = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0)
            };
            StackPanel livePanel = new StackPanel { Orientation = Orientation.Horizontal };
            _liveDot = new Border {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _liveText = new TextBlock {
                Text = "LIVE",
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            livePanel.Children.Add(_liveDot);
            livePanel.Children.Add(_liveText);
            _liveBadge.Child = livePanel;
            actionsPanel.Children.Add(_liveBadge);

            // Active count pill
            _activePill = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0)
            };
            _activeCountText = new TextBlock {
                Text = "0 Active",
                FontSize = 11.5
            };
            _activePill.Child = _activeCountText;
            actionsPanel.Children.Add(_activePill);

            // Uptime pill
            _uptimePill = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0)
            };
            _uptimeText = new TextBlock {
                Text = "⏱ 00:00",
                FontSize = 11.5
            };
            _uptimePill.Child = _uptimeText;
            actionsPanel.Children.Add(_uptimePill);

            // Theme switch button
            _themeBtn = new Button {
                Content = "☀️ Light",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _themeBtn.Click += (s, e) => {
                ThemeManager.ToggleTheme();
            };
            actionsPanel.Children.Add(_themeBtn);

            // Pin on top button
            _topmostBtn = new Button {
                Content = "📌 Float",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _topmostBtn.Click += (s, e) => {
                Topmost = !Topmost;
                ApplyTheme();
            };
            actionsPanel.Children.Add(_topmostBtn);

            // Reset Session button
            _resetBtn = new Button {
                Content = "↻ Reset Session",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _resetBtn.Click += (s, e) => {
                _monitor.ResetSession();
                OnTimerTick(null, null);
            };
            actionsPanel.Children.Add(_resetBtn);

            Grid.SetColumn(actionsPanel, 1);
            headerGrid.Children.Add(actionsPanel);

            Grid.SetRow(headerGrid, 0);
            mainRoot.Children.Add(headerGrid);

            // ════════════════════════════════════════════════
            // ── ROW 1: KPI Summary Cards ──
            // ════════════════════════════════════════════════
            Grid kpiGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Card 1: Download
            _dlCard = CreateKpiCard("↓ DOWNLOAD SPEED",
                out _dlCardTitle, out _dlIconBorder, out _dlIconText,
                out _dlSpeedValueText, out _dlSpeedUnitText, out _dlPeakText);
            _dlCard.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(_dlCard, 0);
            kpiGrid.Children.Add(_dlCard);

            // Card 2: Upload
            _ulCard = CreateKpiCard("↑ UPLOAD SPEED",
                out _ulCardTitle, out _ulIconBorder, out _ulIconText,
                out _ulSpeedValueText, out _ulSpeedUnitText, out _ulPeakText);
            _ulCard.Margin = new Thickness(5, 0, 5, 0);
            Grid.SetColumn(_ulCard, 1);
            kpiGrid.Children.Add(_ulCard);

            // Card 3: Session Totals
            _totalCard = CreateSessionTotalCard();
            _totalCard.Margin = new Thickness(10, 0, 0, 0);
            Grid.SetColumn(_totalCard, 2);
            kpiGrid.Children.Add(_totalCard);

            Grid.SetRow(kpiGrid, 1);
            mainRoot.Children.Add(kpiGrid);

            // ════════════════════════════════════════════════
            // ── ROW 2: Live Speed Chart ──
            // ════════════════════════════════════════════════
            _chartCard = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 18)
            };
            Grid chartInnerGrid = new Grid();
            chartInnerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            chartInnerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Chart Header with live legend
            Grid chartHeader = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            chartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            chartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _chartTitle = new TextBlock {
                Text = "REAL-TIME BANDWIDTH ACTIVITY",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_chartTitle, 0);
            chartHeader.Children.Add(_chartTitle);

            // Legend
            StackPanel legendPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            legendPanel.Children.Add(new TextBlock { Text = "●", Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor), Margin = new Thickness(0, 0, 4, 0) });
            _chartDlSpeedLegend = new TextBlock {
                Text = "Download: 0 B/s",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 18, 0)
            };
            legendPanel.Children.Add(_chartDlSpeedLegend);

            legendPanel.Children.Add(new TextBlock { Text = "●", Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor), Margin = new Thickness(0, 0, 4, 0) });
            _chartUlSpeedLegend = new TextBlock {
                Text = "Upload: 0 B/s",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            legendPanel.Children.Add(_chartUlSpeedLegend);

            Grid.SetColumn(legendPanel, 1);
            chartHeader.Children.Add(legendPanel);

            Grid.SetRow(chartHeader, 0);
            chartInnerGrid.Children.Add(chartHeader);

            _mainChart = new RealtimeChart { IsMini = false };
            Grid.SetRow(_mainChart, 1);
            chartInnerGrid.Children.Add(_mainChart);

            _chartCard.Child = chartInnerGrid;
            Grid.SetRow(_chartCard, 2);
            mainRoot.Children.Add(_chartCard);

            // ════════════════════════════════════════════════
            // ── ROW 3: Network Adapters Section ──
            // ════════════════════════════════════════════════
            Grid ifaceSectionGrid = new Grid();
            ifaceSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ifaceSectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _ifaceSectionTitle = new TextBlock {
                Text = "NETWORK ADAPTERS",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(4, 0, 0, 10)
            };
            Grid.SetRow(_ifaceSectionTitle, 0);
            ifaceSectionGrid.Children.Add(_ifaceSectionTitle);

            ScrollViewer scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _interfaceCardsPanel = new StackPanel();
            scroll.Content = _interfaceCardsPanel;

            Grid.SetRow(scroll, 1);
            ifaceSectionGrid.Children.Add(scroll);

            Grid.SetRow(ifaceSectionGrid, 3);
            mainRoot.Children.Add(ifaceSectionGrid);

            Content = mainRoot;
        }

        private Border CreateKpiCard(string title,
            out TextBlock titleBlock, out Border iconBorder, out TextBlock iconText,
            out TextBlock valText, out TextBlock unitText, out TextBlock peakText) {

            Border card = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18, 14, 18, 14)
            };

            StackPanel sp = new StackPanel();

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            titleBlock = new TextBlock {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(titleBlock, 0);
            header.Children.Add(titleBlock);

            iconBorder = new Border {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13)
            };
            iconText = new TextBlock {
                Text = title.StartsWith("↓") ? "↓" : "↑",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 1);
            header.Children.Add(iconBorder);
            sp.Children.Add(header);

            StackPanel valRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 2) };
            valText = new TextBlock {
                Text = "0",
                FontSize = 32,
                FontWeight = FontWeights.Bold
            };
            unitText = new TextBlock {
                Text = "B/s",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(6, 0, 0, 4),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            valRow.Children.Add(valText);
            valRow.Children.Add(unitText);
            sp.Children.Add(valRow);

            peakText = new TextBlock {
                Text = "Peak: 0 B/s",
                FontSize = 11
            };
            sp.Children.Add(peakText);

            card.Child = sp;
            return card;
        }

        private Border CreateSessionTotalCard() {
            Border card = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18, 14, 18, 14)
            };

            StackPanel sp = new StackPanel();

            _totalCardTitle = new TextBlock {
                Text = "SESSION DATA USAGE",
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            sp.Children.Add(_totalCardTitle);

            StackPanel totalRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 2) };
            _sessionTotalText = new TextBlock {
                Text = "0 B",
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            totalRow.Children.Add(_sessionTotalText);
            sp.Children.Add(totalRow);

            Grid subGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _sessionDlText = new TextBlock {
                Text = "↓ Recv: 0 B",
                FontSize = 11
            };
            Grid.SetColumn(_sessionDlText, 0);
            subGrid.Children.Add(_sessionDlText);

            _sessionUlText = new TextBlock {
                Text = "↑ Sent: 0 B",
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(_sessionUlText, 1);
            subGrid.Children.Add(_sessionUlText);

            sp.Children.Add(subGrid);

            card.Child = sp;
            return card;
        }

        private void OnTimerTick(object sender, EventArgs e) {
            _monitor.Update();

            GlobalStats g = _monitor.GlobalStats;

            // Update Download KPI
            string dlVal, dlUnit;
            Helpers.FormatSpeedParts(g.TotalDownloadSpeed, out dlVal, out dlUnit);
            _dlSpeedValueText.Text = dlVal;
            _dlSpeedUnitText.Text = dlUnit;
            _dlPeakText.Text = "Peak: " + Helpers.FormatSpeed(g.PeakDownloadSpeed);

            // Update Upload KPI
            string ulVal, ulUnit;
            Helpers.FormatSpeedParts(g.TotalUploadSpeed, out ulVal, out ulUnit);
            _ulSpeedValueText.Text = ulVal;
            _ulSpeedUnitText.Text = ulUnit;
            _ulPeakText.Text = "Peak: " + Helpers.FormatSpeed(g.PeakUploadSpeed);

            // Update Session Data KPI
            _sessionTotalText.Text = Helpers.FormatBytes(g.TotalSessionDownloaded + g.TotalSessionUploaded);
            _sessionDlText.Text = "↓ Recv: " + Helpers.FormatBytes(g.TotalSessionDownloaded);
            _sessionUlText.Text = "↑ Sent: " + Helpers.FormatBytes(g.TotalSessionUploaded);

            // Update Header items
            _uptimeText.Text = "⏱ " + Helpers.FormatDuration(_monitor.SessionUptime);

            int connectedCount = 0;
            int totalCount = 0;
            foreach (var iface in _monitor.Interfaces) {
                totalCount++;
                if (iface.IsConnected) connectedCount++;
            }
            _activeCountText.Text = string.Format("{0} of {1} Active", connectedCount, totalCount);

            // Update Chart
            _mainChart.SetData(g.DownloadHistory, g.UploadHistory);
            _chartDlSpeedLegend.Text = "Download: " + Helpers.FormatSpeed(g.TotalDownloadSpeed);
            _chartUlSpeedLegend.Text = "Upload: " + Helpers.FormatSpeed(g.TotalUploadSpeed);

            // Update / Create Interface Cards
            foreach (var iface in _monitor.Interfaces) {
                InterfaceCardControl card;
                if (!_cardControls.TryGetValue(iface.Id, out card)) {
                    card = new InterfaceCardControl(iface);
                    _cardControls[iface.Id] = card;
                    _interfaceCardsPanel.Children.Add(card);
                }
                card.UpdateData();
            }
        }
    }
}
