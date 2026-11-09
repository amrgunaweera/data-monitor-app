using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NetPulse {
    public static class Program {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        private const int ASFW_ANY = -1;
        private const int SW_RESTORE = 9;
        private const string WindowTitle = "NetPulse — Windows Network Data Monitor & History";
        private const string MutexId = "NetPulse_DataMonitor_SingleInstance_Mutex_9B8F2A";
        public const string RestoreMessageName = "NetPulse_Restore_Window_9B8F2A";

        [STAThread]
        public static void Main() {
            // Ensure working directory is always application directory (critical when launched by Windows at logon)
            try {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            } catch {}

            // Apply software rendering if hardware acceleration is disabled in user settings
            try {
                if (AppSettings.Instance.DisableHardwareAcceleration) {
                    RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                }
            } catch {}

            // Global exception logging to avoid silent exit during boot
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) {
                LogException(e.ExceptionObject as Exception);
            };

            bool isNewInstance = false;
            using (Mutex mutex = new Mutex(true, MutexId, out isNewInstance)) {
                if (!isNewInstance) {
                    // Another instance is already running (e.g. autostarted in background)
                    BringExistingInstanceToFront();
                    return;
                }

                Application app = new Application();
                app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
                    LogException(e.Exception);
                    e.Handled = true;
                };

                MainWindow mainWindow = new MainWindow();
                app.Run(mainWindow);
            }
        }

        private static void BringExistingInstanceToFront() {
            try {
                AllowSetForegroundWindow(ASFW_ANY);

                uint msg = RegisterWindowMessage(RestoreMessageName);
                if (msg != 0) {
                    // HWND_BROADCAST (0xffff) guarantees reaching the running instance even if window handle differs
                    PostMessage(new IntPtr(0xffff), msg, IntPtr.Zero, IntPtr.Zero);
                }

                IntPtr hWnd = FindWindow(null, WindowTitle);
                if (hWnd != IntPtr.Zero) {
                    if (msg != 0) {
                        PostMessage(hWnd, msg, IntPtr.Zero, IntPtr.Zero);
                    }
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
            } catch {}
        }

        private static void LogException(Exception ex) {
            if (ex == null) return;
            try {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(localAppData, "NetPulse");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string logFile = Path.Combine(dir, "startup_error.log");
                string entry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] Unhandled Exception: {1}\r\n{2}\r\n\r\n",
                    DateTime.Now, ex.Message, ex.StackTrace);
                File.AppendAllText(logFile, entry);
            } catch {}
        }
    }
}
