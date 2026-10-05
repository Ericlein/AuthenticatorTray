using System.Runtime.InteropServices;
using static AuthenticatorTray.UiScale;
namespace AuthenticatorTray
{
    static class TrayPopup
    {
        static readonly Color Surface = Color.FromArgb(250, 250, 250);
        static readonly Color HeaderBack = Color.FromArgb(248, 248, 248);
        static readonly Color HeaderLine = Color.FromArgb(240, 240, 240);
        static readonly Color PrimaryText = Color.FromArgb(28, 28, 30);
        static readonly Color SecondaryText = Color.FromArgb(60, 60, 67);
        static readonly Color Accent = Color.FromArgb(0, 122, 255);
        static readonly Color AccentDark = Color.FromArgb(0, 80, 180);
        static readonly Color CodeHover = Color.FromArgb(0, 100, 200);
        static readonly Color CardBack = Color.FromArgb(245, 245, 245);
        static readonly Color CardHover = Color.FromArgb(230, 235, 240);
        static readonly Color Success = Color.FromArgb(52, 199, 89);
        static readonly Color SuccessTint = Color.FromArgb(240, 248, 242);
        static readonly Color Warning = Color.FromArgb(255, 149, 0);
        static readonly Color Danger = Color.FromArgb(255, 59, 48);
        const string CopyIcon = "📋";
        const string CopiedIcon = "✅";

        public static void ShowPopup() => new PopupSession().Show();

