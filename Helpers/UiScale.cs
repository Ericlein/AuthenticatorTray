using System.Runtime.InteropServices;
namespace AuthenticatorTray
{
    static class UiScale
    {
        // System DPI snapped to a standard Windows scale step, applied to font sizes
        private static float _scaleFactor = 1.0f;
        // Width of "M" in the 9pt base font at system DPI, measured once
        private static float? _emWidth;
        private static float _systemDpiX = 96f;
        // WinForms already rescales fonts per monitor, so only pixel layout needs the monitor/system DPI ratio
        private static float _monitorScale = 1.0f;
        public static void InitializeScaling()
        {
            using Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            using var baseFont = new Font("Segoe UI", 9, FontStyle.Regular);
            _emWidth = graphics.MeasureString("M", baseFont).Width;
            _systemDpiX = graphics.DpiX;
            float dpiScale = Math.Max(graphics.DpiX, graphics.DpiY) / 96f;
            // Snap to clean DPI scaling values for sharpness
            if (dpiScale >= 2.25f) _scaleFactor = 2.5f;
            else if (dpiScale >= 1.875f) _scaleFactor = 2.0f;
            else if (dpiScale >= 1.375f) _scaleFactor = 1.5f;
            else if (dpiScale >= 1.125f) _scaleFactor = 1.25f;
            else _scaleFactor = 1.0f;
        }
        // Font-based measurement, like CSS em units
        public static int Em(float multiplier)
        {
            if (_emWidth is not float emWidth) return (int)(multiplier * 16 * _scaleFactor * _monitorScale);
            return (int)(emWidth * multiplier * _monitorScale);
        }
        public static void UseMonitorAt(Point point)
        {
            _monitorScale = 1.0f;
            if (_emWidth == null) return;
            try
            {
                IntPtr monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
                if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 && dpiX > 0)
                    _monitorScale = dpiX / _systemDpiX;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // shcore.dll is missing before Windows 8.1; keep system DPI
            }
        }
        // Percentage of the working area of the screen under the cursor
        public static int ScreenWidth(double percent) => (int)(Screen.FromPoint(Cursor.Position).WorkingArea.Width * (percent / 100.0));
        // Returns a new Font that the caller owns and must dispose
        public static Font ScaleFont(string fontFamily, float baseSize, FontStyle style = FontStyle.Regular)
        {
            // Round to the nearest 0.25pt for better rendering
            float roundedSize = Math.Max(6.0f, (float)Math.Round(baseSize * _scaleFactor * 4) / 4);
            return new Font(fontFamily, roundedSize, style);
        }
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const int MDT_EFFECTIVE_DPI = 0;
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);
        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    }
}
