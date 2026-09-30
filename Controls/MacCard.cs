using System.Drawing.Drawing2D;
namespace AuthenticatorTray
{
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
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRoundedRectangle(brush,
                    new Rectangle(0, 0, Width - 1, Height - 1), 4);
            }
            using (var borderPen = new Pen(Color.FromArgb(235, 235, 235), 0.5f))
            {
                e.Graphics.DrawRoundedRectangle(borderPen,
                    new Rectangle(0, 0, Width - 1, Height - 1), 4);
            }
        }
    }
}
