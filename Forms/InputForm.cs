using static AuthenticatorTray.UiScale;
namespace AuthenticatorTray
{
    public class InputForm : RoundedForm
    {
        private static readonly Color Accent = Color.FromArgb(0, 122, 255);
        private static readonly Color AccentHover = Color.FromArgb(0, 100, 200);
        private readonly TextBox inputTextBox;
        private readonly Font titleFont = ScaleFont("Segoe UI", 12);
        private readonly Font textFont = ScaleFont("Segoe UI", 9);
        private readonly Font buttonFont = ScaleFont("Segoe UI", 10);
        private InputForm(string title, string placeholder, string initial)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(250, 250, 250);
            Width = Em(22f);
            Height = Em(12.5f);
            TopMost = true;
            ShowInTaskbar = false;
            int headerHeight = Em(3.5f);
            Panel header = new Panel
            {
                Height = headerHeight,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            header.Controls.Add(new Label
            {
                Text = title,
                Font = titleFont,
                ForeColor = Color.FromArgb(28, 28, 30),
                Location = new Point(Em(1.5f), Em(1.2f)),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            Panel contentPanel = new Panel
            {
                Location = new Point(0, headerHeight),
                Size = new Size(Width, Height - headerHeight),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            inputTextBox = new TextBox
            {
                Font = textFont,
                Location = new Point(Em(1.5f), Em(1.5f)),
                Size = new Size(Width - Em(3f), Em(2.5f)),
                PlaceholderText = placeholder,
                Text = initial
            };
            int buttonY = Em(4.8f);
            int buttonHeight = Em(2.5f);
            int halfWidth = (Width - Em(4.5f)) / 2;
            Button okButton = new Button
            {
                Text = "OK",
                Font = buttonFont,
                ForeColor = Color.White,
                BackColor = Accent,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(Em(1.5f), buttonY),
                Size = new Size(halfWidth, buttonHeight),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.MouseEnter += (s, e) => okButton.BackColor = AccentHover;
            okButton.MouseLeave += (s, e) => okButton.BackColor = Accent;
            Button cancelButton = SettingsForm.CreateSecondaryButton("Cancel", Em(3f) + halfWidth, buttonY, halfWidth, buttonHeight);
            cancelButton.Font = buttonFont;
            cancelButton.DialogResult = DialogResult.Cancel;
            contentPanel.Controls.Add(inputTextBox);
            contentPanel.Controls.Add(okButton);
            contentPanel.Controls.Add(cancelButton);
            Controls.Add(header);
            Controls.Add(contentPanel);
            AcceptButton = okButton;
            CancelButton = cancelButton;
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
        public static string? Prompt(IWin32Window owner, string title, string placeholder, string initial)
        {
            using var form = new InputForm(title, placeholder, initial);
            return form.ShowDialog(owner) == DialogResult.OK ? form.inputTextBox.Text : null;
        }
    }
}
