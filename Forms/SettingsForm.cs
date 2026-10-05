using System.Diagnostics.CodeAnalysis;
namespace AuthenticatorTray
{
    public class SettingsForm : RoundedForm
    {
        private const string NoFolder = "No folder";
        private TextBox nameTextBox;
        private TextBox secretTextBox;
        private TextBox digitsTextBox;
        private TextBox algorithmTextBox;
        private ComboBox folderComboBox;
        private Button addButton;
        private ListBox folderListBox;
        private ListBox accountListBox;
        private ComboBox accountFolderComboBox;
        private readonly Font titleFont = UiScale.ScaleFont("Segoe UI", 12);
        private readonly Font textFont = UiScale.ScaleFont("Segoe UI", 9);
        private readonly Font buttonFont = UiScale.ScaleFont("Segoe UI", 10);
        private bool refreshing;
        // Set when accounts.json changed, so the popup gets reopened with fresh data
        public bool AccountsChanged { get; private set; }
        public SettingsForm()
        {
            InitializeComponent();
            RefreshLists();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                titleFont.Dispose();
                textFont.Dispose();
                buttonFont.Dispose();
            }
        }
        [MemberNotNull(nameof(nameTextBox), nameof(secretTextBox), nameof(digitsTextBox), nameof(algorithmTextBox),
            nameof(folderComboBox), nameof(addButton), nameof(folderListBox), nameof(accountListBox), nameof(accountFolderComboBox))]
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
            header.Controls.Add(new Label
            {
                Text = "Settings",
                Font = titleFont,
                ForeColor = Color.FromArgb(28, 28, 30),
                Location = new Point(UiScale.Em(1.5f), UiScale.Em(1.2f)),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            this.Controls.Add(header);
            Panel contentPanel = new Panel
            {
                Location = new Point(0, UiScale.Em(3.5f)),
                Size = new Size(this.Width, this.Height - UiScale.Em(3.5f)),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            int fieldY = BuildAddSection(contentPanel);
            BuildManageSection(contentPanel, fieldY + UiScale.Em(2.5f) + UiScale.Em(1.5f));
            this.Controls.Add(contentPanel);
        }
        // Scan button, account fields and Add/Close; returns the Y of the Add/Close row
        [MemberNotNull(nameof(nameTextBox), nameof(secretTextBox), nameof(digitsTextBox), nameof(algorithmTextBox),
            nameof(folderComboBox), nameof(addButton))]
        private int BuildAddSection(Panel panel)
        {
            Button scanButton = CreatePrimaryButton("📷 Scan QR Code from Image", buttonFont, Color.FromArgb(0, 122, 255), Color.FromArgb(0, 100, 200),
                new Rectangle(UiScale.Em(1.5f), UiScale.Em(2f), this.Width - UiScale.Em(3f), UiScale.Em(2.5f)));
            scanButton.Click += ScanButton_Click;
            panel.Controls.Add(scanButton);
            int fieldY = UiScale.Em(5.5f);
            nameTextBox = AddField(panel, "Name:", "Account name", ref fieldY);
            secretTextBox = AddField(panel, "Secret:", "Base32 secret key", ref fieldY);
            digitsTextBox = AddField(panel, "Digits:", "6", ref fieldY);
            algorithmTextBox = AddField(panel, "Algorithm:", "SHA1", ref fieldY);
            panel.Controls.Add(CreateFieldLabel("Folder:", fieldY));
            folderComboBox = new ComboBox
            {
                Font = textFont,
                Location = new Point(UiScale.Em(1.5f) + UiScale.Em(6f), fieldY),
                Size = new Size(FieldWidth, UiScale.Em(2.5f)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            panel.Controls.Add(folderComboBox);
            fieldY += UiScale.Em(2.8f) + UiScale.Em(1f);
            int halfWidth = (this.Width - UiScale.Em(4.5f)) / 2;
            addButton = CreatePrimaryButton("Add Account", buttonFont, Color.FromArgb(52, 199, 89), Color.FromArgb(40, 180, 70),
                new Rectangle(UiScale.Em(1.5f), fieldY, halfWidth, UiScale.Em(2.5f)));
            addButton.Enabled = false;
            addButton.Click += AddButton_Click;
            Button cancelButton = CreateSecondaryButton("Close", UiScale.Em(1.5f) + halfWidth + UiScale.Em(1.5f), fieldY, halfWidth, UiScale.Em(2.5f));
            cancelButton.Font = buttonFont;
            cancelButton.Click += (s, e) => this.Close();
            this.CancelButton = cancelButton;
            panel.Controls.Add(addButton);
            panel.Controls.Add(cancelButton);
            return fieldY;
        }
        // Folder and account management below the add form
        [MemberNotNull(nameof(folderListBox), nameof(accountListBox), nameof(accountFolderComboBox))]
        private void BuildManageSection(Panel panel, int sectionY)
        {
            int sideWidth = UiScale.Em(8f);
            int listWidth = this.Width - UiScale.Em(3f) - sideWidth - UiScale.Em(1f);
            int sideX = UiScale.Em(1.5f) + listWidth + UiScale.Em(1f);
            panel.Controls.Add(CreateSectionLabel("Folders", sectionY));
            sectionY += UiScale.Em(2f);
            folderListBox = CreateListBox(sectionY, listWidth, UiScale.Em(7f));
            panel.Controls.Add(folderListBox);
            Button newFolderButton = CreateSecondaryButton("New…", sideX, sectionY, sideWidth, UiScale.Em(2f));
            Button renameFolderButton = CreateSecondaryButton("Rename…", sideX, sectionY + UiScale.Em(2.5f), sideWidth, UiScale.Em(2f));
            Button deleteFolderButton = CreateSecondaryButton("Delete", sideX, sectionY + UiScale.Em(5f), sideWidth, UiScale.Em(2f));
            newFolderButton.Click += (s, e) => CreateFolder();
            renameFolderButton.Click += (s, e) => RenameFolder();
            deleteFolderButton.Click += (s, e) => DeleteFolder();
            panel.Controls.Add(newFolderButton);
            panel.Controls.Add(renameFolderButton);
            panel.Controls.Add(deleteFolderButton);
            sectionY += UiScale.Em(7f) + UiScale.Em(1.5f);
            panel.Controls.Add(CreateSectionLabel("Accounts", sectionY));
            sectionY += UiScale.Em(2f);
            accountListBox = CreateListBox(sectionY, listWidth, UiScale.Em(9f));
            accountListBox.SelectedIndexChanged += (s, e) => SyncAccountFolder();
            panel.Controls.Add(accountListBox);
            accountFolderComboBox = new ComboBox
            {
                Font = textFont,
                Location = new Point(sideX, sectionY),
                Size = new Size(sideWidth, UiScale.Em(2f)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            accountFolderComboBox.SelectedIndexChanged += (s, e) => AssignAccountFolder();
            panel.Controls.Add(accountFolderComboBox);
            Button removeButton = CreatePrimaryButton("Remove…", textFont, Color.FromArgb(255, 59, 48), Color.FromArgb(215, 40, 30),
                new Rectangle(sideX, sectionY + UiScale.Em(2.5f), sideWidth, UiScale.Em(2f)));
            removeButton.Click += (s, e) => RemoveAccount();
            panel.Controls.Add(removeButton);
        }
        private int FieldWidth => this.Width - UiScale.Em(3f) - UiScale.Em(6f) - UiScale.Em(1f);
        private Label CreateFieldLabel(string text, int fieldY)
        {
            return new Label
            {
                Text = text,
                Font = textFont,
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(UiScale.Em(1.5f), fieldY + UiScale.Em(0.5f)),
                Size = new Size(UiScale.Em(6f), UiScale.Em(1.5f)),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
        private TextBox AddField(Panel panel, string label, string placeholder, ref int fieldY)
        {
            TextBox textBox = new TextBox
            {
                Font = textFont,
                Location = new Point(UiScale.Em(1.5f) + UiScale.Em(6f), fieldY),
                Size = new Size(FieldWidth, UiScale.Em(2.5f)),
                PlaceholderText = placeholder,
                Enabled = false
            };
            panel.Controls.Add(CreateFieldLabel(label, fieldY));
            panel.Controls.Add(textBox);
            fieldY += UiScale.Em(2.8f);
            return textBox;
        }
        private Label CreateSectionLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = buttonFont,
                ForeColor = Color.FromArgb(28, 28, 30),
                Location = new Point(UiScale.Em(1.5f), y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
        }
        private ListBox CreateListBox(int y, int width, int height)
        {
            return new ListBox
            {
                Font = textFont,
                ForeColor = Color.FromArgb(60, 60, 67),
                Location = new Point(UiScale.Em(1.5f), y),
                Size = new Size(width, height),
                IntegralHeight = false
            };
        }
        // White text on a solid colour; hover only recolours while enabled
        private static Button CreatePrimaryButton(string text, Font font, Color backColor, Color hoverColor, Rectangle bounds)
        {
            Button button = new Button
            {
                Text = text,
                Font = font,
                ForeColor = Color.White,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Location = bounds.Location,
                Size = bounds.Size,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.MouseEnter += (s, e) => { if (button.Enabled) button.BackColor = hoverColor; };
            button.MouseLeave += (s, e) => { if (button.Enabled) button.BackColor = backColor; };
            return button;
        }
        internal static Button CreateSecondaryButton(string text, int x, int y, int width, int height)
        {
            Font font = UiScale.ScaleFont("Segoe UI", 9);
            Button button = new Button
            {
                Text = text,
                Font = font,
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
            // Callers may replace Font, so free ours on dispose
            button.Disposed += (s, e) => font.Dispose();
            return button;
        }
        private sealed class AccountEntry
        {
            public string Name { get; init; } = string.Empty;
            public string? Folder { get; init; }
            public override string ToString() => Folder == null ? Name : $"{Name}  ·  {Folder}";
        }
        private void RefreshLists()
        {
            var data = AccountStore.LoadData();
            string? selectedFolder = folderListBox.SelectedItem as string;
            string? selectedAccount = (accountListBox.SelectedItem as AccountEntry)?.Name;
            object? addFolder = folderComboBox.SelectedItem;
            refreshing = true;
            try
            {
                folderListBox.Items.Clear();
                folderListBox.Items.AddRange(data.Folders.ToArray());
                if (selectedFolder != null && data.Folders.Contains(selectedFolder)) folderListBox.SelectedItem = selectedFolder;
                folderComboBox.Items.Clear();
                folderComboBox.Items.Add(NoFolder);
                folderComboBox.Items.AddRange(data.Folders.ToArray());
                folderComboBox.SelectedItem = addFolder != null && folderComboBox.Items.Contains(addFolder) ? addFolder : NoFolder;
                accountFolderComboBox.Items.Clear();
                accountFolderComboBox.Items.Add(NoFolder);
                accountFolderComboBox.Items.AddRange(data.Folders.ToArray());
                accountListBox.Items.Clear();
                foreach (var kvp in data.Accounts)
                {
                    var entry = new AccountEntry { Name = kvp.Key, Folder = kvp.Value.Folder };
                    accountListBox.Items.Add(entry);
                    if (kvp.Key == selectedAccount) accountListBox.SelectedItem = entry;
                }
            }
            finally
            {
                refreshing = false;
            }
            SyncAccountFolder();
        }
        private void SyncAccountFolder()
        {
            var entry = accountListBox.SelectedItem as AccountEntry;
            refreshing = true;
            try
            {
                accountFolderComboBox.Enabled = entry != null;
                accountFolderComboBox.SelectedItem = entry == null ? null : entry.Folder ?? NoFolder;
            }
            finally
            {
                refreshing = false;
            }
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
            folderListBox.SelectedItem = name;
        }
        private void RenameFolder()
        {
            if (folderListBox.SelectedItem is not string folder) return;
            string? name = PromptFolderName("Rename Folder", folder, folder);
            if (name == null || name == folder) return;
            Mutate(d => d.RenameFolder(folder, name));
            folderListBox.SelectedItem = name;
        }
        private void DeleteFolder()
        {
            if (folderListBox.SelectedItem is not string folder) return;
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
            if (refreshing || accountListBox.SelectedItem is not AccountEntry entry) return;
            string? folder = accountFolderComboBox.SelectedItem as string;
            if (folder == NoFolder) folder = null;
            if (folder == entry.Folder) return;
            Mutate(d =>
            {
                if (d.Accounts.TryGetValue(entry.Name, out var account)) account.Folder = folder;
            });
        }
        private void RemoveAccount()
        {
            if (accountListBox.SelectedItem is not AccountEntry entry) return;
            if (!TrayPopup.ConfirmRemove(this, entry.Name)) return;
            Mutate(d => d.Accounts.Remove(entry.Name));
        }
        private void ScanButton_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*",
                Title = "Select QR Code Image"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            string imagePath = dialog.FileName;
            try
            {
                string? qrText = QrCodeService.DecodeQrCodeFromImage(imagePath);
                if (string.IsNullOrEmpty(qrText))
                {
                    MessageBox.Show(this, "No QR code found in the image.\n\nTroubleshooting:\n" +
                                  "- Ensure the QR code is clearly visible\n" +
                                  "- Try a higher resolution image\n" +
                                  "- Make sure it's a valid 2FA QR code\n\n" +
                                  $"File: {Path.GetFileName(imagePath)}",
                        "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                AccountJson? account = QrCodeService.ParseOtpAuthUrl(qrText);
                if (account == null)
                {
                    string preview = qrText.Length > 100 ? qrText.Substring(0, 100) + "..." : qrText;
                    MessageBox.Show(this, $"Invalid QR code format. Expected otpauth:// URL.\n\nFound: {preview}",
                        "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                nameTextBox.Text = account.Name;
                secretTextBox.Text = account.Secret;
                digitsTextBox.Text = account.Digits.ToString();
                algorithmTextBox.Text = account.Algorithm;
                foreach (Control field in new Control[] { nameTextBox, secretTextBox, digitsTextBox, algorithmTextBox, folderComboBox, addButton })
                    field.Enabled = true;
            }
            catch (Exception ex)
            {
                string fullError = $"Error processing QR code:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    fullError += $"\n\nInner exception: {ex.InnerException.Message}";
                }
                fullError += $"\n\nStack trace:\n{ex.StackTrace}";
                MessageBox.Show(this, fullError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowValidationError(string message)
        {
            MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void AddButton_Click(object? sender, EventArgs e)
        {
            string name = nameTextBox.Text.Trim();
            string secret = secretTextBox.Text.Trim();
            string algorithm = algorithmTextBox.Text.Trim().ToUpperInvariant();
            string? folder = folderComboBox.SelectedItem as string;
            if (folder == NoFolder) folder = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowValidationError("Please enter an account name.");
                return;
            }
            if (string.IsNullOrWhiteSpace(secret))
            {
                ShowValidationError("Please enter a secret key.");
                return;
            }
            if (!int.TryParse(digitsTextBox.Text.Trim(), out int digits) || digits < 6 || digits > 8)
            {
                ShowValidationError("Digits must be a number between 6 and 8.");
                return;
            }
            if (algorithm is not ("SHA1" or "SHA256" or "SHA512" or "MD5"))
            {
                ShowValidationError("Algorithm must be SHA1, SHA256, SHA512, or MD5.");
                return;
            }
            var account = new Account { Secret = secret, Digits = digits, Algorithm = algorithm };
            if (!CanComputeCode(account))
            {
                ShowValidationError("The secret key is not valid Base32.");
                return;
            }
            try
            {
                var data = AccountStore.LoadData();
                if (data.Accounts.ContainsKey(name) && MessageBox.Show(this,
                        $"An account named '{name}' already exists.\n\nDo you want to replace it?",
                        "Duplicate Account", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
                account.Folder = folder == null ? null : data.AddFolder(folder);
                data.Accounts[name] = account;
                if (!AccountStore.SaveData(data)) return;
                MessageBox.Show(this, $"Account '{name}' added successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                AccountsChanged = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error adding account: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // A secret the popup can't turn into a code would break every popup open
        private static bool CanComputeCode(Account account)
        {
            try
            {
                TotpService.ComputeCode(account);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
