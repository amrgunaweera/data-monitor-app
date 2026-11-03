using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NetPulse.Controls {
    public class InterfaceCardControl : Border {
        private readonly InterfaceInfo _info;

        private TextBlock _titleBlock;
        private Border _wifiSsidBadge;
        private TextBlock _wifiSsidBadgeText;
        private TextBlock _descBlock;
        private Border _iconBadge;
        private Border _statusPill;
        private Border _statusDot;
        private TextBlock _statusText;

        private Border _detailsBorder;
        private readonly List<TextBlock> _detailLabels = new List<TextBlock>();
        private readonly List<TextBlock> _detailValues = new List<TextBlock>();

        private readonly List<TextBlock> _metricLabels = new List<TextBlock>();
        private TextBlock _dlSpeedText;
        private TextBlock _ulSpeedText;
        private TextBlock _dlTotalText;
        private TextBlock _ulTotalText;

        private TextBlock _ssidValText;
        private TextBlock _ssidUsageValText;
        private TextBlock _ipText;
        private TextBlock _macText;
        private TextBlock _speedText;
        private TextBlock _todayUsageText;

        private RealtimeChart _miniChart;

        // Wi-Fi breakdown section (Live page)
        private Border _wifiBreakdownBorder;
        private TextBlock _wifiBreakdownTitle;
        private TextBlock _wifiBreakdownCountBadge;
        private StackPanel _wifiBreakdownListPanel;
        private Button _viewHistoryLinkBtn;

        public event Action RequestNavigateToHistory;

        public string InterfaceId {
            get { return _info.Id; }
        }

        public InterfaceCardControl(InterfaceInfo info) {
            _info = info;

            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(14);
            Padding = new Thickness(18);
            Margin = new Thickness(0, 0, 0, 16);

            BuildUI();
            ApplyTheme();

            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged() {
            ApplyTheme();
        }

        public void ApplyTheme() {
            Background = Helpers.BrushFromHex(ThemeManager.CardBg);
            BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);

            bool isWifi = _info.InterfaceType == "Wi-Fi";
            _iconBadge.Background = Helpers.BrushFromHex(isWifi ? ThemeManager.WifiBadgeBg : ThemeManager.EthBadgeBg);
            _iconBadge.BorderBrush = Helpers.BrushFromHex(isWifi ? ThemeManager.WifiBadgeBorder : ThemeManager.EthBadgeBorder);

            _titleBlock.Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary);
            _descBlock.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);

            if (_wifiSsidBadge != null) {
                _wifiSsidBadge.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
                _wifiSsidBadge.BorderBrush = Helpers.BrushFromHex(ThemeManager.WifiBadgeBorder);
                _wifiSsidBadgeText.Foreground = Helpers.BrushFromHex(_info.IsConnected ? ThemeManager.StatusConnected : ThemeManager.TextSecondary);
            }

            _statusPill.Background = Helpers.BrushFromHex(ThemeManager.BadgeBg);
            _statusDot.Background = Helpers.BrushFromHex(_info.IsConnected ? ThemeManager.StatusConnected : ThemeManager.StatusDisconnected);
            _statusText.Foreground = Helpers.BrushFromHex(_info.IsConnected ? ThemeManager.StatusConnected : ThemeManager.TextSecondary);

            _detailsBorder.Background = Helpers.BrushFromHex(ThemeManager.SubCardBg);

            for (int i = 0; i < _detailLabels.Count; i++) {
                _detailLabels[i].Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
            }
            for (int i = 0; i < _detailValues.Count; i++) {
                _detailValues[i].Foreground = Helpers.BrushFromHex(ThemeManager.TextDetailVal);
            }

            for (int i = 0; i < _metricLabels.Count; i++) {
                _metricLabels[i].Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
            }

            _dlSpeedText.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            _ulSpeedText.Foreground = Helpers.BrushFromHex(ThemeManager.UploadColor);
            _dlTotalText.Foreground = Helpers.BrushFromHex(ThemeManager.TextDetailVal);
            _ulTotalText.Foreground = Helpers.BrushFromHex(ThemeManager.TextDetailVal);

            if (_wifiBreakdownBorder != null) {
                _wifiBreakdownBorder.Background = Helpers.BrushFromHex(ThemeManager.SubCardBg);
                _wifiBreakdownBorder.BorderBrush = Helpers.BrushFromHex(ThemeManager.CardBorder);
                _wifiBreakdownTitle.Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted);
                _wifiBreakdownCountBadge.Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary);
                _viewHistoryLinkBtn.Foreground = Helpers.BrushFromHex(ThemeManager.DownloadColor);
            }
        }

        private void BuildUI() {
            bool isWifi = _info.InterfaceType == "Wi-Fi";

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Details row
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Speed + totals
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) }); // 3: Mini chart
            if (isWifi) {
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Wi-Fi networks breakdown
            }

            // ── Row 0: Header ──
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Name & desc
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Status pill

            _iconBadge = new Border {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 12, 0)
            };
            TextBlock iconLabel = new TextBlock {
                Text = isWifi ? "📶" : "🔌",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _iconBadge.Child = iconLabel;
            Grid.SetColumn(_iconBadge, 0);
            header.Children.Add(_iconBadge);

            StackPanel namePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            StackPanel titleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _titleBlock = new TextBlock {
                Text = isWifi ? "Wi-Fi" : _info.Name + " (" + _info.InterfaceType + ")",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleRow.Children.Add(_titleBlock);

            if (isWifi) {
                _wifiSsidBadge = new Border {
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed
                };
                _wifiSsidBadgeText = new TextBlock {
                    Text = "📶 Connecting...",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                };
                _wifiSsidBadge.Child = _wifiSsidBadgeText;
                titleRow.Children.Add(_wifiSsidBadge);
            }

            _descBlock = new TextBlock {
                Text = _info.Description,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 450,
                Margin = new Thickness(0, 2, 0, 0)
            };

            namePanel.Children.Add(titleRow);
            namePanel.Children.Add(_descBlock);
            Grid.SetColumn(namePanel, 1);
            header.Children.Add(namePanel);

            _statusPill = new Border {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 12, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            StackPanel statusPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _statusDot = new Border {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusText = new TextBlock {
                Text = _info.IsConnected ? "Connected" : "Disconnected",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusPanel.Children.Add(_statusDot);
            statusPanel.Children.Add(_statusText);
            _statusPill.Child = statusPanel;
            Grid.SetColumn(_statusPill, 2);
            header.Children.Add(_statusPill);

            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Row 1: Details (4 Columns) ──
            _detailsBorder = new Border {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 12, 0, 10)
            };
            Grid detailsGrid = new Grid();
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            if (isWifi) {
                _ssidValText = CreateDetailItem(detailsGrid, 0, "CONNECTED SSID", !string.IsNullOrEmpty(_info.ConnectedNetworkName) ? _info.ConnectedNetworkName : "Not Connected");
                _ssidUsageValText = CreateDetailItem(detailsGrid, 1, "TODAY ON THIS SSID", "0 B");
                _ipText = CreateDetailItem(detailsGrid, 2, "IPv4 ADDRESS", _info.IPv4Address);
                _speedText = CreateDetailItem(detailsGrid, 3, "LINK SPEED", _info.LinkSpeed);
            } else {
                _ipText = CreateDetailItem(detailsGrid, 0, "IPv4 ADDRESS", _info.IPv4Address);
                _macText = CreateDetailItem(detailsGrid, 1, "MAC ADDRESS", _info.MacAddress);
                _speedText = CreateDetailItem(detailsGrid, 2, "LINK SPEED", _info.LinkSpeed);
                _todayUsageText = CreateDetailItem(detailsGrid, 3, "TODAY'S USAGE", "0 B");
            }

            _detailsBorder.Child = detailsGrid;
            Grid.SetRow(_detailsBorder, 1);
            root.Children.Add(_detailsBorder);

            // ── Row 2: Live Speeds & Totals ──
            Grid statsGrid = new Grid { Margin = new Thickness(0, 2, 0, 8) };
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _dlSpeedText = CreateMetricItem(statsGrid, 0, "↓ DOWNLOAD SPEED", "0 B/s");
            _ulSpeedText = CreateMetricItem(statsGrid, 1, "↑ UPLOAD SPEED", "0 B/s");
            _dlTotalText = CreateMetricItem(statsGrid, 2, "SESSION RECEIVED", "0 B");
            _ulTotalText = CreateMetricItem(statsGrid, 3, "SESSION SENT", "0 B");

            Grid.SetRow(statsGrid, 2);
            root.Children.Add(statsGrid);

            // ── Row 3: Mini Realtime Chart ──
            _miniChart = new RealtimeChart {
                IsMini = true,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Grid.SetRow(_miniChart, 3);
            root.Children.Add(_miniChart);

            // ── Row 4: Wi-Fi Networks Breakdown (Today) ──
            if (isWifi) {
                _wifiBreakdownBorder = new Border {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Grid wbInner = new Grid();
                wbInner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                wbInner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid wbHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                wbHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                wbHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                _wifiBreakdownTitle = new TextBlock {
                    Text = "📶 WI-FI NETWORKS USAGE (TODAY)",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(_wifiBreakdownTitle, 0);
                wbHeader.Children.Add(_wifiBreakdownTitle);

                StackPanel wbRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                _wifiBreakdownCountBadge = new TextBlock {
                    Text = "1 Network",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                wbRight.Children.Add(_wifiBreakdownCountBadge);

                _viewHistoryLinkBtn = new Button {
                    Content = "View Full History →",
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Padding = new Thickness(0)
                };
                _viewHistoryLinkBtn.Click += delegate(object s, RoutedEventArgs e) {
                    if (RequestNavigateToHistory != null) {
                        RequestNavigateToHistory();
                    }
                };
                wbRight.Children.Add(_viewHistoryLinkBtn);

                Grid.SetColumn(wbRight, 1);
                wbHeader.Children.Add(wbRight);
                Grid.SetRow(wbHeader, 0);
                wbInner.Children.Add(wbHeader);

                _wifiBreakdownListPanel = new StackPanel();
                Grid.SetRow(_wifiBreakdownListPanel, 1);
                wbInner.Children.Add(_wifiBreakdownListPanel);

                _wifiBreakdownBorder.Child = wbInner;
                Grid.SetRow(_wifiBreakdownBorder, 4);
                root.Children.Add(_wifiBreakdownBorder);
            }

            Child = root;
        }

        private TextBlock CreateDetailItem(Grid grid, int col, string label, string value) {
            StackPanel sp = new StackPanel();
            TextBlock lbl = new TextBlock {
                Text = label,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            TextBlock val = new TextBlock {
                Text = value,
                FontSize = 11.5,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _detailLabels.Add(lbl);
            _detailValues.Add(val);
            sp.Children.Add(lbl);
            sp.Children.Add(val);
            Grid.SetColumn(sp, col);
            grid.Children.Add(sp);
            return val;
        }

        private TextBlock CreateMetricItem(Grid grid, int col, string label, string value) {
            StackPanel sp = new StackPanel();
            TextBlock lbl = new TextBlock {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            TextBlock val = new TextBlock {
                Text = value,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            _metricLabels.Add(lbl);
            sp.Children.Add(lbl);
            sp.Children.Add(val);
            Grid.SetColumn(sp, col);
            grid.Children.Add(sp);
            return val;
        }

        public void UpdateData() {
            bool isWifi = _info.InterfaceType == "Wi-Fi";

            // Update status
            _statusDot.Background = Helpers.BrushFromHex(_info.IsConnected ? ThemeManager.StatusConnected : ThemeManager.StatusDisconnected);
            _statusText.Text = _info.IsConnected ? "Connected" : "Disconnected";
            _statusText.Foreground = Helpers.BrushFromHex(_info.IsConnected ? ThemeManager.StatusConnected : ThemeManager.TextSecondary);

            // Fetch current Wi-Fi SSID
            string currentSsid = isWifi ? (_info.ConnectedNetworkName ?? WifiNetworkHelper.GetCurrentWifiSsid()) : null;

            if (isWifi) {
                if (_info.IsConnected && !string.IsNullOrEmpty(currentSsid)) {
                    _wifiSsidBadge.Visibility = Visibility.Visible;
                    _wifiSsidBadgeText.Text = "📶 " + currentSsid;
                    _wifiSsidBadgeText.Foreground = Helpers.BrushFromHex(ThemeManager.StatusConnected);
                    _descBlock.Text = _info.Description;
                    if (_ssidValText != null) _ssidValText.Text = currentSsid;
                } else {
                    _wifiSsidBadge.Visibility = Visibility.Collapsed;
                    _descBlock.Text = _info.Description + (_info.IsConnected ? "" : " (Disconnected)");
                    if (_ssidValText != null) _ssidValText.Text = _info.IsConnected ? "Wi-Fi Connected" : "Not Connected";
                }
            } else {
                _descBlock.Text = _info.Description;
            }

            // Update general details
            if (_ipText != null) _ipText.Text = _info.IPv4Address;
            if (_macText != null) _macText.Text = _info.MacAddress;
            if (_speedText != null) _speedText.Text = _info.LinkSpeed;

            // Update speeds
            _dlSpeedText.Text = Helpers.FormatSpeed(_info.CurrentDownloadSpeed);
            _ulSpeedText.Text = Helpers.FormatSpeed(_info.CurrentUploadSpeed);

            // Update totals
            _dlTotalText.Text = Helpers.FormatBytes(_info.SessionDownloaded);
            _ulTotalText.Text = Helpers.FormatBytes(_info.SessionUploaded);

            // Update mini chart
            _miniChart.SetData(_info.DownloadHistory, _info.UploadHistory);

            // ── Update Today Usage & Wi-Fi Breakdown ──
            DayUsageRecord today = UsageStorage.Instance.GetTodayRecord();

            if (isWifi) {
                // Today on current SSID
                long currentSsidTodayTotal = 0;
                if (!string.IsNullOrEmpty(currentSsid) && today.WifiNetworks != null) {
                    for (int i = 0; i < today.WifiNetworks.Count; i++) {
                        if (string.Equals(today.WifiNetworks[i].Ssid, currentSsid, StringComparison.OrdinalIgnoreCase)) {
                            currentSsidTodayTotal = today.WifiNetworks[i].TotalBytes;
                            break;
                        }
                    }
                }
                if (_ssidUsageValText != null) {
                    _ssidUsageValText.Text = (currentSsidTodayTotal > 0) ? Helpers.FormatBytes(currentSsidTodayTotal) : Helpers.FormatBytes(_info.SessionDownloaded + _info.SessionUploaded);
                }

                // Wi-Fi breakdown list panel
                UpdateWifiBreakdownPanel(today, currentSsid);
            } else {
                if (_todayUsageText != null) {
                    long ethTotal = 0;
                    if (today.Interfaces != null) {
                        for (int i = 0; i < today.Interfaces.Count; i++) {
                            if (today.Interfaces[i].InterfaceId == _info.Id || today.Interfaces[i].InterfaceType == "Ethernet") {
                                ethTotal += today.Interfaces[i].TotalBytes;
                            }
                        }
                    }
                    _todayUsageText.Text = (ethTotal > 0) ? Helpers.FormatBytes(ethTotal) : Helpers.FormatBytes(_info.SessionDownloaded + _info.SessionUploaded);
                }
            }
        }

        private void UpdateWifiBreakdownPanel(DayUsageRecord today, string currentSsid) {
            if (_wifiBreakdownListPanel == null) return;

            _wifiBreakdownListPanel.Children.Clear();

            List<WifiNetworkUsageRecord> networks = new List<WifiNetworkUsageRecord>();
            if (today.WifiNetworks != null && today.WifiNetworks.Count > 0) {
                networks.AddRange(today.WifiNetworks);
            }

            // If current connected SSID is not yet recorded in today's disk log, add a live placeholder entry
            if (!string.IsNullOrEmpty(currentSsid)) {
                bool found = false;
                for (int i = 0; i < networks.Count; i++) {
                    if (string.Equals(networks[i].Ssid, currentSsid, StringComparison.OrdinalIgnoreCase)) {
                        found = true;
                        break;
                    }
                }
                if (!found && (_info.SessionDownloaded > 0 || _info.SessionUploaded > 0)) {
                    networks.Add(new WifiNetworkUsageRecord {
                        Ssid = currentSsid,
                        BytesDownloaded = (long)_info.SessionDownloaded,
                        BytesUploaded = (long)_info.SessionUploaded
                    });
                }
            }

            if (networks.Count == 0) {
                _wifiBreakdownCountBadge.Text = _info.IsConnected ? "Active" : "0 Networks";
                TextBlock empty = new TextBlock {
                    Text = _info.IsConnected
                        ? ("Connected to " + (currentSsid ?? "Wi-Fi") + " • Actively tracking network traffic")
                        : "No Wi-Fi networks recorded today.",
                    FontSize = 10.5,
                    Foreground = Helpers.BrushFromHex(ThemeManager.TextMuted),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                _wifiBreakdownListPanel.Children.Add(empty);
                return;
            }

            // Sort descending by TotalBytes
            networks.Sort(delegate(WifiNetworkUsageRecord a, WifiNetworkUsageRecord b) {
                return b.TotalBytes.CompareTo(a.TotalBytes);
            });

            _wifiBreakdownCountBadge.Text = (networks.Count == 1) ? "1 Network" : string.Format("{0} Networks", networks.Count);

            long totalWifiToday = 0;
            long maxBytes = 1;
            for (int i = 0; i < networks.Count; i++) {
                totalWifiToday += networks[i].TotalBytes;
                if (networks[i].TotalBytes > maxBytes) {
                    maxBytes = networks[i].TotalBytes;
                }
            }

            for (int i = 0; i < networks.Count; i++) {
                WifiNetworkUsageRecord net = networks[i];
                bool isCurrent = (!string.IsNullOrEmpty(currentSsid) && string.Equals(net.Ssid, currentSsid, StringComparison.OrdinalIgnoreCase));

                Border rowBorder = new Border {
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 4),
                    CornerRadius = new CornerRadius(6),
                    Background = Helpers.BrushFromHex(isCurrent ? ThemeManager.CardBg : "Transparent"),
                    BorderThickness = new Thickness(isCurrent ? 1 : 0),
                    BorderBrush = Helpers.BrushFromHex(isCurrent ? ThemeManager.WifiBadgeBorder : "Transparent")
                };

                StackPanel rowSp = new StackPanel();

                Grid lineGrid = new Grid();
                lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel nameSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                TextBlock ssidIcon = new TextBlock {
                    Text = "📶",
                    FontSize = 10.5,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock ssidName = new TextBlock {
                    Text = net.Ssid,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = Helpers.BrushFromHex(ThemeManager.TextPrimary),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 220
                };
                nameSp.Children.Add(ssidIcon);
                nameSp.Children.Add(ssidName);

                if (isCurrent) {
                    Border activePill = new Border {
                        CornerRadius = new CornerRadius(5),
                        Background = Helpers.BrushFromHex(ThemeManager.BadgeBg),
                        Padding = new Thickness(5, 1, 5, 1),
                        Margin = new Thickness(6, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    TextBlock activeText = new TextBlock {
                        Text = "Active",
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = Helpers.BrushFromHex(ThemeManager.StatusConnected)
                    };
                    activePill.Child = activeText;
                    nameSp.Children.Add(activePill);
                }

                Grid.SetColumn(nameSp, 0);
                lineGrid.Children.Add(nameSp);

                StackPanel valSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                TextBlock dlUlText = new TextBlock {
                    Text = string.Format("↓ {0}  •  ↑ {1}", Helpers.FormatBytes(net.BytesDownloaded), Helpers.FormatBytes(net.BytesUploaded)),
                    FontSize = 10,
                    Foreground = Helpers.BrushFromHex(ThemeManager.TextSecondary),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                TextBlock totalText = new TextBlock {
                    Text = Helpers.FormatBytes(net.TotalBytes),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11.5,
                    Foreground = Helpers.BrushFromHex(ThemeManager.TotalColor)
                };
                valSp.Children.Add(dlUlText);
                valSp.Children.Add(totalText);

                Grid.SetColumn(valSp, 1);
                lineGrid.Children.Add(valSp);
                rowSp.Children.Add(lineGrid);

                // Progress Bar
                double sharePct = (totalWifiToday > 0) ? ((double)net.TotalBytes / totalWifiToday) : 1.0;
                Border barBg = new Border {
                    Height = 3.5,
                    CornerRadius = new CornerRadius(1.75),
                    Background = Helpers.BrushFromHex(ThemeManager.CardBorder),
                    Margin = new Thickness(0, 4, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                double barFillW = Math.Max(4, sharePct * 300);
                Border barFill = new Border {
                    Height = 3.5,
                    CornerRadius = new CornerRadius(1.75),
                    Background = Helpers.BrushFromHex(ThemeManager.WifiBadgeBorder),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = barFillW
                };
                barBg.Child = barFill;
                rowSp.Children.Add(barBg);

                rowBorder.Child = rowSp;
                _wifiBreakdownListPanel.Children.Add(rowBorder);
            }
        }
    }
}
