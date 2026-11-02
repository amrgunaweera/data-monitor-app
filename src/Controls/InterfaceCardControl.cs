using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NetPulse.Controls {
    public class InterfaceCardControl : Border {
        private readonly InterfaceInfo _info;

        private TextBlock _titleBlock;
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

        private TextBlock _ipText;
        private TextBlock _macText;
        private TextBlock _speedText;
        private RealtimeChart _miniChart;

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
        }

        private void BuildUI() {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Details row
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Speed + totals
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) }); // Mini chart

            // ── Row 0: Header ──
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Name & desc
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Status pill

            bool isWifi = _info.InterfaceType == "Wi-Fi";
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
            _titleBlock = new TextBlock {
                Text = _info.Name + " (" + _info.InterfaceType + ")",
                FontWeight = FontWeights.Bold,
                FontSize = 15
            };
            _descBlock = new TextBlock {
                Text = _info.Description,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 450
            };
            namePanel.Children.Add(_titleBlock);
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

            // ── Row 1: Details (IP, MAC, Link Speed) ──
            _detailsBorder = new Border {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 12, 0, 10)
            };
            Grid detailsGrid = new Grid();
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _ipText = CreateDetailItem(detailsGrid, 0, "IPv4 ADDRESS", _info.IPv4Address);
            _macText = CreateDetailItem(detailsGrid, 1, "MAC ADDRESS", _info.MacAddress);
            _speedText = CreateDetailItem(detailsGrid, 2, "LINK SPEED", _info.LinkSpeed);

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
                FontSize = 11.5
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
            // Update status
            _statusDot.Background = Helpers.BrushFromHex(_info.IsConnected ? ThemeManager.StatusConnected : ThemeManager.StatusDisconnected);
            _statusText.Text = _info.IsConnected ? "Connected" : "Disconnected";
            _statusText.Foreground = Helpers.BrushFromHex(_info.IsConnected ? ThemeManager.StatusConnected : ThemeManager.TextSecondary);

            // Update details
            _ipText.Text = _info.IPv4Address;
            _macText.Text = _info.MacAddress;
            _speedText.Text = _info.LinkSpeed;

            // Update speeds
            _dlSpeedText.Text = Helpers.FormatSpeed(_info.CurrentDownloadSpeed);
            _ulSpeedText.Text = Helpers.FormatSpeed(_info.CurrentUploadSpeed);

            // Update totals
            _dlTotalText.Text = Helpers.FormatBytes(_info.SessionDownloaded);
            _ulTotalText.Text = Helpers.FormatBytes(_info.SessionUploaded);

            // Update mini chart
            _miniChart.SetData(_info.DownloadHistory, _info.UploadHistory);
        }
    }
}
