using static AuthenticatorTray.UiScale;
namespace AuthenticatorTray
{
    static class TrayPopup
    {
        public static void ShowPopup()
        {
            Rectangle screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            UseMonitorAt(Cursor.Position);
            var data = AccountStore.LoadData();
            int headerHeight = Em(3.2f);
            int panelMargin = Em(0.8f);
            int cardMargin = Em(0.3f);
            int cardStep = Em(4.6f); // 4.2f card + 0.4f spacing
            int folderStep = Em(2.2f);
            int columnWidth = Math.Max((int)(screen.Width * 0.22), Em(17.5f)); // 22% of the working area, but never narrower than a card
            int slotWidth = columnWidth - (panelMargin * 2);
            int marginX = (int)(screen.Width * 0.01);
            int marginY = (int)(screen.Height * 0.01);
            int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
            ModernPopupForm popup = new ModernPopupForm
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(250, 250, 250), // Very subtle off-white
                TopMost = true,
                ShowInTaskbar = false // Prevent taskbar icon
            };
            List<PopupItem> items = new List<PopupItem>();
            int[] itemColumns = Array.Empty<int>();
            int columnCount = 1, contentHeight = 0;
            bool overflow = false;
            int offsetX = 0, finalY = 0;
            void Plan()
            {
                items = BuildPopupItems(data, cardStep, folderStep);
                // New columns start once the popup would pass 80% of the working area height
                int maxColumnHeight = Math.Max(cardStep, (int)(screen.Height * 0.8) - headerHeight - Em(1.5f));
                int maxColumns = Math.Max(1, (screen.Width - (marginX * 2) - (panelMargin * 2) - scrollBarWidth) / slotWidth);
                itemColumns = ColumnLayout.Balance(items.ConvertAll(i => i.Height), items.ConvertAll(i => i.KeepWithNext), maxColumnHeight, maxColumns);
                columnCount = itemColumns.Length == 0 ? 1 : itemColumns.Max() + 1;
                var columnHeights = new int[columnCount];
                for (int i = 0; i < items.Count; i++) columnHeights[itemColumns[i]] += items[i].Height;
                contentHeight = columnHeights.Max();
                overflow = contentHeight > maxColumnHeight;
                popup.Width = (panelMargin * 2) + (columnCount * slotWidth) + (overflow ? scrollBarWidth : 0);
                popup.Height = headerHeight + (overflow ? maxColumnHeight : contentHeight) + Em(1.5f); // Height with card spacing
                // Bottom-right near the tray, kept inside the working area
                offsetX = screen.Right - (int)(screen.Width * 0.12) - popup.Width;
                finalY = screen.Bottom - (int)(screen.Height * 0.08) - popup.Height;
                offsetX = Math.Max(screen.Left + marginX, Math.Min(offsetX, screen.Right - popup.Width - marginX));
                finalY = Math.Max(screen.Top + marginY, Math.Min(finalY, screen.Bottom - popup.Height - marginY));
            }
            Plan();
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
                Font = ScaleFont("Segoe UI", 12, FontStyle.Regular), // Responsive font
                ForeColor = Color.FromArgb(28, 28, 30), // Fully opaque macOS primary text
                Location = new Point(0, Em(0.8f)), // Will be centered after adding to header
                AutoSize = true,
                BackColor = Color.Transparent
            };
            // Calculate initial timer value and color
            int initialRemaining = TotpService.SecondsRemaining();
            // Settings icon - positioned on the left
            Label settingsIcon = new Label
            {
                Text = "⚙️",
                Font = ScaleFont("Segoe UI Emoji", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point(Em(0.8f), Em(0.8f)), // Left side, keeping vertical position
                Size = new Size(Em(2f), Em(2f)), // Keeping size
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
                bool changed;
                using (var settingsForm = new SettingsForm())
                {
                    settingsForm.ShowDialog();
                    changed = settingsForm.AccountsChanged;
                }
                if (changed)
                {
                    if (!popup.IsDisposed)
                    {
                        popup.Close();
                    }
                    ShowPopup();
                }
            };
            // Global timer display in header with better positioning
            Label globalTimerLabel = new Label
            {
                Text = $"{initialRemaining}s",
                Font = ScaleFont("Segoe UI", 12, FontStyle.Regular),
                ForeColor = TimerColor(initialRemaining),
                Location = new Point(popup.Width - Em(3.5f), Em(0.8f)), // Better positioning
                Size = new Size(Em(3f), Em(2f)), // Wider to ensure visibility
                TextAlign = ContentAlignment.MiddleCenter, // Center the text
                BackColor = Color.Transparent
            };
            header.Controls.Add(settingsIcon);
            header.Controls.Add(titleLabel);
            header.Controls.Add(globalTimerLabel);

            // Center the title text after it's been added (so AutoSize has calculated the width)
            titleLabel.Location = new Point((popup.Width - titleLabel.Width) / 2, Em(0.8f));
            // Re-center once WinForms rescales the font for this monitor's DPI
            titleLabel.SizeChanged += (s, e) => titleLabel.Location = new Point((popup.Width - titleLabel.Width) / 2, Em(0.8f));

            popup.Controls.Add(header);
            // Main content panel with responsive sizing
            Panel contentPanel = new Panel
            {
                Location = new Point(0, Em(3.2f)),
                Size = new Size(popup.Width, popup.Height - Em(3.2f)),
                BackColor = Color.FromArgb(250, 250, 250) // Very subtle off-white
            };
            // Inner panel with responsive margins
            Panel scrollPanel = new Panel
            {
                Location = new Point(panelMargin, Em(0.6f)),
                Size = new Size(contentPanel.Width - (panelMargin * 2), contentPanel.Height - Em(1.2f)),
                AutoScroll = false, // Only scrolls when the columns can't fit on screen
                BackColor = Color.FromArgb(250, 250, 250) // Very subtle off-white
            };
            contentPanel.Controls.Add(scrollPanel);
            popup.Controls.Add(contentPanel);
            // Store references for updates
            Dictionary<string, Label> controls = new Dictionary<string, Label>();
            T WithDialog<T>(Func<T> show)
            {
                popup.KeepOpen = true;
                try
                {
                    return show();
                }
                finally
                {
                    popup.KeepOpen = false;
                    if (!popup.IsDisposed) popup.Activate();
                }
            }
            void Relayout()
            {
                if (popup.IsDisposed) return;
                Plan();
                popup.Location = new Point(offsetX, finalY);
                globalTimerLabel.Location = new Point(popup.Width - Em(3.5f), Em(0.8f));
                titleLabel.Location = new Point((popup.Width - titleLabel.Width) / 2, Em(0.8f));
                contentPanel.Size = new Size(popup.Width, popup.Height - Em(3.2f));
                scrollPanel.Size = new Size(contentPanel.Width - (panelMargin * 2), contentPanel.Height - Em(1.2f));
                BuildContent();
            }
            // Reload from disk, change, save, then re-layout
            void Mutate(Action<AccountData> change)
            {
                var fresh = AccountStore.LoadData();
                change(fresh);
                AccountStore.SaveData(fresh);
                data = AccountStore.LoadData();
                popup.BeginInvoke(new Action(Relayout));
            }
            Label CreateFolderHeader(PopupItem item, int x, int y)
            {
                string folder = item.Folder!;
                Label folderLabel = new Label
                {
                    Text = $"{(item.Collapsed ? "▸" : "▾")}  {folder}  ({item.Count})",
                    Font = ScaleFont("Segoe UI", 9, FontStyle.Regular),
                    ForeColor = Color.FromArgb(60, 60, 67),
                    Location = new Point(x + Em(0.5f), y + Em(0.2f)),
                    Size = new Size(slotWidth - Em(0.6f) - Em(0.5f), Em(1.8f)),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent,
                    Tag = "folder:" + folder
                };
                folderLabel.MouseEnter += (s, e) => folderLabel.ForeColor = Color.FromArgb(0, 122, 255);
                folderLabel.MouseLeave += (s, e) => folderLabel.ForeColor = Color.FromArgb(60, 60, 67);
                folderLabel.Click += (s, e) =>
                {
                    if (e is MouseEventArgs me && me.Button != MouseButtons.Left) return;
                    Mutate(d =>
                    {
                        if (!d.CollapsedFolders.Remove(folder)) d.CollapsedFolders.Add(folder);
                    });
                };
                return folderLabel;
            }
            ContextMenuStrip CreateCardMenu(string accountName, Account acc)
            {
                var menu = new ContextMenuStrip { Font = ScaleFont("Segoe UI", 9, FontStyle.Regular) };
                var moveItem = new ToolStripMenuItem("Move to folder");
                void MoveTo(string? folder) => Mutate(d =>
                {
                    if (d.Accounts.TryGetValue(accountName, out var target)) target.Folder = folder == null ? null : d.AddFolder(folder);
                });
                moveItem.DropDownItems.Add(new ToolStripMenuItem("No folder", null, (s, e) => MoveTo(null)) { Checked = acc.Folder == null });
                foreach (string folder in data.Folders)
                {
                    moveItem.DropDownItems.Add(new ToolStripMenuItem(folder, null, (s, e) => MoveTo(folder)) { Checked = acc.Folder == folder });
                }
                moveItem.DropDownItems.Add(new ToolStripSeparator());
                moveItem.DropDownItems.Add(new ToolStripMenuItem("New folder…", null, (s, e) =>
                {
                    string? folder = WithDialog(() => InputForm.Prompt(popup, "New Folder", "Folder name", ""));
                    if (!string.IsNullOrWhiteSpace(folder)) MoveTo(folder);
                }));
                var removeItem = new ToolStripMenuItem("Remove…", null, (s, e) =>
                {
                    var answer = WithDialog(() => ConfirmRemove(popup, accountName));
                    if (answer) Mutate(d => d.Accounts.Remove(accountName));
                });
                menu.Items.Add(moveItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(removeItem);
                return menu;
            }
            Panel CreateCard(string name, int x, int y)
            {
                Account acc = data.Accounts[name];
                // Create responsive macOS-style card
                Panel accountCard = new MacCard
                {
                    Size = new Size(slotWidth - Em(0.6f), Em(4.2f)), // More compact card size
                    Location = new Point(x, y), // Scaled margin
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
                // TOTP code with better vertical centering
                Label codeLabel = new Label
                {
                    Text = TotpService.FormatCode(TotpService.ComputeCode(acc)), // Show real code immediately
                    Font = ScaleFont("SF Mono", 16, FontStyle.Regular), // Responsive monospace font
                    ForeColor = Color.FromArgb(0, 122, 255), // Fully opaque macOS accent blue
                    Location = new Point(Em(0.8f), Em(2.0f)), // Better centered position
                    Size = new Size(Em(8f), Em(1.8f)),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
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
                // Copy functionality for both card and button
                EventHandler copyAction = (sender, args) =>
                {
                    if (args is MouseEventArgs me && me.Button != MouseButtons.Left) return;
                    Clipboard.SetText(TotpService.ComputeCode(acc));
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
                var menu = CreateCardMenu(name, acc);
                accountCard.ContextMenuStrip = menu;
                accountCard.Disposed += (s, e) => menu.Dispose();
                accountCard.Controls.Add(nameLabel);
                accountCard.Controls.Add(codeLabel);
                accountCard.Controls.Add(copyButton);
                controls[name] = codeLabel;
                return accountCard;
            }
            void BuildContent()
            {
                scrollPanel.SuspendLayout();
                scrollPanel.AutoScroll = false;
                while (scrollPanel.Controls.Count > 0) scrollPanel.Controls[0].Dispose();
                controls.Clear();
                var columnY = new int[columnCount];
                for (int i = 0; i < items.Count; i++)
                {
                    int column = itemColumns[i];
                    int x = (column * slotWidth) + cardMargin;
                    int y = columnY[column];
                    columnY[column] += items[i].Height;
                    scrollPanel.Controls.Add(items[i].Folder != null ? CreateFolderHeader(items[i], x, y) : CreateCard(items[i].AccountName!, x, y));
                }
                if (overflow)
                {
                    scrollPanel.AutoScrollMinSize = new Size(0, contentHeight + Em(0.3f));
                    scrollPanel.AutoScroll = true;
                }
                scrollPanel.ResumeLayout();
            }
            BuildContent();
            // Single update timer - reduced frequency for efficiency
            var timer = new System.Windows.Forms.Timer { Interval = 500 }; // Update every 500ms instead of 100ms
            timer.Tick += (s, e) =>
            {
                int remaining = TotpService.SecondsRemaining();
                // Update global timer in header
                globalTimerLabel.Text = $"{remaining}s";
                // Color transitions for global timer
                globalTimerLabel.ForeColor = TimerColor(remaining);
                foreach (var kvp in controls)
                {
                    if (!data.Accounts.TryGetValue(kvp.Key, out var acc)) continue;
                    string formattedCode = TotpService.FormatCode(TotpService.ComputeCode(acc));
                    var codeLabel = kvp.Value;
                    if (!codeLabel.Text.Equals(formattedCode))
                    {
                        codeLabel.Text = formattedCode;
                        AnimateLabel(codeLabel);
                    }
                }
            };
            timer.Start();
            popup.Deactivate += (s, e) =>
            {
                if (popup.KeepOpen) return;
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
        static Color TimerColor(int remaining) =>
            remaining <= 5 ? Color.FromArgb(255, 59, 48) :
            remaining <= 10 ? Color.FromArgb(255, 149, 0) :
            Color.FromArgb(0, 122, 255);
        public static bool ConfirmRemove(IWin32Window owner, string accountName)
        {
            return MessageBox.Show(owner,
                $"Remove '{accountName}'?\n\nIts secret will be deleted and cannot be recovered.",
                "Remove Account", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }
        class PopupItem
        {
            public string? AccountName { get; set; }
            public string? Folder { get; set; }
            public int Count { get; set; }
            public bool Collapsed { get; set; }
            public int Height { get; set; }
            public bool KeepWithNext { get; set; }
        }
        // Ungrouped accounts first, then each non-empty folder header followed by its cards unless collapsed
        static List<PopupItem> BuildPopupItems(AccountData data, int cardStep, int folderStep)
        {
            var items = new List<PopupItem>();
            foreach (var kvp in data.Accounts)
            {
                if (kvp.Value.Folder == null || !data.Folders.Contains(kvp.Value.Folder))
                    items.Add(new PopupItem { AccountName = kvp.Key, Height = cardStep });
            }
            foreach (string folder in data.Folders)
            {
                var members = data.Accounts.Where(kvp => kvp.Value.Folder == folder).Select(kvp => kvp.Key).ToList();
                if (members.Count == 0) continue;
                bool collapsed = data.CollapsedFolders.Contains(folder);
                items.Add(new PopupItem { Folder = folder, Count = members.Count, Collapsed = collapsed, Height = folderStep, KeepWithNext = !collapsed });
                if (collapsed) continue;
                foreach (string name in members)
                    items.Add(new PopupItem { AccountName = name, Height = cardStep });
            }
            return items;
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
    }
}