        public static bool ConfirmRemove(IWin32Window owner, string accountName)
        {
            return MessageBox.Show(owner,
                $"Remove '{accountName}'?\n\nIts secret will be deleted and cannot be recovered.",
                "Remove Account", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        static Color TimerColor(int remaining) =>
            remaining <= 5 ? Danger :
            remaining <= 10 ? Warning :
            Accent;

        // One-shot UI timer
        static void After(int interval, Action action)
        {
            var timer = new System.Windows.Forms.Timer { Interval = interval };
            timer.Tick += (s, e) =>
            {
                timer.Dispose();
                action();
            };
            timer.Start();
        }

        static void AnimateLabel(Label label)
        {
            var originalColor = label.ForeColor;
            label.ForeColor = Color.FromArgb(150, originalColor);
            var animTimer = new System.Windows.Forms.Timer { Interval = 40 };
            int alpha = 150;
            animTimer.Tick += (s, e) =>
            {
                alpha += 25;
                if (alpha >= 255 || label.IsDisposed)
                {
                    animTimer.Dispose();
                    if (!label.IsDisposed) label.ForeColor = originalColor;
                }
                else
                {
                    label.ForeColor = Color.FromArgb(alpha, originalColor);
                }
            };
            animTimer.Start();
        }

        static string GetDisplayName(string fullName)
        {
            int paren = fullName.IndexOf('(');
            if (paren >= 0) return fullName.Substring(0, paren).Trim();
            return fullName.Length > 22 ? fullName.Substring(0, 19) + "..." : fullName;
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

        // Shared fonts for one popup, disposed with it
        sealed class PopupFonts : IDisposable
        {
            public readonly Font Title = ScaleFont("Segoe UI", 12);
            public readonly Font Text = ScaleFont("Segoe UI", 9);
            public readonly Font Small = ScaleFont("Segoe UI", 8);
            public readonly Font Code = ScaleFont("SF Mono", 16);
            public readonly Font HeaderIcon = ScaleFont("Segoe UI Emoji", 12);
            public readonly Font CardIcon = ScaleFont("Segoe UI Emoji", 14);
            public void Dispose()
            {
                foreach (var font in new[] { Title, Text, Small, Code, HeaderIcon, CardIcon }) font.Dispose();
            }
        }

        sealed class PopupSession
        {
            readonly Rectangle screen;
            readonly int headerHeight, panelMargin, cardMargin, cardStep, folderStep, slotWidth, marginX, marginY, scrollBarWidth;
            readonly PopupFonts fonts = new();
            readonly ModernPopupForm popup;
            readonly Dictionary<string, Label> codeLabels = new();
            readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 500 };
            AccountData data;
            List<PopupItem> items = new();
            int[] itemColumns = Array.Empty<int>();
            int columnCount = 1, contentHeight;
            bool overflow;
            int offsetX, finalY;
            Label titleLabel = null!, timerLabel = null!;
            Panel contentPanel = null!, scrollPanel = null!;

            public PopupSession()
            {
                screen = Screen.FromPoint(Cursor.Position).WorkingArea;
                UseMonitorAt(Cursor.Position);
                data = AccountStore.LoadData();
                headerHeight = Em(3.2f);
                panelMargin = Em(0.8f);
                cardMargin = Em(0.3f);
                cardStep = Em(4.6f); // 4.2f card + 0.4f spacing
                folderStep = Em(2.2f);
                int columnWidth = Math.Max((int)(screen.Width * 0.22), Em(17.5f)); // 22% of the working area, but never narrower than a card
                slotWidth = columnWidth - (panelMargin * 2);
                marginX = (int)(screen.Width * 0.01);
                marginY = (int)(screen.Height * 0.01);
                scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                popup = new ModernPopupForm
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = Surface,
                    TopMost = true,
                    ShowInTaskbar = false
                };
            }

            public void Show()
            {
                Plan();
                popup.Location = new Point(offsetX, screen.Bottom);
                popup.Show();
                StartSlideIn();
                popup.Controls.Add(CreateHeader());
                CreateContentPanels();
                BuildContent();
                refreshTimer.Tick += (s, e) => RefreshCodes();
                refreshTimer.Start();
                popup.Deactivate += (s, e) =>
                {
                    if (!popup.KeepOpen) popup.Close();
                };
                popup.FormClosed += (s, e) => refreshTimer.Dispose();
                popup.Disposed += (s, e) => fonts.Dispose();
                popup.Activate();
            }

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
                popup.Height = headerHeight + (overflow ? maxColumnHeight : contentHeight) + Em(1.5f);
                // Bottom-right near the tray, kept inside the working area
                offsetX = screen.Right - (int)(screen.Width * 0.12) - popup.Width;
                finalY = screen.Bottom - (int)(screen.Height * 0.08) - popup.Height;
                offsetX = Math.Max(screen.Left + marginX, Math.Min(offsetX, screen.Right - popup.Width - marginX));
                finalY = Math.Max(screen.Top + marginY, Math.Min(finalY, screen.Bottom - popup.Height - marginY));
            }

            // 200ms ease-out slide up from the bottom of the working area
            void StartSlideIn()
            {
                const double duration = 200;
                int startY = screen.Bottom;
                DateTime startTime = DateTime.Now;
                var slideTimer = new System.Windows.Forms.Timer { Interval = 10 };
                slideTimer.Tick += (s, e) =>
                {
                    if (popup.IsDisposed)
                    {
                        slideTimer.Dispose();
                        return;
                    }
                    double progress = Math.Min((DateTime.Now - startTime).TotalMilliseconds / duration, 1.0);
                    double eased = 1 - Math.Pow(1 - progress, 3);
                    popup.Location = new Point(offsetX, (int)(startY - (startY - finalY) * eased));
                    if (progress >= 1.0) slideTimer.Dispose();
                };
                slideTimer.Start();
            }

            Panel CreateHeader()
            {
                Panel header = new Panel
                {
                    Height = headerHeight,
                    Dock = DockStyle.Top,
                    BackColor = HeaderBack
                };
                header.Controls.Add(new Panel
                {
                    Height = 1,
                    Dock = DockStyle.Bottom,
                    BackColor = HeaderLine
                });
                titleLabel = new Label
                {
                    Text = "Eric's super duper secure auth",
                    Font = fonts.Title,
                    ForeColor = PrimaryText,
                    Location = new Point(0, Em(0.8f)),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                int remaining = TotpService.SecondsRemaining();
                Label settingsIcon = new Label
                {
                    Text = "⚙️",
                    Font = fonts.HeaderIcon,
                    ForeColor = Accent,
                    Location = new Point(Em(0.8f), Em(0.8f)),
                    Size = new Size(Em(2f), Em(2f)),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                settingsIcon.MouseEnter += (s, e) => settingsIcon.ForeColor = AccentDark;
                settingsIcon.MouseLeave += (s, e) => settingsIcon.ForeColor = Accent;
                settingsIcon.Click += (s, e) => OpenSettings();
                timerLabel = new Label
                {
                    Text = $"{remaining}s",
                    Font = fonts.Title,
                    ForeColor = TimerColor(remaining),
                    Size = new Size(Em(3f), Em(2f)),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                header.Controls.Add(settingsIcon);
                header.Controls.Add(titleLabel);
                header.Controls.Add(timerLabel);
                LayoutHeader();
                // Re-center once WinForms rescales the font for this monitor's DPI
                titleLabel.SizeChanged += (s, e) => CenterTitle();
                return header;
            }

            void CenterTitle() => titleLabel.Location = new Point((popup.Width - titleLabel.Width) / 2, Em(0.8f));

            void LayoutHeader()
            {
                timerLabel.Location = new Point(popup.Width - Em(3.5f), Em(0.8f));
                CenterTitle();
            }

            void CreateContentPanels()
            {
                contentPanel = new Panel
                {
                    Location = new Point(0, headerHeight),
                    BackColor = Surface
                };
                scrollPanel = new Panel
                {
                    Location = new Point(panelMargin, Em(0.6f)),
                    AutoScroll = false, // Only scrolls when the columns can't fit on screen
                    BackColor = Surface
                };
                SizeContentPanels();
                contentPanel.Controls.Add(scrollPanel);
                popup.Controls.Add(contentPanel);
            }

            void SizeContentPanels()
            {
                contentPanel.Size = new Size(popup.Width, popup.Height - headerHeight);
                scrollPanel.Size = new Size(contentPanel.Width - (panelMargin * 2), contentPanel.Height - Em(1.2f));
            }

            void OpenSettings()
            {
                bool changed;
                using (var settingsForm = new SettingsForm())
                {
                    settingsForm.ShowDialog();
                    changed = settingsForm.AccountsChanged;
                }
                if (!changed) return;
                if (!popup.IsDisposed) popup.Close();
                ShowPopup();
            }

            void RefreshCodes()
            {
                int remaining = TotpService.SecondsRemaining();
                timerLabel.Text = $"{remaining}s";
                timerLabel.ForeColor = TimerColor(remaining);
                foreach (var (name, codeLabel) in codeLabels)
                {
                    if (!data.Accounts.TryGetValue(name, out var acc)) continue;
                    string formattedCode = TotpService.FormatCode(TotpService.ComputeCode(acc));
                    if (codeLabel.Text != formattedCode)
                    {
                        codeLabel.Text = formattedCode;
                        AnimateLabel(codeLabel);
                    }
                }
            }

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
                LayoutHeader();
                SizeContentPanels();
                BuildContent();
            }

            // Reload from disk, change, save, then re-layout
            void Mutate(Action<AccountData> change)
            {
                var fresh = AccountStore.LoadData();
                change(fresh);
                AccountStore.SaveData(fresh);
                data = AccountStore.LoadData();
                if (popup.IsHandleCreated && !popup.IsDisposed) popup.BeginInvoke(new Action(Relayout));
            }

            void BuildContent()
            {
                scrollPanel.SuspendLayout();
                scrollPanel.AutoScroll = false;
                while (scrollPanel.Controls.Count > 0) scrollPanel.Controls[0].Dispose();
                codeLabels.Clear();
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

            Label CreateFolderHeader(PopupItem item, int x, int y)
            {
                string folder = item.Folder!;
                Label folderLabel = new Label
                {
                    Text = $"{(item.Collapsed ? "▸" : "▾")}  {folder}  ({item.Count})",
                    Font = fonts.Text,
                    ForeColor = SecondaryText,
                    Location = new Point(x + Em(0.5f), y + Em(0.2f)),
                    Size = new Size(slotWidth - Em(0.6f) - Em(0.5f), Em(1.8f)),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent,
                    Tag = "folder:" + folder
                };
                folderLabel.MouseEnter += (s, e) => folderLabel.ForeColor = Accent;
                folderLabel.MouseLeave += (s, e) => folderLabel.ForeColor = SecondaryText;
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

            Panel CreateCard(string name, int x, int y)
            {
                Account acc = data.Accounts[name];
                Panel card = new MacCard
                {
                    Size = new Size(slotWidth - Em(0.6f), Em(4.2f)),
                    Location = new Point(x, y),
                    BackColor = CardBack,
                    Cursor = Cursors.Hand
                };
                Label nameLabel = new Label
                {
                    Text = GetDisplayName(name),
                    Font = fonts.Text,
                    ForeColor = SecondaryText,
                    Location = new Point(Em(0.8f), Em(0.5f)),
                    Size = new Size(Em(12f), Em(1.2f)),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
                Label codeLabel = new Label
                {
                    Text = TotpService.FormatCode(TotpService.ComputeCode(acc)),
                    Font = fonts.Code,
                    ForeColor = Accent,
                    Location = new Point(Em(0.8f), Em(2.0f)),
                    Size = new Size(Em(8f), Em(1.8f)),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
                Label copyButton = new Label
                {
                    Text = CopyIcon,
                    Font = fonts.CardIcon,
                    ForeColor = Accent,
                    Location = new Point(card.Width - Em(2.5f), Em(1.4f)),
                    Size = new Size(Em(2f), Em(2f)),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                card.MouseEnter += (s, e) =>
                {
                    card.BackColor = CardHover;
                    copyButton.ForeColor = AccentDark;
                    codeLabel.ForeColor = CodeHover;
                };
                card.MouseLeave += (s, e) =>
                {
                    card.BackColor = CardBack;
                    copyButton.ForeColor = Accent;
                    codeLabel.ForeColor = Accent;
                };
                EventHandler copyAction = (sender, args) =>
                {
                    if (args is MouseEventArgs me && me.Button != MouseButtons.Left) return;
                    CopyCode(acc, card, copyButton);
                };
                card.Click += copyAction;
                copyButton.Click += copyAction;
                var menu = CreateCardMenu(name, acc);
                card.ContextMenuStrip = menu;
                card.Disposed += (s, e) => menu.Dispose();
                card.Controls.Add(nameLabel);
                card.Controls.Add(codeLabel);
                card.Controls.Add(copyButton);
                codeLabels[name] = codeLabel;
                return card;
            }

            void CopyCode(Account acc, Panel card, Label copyButton)
            {
                try
                {
                    Clipboard.SetText(TotpService.ComputeCode(acc));
                }
                catch (ExternalException)
                {
                    return; // Clipboard held by another process
                }
                copyButton.Text = CopiedIcon;
                copyButton.ForeColor = Success;
                card.BackColor = SuccessTint;
                After(800, () =>
                {
                    copyButton.Text = CopyIcon;
                    copyButton.ForeColor = Accent;
                    card.BackColor = CardBack;
                });
                ShowCopiedTooltip(card);
            }

            void ShowCopiedTooltip(Control nearControl)
            {
                Label tooltip = new Label
                {
                    Text = "Copied",
                    Font = fonts.Small,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(80, 80, 80),
                    AutoSize = true,
                    Padding = new Padding(Em(0.3f), Em(0.15f), Em(0.3f), Em(0.15f))
                };
                Point loc = popup.PointToClient(nearControl.PointToScreen(Point.Empty));
                tooltip.Location = new Point(loc.X + nearControl.Width / 2 - Em(1.5f), loc.Y - Em(1.5f));
                popup.Controls.Add(tooltip);
                tooltip.BringToFront();
                After(600, () =>
                {
                    popup.Controls.Remove(tooltip);
                    tooltip.Dispose();
                });
            }

            ContextMenuStrip CreateCardMenu(string accountName, Account acc)
            {
                var menu = new ContextMenuStrip { Font = fonts.Text };
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
                    if (WithDialog(() => ConfirmRemove(popup, accountName))) Mutate(d => d.Accounts.Remove(accountName));
                });
                menu.Items.Add(moveItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(removeItem);
                return menu;
            }
        }
    }
}
