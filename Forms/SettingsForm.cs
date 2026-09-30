namespace AuthenticatorTray
{
    public class SettingsForm : RoundedForm
    {
        private TextBox? nameTextBox;
        private TextBox? secretTextBox;
        private TextBox? digitsTextBox;
        private TextBox? algorithmTextBox;
        private ComboBox? folderComboBox;
        private Button? addButton;
        private ListBox? folderListBox;
        private ListBox? accountListBox;
        private ComboBox? accountFolderComboBox;
        private AccountJson? pendingAccount;
        private bool refreshing;
        // Set when accounts.json changed, so the popup gets reopened with fresh data
        public bool AccountsChanged { get; private set; }
        public SettingsForm()
        {
            InitializeComponent();
            RefreshLists();
        }
        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.Width = UiScale.ScreenWidth(25);
            this.Height = UiScale.Em(52f); // Room for the folder and account sections
            this.TopMost = true;
            this.ShowInTaskbar = false;
            Panel header = new Panel
            {
                Height = UiScale.Em(3.5f),
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            Label titleLabel = new Label
            {
                Text = "Settings",
                Font = UiScale.ScaleFont("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(28, 28, 30),
                Location = new Point(UiScale.Em(1.5f), UiScale.Em(1.2f)),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            header.Controls.Add(titleLabel);
            this.Controls.Add(header);
            Panel contentPanel = new Panel
            {
                Location = new Point(0, UiScale.Em(3.5f)),
                Size = new Size(this.Width, this.Height - UiScale.Em(3.5f)),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            Button scanButton = new Button
            {
                Text = "📷 Scan QR Code from Image",
                Font = UiScale.ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 122, 255),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(UiScale.Em(1.5f), UiScale.Em(2f)),
                Size = new Size(this.Width - UiScale.Em(3f), UiScale.Em(2.5f)),
                Cursor = Cursors.Hand
            };
            scanButton.FlatAppearance.BorderSize = 0;
            scanButton.Click += ScanButton_Click;
            contentPanel.Controls.Add(scanButton);
            int fieldY = UiScale.Em(5.5f);
            int fieldHeight = UiScale.Em(2.5f);
            int fieldSpacing = UiScale.Em(2.8f);
            int labelWidth = UiScale.Em(6f);
            int fieldWidth = this.Width - UiScale.Em(3f) - labelWidth - UiScale.Em(1f);
            nameTextBox = AddField(contentPanel, "Name:", "Account name", ref fieldY);
            secretTextBox = AddField(contentPanel, "Secret:", "Base32 secret key", ref fieldY);
            digitsTextBox = AddField(contentPanel, "Digits:", "6", ref fieldY);
            algorithmTextBox = AddField(contentPanel, "Algorithm:", "SHA1", ref fieldY);
            contentPanel.Controls.Add(CreateFieldLabel("Folder:", fieldY));
            folderComboBox = new ComboBox
            {
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(UiScale.Em(1.5f) + labelWidth, fieldY),
                Size = new Size(fieldWidth, fieldHeight),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            contentPanel.Controls.Add(folderComboBox);
            fieldY += fieldSpacing + UiScale.Em(1f);
            int halfWidth = (this.Width - UiScale.Em(4.5f)) / 2;
            addButton = new Button
            {
                Text = "Add Account",
                Font = UiScale.ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 199, 89),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(UiScale.Em(1.5f), fieldY),
                Size = new Size(halfWidth, UiScale.Em(2.5f)),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += AddButton_Click;
            Button cancelButton = CreateSecondaryButton("Close", UiScale.Em(1.5f) + halfWidth + UiScale.Em(1.5f), fieldY, halfWidth, UiScale.Em(2.5f));
            cancelButton.Font = UiScale.ScaleFont("Segoe UI", 10, FontStyle.Regular);
            cancelButton.Click += (s, e) => this.Close();
            this.CancelButton = cancelButton;
            contentPanel.Controls.Add(addButton);
            contentPanel.Controls.Add(cancelButton);
            // Folder and account management below the add form
            int sectionY = fieldY + UiScale.Em(2.5f) + UiScale.Em(1.5f);
            int sideWidth = UiScale.Em(8f);
            int listWidth = this.Width - UiScale.Em(3f) - sideWidth - UiScale.Em(1f);
            int sideX = UiScale.Em(1.5f) + listWidth + UiScale.Em(1f);
            contentPanel.Controls.Add(CreateSectionLabel("Folders", sectionY));
            sectionY += UiScale.Em(2f);
            folderListBox = CreateListBox(sectionY, listWidth, UiScale.Em(7f));
            contentPanel.Controls.Add(folderListBox);
            Button newFolderButton = CreateSecondaryButton("New…", sideX, sectionY, sideWidth, UiScale.Em(2f));
            Button renameFolderButton = CreateSecondaryButton("Rename…", sideX, sectionY + UiScale.Em(2.5f), sideWidth, UiScale.Em(2f));
            Button deleteFolderButton = CreateSecondaryButton("Delete", sideX, sectionY + UiScale.Em(5f), sideWidth, UiScale.Em(2f));
            newFolderButton.Click += (s, e) => CreateFolder();
            renameFolderButton.Click += (s, e) => RenameFolder();
            deleteFolderButton.Click += (s, e) => DeleteFolder();
            contentPanel.Controls.Add(newFolderButton);
            contentPanel.Controls.Add(renameFolderButton);
            contentPanel.Controls.Add(deleteFolderButton);
            sectionY += UiScale.Em(7f) + UiScale.Em(1.5f);
            contentPanel.Controls.Add(CreateSectionLabel("Accounts", sectionY));
            sectionY += UiScale.Em(2f);
            accountListBox = CreateListBox(sectionY, listWidth, UiScale.Em(9f));
            accountListBox.SelectedIndexChanged += (s, e) => SyncAccountFolder();
            contentPanel.Controls.Add(accountListBox);
            accountFolderComboBox = new ComboBox
            {
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(sideX, sectionY),
                Size = new Size(sideWidth, UiScale.Em(2f)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            accountFolderComboBox.SelectedIndexChanged += (s, e) => AssignAccountFolder();
            contentPanel.Controls.Add(accountFolderComboBox);
            Button removeButton = new Button
            {
                Text = "Remove…",
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(255, 59, 48),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(sideX, sectionY + UiScale.Em(2.5f)),
                Size = new Size(sideWidth, UiScale.Em(2f)),
                Cursor = Cursors.Hand
            };
            removeButton.FlatAppearance.BorderSize = 0;
            removeButton.MouseEnter += (s, e) => removeButton.BackColor = Color.FromArgb(215, 40, 30);
            removeButton.MouseLeave += (s, e) => removeButton.BackColor = Color.FromArgb(255, 59, 48);
            removeButton.Click += (s, e) => RemoveAccount();
            contentPanel.Controls.Add(removeButton);
            this.Controls.Add(contentPanel);
            scanButton.MouseEnter += (s, e) => scanButton.BackColor = Color.FromArgb(0, 100, 200);
            scanButton.MouseLeave += (s, e) => scanButton.BackColor = Color.FromArgb(0, 122, 255);
            addButton.MouseEnter += (s, e) => { if (addButton.Enabled) addButton.BackColor = Color.FromArgb(40, 180, 70); };
            addButton.MouseLeave += (s, e) => { if (addButton.Enabled) addButton.BackColor = Color.FromArgb(52, 199, 89); };
        }
        private Label CreateFieldLabel(string text, int fieldY)
        {
            return new Label
            {
                Text = text,
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(UiScale.Em(1.5f), fieldY + UiScale.Em(0.5f)),
                Size = new Size(UiScale.Em(6f), UiScale.Em(1.5f)),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
        private TextBox AddField(Panel panel, string label, string placeholder, ref int fieldY)
        {
            int labelWidth = UiScale.Em(6f);
            TextBox textBox = new TextBox
            {
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(UiScale.Em(1.5f) + labelWidth, fieldY),
                Size = new Size(this.Width - UiScale.Em(3f) - labelWidth - UiScale.Em(1f), UiScale.Em(2.5f)),
                PlaceholderText = placeholder,
                Enabled = false
            };
            panel.Controls.Add(CreateFieldLabel(label, fieldY));
            panel.Controls.Add(textBox);
            fieldY += UiScale.Em(2.8f);
            return textBox;
        }
        private static Label CreateSectionLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = UiScale.ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(28, 28, 30),
                Location = new Point(UiScale.Em(1.5f), y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
        }
        private static ListBox CreateListBox(int y, int width, int height)
        {
            return new ListBox
            {
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(UiScale.Em(1.5f), y),
                Size = new Size(width, height),
                IntegralHeight = false
            };
        }
        internal static Button CreateSecondaryButton(string text, int x, int y, int width, int height)
        {
            Button button = new Button
            {
                Text = text,
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 67),
                BackColor = Color.FromArgb(245, 245, 245),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.MouseEnter += (s, e) => button.BackColor = Color.FromArgb(230, 230, 230);
            button.MouseLeave += (s, e) => button.BackColor = Color.FromArgb(245, 245, 245);
            return button;
        }
        private class AccountEntry
        {
            public string Name { get; set; } = string.Empty;
            public string? Folder { get; set; }
            public override string ToString() => Folder == null ? Name : $"{Name}  ·  {Folder}";
        }
        private const string NoFolder = "No folder";
        private void RefreshLists()
        {
            var data = AccountStore.LoadData();
            refreshing = true;
            string? selectedFolder = folderListBox!.SelectedItem as string;
            string? selectedAccount = (accountListBox!.SelectedItem as AccountEntry)?.Name;
            object? addFolder = folderComboBox!.SelectedItem;
            folderListBox.Items.Clear();
            folderListBox.Items.AddRange(data.Folders.ToArray());
            if (selectedFolder != null && data.Folders.Contains(selectedFolder)) folderListBox.SelectedItem = selectedFolder;
            folderComboBox.Items.Clear();
            folderComboBox.Items.Add(NoFolder);
            folderComboBox.Items.AddRange(data.Folders.ToArray());
            folderComboBox.SelectedItem = addFolder != null && folderComboBox.Items.Contains(addFolder) ? addFolder : NoFolder;
            accountFolderComboBox!.Items.Clear();
            accountFolderComboBox.Items.Add(NoFolder);
            accountFolderComboBox.Items.AddRange(data.Folders.ToArray());
            accountListBox.Items.Clear();
            foreach (var kvp in data.Accounts)
            {
                var entry = new AccountEntry { Name = kvp.Key, Folder = kvp.Value.Folder };
                accountListBox.Items.Add(entry);
                if (kvp.Key == selectedAccount) accountListBox.SelectedItem = entry;
            }
            refreshing = false;
            SyncAccountFolder();
        }
        private void SyncAccountFolder()
        {
            var entry = accountListBox!.SelectedItem as AccountEntry;
            refreshing = true;
            accountFolderComboBox!.Enabled = entry != null;
            accountFolderComboBox.SelectedItem = entry == null ? null : entry.Folder ?? NoFolder;
            refreshing = false;
        }
        // Reload from disk, change, save, refresh the lists
        private void Mutate(Action<AccountData> change)
        {
            var data = AccountStore.LoadData();
            change(data);
            if (AccountStore.SaveData(data)) AccountsChanged = true;
            RefreshLists();
        }
        private string? PromptFolderName(string title, string initial, string? current)
        {
            string? name = InputForm.Prompt(this, title, "Folder name", initial)?.Trim();
            if (string.IsNullOrEmpty(name)) return null;
            string? existing = AccountStore.LoadData().FindFolder(name);
            if (existing != null && existing != current)
            {
                MessageBox.Show(this, $"A folder named '{existing}' already exists.", "Folder Exists",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return name;
        }
        private void CreateFolder()
        {
            string? name = PromptFolderName("New Folder", "", null);
            if (name == null) return;
            Mutate(d => d.AddFolder(name));
            folderListBox!.SelectedItem = name;
        }
        private void RenameFolder()
        {
            if (folderListBox!.SelectedItem is not string folder) return;
            string? name = PromptFolderName("Rename Folder", folder, folder);
            if (name == null || name == folder) return;
            Mutate(d => d.RenameFolder(folder, name));
            folderListBox.SelectedItem = name;
        }
        private void DeleteFolder()
        {
            if (folderListBox!.SelectedItem is not string folder) return;
            int count = AccountStore.LoadData().Accounts.Values.Count(a => a.Folder == folder);
            string message = count == 0
                ? $"Delete folder '{folder}'?"
                : $"Delete folder '{folder}'?\n\nIts {count} account(s) will be moved out of the folder, not deleted.";
            if (MessageBox.Show(this, message, "Delete Folder", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            Mutate(d => d.DeleteFolder(folder));
        }
        private void AssignAccountFolder()
        {
            if (refreshing || accountListBox!.SelectedItem is not AccountEntry entry) return;
            string? folder = accountFolderComboBox!.SelectedItem as string;
            if (folder == NoFolder) folder = null;
            if (folder == entry.Folder) return;
            Mutate(d =>
            {
                if (d.Accounts.TryGetValue(entry.Name, out var account)) account.Folder = folder;
            });
        }
        private void RemoveAccount()
        {
            if (accountListBox!.SelectedItem is not AccountEntry entry) return;
            if (!TrayPopup.ConfirmRemove(this, entry.Name)) return;
            Mutate(d => d.Accounts.Remove(entry.Name));
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
                        string selectedImagePath = dialog.FileName;
                        using (var testImage = new Bitmap(selectedImagePath))
                        {
                            string debugInfo = $"Image loaded successfully:\n" +
                                             $"Path: {selectedImagePath}\n" +
                                             $"Size: {new FileInfo(selectedImagePath).Length} bytes\n" +
                                             $"Dimensions: {testImage.Width}x{testImage.Height}\n" +
                                             $"Format: {testImage.PixelFormat}";
                            System.Diagnostics.Debug.WriteLine(debugInfo);
                        }
                        string? qrText = QrCodeService.DecodeQrCodeFromImage(selectedImagePath);
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
                        AccountJson? account = QrCodeService.ParseOtpAuthUrl(qrText);
                        if (account == null)
                        {
                            string preview = qrText.Length > 100 ? qrText.Substring(0, 100) + "..." : qrText;
                            MessageBox.Show($"Invalid QR code format. Expected otpauth:// URL.\n\nFound: {preview}",
                                "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        pendingAccount = account;
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
                        if (folderComboBox != null)
                        {
                            folderComboBox.Enabled = true;
                        }
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
            string name = nameTextBox?.Text?.Trim() ?? "";
            string secret = secretTextBox?.Text?.Trim() ?? "";
            string digitsStr = digitsTextBox?.Text?.Trim() ?? "6";
            string algorithm = algorithmTextBox?.Text?.Trim()?.ToUpper() ?? "SHA1";
            string? folder = folderComboBox?.SelectedItem as string;
            if (folder == NoFolder) folder = null;
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
                var data = AccountStore.LoadData();
                if (data.Accounts.ContainsKey(name))
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
                data.Accounts[name] = new Account
                {
                    Secret = secret,
                    Digits = digits,
                    Algorithm = algorithm,
                    Folder = folder == null ? null : data.AddFolder(folder)
                };
                if (!AccountStore.SaveData(data))
                {
                    return;
                }
                MessageBox.Show($"Account '{name}' added successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                AccountsChanged = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding account: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
