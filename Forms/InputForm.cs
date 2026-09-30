namespace AuthenticatorTray
{
    public class InputForm : RoundedForm
    {
        private readonly TextBox inputTextBox;
        private InputForm(string title, string placeholder, string initial)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.Width = UiScale.Em(22f);
            this.Height = UiScale.Em(12.5f);
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
                Text = title,
                Font = UiScale.ScaleFont("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(28, 28, 30),
                Location = new Point(UiScale.Em(1.5f), UiScale.Em(1.2f)),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            Panel contentPanel = new Panel
            {
                Location = new Point(0, UiScale.Em(3.5f)),
                Size = new Size(this.Width, this.Height - UiScale.Em(3.5f)),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            inputTextBox = new TextBox
            {
                Font = UiScale.ScaleFont("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(UiScale.Em(1.5f), UiScale.Em(1.5f)),
                Size = new Size(this.Width - UiScale.Em(3f), UiScale.Em(2.5f)),
                PlaceholderText = placeholder,
                Text = initial
            };
            int halfWidth = (this.Width - UiScale.Em(4.5f)) / 2;
            Button okButton = new Button
            {
                Text = "OK",
                Font = UiScale.ScaleFont("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 122, 255),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(UiScale.Em(1.5f), UiScale.Em(4.8f)),
                Size = new Size(halfWidth, UiScale.Em(2.5f)),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.MouseEnter += (s, e) => okButton.BackColor = Color.FromArgb(0, 100, 200);
            okButton.MouseLeave += (s, e) => okButton.BackColor = Color.FromArgb(0, 122, 255);
            Button cancelButton = SettingsForm.CreateSecondaryButton("Cancel", UiScale.Em(3f) + halfWidth, UiScale.Em(4.8f), halfWidth, UiScale.Em(2.5f));
            cancelButton.Font = UiScale.ScaleFont("Segoe UI", 10, FontStyle.Regular);
            cancelButton.DialogResult = DialogResult.Cancel;
            contentPanel.Controls.Add(inputTextBox);
            contentPanel.Controls.Add(okButton);
            contentPanel.Controls.Add(cancelButton);
            this.Controls.Add(header);
            this.Controls.Add(contentPanel);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        public static string? Prompt(IWin32Window owner, string title, string placeholder, string initial)
        {
            using (var form = new InputForm(title, placeholder, initial))
            {
                return form.ShowDialog(owner) == DialogResult.OK ? form.inputTextBox.Text : null;
            }
        }
    }
}
