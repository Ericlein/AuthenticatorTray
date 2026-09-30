using System.Drawing.Drawing2D;
namespace AuthenticatorTray
{
    // Borderless form with the shared shadow, rounded border and rounded region
    public class RoundedForm : Form
    {
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern System.IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 3; i >= 0; i--)
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(8 + (i * 3), 0, 0, 0)))
                {
                    e.Graphics.FillRoundedRectangle(shadowBrush,
                        new Rectangle(i + 1, i + 1, Width - (i * 2) - 1, Height - (i * 2) - 1), 8);
                }
            }
            using (var formBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRoundedRectangle(formBrush, new Rectangle(0, 0, Width - 6, Height - 6), 8);
            }
            using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 0.5f))
            {
                e.Graphics.DrawRoundedRectangle(borderPen, new Rectangle(0, 0, Width - 6, Height - 6), 8);
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyRoundedRegion();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion();
        }
        private void ApplyRoundedRegion()
        {
            int cornerRadius = UiScale.Em(0.8f);
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, cornerRadius, cornerRadius));
        }
    }
}
