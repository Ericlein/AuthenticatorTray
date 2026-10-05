using System.ComponentModel;
namespace AuthenticatorTray
{
    public class ModernPopupForm : RoundedForm
    {
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        // Suppresses close-on-deactivate while a dialog opened from the popup is showing
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool KeepOpen { get; set; }
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
                cp.ExStyle |= WS_EX_TOOLWINDOW; // Keeps the popup out of Alt+Tab
                return cp;
            }
        }
    }
}
