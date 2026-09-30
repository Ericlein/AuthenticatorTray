using System.Reflection;
namespace AuthenticatorTray
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Enable DPI awareness for crisp text rendering
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            using (var mutex = new Mutex(true, "AuthenticatorTrayApp", out bool createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Authenticator is already running in the system tray.", "Already Running",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // Initialize responsive scaling
                UiScale.InitializeScaling();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Icon appIcon;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("AuthenticatorTray.authenticator_icon.ico"))
                {
                    if (stream != null)
                    {
                        appIcon = new Icon(stream);
                    }
                    else
                    {
                        appIcon = SystemIcons.Shield; // Fallback if resource not found
                    }
                }
            }
            catch
            {
                appIcon = SystemIcons.Shield; // Fallback on any error
            }
            NotifyIcon trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "Eric's super duper secure auth",
                Visible = true
            };
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    TrayPopup.ShowPopup();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                    Application.Exit();
                }
            };
            // Cleanup on application exit
            Application.ApplicationExit += (s, e) =>
            {
                trayIcon?.Dispose();
                appIcon?.Dispose();
            };
                Application.Run();
                trayIcon.Visible = false;
                appIcon?.Dispose();
            }
        }
    }
}
