namespace AuthenticatorTray
{
    static class UiScale
    {
        // Responsive scaling system - percentage-based instead of pixel-based
        private static float _scaleFactor = 1.0f;
        private static Graphics? _graphics = null;
        private static Font? _baseFont = null;
        public static void InitializeScaling()
        {
            // Initialize graphics context for font measurements
            _graphics = Graphics.FromHwnd(IntPtr.Zero);
            _baseFont = new Font("Segoe UI", 9, FontStyle.Regular);
            // Calculate scale factor purely based on DPI for crisp rendering
            float dpiX = _graphics.DpiX;
            float dpiY = _graphics.DpiY;
            float baseDpi = 96f; // Standard Windows DPI
            float dpiScale = Math.Max(dpiX, dpiY) / baseDpi;
            // Snap to clean DPI scaling values for sharpness
            if (dpiScale >= 2.25f) _scaleFactor = 2.5f;        // 250%
            else if (dpiScale >= 1.875f) _scaleFactor = 2.0f;  // 200%
            else if (dpiScale >= 1.375f) _scaleFactor = 1.5f;  // 150%
            else if (dpiScale >= 1.125f) _scaleFactor = 1.25f; // 125%
            else _scaleFactor = 1.0f;                          // 100%
        }
        private static float ScaleValue(float value) => value * _scaleFactor;
        // Font-based measurements (like CSS em units)
        public static int Em(float multiplier)
        {
            if (_graphics == null || _baseFont == null) return (int)(multiplier * 16 * _scaleFactor * _monitorScale); // Fallback
            var size = _graphics.MeasureString("M", _baseFont);
            return (int)(size.Width * multiplier * _monitorScale);
        }
        // WinForms already rescales fonts per monitor, so only pixel layout needs the monitor/system DPI ratio
        private static float _monitorScale = 1.0f;
        public static void UseMonitorAt(Point point)
        {
            _monitorScale = 1.0f;
            try
            {
                IntPtr monitor = MonitorFromPoint(point, 2);
                if (_graphics != null && GetDpiForMonitor(monitor, 0, out uint dpiX, out _) == 0 && dpiX > 0)
                    _monitorScale = dpiX / _graphics.DpiX;
            }
            catch { }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
        // Percentage of the working area of the screen under the cursor
        public static int ScreenWidth(double percent) => (int)(Screen.FromPoint(Cursor.Position).WorkingArea.Width * (percent / 100.0));
        public static Font ScaleFont(string fontFamily, float baseSize, FontStyle style = FontStyle.Regular)
        {
            // Round font sizes to nearest 0.25 for better rendering
            float scaledSize = ScaleValue(baseSize);
            float roundedSize = Math.Max(6.0f, (float)Math.Round(scaledSize * 4) / 4);
            return new Font(fontFamily, roundedSize, style);
        }
    }
}
