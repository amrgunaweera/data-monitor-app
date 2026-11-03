using System;
using Microsoft.Win32;

namespace NetPulse {
    public static class ThemeManager {
        public static bool IsDark { get; private set; }

        public static event Action ThemeChanged;

        static ThemeManager() {
            SyncWithSettingsOrWindows();
        }

        public static void SyncWithSettingsOrWindows() {
            string pref = "System";
            try {
                if (AppSettings.Instance != null && !string.IsNullOrEmpty(AppSettings.Instance.ThemePreference)) {
                    pref = AppSettings.Instance.ThemePreference;
                }
            } catch {}

            if (pref == "Dark") {
                SetTheme(true);
            } else if (pref == "Light") {
                SetTheme(false);
            } else {
                SetTheme(!IsWindowsLightTheme());
            }
        }

        public static bool IsWindowsLightTheme() {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) {
                    if (key != null) {
                        object val = key.GetValue("AppsUseLightTheme");
                        if (val is int) {
                            return ((int)val) > 0;
                        }
                    }
                }
            } catch {}
            return false;
        }

        public static void SetTheme(bool dark) {
            if (IsDark != dark) {
                IsDark = dark;
                if (ThemeChanged != null) {
                    ThemeChanged();
                }
            }
        }

        public static void ToggleTheme() {
            SetTheme(!IsDark);
        }

        public static void SyncWithWindows() {
            SetTheme(!IsWindowsLightTheme());
        }

        // ── Palette Accessors ──
        public static string WindowBg {
            get { return IsDark ? "#0B0E17" : "#F1F5F9"; }
        }

        public static string CardBg {
            get { return IsDark ? "#131728" : "#FFFFFF"; }
        }

        public static string CardBorder {
            get { return IsDark ? "#212842" : "#E2E8F0"; }
        }

        public static string SubCardBg {
            get { return IsDark ? "#101424" : "#F8FAFC"; }
        }

        public static string BadgeBg {
            get { return IsDark ? "#14192A" : "#F1F5F9"; }
        }

        public static string BadgeBorder {
            get { return IsDark ? "#222A45" : "#CBD5E1"; }
        }

        public static string TextPrimary {
            get { return IsDark ? "#FFFFFF" : "#0F172A"; }
        }

        public static string TextSecondary {
            get { return IsDark ? "#8C96B8" : "#475569"; }
        }

        public static string TextMuted {
            get { return IsDark ? "#5E6988" : "#64748B"; }
        }

        public static string TextDetailVal {
            get { return IsDark ? "#CAD4EE" : "#1E293B"; }
        }

        public static string ChartBg {
            get { return IsDark ? "#121626" : "#F8FAFC"; }
        }

        public static string ChartGrid {
            get { return IsDark ? "#242A42" : "#E2E8F0"; }
        }

        public static string ChartText {
            get { return IsDark ? "#6E7A9F" : "#64748B"; }
        }

        public static string DownloadColor {
            get { return IsDark ? "#00E5A3" : "#059669"; }
        }

        public static string DownloadBg {
            get { return IsDark ? "#0F2826" : "#ECFDF5"; }
        }

        public static string UploadColor {
            get { return IsDark ? "#FF5B79" : "#E11D48"; }
        }

        public static string UploadBg {
            get { return IsDark ? "#2B1622" : "#FFF1F2"; }
        }

        public static string TotalColor {
            get { return IsDark ? "#A78BFA" : "#7C3AED"; }
        }

        public static string ButtonBg {
            get { return IsDark ? "#161D32" : "#FFFFFF"; }
        }

        public static string ButtonFg {
            get { return IsDark ? "#8F9DC4" : "#334155"; }
        }

        public static string ButtonBorder {
            get { return IsDark ? "#2A3456" : "#CBD5E1"; }
        }

        public static string ResetBtnBg {
            get { return IsDark ? "#211929" : "#FFF1F2"; }
        }

        public static string ResetBtnFg {
            get { return IsDark ? "#FF7B90" : "#E11D48"; }
        }

        public static string ResetBtnBorder {
            get { return IsDark ? "#48253E" : "#FECDD3"; }
        }

        public static string WifiBadgeBg {
            get { return IsDark ? "#2D1F50" : "#EEF2FF"; }
        }

        public static string WifiBadgeBorder {
            get { return IsDark ? "#6366F1" : "#818CF8"; }
        }

        public static string EthBadgeBg {
            get { return IsDark ? "#14314E" : "#E0F2FE"; }
        }

        public static string EthBadgeBorder {
            get { return IsDark ? "#0284C7" : "#38BDF8"; }
        }

        public static string StatusConnected {
            get { return IsDark ? "#00E5A3" : "#059669"; }
        }

        public static string StatusDisconnected {
            get { return IsDark ? "#6E7A9F" : "#94A3B8"; }
        }

        public static string TabActiveBg {
            get { return IsDark ? "#2A244D" : "#EDE9FE"; }
        }

        public static string TabActiveFg {
            get { return IsDark ? "#C4B5FD" : "#6D28D9"; }
        }

        public static string TabActiveBorder {
            get { return IsDark ? "#7C3AED" : "#8B5CF6"; }
        }

        public static string TabInactiveBg {
            get { return IsDark ? "#14192A" : "#F1F5F9"; }
        }

        public static string TabInactiveFg {
            get { return IsDark ? "#8C96B8" : "#64748B"; }
        }

        public static string TabInactiveBorder {
            get { return IsDark ? "#222A45" : "#CBD5E1"; }
        }
    }
}

