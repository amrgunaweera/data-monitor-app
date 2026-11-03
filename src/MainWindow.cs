using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
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
        private System.Windows.Forms.NotifyIcon _trayIcon;
        private bool _isExiting = false;

        // Navigation tab: 0 = Live, 1 = History, 2 = Settings
        private int _currentTab = 0;
        private TimeRangePreset _currentHistoryPreset = TimeRangePreset.Last7Days;

        // UI references for theming & navigation
        private TextBlock _titleText;
        private TextBlock _subtitleText;
        private Border _versionBadge;
        private TextBlock _versionBadgeText;

        // Navigation Tabs
        private Button _navLiveBtn;
        private Button _navHistoryBtn;
        private Button _navSettingsBtn;

        // Header status badges & actions
        private Border _liveBadge;
        private Border _liveDot;
        private TextBlock _liveText;

        private Border _uptimePill;
        private TextBlock _uptimeText;

        private Button _resetBtn;

        // Views
        private Grid _liveViewGrid;
        private Grid _historyViewGrid;
        private ScrollViewer _settingsViewScroll;

        // ── LIVE VIEW ELEMENTS ──
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
        private TextBlock _liveHistoryTeaserText;
        private Button _viewHistoryQuickBtn;

        private Border _chartCard;
        private TextBlock _chartTitle;
        private RealtimeChart _mainChart;
        private TextBlock _chartDlSpeedLegend;
        private TextBlock _chartUlSpeedLegend;

        private TextBlock _ifaceSectionTitle;
        private StackPanel _interfaceCardsPanel;

        // ── HISTORY VIEW ELEMENTS ──
        private readonly List<Button> _presetButtons = new List<Button>();
        private TextBlock _historyRangeBadgeText;
        private Border _historyRangeBadge;
        private Button _exportCsvBtn;
        private Button _demoDataBtn;
        private Button _clearHistoryBtn;

        private Border _hTotalCard;
        private TextBlock _hTotalTitle;
        private TextBlock _hTotalValText;
        private TextBlock _hTotalSubText;

        private Border _hDlCard;
        private TextBlock _hDlTitle;
        private TextBlock _hDlValText;
        private TextBlock _hDlSubText;

        private Border _hUlCard;
        private TextBlock _hUlTitle;
        private TextBlock _hUlValText;
        private TextBlock _hUlSubText;

        private Border _hAvgCard;
        private TextBlock _hAvgTitle;
        private TextBlock _hAvgValText;
        private TextBlock _hAvgSubText;

        private Border _historyChartCard;
        private TextBlock _historyChartTitle;
        private HistoricalBarChart _historyBarChart;

        private Border _wifiBreakdownCard;
        private TextBlock _wifiTitleText;
        private TextBlock _wifiTotalText;
        private TextBlock _wifiSubText;
        private Border _wifiProgressBg;
        private Border _wifiProgressFill;

        private Border _ethBreakdownCard;
        private TextBlock _ethTitleText;
        private TextBlock _ethTotalText;
        private TextBlock _ethSubText;
        private Border _ethProgressBg;
        private Border _ethProgressFill;

        private Border _wifiNetworksCard;
        private TextBlock _wifiNetworksTitleText;
        private TextBlock _wifiNetworksCountBadgeText;
        private Border _wifiNetworksCountBadge;
        private StackPanel _wifiNetworksListPanel;

        private Border _tableCard;
        private TextBlock _tableTitleText;
        private StackPanel _tableRowsPanel;

        // ── SETTINGS VIEW ELEMENTS ──
        private readonly List<Border> _settingsCards = new List<Border>();
        private readonly List<TextBlock> _settingsCardTitles = new List<TextBlock>();
        private readonly List<TextBlock> _settingsLabels = new List<TextBlock>();
        private readonly List<TextBlock> _settingsSubLabels = new List<TextBlock>();

        private CheckBox _runStartupCheck;
        private CheckBox _startMinimizedCheck;
        private CheckBox _closeToTrayCheck;
        private CheckBox _alwaysOnTopCheck;

        private ComboBox _pollingRateCombo;
        private ComboBox _unitModeCombo;
        private CheckBox _hideDisconnectedCheck;

        private ComboBox _themeModeCombo;
        private Button _themeAutoBtn;
        private Button _themeDarkBtn;
        private Button _themeLightBtn;

        private CheckBox _enableQuotaCheck;
        private TextBox _quotaLimitBox;
        private TextBox _quotaResetDayBox;
        private Border _quotaCard;
        private TextBlock _quotaTitleText;
        private TextBlock _quotaStatusText;
        private TextBlock _quotaRemainingText;
        private Border _quotaProgressBg;
        private Border _quotaProgressFill;

        private TextBlock _storageLocationText;
        private TextBlock _storageStatsText;
        private Button _openFolderBtn;
        private Button _exportAllDataBtn;
        private Button _settingsClearDataBtn;

        public MainWindow() {
            Title = "⚡ NetPulse — Windows Network Data Monitor & History";
            Width = 1160;
            Height = 850;
            MinWidth = 980;
            MinHeight = 690;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            if (System.IO.File.Exists("app.ico")) {
                try {
                    Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(System.IO.Path.GetFullPath("app.ico")));
                } catch {}
            }

            // Apply saved settings
            AppSettings settings = AppSettings.Instance;
            if (settings.AlwaysOnTop) {
                Topmost = true;
            }

            _monitor = new NetworkMonitor();
            _cardControls = new Dictionary<string, InterfaceCardControl>();

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;

            BuildUI();
            ApplyTheme();
            SetupTrayIcon();

            ThemeManager.ThemeChanged += OnThemeChanged;
            AppSettings.Instance.SettingsChanged += OnSettingsChanged;

            _timer = new DispatcherTimer();
            double interval = settings.PollingIntervalSeconds;
            if (interval < 0.2 || interval > 10) interval = 1.0;
            _timer.Interval = TimeSpan.FromSeconds(interval);
            _timer.Tick += OnTimerTick;
            _timer.Start();

            if (settings.StartMinimized) {
                WindowState = WindowState.Minimized;
            }
        }

        private void SetupTrayIcon() {
            try {
                _trayIcon = new System.Windows.Forms.NotifyIcon();
                if (File.Exists("app.ico")) {
                    _trayIcon.Icon = new System.Drawing.Icon("app.ico");
                } else {
                    _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                }
                _trayIcon.Text = "NetPulse Monitor";
                _trayIcon.Visible = true;

                System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
                menu.MenuItems.Add("Show NetPulse", delegate(object s, EventArgs e) {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
                menu.MenuItems.Add("Reset Session", delegate(object s, EventArgs e) {
                    _monitor.ResetSession();
                    OnTimerTick(null, null);
                });
                menu.MenuItems.Add("-");
                menu.MenuItems.Add("Exit", delegate(object s, EventArgs e) {
                    _isExiting = true;
                    Close();
                });
                _trayIcon.ContextMenu = menu;

                _trayIcon.DoubleClick += delegate(object s, EventArgs e) {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                };
            } catch {}
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            UpdateTitleBar();

            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null) {
                source.AddHook(WndProc);
            }

            OnTimerTick(null, null);
            RefreshHistoryView();
            RefreshSettingsView();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!_isExiting && AppSettings.Instance.CloseToTray) {
                e.Cancel = true;
                Hide();
                if (_trayIcon != null) {
                    _trayIcon.ShowBalloonTip(2000, "NetPulse Running in Background",
                        "NetPulse is minimized to the system tray and continuing to monitor your data.",
                        System.Windows.Forms.ToolTipIcon.Info);
                }
                return;
            }

            UsageStorage.Instance.SaveToDisk(true);
            AppSettings.Instance.Save();

            if (_trayIcon != null) {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == WM_SETTINGCHANGE) {
                string area = Marshal.PtrToStringAuto(lParam);
                if (area == "ImmersiveColorSet" || area == null) {
                    if (AppSettings.Instance.ThemePreference == "System") {
                        ThemeManager.SyncWithWindows();
                    }
                }
            }
            return IntPtr.Zero;
        }

        private void OnThemeChanged() {
            UpdateTitleBar();
            ApplyTheme();
            if (_currentTab == 1) {
                RefreshHistoryView();
            } else if (_currentTab == 2) {
                RefreshSettingsView();
            }
        }

        private void OnSettingsChanged() {
            Topmost = AppSettings.Instance.AlwaysOnTop;
            _timer.Interval = TimeSpan.FromSeconds(AppSettings.Instance.PollingIntervalSeconds);
            OnTimerTick(null, null);
            RefreshSettingsView();
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

        private void SwitchTab(int tabIndex) {
            _currentTab = tabIndex;
            _liveViewGrid.Visibility = (_currentTab == 0) ? Visibility.Visible : Visibility.Collapsed;
            _historyViewGrid.Visibility = (_currentTab == 1) ? Visibility.Visible : Visibility.Collapsed;
            _settingsViewScroll.Visibility = (_currentTab == 2) ? Visibility.Visible : Visibility.Collapsed;

            ApplyTabButtonStyles();

            if (_currentTab == 1) {
                RefreshHistoryView();
            } else if (_currentTab == 2) {
                RefreshSettingsView();
            }
        }

        private void ApplyTabButtonStyles() {
            if (_navLiveBtn == null || _navHistoryBtn == null || _navSettingsBtn == null) return;

            StyleTabButton(_navLiveBtn, _currentTab == 0);
            StyleTabButton(_navHistoryBtn, _currentTab == 1);
            StyleTabButton(_navSettingsBtn, _currentTab == 2);
        }

        private void StyleTabButton(Button btn, bool isActive) {
            if (isActive) {
                btn.Background = Helpers.BrushFromHex(ThemeManager.TabActiveBg);
                btn.Foreground = Helpers.BrushFromHex(ThemeManager.TabActiveFg);
                btn.BorderBrush = Helpers.BrushFromHex(ThemeManager.TabActiveBorder);
                btn.FontWeight = FontWeights.Bold;
            } else {
                btn.Background = Helpers.BrushFromHex(ThemeManager.TabInactiveBg);
                btn.Foreground = Helpers.BrushFromHex(ThemeManager.TabInactiveFg);
                btn.BorderBrush = Helpers.BrushFromHex(ThemeManager.TabInactiveBorder);
                btn.FontWeight = FontWeights.Normal;
            }
        }

        private void ApplyTheme() {
            Background = Helpers.BrushFromHex(ThemeManager.WindowBg);

            _titleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _subtitleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            _versionBadge.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
            _versionBadge.BorderBrush = Helpers.BrushFromHex(ThemeManager.BadgeBorder);
            _versionBadgeText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);

            ApplyTabButtonStyles();

            _liveBadge.Background = Helpers.BrushFromHex(ThemeManager.DownloadBg);
            _liveBadge.BorderBrush = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _liveDot.Background = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _liveText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);

            _uptimePill.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
            _uptimePill.BorderBrush = Helpers.BrushFromHex(ThemeManager.BadgeBorder);
            _uptimeText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            _resetBtn.Background = Helpers.BrushFromHex(ThemeManager.ResetBtnBg);
            _resetBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ResetBtnFg);
            _resetBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ResetBtnBorder);

            // ── Live View Theming ──
            _dlCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _dlCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _dlCardTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _dlIconBorder.Background = Helpers.BrushFromHex(ThemeManager.DownloadBg);
            _dlIconText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _dlSpeedValueText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _dlSpeedUnitText.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _dlPeakText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            _ulCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _ulCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _ulCardTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _ulIconBorder.Background = Helpers.BrushFromHex(ThemeManager.UploadBg);
            _ulIconText.Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor);
            _ulSpeedValueText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _ulSpeedUnitText.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _ulPeakText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            _totalCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _totalCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _totalCardTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _sessionTotalText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);
            _sessionDlText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _sessionUlText.Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor);
            _liveHistoryTeaserText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
            _viewHistoryQuickBtn.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);

            _chartCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _chartCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _chartTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            _chartDlSpeedLegend.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _chartUlSpeedLegend.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);

            _ifaceSectionTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);

            // ── History View Theming ──
            _historyRangeBadge.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
            _historyRangeBadge.BorderBrush = Helpers.BrushFromHex(ThemeManager.BadgeBorder);
            _historyRangeBadgeText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);

            _exportCsvBtn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
            _exportCsvBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ButtonFg);
            _exportCsvBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ButtonBorder);

            _demoDataBtn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
            _demoDataBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ButtonFg);
            _demoDataBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ButtonBorder);

            _clearHistoryBtn.Background = Helpers.BrushFromHex(ThemeManager.ResetBtnBg);
            _clearHistoryBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ResetBtnFg);
            _clearHistoryBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ResetBtnBorder);

            StyleHistoryKpiCard(_hTotalCard, _hTotalTitle, _hTotalValText, _hTotalSubText, ThemeManager.TotalColor);
            StyleHistoryKpiCard(_hDlCard, _hDlTitle, _hDlValText, _hDlSubText, ThemeManager.DownloadColor);
            StyleHistoryKpiCard(_hUlCard, _hUlTitle, _hUlValText, _hUlSubText, ThemeManager.UploadColor);
            StyleHistoryKpiCard(_hAvgCard, _hAvgTitle, _hAvgValText, _hAvgSubText, ThemeManager.TextPrimary);

            _historyChartCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _historyChartCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _historyChartTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);

            _wifiBreakdownCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _wifiBreakdownCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _wifiTitleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _wifiTotalText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);
            _wifiSubText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
            _wifiProgressBg.Background = Helpers.BrushFromHex(ThemeManager.SubCardBg);
            _wifiProgressFill.Background = Helpers.BrushFromHex(ThemeManager.WifiBadgeBorder);

            _ethBreakdownCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _ethBreakdownCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _ethTitleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _ethTotalText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);
            _ethSubText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
            _ethProgressBg.Background = Helpers.BrushFromHex(ThemeManager.SubCardBg);
            _ethProgressFill.Background = Helpers.BrushFromHex(ThemeManager.EthBadgeBorder);

            if (_wifiNetworksCard != null) {
                _wifiNetworksCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
                _wifiNetworksCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
                _wifiNetworksTitleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
                _wifiNetworksCountBadge.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
                _wifiNetworksCountBadgeText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
            }

            _tableCard.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            _tableCard.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            _tableTitleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);

            UpdatePresetButtonsTheme();

            // ── Settings Theming ──
            for (int i = 0; i < _settingsCards.Count; i++) {
                _settingsCards[i].Background = Helpers.BrushFromHex(ThemeManager.CardBg);
                _settingsCards[i].BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            }
            for (int i = 0; i < _settingsCardTitles.Count; i++) {
                _settingsCardTitles[i].Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            }
            for (int i = 0; i < _settingsLabels.Count; i++) {
                _settingsLabels[i].Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            }
            for (int i = 0; i < _settingsSubLabels.Count; i++) {
                _settingsSubLabels[i].Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
            }

            if (_quotaCard != null) {
                _quotaCard.Background = Helpers.BrushFromHex(ThemeManager.SubCardBg);
                _quotaTitleText.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
                _quotaStatusText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);
                _quotaRemainingText.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
                _quotaProgressBg.Background = Helpers.BrushFromHex(ThemeManager.CardBorder);
                _quotaProgressFill.Background = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            }

            if (_openFolderBtn != null) {
                _openFolderBtn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
                _openFolderBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ButtonFg);
                _openFolderBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ButtonBorder);
            }
            if (_exportAllDataBtn != null) {
                _exportAllDataBtn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
                _exportAllDataBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ButtonFg);
                _exportAllDataBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ButtonBorder);
            }
            if (_settingsClearDataBtn != null) {
                _settingsClearDataBtn.Background = Helpers.BrushFromHex(ThemeManager.ResetBtnBg);
                _settingsClearDataBtn.Foreground = Helpers.BrushFromHex(ThemeManager.ResetBtnFg);
                _settingsClearDataBtn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ResetBtnBorder);
            }

            UpdateThemeButtonsHighlight();

            if (_currentTab == 1) {
                RefreshHistoryView();
            }
        }

        private void StyleHistoryKpiCard(Border card, TextBlock title, TextBlock val, TextBlock sub, string valHex) {
            card.Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            card.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
            title.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            val.Foreground = Helpers.BrushFromHex(valHex);
            sub.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
        }

        private void UpdatePresetButtonsTheme() {
            for (int i = 0; i < _presetButtons.Count; i++) {
                Button btn = _presetButtons[i];
                TimeRangePreset preset = (TimeRangePreset)btn.Tag;
                bool isSelected = (preset == _currentHistoryPreset);

                if (isSelected) {
                    btn.Background = Helpers.BrushFromHex(ThemeManager.TabActiveBg);
                    btn.Foreground = Helpers.BrushFromHex(ThemeManager.TabActiveFg);
                    btn.BorderBrush = Helpers.BrushFromHex(ThemeManager.TabActiveBorder);
                    btn.FontWeight = FontWeights.Bold;
                } else {
                    btn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
                    btn.Foreground = Helpers.BrushFromHex(ThemeManager.ButtonFg);
                    btn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ButtonBorder);
                    btn.FontWeight = FontWeights.Normal;
                }
            }
        }

        private void BuildUI() {
            Grid mainRoot = new Grid();
            mainRoot.Margin = new Thickness(24, 20, 24, 20);
            mainRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header
            mainRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1: Active View Container

            // ════════════════════════════════════════════════
            // ── ROW 0: Top Header & Navigation Tabs ──
            // ════════════════════════════════════════════════
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left: Logo & Title
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Center: Tabs
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right: Badges & Controls

            // Left branding
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
                FontSize = 21,
                VerticalAlignment = VerticalAlignment.Center
            };
            _versionBadge = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _versionBadgeText = new TextBlock {
                Text = "Windows Native",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            _versionBadge.Child = _versionBadgeText;

            logoRow.Children.Add(logoIcon);
            logoRow.Children.Add(_titleText);
            logoRow.Children.Add(_versionBadge);

            _subtitleText = new TextBlock {
                Text = "Real-time bandwidth & persistent daily, weekly, monthly usage analytics",
                FontSize = 11.5,
                Margin = new Thickness(0, 3, 0, 0)
            };

            titlePanel.Children.Add(logoRow);
            titlePanel.Children.Add(_subtitleText);
            Grid.SetColumn(titlePanel, 0);
            headerGrid.Children.Add(titlePanel);

            // Center: Navigation Tabs
            StackPanel navTabsPanel = new StackPanel {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 16, 0)
            };

            _navLiveBtn = CreateTabButton("⚡ Live Dashboard", 0);
            _navHistoryBtn = CreateTabButton("📊 Usage History", 1);
            _navSettingsBtn = CreateTabButton("⚙️ Settings", 2);

            navTabsPanel.Children.Add(_navLiveBtn);
            navTabsPanel.Children.Add(_navHistoryBtn);
            navTabsPanel.Children.Add(_navSettingsBtn);
            Grid.SetColumn(navTabsPanel, 1);
            headerGrid.Children.Add(navTabsPanel);

            // Right: Live badges and utility actions
            StackPanel actionsPanel = new StackPanel {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _liveBadge = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 0)
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
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            };
            livePanel.Children.Add(_liveDot);
            livePanel.Children.Add(_liveText);
            _liveBadge.Child = livePanel;
            actionsPanel.Children.Add(_liveBadge);

            _uptimePill = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 0)
            };
            _uptimeText = new TextBlock {
                Text = "⏱ 00:00",
                FontSize = 11
            };
            _uptimePill.Child = _uptimeText;
            actionsPanel.Children.Add(_uptimePill);

            _resetBtn = new Button {
                Content = "↻",
                ToolTip = "Reset Session Traffic",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _resetBtn.Click += delegate(object s, RoutedEventArgs e) {
                _monitor.ResetSession();
                OnTimerTick(null, null);
            };
            actionsPanel.Children.Add(_resetBtn);

            Grid.SetColumn(actionsPanel, 2);
            headerGrid.Children.Add(actionsPanel);

            Grid.SetRow(headerGrid, 0);
            mainRoot.Children.Add(headerGrid);

            // ════════════════════════════════════════════════
            // ── ROW 1: Active View Container ──
            // ════════════════════════════════════════════════
            Grid viewContainer = new Grid();

            // Build all 3 views
            _liveViewGrid = BuildLiveView();
            _historyViewGrid = BuildHistoryView();
            _settingsViewScroll = BuildSettingsView();

            _historyViewGrid.Visibility = Visibility.Collapsed;
            _settingsViewScroll.Visibility = Visibility.Collapsed;

            viewContainer.Children.Add(_liveViewGrid);
            viewContainer.Children.Add(_historyViewGrid);
            viewContainer.Children.Add(_settingsViewScroll);

            Grid.SetRow(viewContainer, 1);
            mainRoot.Children.Add(viewContainer);

            Content = mainRoot;
        }

        private Button CreateTabButton(string text, int tabIndex) {
            Button btn = new Button {
                Content = text,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 7, 14, 7),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            btn.Click += delegate(object s, RoutedEventArgs e) { SwitchTab(tabIndex); };
            return btn;
        }

        // ════════════════════════════════════════════════════════
        // ── LIVE DASHBOARD VIEW BUILDER ──
        // ════════════════════════════════════════════════════════
        private Grid BuildLiveView() {
            Grid liveGrid = new Grid();
            liveGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            liveGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });
            liveGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid kpiGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });

            _dlCard = CreateKpiCard("↓ DOWNLOAD SPEED",
                out _dlCardTitle, out _dlIconBorder, out _dlIconText,
                out _dlSpeedValueText, out _dlSpeedUnitText, out _dlPeakText);
            _dlCard.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(_dlCard, 0);
            kpiGrid.Children.Add(_dlCard);

            _ulCard = CreateKpiCard("↑ UPLOAD SPEED",
                out _ulCardTitle, out _ulIconBorder, out _ulIconText,
                out _ulSpeedValueText, out _ulSpeedUnitText, out _ulPeakText);
            _ulCard.Margin = new Thickness(5, 0, 5, 0);
            Grid.SetColumn(_ulCard, 1);
            kpiGrid.Children.Add(_ulCard);

            _totalCard = CreateLiveSessionAndHistoryCard();
            _totalCard.Margin = new Thickness(10, 0, 0, 0);
            Grid.SetColumn(_totalCard, 2);
            kpiGrid.Children.Add(_totalCard);

            Grid.SetRow(kpiGrid, 0);
            liveGrid.Children.Add(kpiGrid);

            _chartCard = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid chartInnerGrid = new Grid();
            chartInnerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            chartInnerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid chartHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            chartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            chartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _chartTitle = new TextBlock {
                Text = "REAL-TIME BANDWIDTH ACTIVITY",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_chartTitle, 0);
            chartHeader.Children.Add(_chartTitle);

            StackPanel legendPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            legendPanel.Children.Add(new TextBlock { Text = "●", Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor), Margin = new Thickness(0, 0, 4, 0) });
            _chartDlSpeedLegend = new TextBlock {
                Text = "Download: 0 B/s",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 16, 0)
            };
            legendPanel.Children.Add(_chartDlSpeedLegend);

            legendPanel.Children.Add(new TextBlock { Text = "●", Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor), Margin = new Thickness(0, 0, 4, 0) });
            _chartUlSpeedLegend = new TextBlock {
                Text = "Upload: 0 B/s",
                FontSize = 11.5,
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
            Grid.SetRow(_chartCard, 1);
            liveGrid.Children.Add(_chartCard);

            Grid ifaceSectionGrid = new Grid();
            ifaceSectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ifaceSectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _ifaceSectionTitle = new TextBlock {
                Text = "NETWORK ADAPTERS",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(4, 0, 0, 8)
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

            Grid.SetRow(ifaceSectionGrid, 2);
            liveGrid.Children.Add(ifaceSectionGrid);

            return liveGrid;
        }

        private Border CreateKpiCard(string title,
            out TextBlock titleBlock, out Border iconBorder, out TextBlock iconText,
            out TextBlock valText, out TextBlock unitText, out TextBlock peakText) {

            Border card = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel sp = new StackPanel();

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            titleBlock = new TextBlock {
                Text = title,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(titleBlock, 0);
            header.Children.Add(titleBlock);

            iconBorder = new Border {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12)
            };
            iconText = new TextBlock {
                Text = title.StartsWith("↓") ? "↓" : "↑",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 1);
            header.Children.Add(iconBorder);
            sp.Children.Add(header);

            StackPanel valRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 2) };
            valText = new TextBlock {
                Text = "0",
                FontSize = 28,
                FontWeight = FontWeights.Bold
            };
            unitText = new TextBlock {
                Text = "B/s",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(6, 0, 0, 4),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            valRow.Children.Add(valText);
            valRow.Children.Add(unitText);
            sp.Children.Add(valRow);

            peakText = new TextBlock {
                Text = "Peak: 0 B/s",
                FontSize = 10.5
            };
            sp.Children.Add(peakText);

            card.Child = sp;
            return card;
        }

        private Border CreateLiveSessionAndHistoryCard() {
            Border card = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel sp = new StackPanel();

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _totalCardTitle = new TextBlock {
                Text = "SESSION DATA USAGE",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(_totalCardTitle, 0);
            header.Children.Add(_totalCardTitle);

            _viewHistoryQuickBtn = new Button {
                Content = "View History →",
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(0)
            };
            _viewHistoryQuickBtn.Click += delegate(object s, RoutedEventArgs e) { SwitchTab(1); };
            Grid.SetColumn(_viewHistoryQuickBtn, 1);
            header.Children.Add(_viewHistoryQuickBtn);
            sp.Children.Add(header);

            StackPanel totalRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 2) };
            _sessionTotalText = new TextBlock {
                Text = "0 B",
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            totalRow.Children.Add(_sessionTotalText);
            sp.Children.Add(totalRow);

            Grid subGrid = new Grid { Margin = new Thickness(0, 2, 0, 4) };
            subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _sessionDlText = new TextBlock {
                Text = "↓ Recv: 0 B",
                FontSize = 10.5
            };
            Grid.SetColumn(_sessionDlText, 0);
            subGrid.Children.Add(_sessionDlText);

            _sessionUlText = new TextBlock {
                Text = "↑ Sent: 0 B",
                FontSize = 10.5,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(_sessionUlText, 1);
            subGrid.Children.Add(_sessionUlText);
            sp.Children.Add(subGrid);

            _liveHistoryTeaserText = new TextBlock {
                Text = "Today: 0 B • Last 7 Days: 0 B • This Month: 0 B",
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            sp.Children.Add(_liveHistoryTeaserText);

            card.Child = sp;
            return card;
        }

        // ════════════════════════════════════════════════════════
        // ── USAGE HISTORY & ANALYTICS VIEW BUILDER ──
        // ════════════════════════════════════════════════════════
        private Grid BuildHistoryView() {
            Grid historyGrid = new Grid();
            historyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            historyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            historyGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });
            historyGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid controlBar = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            controlBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel presetsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _presetButtons.Clear();
            _presetButtons.Add(CreatePresetFilterButton("Today", TimeRangePreset.Today));
            _presetButtons.Add(CreatePresetFilterButton("Yesterday", TimeRangePreset.Yesterday));
            _presetButtons.Add(CreatePresetFilterButton("Last 7 Days", TimeRangePreset.Last7Days));
            _presetButtons.Add(CreatePresetFilterButton("Last Week", TimeRangePreset.LastWeek));
            _presetButtons.Add(CreatePresetFilterButton("This Month", TimeRangePreset.ThisMonth));
            _presetButtons.Add(CreatePresetFilterButton("Last Month", TimeRangePreset.LastMonth));
            _presetButtons.Add(CreatePresetFilterButton("All Time", TimeRangePreset.AllTime));

            for (int i = 0; i < _presetButtons.Count; i++) {
                presetsPanel.Children.Add(_presetButtons[i]);
            }
            Grid.SetColumn(presetsPanel, 0);
            controlBar.Children.Add(presetsPanel);

            StackPanel rightActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _historyRangeBadge = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 0)
            };
            _historyRangeBadgeText = new TextBlock {
                Text = "📅 Date Range",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            _historyRangeBadge.Child = _historyRangeBadgeText;
            rightActions.Children.Add(_historyRangeBadge);

            _exportCsvBtn = new Button {
                Content = "📁 Export CSV",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            _exportCsvBtn.Click += OnExportCsvClicked;
            rightActions.Children.Add(_exportCsvBtn);

            _demoDataBtn = new Button {
                Content = "🎲 Sample Data",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            _demoDataBtn.Click += OnDemoDataClicked;
            rightActions.Children.Add(_demoDataBtn);

            _clearHistoryBtn = new Button {
                Content = "🗑 Clear",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            _clearHistoryBtn.Click += OnClearHistoryClicked;
            rightActions.Children.Add(_clearHistoryBtn);

            Grid.SetColumn(rightActions, 2);
            controlBar.Children.Add(rightActions);

            Grid.SetRow(controlBar, 0);
            historyGrid.Children.Add(controlBar);

            Grid kpiGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _hTotalCard = CreateHistorySummaryCard("TOTAL USAGE", out _hTotalTitle, out _hTotalValText, out _hTotalSubText);
            _hTotalCard.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(_hTotalCard, 0);
            kpiGrid.Children.Add(_hTotalCard);

            _hDlCard = CreateHistorySummaryCard("DOWNLOADED", out _hDlTitle, out _hDlValText, out _hDlSubText);
            _hDlCard.Margin = new Thickness(4, 0, 4, 0);
            Grid.SetColumn(_hDlCard, 1);
            kpiGrid.Children.Add(_hDlCard);

            _hUlCard = CreateHistorySummaryCard("UPLOADED", out _hUlTitle, out _hUlValText, out _hUlSubText);
            _hUlCard.Margin = new Thickness(4, 0, 4, 0);
            Grid.SetColumn(_hUlCard, 2);
            kpiGrid.Children.Add(_hUlCard);

            _hAvgCard = CreateHistorySummaryCard("DAILY AVERAGE", out _hAvgTitle, out _hAvgValText, out _hAvgSubText);
            _hAvgCard.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(_hAvgCard, 3);
            kpiGrid.Children.Add(_hAvgCard);

            Grid.SetRow(kpiGrid, 1);
            historyGrid.Children.Add(kpiGrid);

            _historyChartCard = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid hChartInner = new Grid();
            hChartInner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            hChartInner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid hChartHeader = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            hChartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hChartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _historyChartTitle = new TextBlock {
                Text = "DAILY DATA USAGE BREAKDOWN (STACKED RECV & SENT)",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_historyChartTitle, 0);
            hChartHeader.Children.Add(_historyChartTitle);

            StackPanel hLegend = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            hLegend.Children.Add(new TextBlock { Text = "●", Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor), Margin = new Thickness(0, 0, 4, 0) });
            hLegend.Children.Add(new TextBlock { Text = "Download", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary), Margin = new Thickness(0, 0, 14, 0) });
            hLegend.Children.Add(new TextBlock { Text = "●", Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor), Margin = new Thickness(0, 0, 4, 0) });
            hLegend.Children.Add(new TextBlock { Text = "Upload", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary) });
            Grid.SetColumn(hLegend, 1);
            hChartHeader.Children.Add(hLegend);

            Grid.SetRow(hChartHeader, 0);
            hChartInner.Children.Add(hChartHeader);

            _historyBarChart = new HistoricalBarChart();
            Grid.SetRow(_historyBarChart, 1);
            hChartInner.Children.Add(_historyBarChart);

            _historyChartCard.Child = hChartInner;
            Grid.SetRow(_historyChartCard, 2);
            historyGrid.Children.Add(_historyChartCard);

            Grid bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            ScrollViewer leftScroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 12, 0)
            };

            StackPanel leftPanel = new StackPanel();

            _wifiBreakdownCard = CreateAdapterBreakdownCard("📶 Wi-Fi Total Usage",
                out _wifiTitleText, out _wifiTotalText, out _wifiSubText,
                out _wifiProgressBg, out _wifiProgressFill);
            _wifiBreakdownCard.Margin = new Thickness(0, 0, 0, 10);
            leftPanel.Children.Add(_wifiBreakdownCard);

            _ethBreakdownCard = CreateAdapterBreakdownCard("🔌 Ethernet Total Usage",
                out _ethTitleText, out _ethTotalText, out _ethSubText,
                out _ethProgressBg, out _ethProgressFill);
            _ethBreakdownCard.Margin = new Thickness(0, 0, 0, 10);
            leftPanel.Children.Add(_ethBreakdownCard);

            // Wi-Fi Networks Breakdown Card
            _wifiNetworksCard = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 12, 14, 12)
            };

            Grid wifiNetsInner = new Grid();
            wifiNetsInner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            wifiNetsInner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid wifiNetsHeader = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            wifiNetsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            wifiNetsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _wifiNetworksTitleText = new TextBlock {
                Text = "📶 WI-FI NETWORKS (BY SSID)",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_wifiNetworksTitleText, 0);
            wifiNetsHeader.Children.Add(_wifiNetworksTitleText);

            _wifiNetworksCountBadge = new Border {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            _wifiNetworksCountBadgeText = new TextBlock {
                Text = "0 Networks",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            _wifiNetworksCountBadge.Child = _wifiNetworksCountBadgeText;
            Grid.SetColumn(_wifiNetworksCountBadge, 1);
            wifiNetsHeader.Children.Add(_wifiNetworksCountBadge);

            Grid.SetRow(wifiNetsHeader, 0);
            wifiNetsInner.Children.Add(wifiNetsHeader);

            _wifiNetworksListPanel = new StackPanel();
            Grid.SetRow(_wifiNetworksListPanel, 1);
            wifiNetsInner.Children.Add(_wifiNetworksListPanel);

            _wifiNetworksCard.Child = wifiNetsInner;
            leftPanel.Children.Add(_wifiNetworksCard);

            leftScroll.Content = leftPanel;
            Grid.SetColumn(leftScroll, 0);
            bottomGrid.Children.Add(leftScroll);

            _tableCard = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 12, 16, 12)
            };
            Grid tableInner = new Grid();
            tableInner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tableInner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _tableTitleText = new TextBlock {
                Text = "DAILY DATA USAGE LOG",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(_tableTitleText, 0);
            tableInner.Children.Add(_tableTitleText);

            ScrollViewer tableScroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _tableRowsPanel = new StackPanel();
            tableScroll.Content = _tableRowsPanel;
            Grid.SetRow(tableScroll, 1);
            tableInner.Children.Add(tableScroll);

            _tableCard.Child = tableInner;
            Grid.SetColumn(_tableCard, 1);
            bottomGrid.Children.Add(_tableCard);

            Grid.SetRow(bottomGrid, 3);
            historyGrid.Children.Add(bottomGrid);

            return historyGrid;
        }

        private Button CreatePresetFilterButton(string label, TimeRangePreset preset) {
            Button btn = new Button {
                Content = label,
                Tag = preset,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            btn.Click += delegate(object s, RoutedEventArgs e) {
                _currentHistoryPreset = preset;
                UpdatePresetButtonsTheme();
                RefreshHistoryView();
            };
            return btn;
        }

        private Border CreateHistorySummaryCard(string title,
            out TextBlock titleBlock, out TextBlock valBlock, out TextBlock subBlock) {

            Border card = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 10, 14, 10)
            };

            StackPanel sp = new StackPanel();

            titleBlock = new TextBlock {
                Text = title,
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            sp.Children.Add(titleBlock);

            valBlock = new TextBlock {
                Text = "0 B",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 2)
            };
            sp.Children.Add(valBlock);

            subBlock = new TextBlock {
                Text = "-",
                FontSize = 10.5
            };
            sp.Children.Add(subBlock);

            card.Child = sp;
            return card;
        }

        private Border CreateAdapterBreakdownCard(string defaultTitle,
            out TextBlock titleBlock, out TextBlock totalBlock, out TextBlock subBlock,
            out Border progressBg, out Border progressFill) {

            Border card = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 10, 14, 10)
            };

            StackPanel sp = new StackPanel();

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            titleBlock = new TextBlock {
                Text = defaultTitle,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(titleBlock, 0);
            header.Children.Add(titleBlock);

            totalBlock = new TextBlock {
                Text = "0 B",
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(totalBlock, 1);
            header.Children.Add(totalBlock);
            sp.Children.Add(header);

            subBlock = new TextBlock {
                Text = "↓ 0 B  •  ↑ 0 B (0% of period)",
                FontSize = 10.5,
                Margin = new Thickness(0, 4, 0, 6)
            };
            sp.Children.Add(subBlock);

            progressBg = new Border {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            progressFill = new Border {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0
            };
            progressBg.Child = progressFill;
            sp.Children.Add(progressBg);

            card.Child = sp;
            return card;
        }

        // ════════════════════════════════════════════════════════
        // ── SETTINGS VIEW BUILDER ──
        // ════════════════════════════════════════════════════════
        private ScrollViewer BuildSettingsView() {
            ScrollViewer scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            Grid settingsGrid = new Grid();
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel leftCol = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            StackPanel rightCol = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };

            // ── Card 1: 🖥️ System & Startup ──
            Border systemCard = CreateSettingsCard("🖥️ SYSTEM & STARTUP");
            StackPanel sysSp = new StackPanel();

            _runStartupCheck = CreateSettingsCheckBox("Run on Windows Startup",
                "Launch NetPulse automatically in the background when logging in to Windows",
                delegate(bool isChecked) {
                    AppSettings.Instance.RunOnStartup = isChecked;
                    AppSettings.Instance.Save();
                });
            sysSp.Children.Add(_runStartupCheck);

            _startMinimizedCheck = CreateSettingsCheckBox("Start Minimized",
                "Open NetPulse minimized on startup without showing main window",
                delegate(bool isChecked) {
                    AppSettings.Instance.StartMinimized = isChecked;
                    AppSettings.Instance.Save();
                });
            sysSp.Children.Add(_startMinimizedCheck);

            _closeToTrayCheck = CreateSettingsCheckBox("Minimize to System Tray on Close",
                "Keep monitoring and calculating background usage when window is closed",
                delegate(bool isChecked) {
                    AppSettings.Instance.CloseToTray = isChecked;
                    AppSettings.Instance.Save();
                });
            sysSp.Children.Add(_closeToTrayCheck);

            _alwaysOnTopCheck = CreateSettingsCheckBox("Always on Top (Floating Window)",
                "Keep NetPulse above all other windows by default",
                delegate(bool isChecked) {
                    AppSettings.Instance.AlwaysOnTop = isChecked;
                    Topmost = isChecked;
                    AppSettings.Instance.Save();
                    ApplyTheme();
                });
            sysSp.Children.Add(_alwaysOnTopCheck);

            systemCard.Child = sysSp;
            leftCol.Children.Add(systemCard);

            // ── Card 2: ⏱️ Monitoring & Speed Units ──
            Border monitorCard = CreateSettingsCard("⏱️ MONITORING & SPEED UNITS");
            StackPanel monSp = new StackPanel();

            monSp.Children.Add(CreateSettingsOptionLabel("Bandwidth Polling Rate", "Frequency of network adapter traffic sampling"));
            _pollingRateCombo = new ComboBox {
                Margin = new Thickness(0, 4, 0, 14),
                Height = 30,
                FontSize = 12
            };
            _pollingRateCombo.Items.Add("0.5 Seconds (High Precision)");
            _pollingRateCombo.Items.Add("1.0 Second (Recommended / Default)");
            _pollingRateCombo.Items.Add("2.0 Seconds (Eco Mode / Battery Saver)");
            _pollingRateCombo.SelectionChanged += delegate(object s, SelectionChangedEventArgs e) {
                if (_pollingRateCombo.SelectedIndex == 0) AppSettings.Instance.PollingIntervalSeconds = 0.5;
                else if (_pollingRateCombo.SelectedIndex == 1) AppSettings.Instance.PollingIntervalSeconds = 1.0;
                else if (_pollingRateCombo.SelectedIndex == 2) AppSettings.Instance.PollingIntervalSeconds = 2.0;
                _timer.Interval = TimeSpan.FromSeconds(AppSettings.Instance.PollingIntervalSeconds);
                AppSettings.Instance.Save();
            };
            monSp.Children.Add(_pollingRateCombo);

            monSp.Children.Add(CreateSettingsOptionLabel("Speed Units Display", "Choose format for download & upload transfer rates"));
            _unitModeCombo = new ComboBox {
                Margin = new Thickness(0, 4, 0, 14),
                Height = 30,
                FontSize = 12
            };
            _unitModeCombo.Items.Add("Auto Scale (B/s, KB/s, MB/s, GB/s)");
            _unitModeCombo.Items.Add("Bits per Second (bps, Kbps, Mbps, Gbps)");
            _unitModeCombo.Items.Add("Strict MB/s Only");
            _unitModeCombo.SelectionChanged += delegate(object s, SelectionChangedEventArgs e) {
                if (_unitModeCombo.SelectedIndex == 0) AppSettings.Instance.SpeedUnitMode = "Auto";
                else if (_unitModeCombo.SelectedIndex == 1) AppSettings.Instance.SpeedUnitMode = "Bits";
                else if (_unitModeCombo.SelectedIndex == 2) AppSettings.Instance.SpeedUnitMode = "BytesOnly";
                AppSettings.Instance.Save();
                OnTimerTick(null, null);
            };
            monSp.Children.Add(_unitModeCombo);

            _hideDisconnectedCheck = CreateSettingsCheckBox("Hide Inactive / Disconnected Adapters",
                "Show only active connected adapters in the Live network cards view",
                delegate(bool isChecked) {
                    AppSettings.Instance.HideDisconnectedAdapters = isChecked;
                    AppSettings.Instance.Save();
                    OnTimerTick(null, null);
                });
            monSp.Children.Add(_hideDisconnectedCheck);

            monitorCard.Child = monSp;
            leftCol.Children.Add(monitorCard);

            // ── Card 3: 🎨 Appearance & Themes ──
            Border themeCard = CreateSettingsCard("🎨 APPEARANCE & THEMES");
            StackPanel themeSp = new StackPanel();

            themeSp.Children.Add(CreateSettingsOptionLabel("Theme Switcher", "Toggle dark mode, light mode, or follow Windows system setting"));

            StackPanel themeBtnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 10) };
            _themeAutoBtn = CreateThemeOptionButton("💻 System Auto", "System");
            _themeDarkBtn = CreateThemeOptionButton("🌙 Dark Mode", "Dark");
            _themeLightBtn = CreateThemeOptionButton("☀️ Light Mode", "Light");
            themeBtnRow.Children.Add(_themeAutoBtn);
            themeBtnRow.Children.Add(_themeDarkBtn);
            themeBtnRow.Children.Add(_themeLightBtn);
            themeSp.Children.Add(themeBtnRow);

            _themeModeCombo = new ComboBox {
                Margin = new Thickness(0, 0, 0, 4),
                Height = 30,
                FontSize = 12
            };
            _themeModeCombo.Items.Add("Follow Windows System Preference (Auto)");
            _themeModeCombo.Items.Add("🌙 Always Dark Theme");
            _themeModeCombo.Items.Add("☀️ Always Light Theme");
            _themeModeCombo.SelectionChanged += delegate(object s, SelectionChangedEventArgs e) {
                if (_themeModeCombo.SelectedIndex == 0) AppSettings.Instance.ThemePreference = "System";
                else if (_themeModeCombo.SelectedIndex == 1) AppSettings.Instance.ThemePreference = "Dark";
                else if (_themeModeCombo.SelectedIndex == 2) AppSettings.Instance.ThemePreference = "Light";
                AppSettings.Instance.Save();
                ThemeManager.SyncWithSettingsOrWindows();
                UpdateThemeButtonsHighlight();
            };
            themeSp.Children.Add(_themeModeCombo);

            themeCard.Child = themeSp;
            leftCol.Children.Add(themeCard);

            // ── Card 4: 🎯 Monthly Data Quota & Alerts ──
            Border quotaCardOuter = CreateSettingsCard("🎯 MONTHLY DATA QUOTA & USAGE CAP");
            StackPanel quotaSp = new StackPanel();

            _enableQuotaCheck = CreateSettingsCheckBox("Enable Monthly Data Quota",
                "Track remaining bandwidth and visualize monthly cap consumption",
                delegate(bool isChecked) {
                    AppSettings.Instance.EnableMonthlyQuota = isChecked;
                    AppSettings.Instance.Save();
                    RefreshQuotaVisualizer();
                });
            quotaSp.Children.Add(_enableQuotaCheck);

            Grid quotaInputsGrid = new Grid { Margin = new Thickness(0, 8, 0, 12) };
            quotaInputsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            quotaInputsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel q1 = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            q1.Children.Add(CreateSettingsOptionLabel("Monthly Limit (GB)", "Data allowance per billing cycle"));
            _quotaLimitBox = new TextBox {
                Height = 28,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                Padding = new Thickness(6, 4, 6, 4)
            };
            _quotaLimitBox.LostFocus += delegate(object s, RoutedEventArgs e) {
                double val;
                if (double.TryParse(_quotaLimitBox.Text, out val) && val > 0) {
                    AppSettings.Instance.MonthlyQuotaGB = val;
                    AppSettings.Instance.Save();
                    RefreshQuotaVisualizer();
                }
            };
            q1.Children.Add(_quotaLimitBox);
            Grid.SetColumn(q1, 0);
            quotaInputsGrid.Children.Add(q1);

            StackPanel q2 = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            q2.Children.Add(CreateSettingsOptionLabel("Cycle Reset Day", "Day of the month the allowance resets (1-31)"));
            _quotaResetDayBox = new TextBox {
                Height = 28,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                Padding = new Thickness(6, 4, 6, 4)
            };
            _quotaResetDayBox.LostFocus += delegate(object s, RoutedEventArgs e) {
                int day;
                if (int.TryParse(_quotaResetDayBox.Text, out day) && day >= 1 && day <= 31) {
                    AppSettings.Instance.QuotaResetDay = day;
                    AppSettings.Instance.Save();
                    RefreshQuotaVisualizer();
                }
            };
            q2.Children.Add(_quotaResetDayBox);
            Grid.SetColumn(q2, 1);
            quotaInputsGrid.Children.Add(q2);

            quotaSp.Children.Add(quotaInputsGrid);

            // Visualizer Box
            _quotaCard = new Border {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 4, 0, 0)
            };
            StackPanel qBox = new StackPanel();

            Grid qHeader = new Grid();
            qHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            qHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _quotaTitleText = new TextBlock { Text = "CURRENT MONTH QUOTA USAGE", FontSize = 10, FontWeight = FontWeights.Bold };
            Grid.SetColumn(_quotaTitleText, 0);
            qHeader.Children.Add(_quotaTitleText);

            _quotaStatusText = new TextBlock { Text = "0.0% Used", FontSize = 11, FontWeight = FontWeights.Bold };
            Grid.SetColumn(_quotaStatusText, 1);
            qHeader.Children.Add(_quotaStatusText);
            qBox.Children.Add(qHeader);

            _quotaRemainingText = new TextBlock { Text = "Used: 0 GB of 100 GB (100 GB remaining)", FontSize = 11, Margin = new Thickness(0, 4, 0, 6) };
            qBox.Children.Add(_quotaRemainingText);

            _quotaProgressBg = new Border { Height = 8, CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Stretch };
            _quotaProgressFill = new Border { Height = 8, CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
            _quotaProgressBg.Child = _quotaProgressFill;
            qBox.Children.Add(_quotaProgressBg);

            _quotaCard.Child = qBox;
            quotaSp.Children.Add(_quotaCard);

            quotaCardOuter.Child = quotaSp;
            rightCol.Children.Add(quotaCardOuter);

            // ── Card 5: 💾 Storage & Data Management ──
            Border storageCard = CreateSettingsCard("💾 STORAGE & DATA MANAGEMENT");
            StackPanel storeSp = new StackPanel();

            _storageLocationText = new TextBlock {
                Text = "Storage: " + UsageStorage.Instance.StorageFilePath,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 0, 2)
            };
            _settingsLabels.Add(_storageLocationText);
            storeSp.Children.Add(_storageLocationText);

            _storageStatsText = new TextBlock {
                Text = "Recorded data is saved automatically every 20 seconds.",
                FontSize = 10.5,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _settingsSubLabels.Add(_storageStatsText);
            storeSp.Children.Add(_storageStatsText);

            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal };

            _openFolderBtn = new Button {
                Content = "📂 Open Storage Folder",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            _openFolderBtn.Click += delegate(object s, RoutedEventArgs e) {
                try {
                    string dir = Path.GetDirectoryName(UsageStorage.Instance.StorageFilePath);
                    if (Directory.Exists(dir)) {
                        Process.Start("explorer.exe", dir);
                    }
                } catch {}
            };
            btnRow.Children.Add(_openFolderBtn);

            _exportAllDataBtn = new Button {
                Content = "📁 Export All (CSV)",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            _exportAllDataBtn.Click += OnExportCsvClicked;
            btnRow.Children.Add(_exportAllDataBtn);

            _settingsClearDataBtn = new Button {
                Content = "🗑 Clear All Data",
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            _settingsClearDataBtn.Click += OnClearHistoryClicked;
            btnRow.Children.Add(_settingsClearDataBtn);

            storeSp.Children.Add(btnRow);

            storageCard.Child = storeSp;
            rightCol.Children.Add(storageCard);

            // ── Card 6: ℹ️ About NetPulse ──
            Border aboutCard = CreateSettingsCard("ℹ️ ABOUT NETPULSE");
            StackPanel abSp = new StackPanel();

            TextBlock appName = new TextBlock {
                Text = "⚡ NetPulse Monitor v2.1",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            _settingsLabels.Add(appName);
            abSp.Children.Add(appName);

            TextBlock appDetails = new TextBlock {
                Text = "Native 64-bit Windows Network & Data Usage Monitor.\nZero-dependency self-contained executable built on .NET Framework 4.8.\nMIT License — Open source and privacy friendly.",
                FontSize = 11,
                LineHeight = 16
            };
            _settingsSubLabels.Add(appDetails);
            abSp.Children.Add(appDetails);

            aboutCard.Child = abSp;
            rightCol.Children.Add(aboutCard);

            Grid.SetColumn(leftCol, 0);
            Grid.SetColumn(rightCol, 1);
            settingsGrid.Children.Add(leftCol);
            settingsGrid.Children.Add(rightCol);

            scroll.Content = settingsGrid;
            return scroll;
        }

        private Border CreateSettingsCard(string title) {
            Border card = new Border {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 0, 0, 14)
            };

            TextBlock titleBlock = new TextBlock {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _settingsCards.Add(card);
            _settingsCardTitles.Add(titleBlock);

            StackPanel sp = new StackPanel();
            sp.Children.Add(titleBlock);
            card.Child = sp;
            return card;
        }

        private TextBlock CreateSettingsOptionLabel(string title, string description) {
            TextBlock tb = new TextBlock {
                Text = title + " — " + description,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            _settingsLabels.Add(tb);
            return tb;
        }

        private CheckBox CreateSettingsCheckBox(string title, string description, Action<bool> onChange) {
            CheckBox cb = new CheckBox {
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            StackPanel sp = new StackPanel { Margin = new Thickness(4, -2, 0, 0) };
            TextBlock titleBlock = new TextBlock {
                Text = title,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold
            };
            TextBlock descBlock = new TextBlock {
                Text = description,
                FontSize = 10.5
            };

            _settingsLabels.Add(titleBlock);
            _settingsSubLabels.Add(descBlock);

            sp.Children.Add(titleBlock);
            sp.Children.Add(descBlock);
            cb.Content = sp;

            cb.Checked += delegate(object s, RoutedEventArgs e) { onChange(true); };
            cb.Unchecked += delegate(object s, RoutedEventArgs e) { onChange(false); };

            return cb;
        }

        private Button CreateThemeOptionButton(string label, string mode) {
            Button btn = new Button {
                Content = label,
                Tag = mode,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11.5
            };
            btn.Click += delegate(object s, RoutedEventArgs e) {
                AppSettings.Instance.ThemePreference = mode;
                AppSettings.Instance.Save();
                ThemeManager.SyncWithSettingsOrWindows();
                UpdateThemeButtonsHighlight();
            };
            return btn;
        }

        private void UpdateThemeButtonsHighlight() {
            if (_themeAutoBtn == null || _themeDarkBtn == null || _themeLightBtn == null) return;
            string pref = AppSettings.Instance.ThemePreference;
            StyleThemeOptionButton(_themeAutoBtn, pref == "System");
            StyleThemeOptionButton(_themeDarkBtn, pref == "Dark");
            StyleThemeOptionButton(_themeLightBtn, pref == "Light");
        }

        private void StyleThemeOptionButton(Button btn, bool isSelected) {
            if (isSelected) {
                btn.Background = Helpers.BrushFromHex(ThemeManager.TabActiveBg);
                btn.Foreground = Helpers.BrushFromHex(ThemeManager.TabActiveFg);
                btn.BorderBrush = Helpers.BrushFromHex(ThemeManager.TabActiveBorder);
                btn.FontWeight = FontWeights.Bold;
            } else {
                btn.Background = Helpers.BrushFromHex(ThemeManager.ButtonBg);
                btn.Foreground = Helpers.BrushFromHex(ThemeManager.ButtonFg);
                btn.BorderBrush = Helpers.BrushFromHex(ThemeManager.ButtonBorder);
                btn.FontWeight = FontWeights.Normal;
            }
        }

        private void RefreshSettingsView() {
            AppSettings settings = AppSettings.Instance;

            _runStartupCheck.IsChecked = settings.RunOnStartup;
            _startMinimizedCheck.IsChecked = settings.StartMinimized;
            _closeToTrayCheck.IsChecked = settings.CloseToTray;
            _alwaysOnTopCheck.IsChecked = settings.AlwaysOnTop;

            if (settings.PollingIntervalSeconds <= 0.6) _pollingRateCombo.SelectedIndex = 0;
            else if (settings.PollingIntervalSeconds >= 1.8) _pollingRateCombo.SelectedIndex = 2;
            else _pollingRateCombo.SelectedIndex = 1;

            if (settings.SpeedUnitMode == "Bits") _unitModeCombo.SelectedIndex = 1;
            else if (settings.SpeedUnitMode == "BytesOnly") _unitModeCombo.SelectedIndex = 2;
            else _unitModeCombo.SelectedIndex = 0;

            _hideDisconnectedCheck.IsChecked = settings.HideDisconnectedAdapters;

            if (settings.ThemePreference == "Dark") _themeModeCombo.SelectedIndex = 1;
            else if (settings.ThemePreference == "Light") _themeModeCombo.SelectedIndex = 2;
            else _themeModeCombo.SelectedIndex = 0;

            UpdateThemeButtonsHighlight();

            _enableQuotaCheck.IsChecked = settings.EnableMonthlyQuota;
            _quotaLimitBox.Text = settings.MonthlyQuotaGB.ToString("0.#");
            _quotaResetDayBox.Text = settings.QuotaResetDay.ToString();

            // Refresh storage stats
            try {
                if (File.Exists(UsageStorage.Instance.StorageFilePath)) {
                    FileInfo fi = new FileInfo(UsageStorage.Instance.StorageFilePath);
                    _storageStatsText.Text = string.Format("Storage file size: {0} • Auto-save active", Helpers.FormatBytes(fi.Length));
                }
            } catch {}

            RefreshQuotaVisualizer();
        }

        private void RefreshQuotaVisualizer() {
            AppSettings settings = AppSettings.Instance;
            PeriodUsageSummary monthSummary = UsageStorage.Instance.GetSummary(TimeRangePreset.ThisMonth);

            double limitBytes = settings.MonthlyQuotaGB * 1024L * 1024L * 1024L;
            double usedBytes = monthSummary.TotalBytes;
            double pct = (limitBytes > 0) ? (usedBytes / limitBytes * 100.0) : 0;
            double remainingBytes = Math.Max(0, limitBytes - usedBytes);

            _quotaStatusText.Text = string.Format("{0:0.#}% Used", pct);
            _quotaRemainingText.Text = string.Format("Used: {0} of {1} GB ({2} remaining)",
                Helpers.FormatBytes(usedBytes),
                settings.MonthlyQuotaGB.ToString("0.#"),
                Helpers.FormatBytes(remainingBytes));

            double barW = Math.Max(0, Math.Min(320, (pct / 100.0) * 320));
            _quotaProgressFill.Width = barW;

            if (pct >= 90) {
                _quotaProgressFill.Background = Helpers.BrushFromHex(ThemeManager.UploadColor);
                _quotaStatusText.Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor);
            } else if (pct >= 75) {
                _quotaProgressFill.Background = Helpers.BrushFromHex(ThemeManager.TotalColor);
                _quotaStatusText.Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor);
            } else {
                _quotaProgressFill.Background = Helpers.BrushFromHex(ThemeManager.DownloadColor);
                _quotaStatusText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            }
        }

        // ════════════════════════════════════════════════════════
        // ── HISTORY DATA REFRESH & EVENTS ──
        // ════════════════════════════════════════════════════════
        private void RefreshHistoryView() {
            PeriodUsageSummary summary = UsageStorage.Instance.GetSummary(_currentHistoryPreset);

            _historyRangeBadgeText.Text = "📅 " + summary.DateRangeLabel;

            _hTotalValText.Text = Helpers.FormatBytes(summary.TotalBytes);
            _hTotalSubText.Text = string.Format("{0} total days in period", summary.DailyRecords.Count);

            _hDlValText.Text = Helpers.FormatBytes(summary.TotalDownloaded);
            double dlShare = (summary.TotalBytes > 0) ? ((double)summary.TotalDownloaded / summary.TotalBytes * 100.0) : 0;
            _hDlSubText.Text = string.Format("{0:0.#}% of total data", dlShare);

            _hUlValText.Text = Helpers.FormatBytes(summary.TotalUploaded);
            double ulShare = (summary.TotalBytes > 0) ? ((double)summary.TotalUploaded / summary.TotalBytes * 100.0) : 0;
            _hUlSubText.Text = string.Format("{0:0.#}% of total data", ulShare);

            _hAvgValText.Text = Helpers.FormatBytes(summary.DailyAverageBytes) + "/day";
            if (!string.IsNullOrEmpty(summary.PeakDayDate)) {
                DateTime pdt;
                string pDate = DateTime.TryParse(summary.PeakDayDate, out pdt) ? pdt.ToString("MMM dd") : summary.PeakDayDate;
                _hAvgSubText.Text = string.Format("Peak: {0} ({1})", Helpers.FormatBytes(summary.PeakDayBytes), pDate);
            } else {
                _hAvgSubText.Text = "Peak: 0 B";
            }

            _historyBarChart.SetData(summary);

            long totalAll = summary.TotalBytes;
            double wifiPct = (totalAll > 0) ? ((double)summary.WifiTotal / totalAll * 100.0) : 0;
            double ethPct = (totalAll > 0) ? ((double)summary.EthernetTotal / totalAll * 100.0) : 0;

            _wifiTotalText.Text = Helpers.FormatBytes(summary.WifiTotal);
            _wifiSubText.Text = string.Format("↓ {0}  •  ↑ {1} ({2:0.#}%)",
                Helpers.FormatBytes(summary.WifiDownloaded),
                Helpers.FormatBytes(summary.WifiUploaded),
                wifiPct);
            _wifiProgressFill.Width = Math.Max(0, Math.Min(300, (wifiPct / 100.0) * 300));

            _ethTotalText.Text = Helpers.FormatBytes(summary.EthernetTotal);
            _ethSubText.Text = string.Format("↓ {0}  •  ↑ {1} ({2:0.#}%)",
                Helpers.FormatBytes(summary.EthernetDownloaded),
                Helpers.FormatBytes(summary.EthernetUploaded),
                ethPct);
            _ethProgressFill.Width = Math.Max(0, Math.Min(300, (ethPct / 100.0) * 300));

            // Populate Wi-Fi Networks Breakdown
            if (_wifiNetworksListPanel != null) {
                _wifiNetworksListPanel.Children.Clear();

                string currentSsid = WifiNetworkHelper.GetCurrentWifiSsid();

                if (summary.WifiNetworkBreakdown == null || summary.WifiNetworkBreakdown.Count == 0 || summary.WifiTotal == 0) {
                    _wifiNetworksCountBadgeText.Text = "0 Networks";
                    TextBlock emptyText = new TextBlock {
                        Text = "No Wi-Fi networks recorded in this period.",
                        FontSize = 11,
                        Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted),
                        Margin = new Thickness(4, 6, 4, 4)
                    };
                    _wifiNetworksListPanel.Children.Add(emptyText);
                } else {
                    int count = summary.WifiNetworkBreakdown.Count;
                    _wifiNetworksCountBadgeText.Text = (count == 1) ? "1 Network" : string.Format("{0} Networks", count);

                    long maxWifiBytes = 1;
                    for (int i = 0; i < count; i++) {
                        if (summary.WifiNetworkBreakdown[i].TotalBytes > maxWifiBytes) {
                            maxWifiBytes = summary.WifiNetworkBreakdown[i].TotalBytes;
                        }
                    }

                    for (int i = 0; i < count; i++) {
                        WifiNetworkUsageRecord net = summary.WifiNetworkBreakdown[i];
                        bool isCurrentConnected = (!string.IsNullOrEmpty(currentSsid) && string.Equals(net.Ssid, currentSsid, StringComparison.OrdinalIgnoreCase));

                        Border itemCard = new Border {
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(10, 8, 10, 8),
                            Margin = new Thickness(0, 0, 0, 6),
                            Background = Helpers.BrushFromHex(ThemeManager.SubCardBg),
                            BorderThickness = new Thickness(isCurrentConnected ? 1 : 0),
                            BorderBrush = Helpers.BrushFromHex(isCurrentConnected ? ThemeManager.StatusConnected : ThemeManager.CardBorder)
                        };

                        StackPanel itemSp = new StackPanel();

                        // Top line: SSID Name (and optional [Active] badge) + Total Bytes
                        Grid topLine = new Grid();
                        topLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        topLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        StackPanel nameSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                        TextBlock ssidIcon = new TextBlock {
                            Text = "📶",
                            FontSize = 11,
                            Margin = new Thickness(0, 0, 6, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        TextBlock ssidName = new TextBlock {
                            Text = net.Ssid,
                            FontWeight = FontWeights.Bold,
                            FontSize = 12,
                            Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary),
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            MaxWidth = 180
                        };
                        nameSp.Children.Add(ssidIcon);
                        nameSp.Children.Add(ssidName);

                        if (isCurrentConnected) {
                            Border connBadge = new Border {
                                CornerRadius = new CornerRadius(6),
                                Background = Helpers.BrushFromHex(ThemeManager.BadgeBg),
                                Padding = new Thickness(5, 1, 5, 1),
                                Margin = new Thickness(6, 0, 0, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            TextBlock connText = new TextBlock {
                                Text = "Active",
                                FontSize = 9.5,
                                FontWeight = FontWeights.Bold,
                                Foreground = Helpers.BrushFromHex(ThemeManager.StatusConnected)
                            };
                            connBadge.Child = connText;
                            nameSp.Children.Add(connBadge);
                        }

                        Grid.SetColumn(nameSp, 0);
                        topLine.Children.Add(nameSp);

                        TextBlock totalBytesText = new TextBlock {
                            Text = Helpers.FormatBytes(net.TotalBytes),
                            FontWeight = FontWeights.Bold,
                            FontSize = 12,
                            Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(totalBytesText, 1);
                        topLine.Children.Add(totalBytesText);
                        itemSp.Children.Add(topLine);

                        // Sub line: Download / Upload breakdown & Share of Wi-Fi
                        double sharePct = (summary.WifiTotal > 0) ? ((double)net.TotalBytes / summary.WifiTotal * 100.0) : 0;
                        Grid subLine = new Grid { Margin = new Thickness(0, 3, 0, 5) };
                        subLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        subLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        TextBlock dlUlText = new TextBlock {
                            Text = string.Format("↓ {0}  •  ↑ {1}", Helpers.FormatBytes(net.BytesDownloaded), Helpers.FormatBytes(net.BytesUploaded)),
                            FontSize = 10,
                            Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary)
                        };
                        Grid.SetColumn(dlUlText, 0);
                        subLine.Children.Add(dlUlText);

                        TextBlock shareText = new TextBlock {
                            Text = string.Format("{0:0.#}% of Wi-Fi", sharePct),
                            FontSize = 10,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted)
                        };
                        Grid.SetColumn(shareText, 1);
                        subLine.Children.Add(shareText);
                        itemSp.Children.Add(subLine);

                        // Progress bar
                        Border barBg = new Border {
                            Height = 4,
                            CornerRadius = new CornerRadius(2),
                            Background = Helpers.BrushFromHex(ThemeManager.CardBorder),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        double relPct = (maxWifiBytes > 0) ? ((double)net.TotalBytes / maxWifiBytes) : 0;
                        double barWidth = Math.Max(2, relPct * 320);

                        Border barFill = new Border {
                            Height = 4,
                            CornerRadius = new CornerRadius(2),
                            Background = Helpers.BrushFromHex(ThemeManager.WifiBadgeBorder),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Width = barWidth
                        };
                        barBg.Child = barFill;
                        itemSp.Children.Add(barBg);

                        itemCard.Child = itemSp;
                        _wifiNetworksListPanel.Children.Add(itemCard);
                    }
                }
            }

            _tableRowsPanel.Children.Clear();

            Grid thGrid = new Grid { Margin = new Thickness(6, 0, 6, 6) };
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock h1 = new TextBlock { Text = "DATE", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted) };
            TextBlock h2 = new TextBlock { Text = "DOWNLOAD", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted) };
            TextBlock h3 = new TextBlock { Text = "UPLOAD", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted) };
            TextBlock h4 = new TextBlock { Text = "TOTAL", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted), HorizontalAlignment = HorizontalAlignment.Right };

            Grid.SetColumn(h1, 0); thGrid.Children.Add(h1);
            Grid.SetColumn(h2, 1); thGrid.Children.Add(h2);
            Grid.SetColumn(h3, 2); thGrid.Children.Add(h3);
            Grid.SetColumn(h4, 3); thGrid.Children.Add(h4);
            _tableRowsPanel.Children.Add(thGrid);

            for (int i = summary.DailyRecords.Count - 1; i >= 0; i--) {
                DayUsageRecord day = summary.DailyRecords[i];

                Border rowBorder = new Border {
                    Padding = new Thickness(6, 6, 6, 6),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(0, 0, 0, 4),
                    Background = Helpers.BrushFromHex((i % 2 == 0) ? ThemeManager.SubCardBg : "Transparent")
                };

                Grid rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                DateTime dt;
                string dateStr = DateTime.TryParse(day.Date, out dt) ? dt.ToString("yyyy-MM-dd (ddd)") : day.Date;

                TextBlock c1 = new TextBlock { Text = dateStr, FontSize = 11, Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary) };
                TextBlock c2 = new TextBlock { Text = Helpers.FormatBytes(day.TotalDownloaded), FontSize = 11, Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor) };
                TextBlock c3 = new TextBlock { Text = Helpers.FormatBytes(day.TotalUploaded), FontSize = 11, Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor) };
                TextBlock c4 = new TextBlock { Text = Helpers.FormatBytes(day.TotalBytes), FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor), HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetColumn(c1, 0); rowGrid.Children.Add(c1);
                Grid.SetColumn(c2, 1); rowGrid.Children.Add(c2);
                Grid.SetColumn(c3, 2); rowGrid.Children.Add(c3);
                Grid.SetColumn(c4, 3); rowGrid.Children.Add(c4);

                rowBorder.Child = rowGrid;
                _tableRowsPanel.Children.Add(rowBorder);
            }
        }

        private void OnExportCsvClicked(object sender, RoutedEventArgs e) {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = string.Format("NetPulse_Usage_{0}_{1}.csv", _currentHistoryPreset, DateTime.Now.ToString("yyyyMMdd"));
            dlg.Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*";
            if (dlg.ShowDialog() == true) {
                if (UsageStorage.Instance.ExportToCsv(dlg.FileName, _currentHistoryPreset)) {
                    MessageBox.Show("Usage report exported successfully!\nSaved to:\n" + dlg.FileName,
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show("Could not export CSV file. Please check permissions.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnDemoDataClicked(object sender, RoutedEventArgs e) {
            MessageBoxResult res = MessageBox.Show(
                "Populate 45 days of realistic sample network data to preview Last Week, Last Month, and analytics charts?",
                "Generate Sample History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes) {
                UsageStorage.Instance.GenerateSampleData();
                RefreshHistoryView();
                RefreshQuotaVisualizer();
                MessageBox.Show("Sample history generated! Explore 'Last Week', 'Last Month', and 'All Time' filters to see the data.", "Sample Data Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnClearHistoryClicked(object sender, RoutedEventArgs e) {
            MessageBoxResult res = MessageBox.Show(
                "Are you sure you want to clear all persistent usage history? This cannot be undone.",
                "Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes) {
                UsageStorage.Instance.ClearHistory();
                RefreshHistoryView();
                RefreshQuotaVisualizer();
            }
        }

        // ════════════════════════════════════════════════════════
        // ── TIMER TICK (INTERVAL POLLING) ──
        // ════════════════════════════════════════════════════════
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

            // Update quick history teaser
            DayUsageRecord todayRec = UsageStorage.Instance.GetTodayRecord();
            PeriodUsageSummary weekSummary = UsageStorage.Instance.GetSummary(TimeRangePreset.Last7Days);
            PeriodUsageSummary monthSummary = UsageStorage.Instance.GetSummary(TimeRangePreset.ThisMonth);

            string curSsid = WifiNetworkHelper.GetCurrentWifiSsid();
            string wifiTeaser = !string.IsNullOrEmpty(curSsid) ? (" (📶 " + curSsid + ")") : "";

            _liveHistoryTeaserText.Text = string.Format("Today: {0}{1}  •  Last 7d: {2}  •  This Month: {3}",
                Helpers.FormatBytes(todayRec.TotalBytes),
                wifiTeaser,
                Helpers.FormatBytes(weekSummary.TotalBytes),
                Helpers.FormatBytes(monthSummary.TotalBytes));

            // Update Header items
            _uptimeText.Text = "⏱ " + Helpers.FormatDuration(_monitor.SessionUptime);

            bool hideDisconnected = AppSettings.Instance.HideDisconnectedAdapters;

            foreach (var iface in _monitor.Interfaces) {
                InterfaceCardControl card;
                if (!_cardControls.TryGetValue(iface.Id, out card)) {
                    card = new InterfaceCardControl(iface);
                    card.RequestNavigateToHistory += delegate { SwitchTab(1); };
                    _cardControls[iface.Id] = card;
                    _interfaceCardsPanel.Children.Add(card);
                }

                if (hideDisconnected && !iface.IsConnected) {
                    card.Visibility = Visibility.Collapsed;
                } else {
                    card.Visibility = Visibility.Visible;
                }
                card.UpdateData();
            }

            // Update Chart
            _mainChart.SetData(g.DownloadHistory, g.UploadHistory);
            _chartDlSpeedLegend.Text = "Download: " + Helpers.FormatSpeed(g.TotalDownloadSpeed);
            _chartUlSpeedLegend.Text = "Upload: " + Helpers.FormatSpeed(g.TotalUploadSpeed);

            // Refresh history/settings live stats periodically
            if (_currentTab == 1 && ((int)_monitor.SessionUptime.TotalSeconds % 5 == 0)) {
                RefreshHistoryView();
            } else if (_currentTab == 2 && ((int)_monitor.SessionUptime.TotalSeconds % 5 == 0)) {
                RefreshQuotaVisualizer();
            }
        }
    }
}
