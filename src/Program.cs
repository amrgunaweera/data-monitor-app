using System;
using System.Diagnostics;
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
        public static void Main(string[] args) {
            // Ensure working directory is always application directory (critical when launched by Windows at logon)
            try {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            } catch {}

            // Global exception logging to avoid silent exit during boot
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) {
                LogException(e.ExceptionObject as Exception);
            };

            // Check if user requested a force restart / fresh start via CLI
            bool forceRestart = false;
            if (args != null && args.Length > 0) {
                for (int i = 0; i < args.Length; i++) {
                    string a = args[i].ToLowerInvariant();
                    if (a == "--restart" || a == "-r" || a == "/restart" || a == "--force" || a == "-f") {
                        forceRestart = true;
                        break;
                    }
                }
            }

            if (forceRestart) {
                KillOtherInstances();
            }

            // Apply software rendering if hardware acceleration is disabled in user settings
            try {
                if (AppSettings.Instance.DisableHardwareAcceleration) {
                    RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                }
            } catch {}

            bool isNewInstance = false;
            Mutex mutex = null;
            try {
                mutex = new Mutex(true, MutexId, out isNewInstance);
            } catch (AbandonedMutexException) {
                // Previous owner terminated or crashed without cleanly releasing mutex.
                // The operating system has granted us ownership of the mutex.
                isNewInstance = true;
            } catch {
                // In case of any unexpected security or ACL restriction on the mutex, allow application to continue
                isNewInstance = true;
            }

            try {
                if (!isNewInstance && !forceRestart) {
                    // Another instance is already running (e.g. autostarted in background or minimized to tray)
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
            } catch (Exception ex) {
                LogException(ex);
                try {
                    MessageBox.Show(
                        "NetPulse encountered an error during startup:\n\n" + ex.Message + "\n\nSee startup_error.log for full details.",
                        "NetPulse Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                } catch {}
            } finally {
                if (mutex != null) {
                    try {
                        mutex.ReleaseMutex();
                    } catch {}
                    try {
                        mutex.Dispose();
                    } catch {}
                }
            }
        }

        private static void KillOtherInstances() {
            try {
                int currentId = Process.GetCurrentProcess().Id;
                Process[] processes = Process.GetProcesses();
                for (int i = 0; i < processes.Length; i++) {
                    Process p = processes[i];
                    try {
                        if (p.Id != currentId &&
                            (p.ProcessName.Equals("DataMonitor", StringComparison.OrdinalIgnoreCase) ||
                             p.ProcessName.Equals("NetPulseMonitor", StringComparison.OrdinalIgnoreCase))) {
                            p.Kill();
                        }
                    } catch {}
                }
            } catch {}
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
            string entry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] Unhandled Exception: {1}\r\n{2}\r\n\r\n",
                DateTime.Now, ex.Message, ex.StackTrace);

            try {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(localAppData, "NetPulse");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string logFile = Path.Combine(dir, "startup_error.log");
                File.AppendAllText(logFile, entry);
            } catch {}

            try {
                string baseLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
                File.AppendAllText(baseLog, entry);
            } catch {}
        }
    }
}
