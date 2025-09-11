using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Forms;
using OtpNet;

namespace AuthenticatorTray
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
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

        static void ShowPopup()
        {
            var accounts = LoadAccounts();

            // Create custom form with subtle gray/white colors - no taskbar icon
            ModernPopupForm popup = new ModernPopupForm
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(250, 250, 250), // Very subtle off-white
                TopMost = true,
                Width = 320, // Narrower, more macOS-like
                Height = 60 + (accounts.Count * 68), // Fixed height to fit all accounts without scrolling
                ShowInTaskbar = false // Prevent taskbar icon
            };

            // Position more centered while staying near tray area (bottom center-right)
            Rectangle screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            int centerX = screen.Left + (screen.Width / 2);
            int offsetX = centerX + (popup.Width); // A bit more to the right
            int finalY = screen.Bottom - popup.Height - 20;
            
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

            // Header with subtle gray background
            Panel header = new Panel
            {
                Height = 44, // macOS standard header height
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
                Font = new Font("Segoe UI", 11, FontStyle.Regular), // Smaller, less bold
                ForeColor = Color.FromArgb(28, 28, 30), // Fully opaque macOS primary text
                Location = new Point(16, 14),
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

            // Global timer display in header with correct initial value and color
            Label globalTimerLabel = new Label
            {
                Text = $"{initialRemaining}s",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = initialTimerColor,
                Location = new Point(280, 14),
                Size = new Size(30, 16),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            header.Controls.Add(titleLabel);
            header.Controls.Add(globalTimerLabel);
            popup.Controls.Add(header);

            // Main content panel with subtle white
            Panel contentPanel = new Panel
            {
                Location = new Point(0, 44),
                Size = new Size(popup.Width, popup.Height - 44),
                BackColor = Color.FromArgb(250, 250, 250) // Very subtle off-white
            };

            // Inner panel with clean background
            Panel scrollPanel = new Panel
            {
                Location = new Point(12, 8), // Equal left/right margins
                Size = new Size(contentPanel.Width - 24, contentPanel.Height - 16), // Full width minus equal margins
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

                // Create macOS-style card with clean colors
                Panel accountCard = new MacCard
                {
                    Size = new Size(scrollPanel.Width - 8, 60), // Full width with minimal margins
                    Location = new Point(4, yPosition), // Small left margin
                    BackColor = Color.FromArgb(245, 245, 245), // Light gray for cards
                    Cursor = Cursors.Hand
                };

                // Account name - darker and more visible
                Label nameLabel = new Label
                {
                    Text = GetDisplayName(name),
                    Font = new Font("Segoe UI", 9, FontStyle.Regular), // Smaller font
                    ForeColor = Color.FromArgb(60, 60, 67), // Much darker for better visibility
                    Location = new Point(12, 10),
                    Size = new Size(180, 14),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };

                // Calculate initial TOTP code immediately
                var totp = new Totp(Base32Encoding.ToBytes(acc.Secret), step: 30, totpSize: acc.Digits);
                string initialCode = totp.ComputeTotp();
                string formattedInitialCode = initialCode.Length == 6 ? 
                    $"{initialCode.Substring(0, 3)} {initialCode.Substring(3, 3)}" : initialCode;

                // TOTP code - refined sizing with real code from start
                Label codeLabel = new Label
                {
                    Text = formattedInitialCode, // Show real code immediately
                    Font = new Font("SF Mono", 16, FontStyle.Regular), // Slightly smaller, medium weight
                    ForeColor = Color.FromArgb(0, 122, 255), // Fully opaque macOS accent blue
                    Location = new Point(12, 28),
                    Size = new Size(120, 22),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };

                // Removed individual time label and progress bar - using global timer in header instead

                // Copy button with icon - positioned relative to card width
                Label copyButton = new Label
                {
                    Text = "📋", // Clipboard icon
                    Font = new Font("Segoe UI Emoji", 14, FontStyle.Regular),
                    ForeColor = Color.FromArgb(0, 122, 255), // Fully opaque blue
                    Location = new Point(accountCard.Width - 35, 25), // 35px from right edge
                    Size = new Size(25, 25),
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

                yPosition += 64; // Adjusted spacing between cards
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
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(80, 80, 80), // Subtle dark tooltip
                AutoSize = true,
                Padding = new Padding(4, 2, 4, 2) // Minimal padding
            };

            Point loc = nearControl.PointToScreen(Point.Empty);
            loc = parent.PointToClient(loc);
            tooltip.Location = new Point(loc.X + nearControl.Width / 2 - 20, loc.Y - 20);

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

        static Dictionary<string, Account> LoadAccounts()
        {
            try
            {
                // Load accounts from embedded JSON resource
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
                MessageBox.Show($"Error loading accounts: {ex.Message}\nUsing default accounts.", 
                    "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Return empty dictionary if JSON loading fails
            return new Dictionary<string, Account>();
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
            // Apply rounded corners to the window
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 12, 12));
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern System.IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Update region when window is resized
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 12, 12));
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

    // Minimal macOS-style progress bar
    public class MacProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                if (value < 0) value = 0;
                if (value > _maximum) value = _maximum;
                if (_value != value)
                {
                    _value = value;
                    Invalidate();
                }
            }
        }

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = value; Invalidate(); }
        }

        public MacProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Background track
            using (var bgBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRoundedRectangle(bgBrush, ClientRectangle, 2);
            }

            // Progress fill
            if (Value > 0 && Maximum > 0)
            {
                int width = (int)((double)Value / Maximum * Width);
                if (width > 0)
                {
                    Rectangle progressRect = new Rectangle(0, 0, width, Height);
                    using (var progressBrush = new SolidBrush(ForeColor))
                    {
                        e.Graphics.FillRoundedRectangle(progressBrush, progressRect, 2);
                    }
                }
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