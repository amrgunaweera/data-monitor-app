using System;
using System.Windows;

namespace NetPulse {
    public static class Program {
        [STAThread]
        public static void Main() {
            Application app = new Application();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
    }
}

