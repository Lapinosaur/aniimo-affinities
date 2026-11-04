using System;
using System.Drawing;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed class TransparencyDialog : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        internal TransparencyDialog(Action lockTutorial = null)
        {
            Text = Language.T("Window transparency");
            ClientSize = new Size(460, 406);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            Controls.Add(new Label { Text = Language.T("Frosted glass · sharp text and element icons"), ForeColor = Theme.Muted, Bounds = new Rectangle(24, 52, 412, 30) });
            var locked = Choice(Language.T("Locked overlay"), Language.T("Enabled by default. The spaces between panels stay clear."), 90, TransparencyPreferences.Locked, 0);
            var tutorial = new SettingsButton { Text = Language.T("Lock tutorial"), Bounds = new Rectangle(292, 88, 144, 30), Enabled = lockTutorial != null, TabIndex = 1 };
            tutorial.Click += (s, e) => { if (lockTutorial != null) lockTutorial(); };
            Controls.Add(tutorial);
            var children = Choice(Language.T("Secondary windows"), Language.T("Settings, downloads, team editor, radar previews and other windows."), 176, TransparencyPreferences.Children, 2);
            var main = Choice(Language.T("Main window"), Language.T("Frosted glass while unlocked. Clicking and dragging stay available."), 262, TransparencyPreferences.Main, 3);
            var status = new Label { ForeColor = Theme.Error, Bounds = new Rectangle(24, 326, 412, 24) };
            var save = new SettingsButton { Text = Language.T("Apply"), Primary = true, Bounds = new Rectangle(310, 354, 126, 34), TabIndex = 5 };
            save.Click += (s, e) =>
            {
                try
                {
                    TransparencyPreferences.Save(locked.Checked, children.Checked, main.Checked);
                    status.ForeColor = Theme.Muted;
                    status.Text = Language.T("Saved · applied to open and future windows.");
                }
                catch (System.IO.IOException) { status.Text = Language.T("Unable to save transparency preferences."); }
                catch (UnauthorizedAccessException) { status.Text = Language.T("Unable to save transparency preferences."); }
            };
            var close = new SettingsButton { Text = Language.T("Close"), DialogResult = DialogResult.Cancel, Bounds = new Rectangle(176, 354, 126, 34), TabIndex = 4 };
            Controls.AddRange(new Control[] { status, close, save });
            AcceptButton = save;
            CancelButton = close;
        }
        private RoundCheckBox Choice(string title, string description, int y, bool enabled, int tabIndex)
        {
            var check = new RoundCheckBox { Text = title, Checked = enabled, ForeColor = Theme.TextColor, Bounds = new Rectangle(24, y, 260, 30), TabIndex = tabIndex, AccessibleName = title + Language.T(" frosted glass"), AccessibleDescription = description };
            Controls.Add(check);
            Controls.Add(new Label { Text = description, ForeColor = Theme.Muted, Bounds = new Rectangle(52, y + 34, 384, 42) });
            return check;
        }
    }
}
