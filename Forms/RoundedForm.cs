using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace AuthenticatorTray
{
    // Borderless form with the shared shadow, rounded border and rounded region
    public class RoundedForm : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        [DllImport("Gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
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
            var bounds = new Rectangle(0, 0, Width - 6, Height - 6);
            using (var formBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRoundedRectangle(formBrush, bounds, 8);
            }
            using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 0.5f))
            {
                e.Graphics.DrawRoundedRectangle(borderPen, bounds, 8);
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
            IntPtr hRgn = CreateRoundRectRgn(0, 0, Width, Height, cornerRadius, cornerRadius);
            Region? oldRegion = Region;
            try
            {
                // FromHrgn copies the region, so the native handle is still ours to free
                Region = Region.FromHrgn(hRgn);
            }
            finally
            {
                DeleteObject(hRgn);
            }
            oldRegion?.Dispose();
        }
    }
}
