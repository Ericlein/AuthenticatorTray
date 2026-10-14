using System.Reflection;
namespace AuthenticatorTray
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            using var mutex = new Mutex(true, "AuthenticatorTrayApp", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Authenticator is already running in the system tray.", "Already Running",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            UiScale.InitializeScaling();
            using Icon? ownedIcon = LoadEmbeddedIcon();
            using var trayIcon = new NotifyIcon
            {
                Icon = ownedIcon ?? SystemIcons.Shield,
                Text = "Eric's super duper secure auth",
                Visible = true
            };
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) TrayPopup.ShowPopup();
                else if (e.Button == MouseButtons.Right) Application.Exit();
            };
            // Don't leave a ghost tray icon behind if the app crashes
            AppDomain.CurrentDomain.UnhandledException += (s, e) => trayIcon.Visible = false;
            Application.Run();
            trayIcon.Visible = false;
        }
        static Icon? LoadEmbeddedIcon()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AuthenticatorTray.authenticator_icon.ico");
                return stream == null ? null : new Icon(stream);
            }
            catch (Exception)
            {
                // Fall back to the stock shield icon
                return null;
            }
        }
    }
}
