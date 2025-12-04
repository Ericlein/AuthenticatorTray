using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Forms;
using OtpNet;
using ZXing;
namespace AuthenticatorTray
{
    static class Program
    {
        // Responsive scaling system - percentage-based instead of pixel-based
        private static float _scaleFactor = 1.0f;
        private static Graphics? _graphics = null;
        private static Font? _baseFont = null;
        [STAThread]
        static void Main()
        {
            // Enable DPI awareness for crisp text rendering
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            // Prevent multiple instances
            using (var mutex = new Mutex(true, "AuthenticatorTrayApp", out bool createdNew))
            {
                if (!createdNew)
                {
                    // Another instance is already running
                    MessageBox.Show("Authenticator is already running in the system tray.", "Already Running",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // Initialize responsive scaling
                InitializeScaling();
                // Optimize memory usage
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Icon appIcon;
            try
            {
                // Load icon from embedded resources
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
                    ShowPopup();
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
        static void InitializeScaling()
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
        // Helper methods for responsive scaling using relative units
        public static int ScaleValue(int value) => (int)(value * _scaleFactor);
        public static float ScaleValue(float value) => value * _scaleFactor;
        // Font-based measurements (like CSS em units)
        public static int Em(float multiplier) 
        {
            if (_graphics == null || _baseFont == null) return (int)(multiplier * 16 * _scaleFactor); // Fallback
            var size = _graphics.MeasureString("M", _baseFont);
            return (int)(size.Width * multiplier);
        }
        // Screen percentage-based measurements
        public static int ScreenWidth(double percent)
        {
            var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            return (int)(screen.Width * (percent / 100.0));
        }
        public static int ScreenHeight(double percent)
        {
            var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            return (int)(screen.Height * (percent / 100.0));
        }
        public static Font ScaleFont(string fontFamily, float baseSize, FontStyle style = FontStyle.Regular)
        {
            // Round font sizes to nearest 0.25 for better rendering
            float scaledSize = ScaleValue(baseSize);
            float roundedSize = Math.Max(6.0f, (float)Math.Round(scaledSize * 4) / 4);
            return new Font(fontFamily, roundedSize, style);
        }
        public static Size ScaleSize(Size size) => new Size(ScaleValue(size.Width), ScaleValue(size.Height));
        public static Point ScalePoint(Point point) => new Point(ScaleValue(point.X), ScaleValue(point.Y));
        public static Rectangle ScaleRectangle(Rectangle rect) => new Rectangle(ScaleValue(rect.X), ScaleValue(rect.Y), ScaleValue(rect.Width), ScaleValue(rect.Height));
        public static Padding ScalePadding(Padding padding) => new Padding(ScaleValue(padding.Left), ScaleValue(padding.Top), ScaleValue(padding.Right), ScaleValue(padding.Bottom));
        public static void ShowPopup()
        {
            var accounts = LoadAccounts();
            // Create custom form with relative sizing (no hardcoded pixels!)
            ModernPopupForm popup = new ModernPopupForm
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(250, 250, 250), // Very subtle off-white
                TopMost = true,
                Width = ScreenWidth(22), // 22% of screen width (wider)
                Height = Em(3.2f) + (accounts.Count * Em(4.6f)) + Em(1.5f), // Height with card spacing
                ShowInTaskbar = false // Prevent taskbar icon
            };
            // Truly responsive positioning using screen percentages
            Rectangle screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            // Position popup at bottom-right using screen percentages
            int offsetX = screen.Right - ScreenWidth(12) - popup.Width; // 12% from right edge
            int finalY = screen.Bottom - ScreenHeight(8) - popup.Height;  // 8% from bottom edge
            // Ensure popup stays within screen bounds with percentage-based margins
            int marginX = ScreenWidth(1); // 1% screen width margin
            int marginY = ScreenHeight(1); // 1% screen height margin
            offsetX = Math.Max(screen.Left + marginX, Math.Min(offsetX, screen.Right - popup.Width - marginX));
            finalY = Math.Max(screen.Top + marginY, Math.Min(finalY, screen.Bottom - popup.Height - marginY));
            // Fast smooth slide-up animation
            popup.Location = new Point(offsetX, screen.Bottom);
            popup.Show();
            System.Windows.Forms.Timer slideTimer = new() { Interval = 10 }; // 100fps for smoothness
            int startY = screen.Bottom;
            int animationDuration = 200; // 200ms total
            DateTime startTime = DateTime.Now;
            slideTimer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                double progress = Math.Min(elapsed / animationDuration, 1.0);
                // Ease-out animation for smooth deceleration
                double easeProgress = 1 - Math.Pow(1 - progress, 3);
                int currentY = (int)(startY - (startY - finalY) * easeProgress);
                popup.Location = new Point(offsetX, currentY);
                if (progress >= 1.0)
                {
                    slideTimer.Stop();
                    slideTimer.Dispose();
                }
            };
            slideTimer.Start();
            // Header with em-based sizing (more compact)
            Panel header = new Panel
            {
                Height = Em(3.2f), // More compact header height
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 248, 248) // Light gray
            };
            // Very subtle bottom border
            Panel headerBorder = new Panel
            {
                Height = 1,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(240, 240, 240) // Lighter border
            };
            header.Controls.Add(headerBorder);
            Label titleLabel = new Label
            {
                Text = "Eric's super duper secure auth",
                Font = ScaleFont("Segoe UI", 11, FontStyle.Regular), // Responsive font
                ForeColor = Color.FromArgb(28, 28, 30), // Fully opaque macOS primary text
                Location = new Point(Em(1.2f), Em(1.1f)), // Better vertical centering
                AutoSize = true,
                BackColor = Color.Transparent
            };
            // Calculate initial timer value and color
            int totalSeconds = 30;
            double elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % totalSeconds;
            int initialRemaining = totalSeconds - (int)elapsed;
            Color initialTimerColor = initialRemaining <= 5 ? Color.FromArgb(255, 59, 48) :
                                     initialRemaining <= 10 ? Color.FromArgb(255, 149, 0) :
                                     Color.FromArgb(0, 122, 255);
            // Settings icon
            Label settingsIcon = new Label
            {
                Text = "⚙️",
                Font = ScaleFont("Segoe UI Emoji", 14, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point(popup.Width - Em(7.5f), Em(1.0f)),
                Size = new Size(Em(1.8f), Em(1.8f)),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            settingsIcon.MouseEnter += (s, e) =>
            {
                settingsIcon.ForeColor = Color.FromArgb(0, 80, 180);
            };
            settingsIcon.MouseLeave += (s, e) =>
            {
                settingsIcon.ForeColor = Color.FromArgb(0, 122, 255);
            };
            settingsIcon.Click += (s, e) =>
            {
                using (var settingsForm = new SettingsForm(popup))
                {
                    settingsForm.ShowDialog();
                }
            };
            // Global timer display in header with better positioning
            Label globalTimerLabel = new Label
            {
                Text = $"{initialRemaining}s",
                Font = ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = initialTimerColor,
                Location = new Point(popup.Width - Em(3.5f), Em(1.1f)), // Better positioning
                Size = new Size(Em(3f), Em(1.4f)), // Wider to ensure visibility
                TextAlign = ContentAlignment.MiddleCenter, // Center the text
                BackColor = Color.Transparent
            };
            header.Controls.Add(titleLabel);
            header.Controls.Add(settingsIcon);
            header.Controls.Add(globalTimerLabel);
            popup.Controls.Add(header);
            // Main content panel with responsive sizing
            Panel contentPanel = new Panel
            {
                Location = new Point(0, Em(3.2f)),
                Size = new Size(popup.Width, popup.Height - Em(3.2f)),
                BackColor = Color.FromArgb(250, 250, 250) // Very subtle off-white
            };
            // Inner panel with responsive margins
            int panelMargin = Em(0.8f);
            Panel scrollPanel = new Panel
            {
                Location = new Point(panelMargin, Em(0.6f)),
                Size = new Size(contentPanel.Width - (panelMargin * 2), contentPanel.Height - Em(1.2f)),
                AutoScroll = false, // No scrollbar
                BackColor = Color.FromArgb(250, 250, 250) // Very subtle off-white
            };
            contentPanel.Controls.Add(scrollPanel);
            popup.Controls.Add(contentPanel);
            // Store references for updates (removed individual time labels and progress bars)
            Dictionary<string, Label> controls = new Dictionary<string, Label>();
            int yPosition = 0;
            foreach (var kvp in accounts)
            {
                string name = kvp.Key;
                Account acc = kvp.Value;
                // Create responsive macOS-style card
                int cardMargin = Em(0.3f);
                Panel accountCard = new MacCard
                {
                    Size = new Size(scrollPanel.Width - Em(0.6f), Em(4.2f)), // More compact card size
                    Location = new Point(cardMargin, yPosition), // Scaled margin
                    BackColor = Color.FromArgb(245, 245, 245), // Light gray for cards
                    Cursor = Cursors.Hand
                };
                // Account name with better vertical centering
                Label nameLabel = new Label
                {
                    Text = GetDisplayName(name),
                    Font = ScaleFont("Segoe UI", 9, FontStyle.Regular), // Responsive font
                    ForeColor = Color.FromArgb(60, 60, 67), // Much darker for better visibility
                    Location = new Point(Em(0.8f), Em(0.5f)), // Higher position
                    Size = new Size(Em(12f), Em(1.2f)),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
                // Calculate initial TOTP code immediately
                var totp = new Totp(Base32Encoding.ToBytes(acc.Secret), step: 30, totpSize: acc.Digits);
                string initialCode = totp.ComputeTotp();
                string formattedInitialCode = initialCode.Length == 6 ? 
                    $"{initialCode.Substring(0, 3)} {initialCode.Substring(3, 3)}" : initialCode;
                // TOTP code with better vertical centering
                Label codeLabel = new Label
                {
                    Text = formattedInitialCode, // Show real code immediately
                    Font = ScaleFont("SF Mono", 16, FontStyle.Regular), // Responsive monospace font
                    ForeColor = Color.FromArgb(0, 122, 255), // Fully opaque macOS accent blue
                    Location = new Point(Em(0.8f), Em(2.0f)), // Better centered position
                    Size = new Size(Em(8f), Em(1.8f)),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
                // Removed individual time label and progress bar - using global timer in header instead
                // Copy button with centered positioning
                Label copyButton = new Label
                {
                    Text = "📋", // Clipboard icon
                    Font = ScaleFont("Segoe UI Emoji", 14, FontStyle.Regular),
                    ForeColor = Color.FromArgb(0, 122, 255), // Fully opaque blue
                    Location = new Point(accountCard.Width - Em(2.5f), Em(1.4f)), // Vertically centered
                    Size = new Size(Em(2f), Em(2f)),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                // Strong hover effects for better visibility
                accountCard.MouseEnter += (s, e) =>
                {
                    accountCard.BackColor = Color.FromArgb(230, 235, 240); // Subtle blue-gray highlight
                    copyButton.ForeColor = Color.FromArgb(0, 80, 180); // Darker blue on hover
                    codeLabel.ForeColor = Color.FromArgb(0, 100, 200); // Slightly darker blue for code
                };
                accountCard.MouseLeave += (s, e) =>
                {
                    accountCard.BackColor = Color.FromArgb(245, 245, 245); // Back to light gray
                    copyButton.ForeColor = Color.FromArgb(0, 122, 255); // Back to original
                    codeLabel.ForeColor = Color.FromArgb(0, 122, 255); // Back to original
                };
                string accountName = name; // Capture for closure
                // Copy functionality for both card and button
                EventHandler copyAction = (sender, args) =>
                {
                    var totp = new Totp(Base32Encoding.ToBytes(acc.Secret), step: 30, totpSize: acc.Digits);
                    string code = totp.ComputeTotp();
                    Clipboard.SetText(code);
                    // Subtle visual feedback with icon
                    copyButton.Text = "✅"; // Checkmark icon
                    copyButton.ForeColor = Color.FromArgb(52, 199, 89); // Success green
                    accountCard.BackColor = Color.FromArgb(240, 248, 242); // Light green tint
                    System.Windows.Forms.Timer feedbackTimer = new () { Interval = 800 };
                    feedbackTimer.Tick += (s, e) =>
                    {
                        copyButton.Text = "📋"; // Back to clipboard icon
                        copyButton.ForeColor = Color.FromArgb(0, 122, 255);
                        accountCard.BackColor = Color.FromArgb(245, 245, 245); // Back to light gray
                        feedbackTimer.Stop();
                        feedbackTimer.Dispose();
                    };
                    feedbackTimer.Start();
                    // Show minimal copied tooltip
                    ShowCopiedTooltip(popup, accountCard);
                };
                accountCard.Click += copyAction;
                copyButton.Click += copyAction;
                accountCard.Controls.Add(nameLabel);
                accountCard.Controls.Add(codeLabel);
                accountCard.Controls.Add(copyButton);
                scrollPanel.Controls.Add(accountCard);
                controls[name] = codeLabel;
                yPosition += Em(4.6f); // Add gap between cards (4.2f card + 0.4f spacing)
            }
            // Single update timer - reduced frequency for efficiency
            var timer = new System.Windows.Forms.Timer { Interval = 500 }; // Update every 500ms instead of 100ms
            timer.Tick += (s, e) =>
            {
                int totalSeconds = 30;
                double elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % totalSeconds;
                int remaining = totalSeconds - (int)elapsed;
                double progress = (totalSeconds - elapsed) / totalSeconds;
                // Update global timer in header
                globalTimerLabel.Text = $"{remaining}s";
                // Color transitions for global timer
                if (remaining <= 5)
                {
                    globalTimerLabel.ForeColor = Color.FromArgb(255, 59, 48);
                }
                else if (remaining <= 10)
                {
                    globalTimerLabel.ForeColor = Color.FromArgb(255, 149, 0);
                }
                else
                {
                    globalTimerLabel.ForeColor = Color.FromArgb(0, 122, 255);
                }
                foreach (var kvp in accounts)
                {
                    string name = kvp.Key;
                    Account acc = kvp.Value;
                    var totp = new Totp(Base32Encoding.ToBytes(acc.Secret), step: 30, totpSize: acc.Digits);
                    string code = totp.ComputeTotp();
                    string formattedCode = code.Length == 6 ? 
                        $"{code.Substring(0, 3)} {code.Substring(3, 3)}" : code;
                    if (controls.ContainsKey(name))
                    {
                        var codeLabel = controls[name];
                        if (!codeLabel.Text.Equals(formattedCode))
                        {
                            codeLabel.Text = formattedCode;
                            AnimateLabel(codeLabel);
                        }
                    }
                }
            };
            timer.Start();
            popup.Deactivate += (s, e) => 
            {
                timer.Stop();
                timer.Dispose();
                popup.Close();
                // Force garbage collection to free up memory after closing popup
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            };
            popup.Activate();
        }
        static void AnimateLabel(Label label)
        {
            var originalColor = label.ForeColor;
            label.ForeColor = Color.FromArgb(150, originalColor);
            System.Windows.Forms.Timer animTimer = new () { Interval = 40 };
            int alpha = 150;
            animTimer.Tick += (s, e) =>
            {
                alpha += 25;
                if (alpha >= 255)
                {
                    label.ForeColor = originalColor;
                    animTimer.Stop();
                    animTimer.Dispose();
                }
                else
                {
                    label.ForeColor = Color.FromArgb(alpha, originalColor);
                }
            };
            animTimer.Start();
        }
        static void ShowCopiedTooltip(Form parent, Control nearControl)
        {
            Label tooltip = new Label
            {
                Text = "Copied",
                Font = ScaleFont("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(80, 80, 80), // Subtle dark tooltip
                AutoSize = true,
                Padding = new Padding(Em(0.3f), Em(0.15f), Em(0.3f), Em(0.15f)) // Responsive padding in em
            };
            Point loc = nearControl.PointToScreen(Point.Empty);
            loc = parent.PointToClient(loc);
            tooltip.Location = new Point(loc.X + nearControl.Width / 2 - Em(1.5f), loc.Y - Em(1.5f));
            parent.Controls.Add(tooltip);
            tooltip.BringToFront();
            System.Windows.Forms.Timer fadeTimer = new () { Interval = 600 };
            fadeTimer.Tick += (s, e) =>
            {
                parent.Controls.Remove(tooltip);
                tooltip.Dispose();
                fadeTimer.Stop();
                fadeTimer.Dispose();
            };
            fadeTimer.Start();
        }
        static string GetDisplayName(string fullName)
        {
            if (fullName.Contains("("))
            {
                return fullName.Substring(0, fullName.IndexOf("(")).Trim();
            }
            return fullName.Length > 22 ? fullName.Substring(0, 19) + "..." : fullName;
        }
        public static string? DecodeQrCodeFromImage(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image file not found: {imagePath}");
            }
            try
            {
                // Load the image
                using (var originalBitmap = new Bitmap(imagePath))
                {
                    // Ensure we have valid dimensions
                    if (originalBitmap.Width <= 0 || originalBitmap.Height <= 0)
                    {
                        throw new Exception($"Invalid image dimensions: {originalBitmap.Width}x{originalBitmap.Height}");
                    }
                    // Try multiple scales if the image is small
                    List<int> scalesToTry = new List<int>();
                    if (originalBitmap.Width < 300 || originalBitmap.Height < 300)
                    {
                        scalesToTry.AddRange(new[] { 3, 2, 1 }); // Try 3x, 2x, then original
                    }
                    else
                    {
                        scalesToTry.Add(1); // Just try original size
                    }
                    foreach (int scale in scalesToTry)
                    {
                        int width = originalBitmap.Width * scale;
                        int height = originalBitmap.Height * scale;
                        // Convert to RGB24 format for consistent processing
                        using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        {
                            using (var graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                                graphics.DrawImage(originalBitmap, 0, 0, width, height);
                            }
                        var reader = new BarcodeReaderGeneric();
                        var options = new ZXing.Common.DecodingOptions
                        {
                            TryHarder = true,
                            TryInverted = true,
                            PossibleFormats = new List<ZXing.BarcodeFormat>
                            {
                                ZXing.BarcodeFormat.QR_CODE
                            }
                        };
                        reader.Options = options;
                        // Method 1: Try with RGB24 format
                        var bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        try
                        {
                            int stride = Math.Abs(bitmapData.Stride);
                            int bytes = stride * bitmap.Height;
                            byte[] rgbValues = new byte[bytes];
                            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, rgbValues, 0, bytes);
                            var luminanceSource = new ZXing.RGBLuminanceSource(
                                rgbValues,
                                bitmap.Width,
                                bitmap.Height,
                                ZXing.RGBLuminanceSource.BitmapFormat.RGB24);
                            var result = reader.Decode(luminanceSource);
                            if (result != null)
                            {
                                return result.Text;
                            }
                            // Try inverted
                            var invertedSource = new ZXing.InvertedLuminanceSource(luminanceSource);
                            result = reader.Decode(invertedSource);
                            if (result != null)
                            {
                                return result.Text;
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData);
                        }
                        // Method 2: Try with grayscale conversion
                        // Re-lock bits to get RGB data for grayscale conversion
                        var bitmapData2 = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        try
                        {
                            int stride2 = Math.Abs(bitmapData2.Stride);
                            int bytes2 = stride2 * bitmap.Height;
                            byte[] rgbValues2 = new byte[bytes2];
                            System.Runtime.InteropServices.Marshal.Copy(bitmapData2.Scan0, rgbValues2, 0, bytes2);
                            // Convert RGB24 to grayscale manually
                            int grayWidth = bitmap.Width;
                            int grayHeight = bitmap.Height;
                            byte[] grayValues = new byte[grayWidth * grayHeight];
                            // Convert RGB to grayscale using luminance formula
                            for (int y = 0; y < grayHeight; y++)
                            {
                                for (int x = 0; x < grayWidth; x++)
                                {
                                    int rgbIndex = (y * stride2) + (x * 3);
                                    if (rgbIndex + 2 < rgbValues2.Length)
                                    {
                                        byte r = rgbValues2[rgbIndex + 2];     // BGR order
                                        byte g = rgbValues2[rgbIndex + 1];
                                        byte b = rgbValues2[rgbIndex];
                                        // Luminance formula: 0.299*R + 0.587*G + 0.114*B
                                        byte gray = (byte)((r * 77 + g * 150 + b * 29) >> 8);
                                        grayValues[y * grayWidth + x] = gray;
                                    }
                                }
                            }
                            var grayLuminanceSource = new ZXing.RGBLuminanceSource(
                                grayValues,
                                grayWidth,
                                grayHeight,
                                ZXing.RGBLuminanceSource.BitmapFormat.Gray8);
                            var grayResult = reader.Decode(grayLuminanceSource);
                            if (grayResult != null)
                            {
                                return grayResult.Text;
                            }
                            // Try inverted grayscale
                            var invertedGraySource = new ZXing.InvertedLuminanceSource(grayLuminanceSource);
                            grayResult = reader.Decode(invertedGraySource);
                            if (grayResult != null)
                            {
                                return grayResult.Text;
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData2);
                        }
                    }
                    } // End of foreach scale
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decode QR code: {ex.Message}", ex);
            }
            return null;
        }
        public static AccountJson? ParseOtpAuthUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return null;
                }
                if (!url.StartsWith("otpauth://"))
                {
                    return null;
                }
                var uri = new Uri(url);
                if (uri.Scheme != "otpauth")
                {
                    return null;
                }
                // Extract label (path without leading /)
                string label = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
                // Parse query parameters
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    var query = uri.Query.TrimStart('?');
                    foreach (var param in query.Split('&'))
                    {
                        var parts = param.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            queryParams[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                        }
                    }
                }
                // Get secret (required)
                if (!queryParams.TryGetValue("secret", out string? secret) || string.IsNullOrEmpty(secret))
                {
                    return null;
                }
                // Get issuer and account name
                string? issuer = queryParams.TryGetValue("issuer", out string? issuerValue) ? issuerValue : null;
                string accountName = label;
                // Parse label format: "Issuer:AccountName" or just "AccountName"
                if (label.Contains(":"))
                {
                    var parts = label.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        if (string.IsNullOrEmpty(issuer))
                        {
                            issuer = parts[0];
                        }
                        accountName = parts[1];
                    }
                }
                // Build display name
                string displayName;
                if (!string.IsNullOrEmpty(issuer) && !string.IsNullOrEmpty(accountName))
                {
                    displayName = $"{issuer} ({accountName})";
                }
                else if (!string.IsNullOrEmpty(issuer))
                {
                    displayName = issuer;
                }
                else if (!string.IsNullOrEmpty(accountName))
                {
                    displayName = accountName;
                }
                else
                {
                    displayName = "Unknown";
                }
                // Get algorithm (default SHA1)
                string algorithm = queryParams.TryGetValue("algorithm", out string? algValue) ? algValue.ToUpper() : "SHA1";
                if (algorithm != "SHA1" && algorithm != "SHA256" && algorithm != "SHA512" && algorithm != "MD5")
                {
                    algorithm = "SHA1";
                }
                // Get digits (default 6)
                int digits = 6;
                if (queryParams.TryGetValue("digits", out string? digitsValue))
                {
                    if (int.TryParse(digitsValue, out int parsedDigits) && (parsedDigits == 6 || parsedDigits == 7 || parsedDigits == 8))
                    {
                        digits = parsedDigits;
                    }
                }
                return new AccountJson
                {
                    Name = displayName,
                    Secret = secret,
                    Digits = digits,
                    Algorithm = algorithm
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse otpauth URL: {ex.Message}", ex);
            }
        }
        static string GetAccountsJsonPath()
        {
            // Get the application directory (where the executable is)
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDirectory, "accounts.json");
        }
        public static Dictionary<string, Account> LoadAccounts()
        {
            string accountsPath = GetAccountsJsonPath();
            // Try loading from file first
            if (File.Exists(accountsPath))
            {
                try
                {
                    var json = File.ReadAllText(accountsPath);
                    var accountsData = JsonSerializer.Deserialize<AccountsRoot>(json);
                    var accounts = new Dictionary<string, Account>();
                    if (accountsData?.Accounts != null)
                    {
                        foreach (var accountJson in accountsData.Accounts)
                        {
                            accounts[accountJson.Name] = new Account
                            {
                                Secret = accountJson.Secret,
                                Digits = accountJson.Digits,
                                Algorithm = accountJson.Algorithm
                            };
                        }
                    }
                    return accounts;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading accounts from file: {ex.Message}\nTrying embedded resource...", 
                        "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            // Fallback to embedded resource
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("AuthenticatorTray.accounts.json"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var json = reader.ReadToEnd();
                            var accountsData = JsonSerializer.Deserialize<AccountsRoot>(json);
                            var accounts = new Dictionary<string, Account>();
                            if (accountsData?.Accounts != null)
                            {
                                foreach (var accountJson in accountsData.Accounts)
                                {
                                    accounts[accountJson.Name] = new Account
                                    {
                                        Secret = accountJson.Secret,
                                        Digits = accountJson.Digits,
                                        Algorithm = accountJson.Algorithm
                                    };
                                }
                            }
                            return accounts;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading accounts: {ex.Message}\nUsing empty accounts.", 
                    "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            // Return empty dictionary if JSON loading fails
            return new Dictionary<string, Account>();
        }
        public static void SaveAccounts(Dictionary<string, Account> accounts)
        {
            try
            {
                string accountsPath = GetAccountsJsonPath();
                var accountsList = new List<AccountJson>();
                foreach (var kvp in accounts)
                {
                    accountsList.Add(new AccountJson
                    {
                        Name = kvp.Key,
                        Secret = kvp.Value.Secret,
                        Digits = kvp.Value.Digits,
                        Algorithm = kvp.Value.Algorithm
                    });
                }
                var accountsRoot = new AccountsRoot { Accounts = accountsList };
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(accountsRoot, options);
                File.WriteAllText(accountsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving accounts: {ex.Message}", 
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
    }
    public class Account
    {
        public string Secret { get; set; } = string.Empty;
        public int Digits { get; set; } = 6;
        public string Algorithm { get; set; } = "SHA1";
    }
    // JSON classes for deserialization
    public class AccountsRoot
    {
        [JsonPropertyName("accounts")]
        public List<AccountJson> Accounts { get; set; } = new List<AccountJson>();
    }
    public class AccountJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("secret")]
        public string Secret { get; set; } = string.Empty;
        [JsonPropertyName("digits")]
        public int Digits { get; set; } = 6;
        [JsonPropertyName("algorithm")]
        public string Algorithm { get; set; } = "SHA1";
    }
    // Settings dialog form for adding new accounts
    public class SettingsForm : Form
    {
        private TextBox? nameTextBox;
        private TextBox? secretTextBox;
        private TextBox? digitsTextBox;
        private TextBox? algorithmTextBox;
        private Button? addButton;
        private AccountJson? pendingAccount;
        private ModernPopupForm? parentPopup;
        public SettingsForm(ModernPopupForm parent)
        {
            parentPopup = parent;
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            this.Text = "Add 2FA Account";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.Width = Program.ScreenWidth(25);
            this.Height = Program.Em(28f); // Increased for more fields
            this.TopMost = true;
            this.ShowInTaskbar = false;
            // Header
            Panel header = new Panel
            {
                Height = Program.Em(3.5f),
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            Label titleLabel = new Label
            {
                Text = "Add 2FA Account",
                Font = Program.ScaleFont("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(28, 28, 30),
                Location = new Point(Program.Em(1.5f), Program.Em(1.2f)),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            header.Controls.Add(titleLabel);
            this.Controls.Add(header);
            // Content panel
            Panel contentPanel = new Panel
            {
                Location = new Point(0, Program.Em(3.5f)),
                Size = new Size(this.Width, this.Height - Program.Em(3.5f)),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            // Scan QR Code button
            Button scanButton = new Button
            {
                Text = "📷 Scan QR Code from Image",
                Font = Program.ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 122, 255),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(Program.Em(1.5f), Program.Em(2f)),
                Size = new Size(this.Width - Program.Em(3f), Program.Em(2.5f)),
                Cursor = Cursors.Hand
            };
            scanButton.FlatAppearance.BorderSize = 0;
            scanButton.Click += ScanButton_Click;
            // Editable fields for account info
            int fieldY = Program.Em(5.5f);
            int fieldHeight = Program.Em(2.5f);
            int fieldSpacing = Program.Em(2.8f);
            int labelWidth = Program.Em(6f);
            int fieldWidth = this.Width - Program.Em(3f) - labelWidth - Program.Em(1f);
            // Name field
            Label nameLabel = new Label
            {
                Text = "Name:",
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(Program.Em(1.5f), fieldY + Program.Em(0.5f)),
                Size = new Size(labelWidth, Program.Em(1.5f)),
                TextAlign = ContentAlignment.MiddleLeft
            };
            nameTextBox = new TextBox
            {
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(Program.Em(1.5f) + labelWidth, fieldY),
                Size = new Size(fieldWidth, fieldHeight),
                PlaceholderText = "Account name",
                Enabled = false
            };
            // Secret field
            fieldY += fieldSpacing;
            Label secretLabel = new Label
            {
                Text = "Secret:",
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(Program.Em(1.5f), fieldY + Program.Em(0.5f)),
                Size = new Size(labelWidth, Program.Em(1.5f)),
                TextAlign = ContentAlignment.MiddleLeft
            };
            secretTextBox = new TextBox
            {
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(Program.Em(1.5f) + labelWidth, fieldY),
                Size = new Size(fieldWidth, fieldHeight),
                PlaceholderText = "Base32 secret key",
                Enabled = false
            };
            // Digits field
            fieldY += fieldSpacing;
            Label digitsLabel = new Label
            {
                Text = "Digits:",
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(Program.Em(1.5f), fieldY + Program.Em(0.5f)),
                Size = new Size(labelWidth, Program.Em(1.5f)),
                TextAlign = ContentAlignment.MiddleLeft
            };
            digitsTextBox = new TextBox
            {
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(Program.Em(1.5f) + labelWidth, fieldY),
                Size = new Size(fieldWidth, fieldHeight),
                PlaceholderText = "6",
                Enabled = false
            };
            // Algorithm field
            fieldY += fieldSpacing;
            Label algorithmLabel = new Label
            {
                Text = "Algorithm:",
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(Program.Em(1.5f), fieldY + Program.Em(0.5f)),
                Size = new Size(labelWidth, Program.Em(1.5f)),
                TextAlign = ContentAlignment.MiddleLeft
            };
            algorithmTextBox = new TextBox
            {
                Font = Program.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(Program.Em(1.5f) + labelWidth, fieldY),
                Size = new Size(fieldWidth, fieldHeight),
                PlaceholderText = "SHA1",
                Enabled = false
            };
            // Add Account button
            fieldY += fieldSpacing + Program.Em(1f);
            addButton = new Button
            {
                Text = "Add Account",
                Font = Program.ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 199, 89),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(Program.Em(1.5f), fieldY),
                Size = new Size((this.Width - Program.Em(4.5f)) / 2, Program.Em(2.5f)),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += AddButton_Click;
            // Cancel button
            Button cancelButton = new Button
            {
                Text = "Cancel",
                Font = Program.ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                BackColor = Color.FromArgb(245, 245, 245),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(Program.Em(1.5f) + (this.Width - Program.Em(4.5f)) / 2 + Program.Em(1.5f), fieldY),
                Size = new Size((this.Width - Program.Em(4.5f)) / 2, Program.Em(2.5f)),
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => this.Close();
            // Add all controls
            contentPanel.Controls.Add(scanButton);
            contentPanel.Controls.Add(nameLabel);
            contentPanel.Controls.Add(nameTextBox);
            contentPanel.Controls.Add(secretLabel);
            contentPanel.Controls.Add(secretTextBox);
            contentPanel.Controls.Add(digitsLabel);
            contentPanel.Controls.Add(digitsTextBox);
            contentPanel.Controls.Add(algorithmLabel);
            contentPanel.Controls.Add(algorithmTextBox);
            contentPanel.Controls.Add(addButton);
            contentPanel.Controls.Add(cancelButton);
            this.Controls.Add(contentPanel);
            // Hover effects
            scanButton.MouseEnter += (s, e) => scanButton.BackColor = Color.FromArgb(0, 100, 200);
            scanButton.MouseLeave += (s, e) => scanButton.BackColor = Color.FromArgb(0, 122, 255);
            addButton.MouseEnter += (s, e) => { if (addButton.Enabled) addButton.BackColor = Color.FromArgb(40, 180, 70); };
            addButton.MouseLeave += (s, e) => { if (addButton.Enabled) addButton.BackColor = Color.FromArgb(52, 199, 89); };
            cancelButton.MouseEnter += (s, e) => cancelButton.BackColor = Color.FromArgb(230, 230, 230);
            cancelButton.MouseLeave += (s, e) => cancelButton.BackColor = Color.FromArgb(245, 245, 245);
        }
        private void ScanButton_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*";
                dialog.Title = "Select QR Code Image";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Decode directly from the selected file
                        string selectedImagePath = dialog.FileName;
                        // Load and validate the image first
                        using (var testImage = new Bitmap(selectedImagePath))
                        {
                            string debugInfo = $"Image loaded successfully:\n" +
                                             $"Path: {selectedImagePath}\n" +
                                             $"Size: {new FileInfo(selectedImagePath).Length} bytes\n" +
                                             $"Dimensions: {testImage.Width}x{testImage.Height}\n" +
                                             $"Format: {testImage.PixelFormat}";
                            System.Diagnostics.Debug.WriteLine(debugInfo);
                        }
                        string? qrText = Program.DecodeQrCodeFromImage(selectedImagePath);
                        if (string.IsNullOrEmpty(qrText))
                        {
                            MessageBox.Show($"No QR code found in the image.\n\nTroubleshooting:\n" +
                                          $"- Ensure the QR code is clearly visible\n" +
                                          $"- Try a higher resolution image\n" +
                                          $"- Make sure it's a valid 2FA QR code\n\n" +
                                          $"File: {Path.GetFileName(selectedImagePath)}",
                                "Scan Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        AccountJson? account = Program.ParseOtpAuthUrl(qrText);
                        if (account == null)
                        {
                            string preview = qrText.Length > 100 ? qrText.Substring(0, 100) + "..." : qrText;
                            MessageBox.Show($"Invalid QR code format. Expected otpauth:// URL.\n\nFound: {preview}",
                                "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        pendingAccount = account;
                        // Populate the editable fields
                        if (nameTextBox != null)
                        {
                            nameTextBox.Text = account.Name;
                            nameTextBox.Enabled = true;
                        }
                        if (secretTextBox != null)
                        {
                            secretTextBox.Text = account.Secret;
                            secretTextBox.Enabled = true;
                        }
                        if (digitsTextBox != null)
                        {
                            digitsTextBox.Text = account.Digits.ToString();
                            digitsTextBox.Enabled = true;
                        }
                        if (algorithmTextBox != null)
                        {
                            algorithmTextBox.Text = account.Algorithm;
                            algorithmTextBox.Enabled = true;
                        }
                        // Enable Add Account button
                        if (addButton != null)
                        {
                            addButton.Enabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        string fullError = $"Error processing QR code:\n\n{ex.Message}";
                        if (ex.InnerException != null)
                        {
                            fullError += $"\n\nInner exception: {ex.InnerException.Message}";
                        }
                        fullError += $"\n\nStack trace:\n{ex.StackTrace}";
                        MessageBox.Show(fullError, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void AddButton_Click(object? sender, EventArgs e)
        {
            // Read values from TextBoxes
            string name = nameTextBox?.Text?.Trim() ?? "";
            string secret = secretTextBox?.Text?.Trim() ?? "";
            string digitsStr = digitsTextBox?.Text?.Trim() ?? "6";
            string algorithm = algorithmTextBox?.Text?.Trim()?.ToUpper() ?? "SHA1";
            // Validate inputs
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter an account name.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(secret))
            {
                MessageBox.Show("Please enter a secret key.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(digitsStr, out int digits) || digits < 6 || digits > 8)
            {
                MessageBox.Show("Digits must be a number between 6 and 8.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (algorithm != "SHA1" && algorithm != "SHA256" && algorithm != "SHA512" && algorithm != "MD5")
            {
                MessageBox.Show("Algorithm must be SHA1, SHA256, SHA512, or MD5.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                var accounts = Program.LoadAccounts();
                // Check for duplicate
                if (accounts.ContainsKey(name))
                {
                    var result = MessageBox.Show(
                        $"An account named '{name}' already exists.\n\nDo you want to replace it?",
                        "Duplicate Account",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }
                // Add account
                accounts[name] = new Account
                {
                    Secret = secret,
                    Digits = digits,
                    Algorithm = algorithm
                };
                // Save accounts
                Program.SaveAccounts(accounts);
                MessageBox.Show($"Account '{name}' added successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Close this dialog and parent popup, then reopen popup
                this.DialogResult = DialogResult.OK;
                if (parentPopup != null)
                {
                    parentPopup.Close();
                }
                this.Close();
                // Reopen popup to show new account
                Program.ShowPopup();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding account: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            // Subtle macOS shadow
            for (int i = 3; i >= 0; i--)
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(8 + (i * 3), 0, 0, 0)))
                {
                    e.Graphics.FillRoundedRectangle(shadowBrush, 
                        new Rectangle(i + 1, i + 1, Width - (i * 2) - 1, Height - (i * 2) - 1), 8);
                }
            }
            // Main form background
            using (var formBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRoundedRectangle(formBrush, new Rectangle(0, 0, Width - 6, Height - 6), 8);
            }
            // Very subtle border
            using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 0.5f))
            {
                e.Graphics.DrawRoundedRectangle(borderPen, new Rectangle(0, 0, Width - 6, Height - 6), 8);
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int cornerRadius = Program.Em(0.8f);
            this.Region = Region.FromHrgn(ModernPopupForm.CreateRoundRectRgn(0, 0, this.Width, this.Height, cornerRadius, cornerRadius));
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int cornerRadius = Program.Em(0.8f);
            this.Region = Region.FromHrgn(ModernPopupForm.CreateRoundRectRgn(0, 0, this.Width, this.Height, cornerRadius, cornerRadius));
        }
    }
    // Refined macOS-style popup form with rounded corners
    public class ModernPopupForm : Form
    {
        public ModernPopupForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Apply responsive rounded corners using em units
            int cornerRadius = Program.Em(0.8f);
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, cornerRadius, cornerRadius));
        }
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern System.IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Update region when window is resized using em units
            int cornerRadius = Program.Em(0.8f);
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, cornerRadius, cornerRadius));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            // Subtle macOS shadow
            for (int i = 3; i >= 0; i--)
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(8 + (i * 3), 0, 0, 0)))
                {
                    e.Graphics.FillRoundedRectangle(shadowBrush, 
                        new Rectangle(i + 1, i + 1, Width - (i * 2) - 1, Height - (i * 2) - 1), 8);
                }
            }
            // Main form background - pure white
            using (var formBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRoundedRectangle(formBrush, new Rectangle(0, 0, Width - 6, Height - 6), 8);
            }
            // Very subtle border
            using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 0.5f))
            {
                e.Graphics.DrawRoundedRectangle(borderPen, new Rectangle(0, 0, Width - 6, Height - 6), 8);
            }
        }
    }
    // Minimal macOS-style card
    public class MacCard : Panel
    {
        public MacCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            // Card background with rounded corners
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRoundedRectangle(brush, 
                    new Rectangle(0, 0, Width - 1, Height - 1), 4);
            }
            // Subtle border only
            using (var borderPen = new Pen(Color.FromArgb(235, 235, 235), 0.5f))
            {
                e.Graphics.DrawRoundedRectangle(borderPen, 
                    new Rectangle(0, 0, Width - 1, Height - 1), 4);
            }
        }
    }
    // Extension methods for rounded rectangles
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = GetRoundedRectanglePath(bounds, radius))
            {
                graphics.FillPath(brush, path);
            }
        }
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = GetRoundedRectanglePath(bounds, radius))
            {
                graphics.DrawPath(pen, path);
            }
        }
        private static GraphicsPath GetRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();
            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}