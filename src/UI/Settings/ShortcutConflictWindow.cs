using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class ShortcutConflictWindow : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        internal int Choice;
        internal ShortcutConflictWindow(string key, string target, string previousAction, string previousKey, bool canSwap = true)
        {
            Text = "Shortcut already assigned";
            ClientSize = new Size(480, 285);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            Controls.Add(new Label { Text = key + " is assigned to " + previousAction + ".", Location = new Point(24, 52), Size = new Size(432, 38), ForeColor = Theme.TextColor });
            Controls.Add(new Label { Text = "Choose how to assign it to " + target + ".\nChanges are kept only when you save Settings.", Location = new Point(24, 92), Size = new Size(432, 42), ForeColor = Theme.Muted });
            var swap = new SettingsButton { Text = "Swap shortcuts", Location = new Point(24, 144), Size = new Size(210, 34), Primary = true };
            var replace = new SettingsButton { Text = "Unbind other & assign", Location = new Point(246, 144), Size = new Size(210, 34) };
            swap.Enabled = canSwap;
            Controls.Add(new Label { Text = canSwap ? previousAction + " → " + previousKey : "This action requires a single key.", Location = new Point(24, 184), Size = new Size(210, 38), ForeColor = Theme.Muted });
            Controls.Add(new Label { Text = previousAction + " → Unassigned", Location = new Point(246, 184), Size = new Size(210, 38), ForeColor = Theme.Muted });
            var cancel = new SettingsButton { Text = "Cancel", Location = new Point(346, 234), Size = new Size(110, 32), DialogResult = DialogResult.Cancel };
            swap.Click += (s, e) => { Choice = 1; DialogResult = DialogResult.OK; Close(); };
            replace.Click += (s, e) => { Choice = 2; DialogResult = DialogResult.OK; Close(); };
            Controls.Add(swap);
            Controls.Add(replace);
            Controls.Add(cancel);
            CancelButton = cancel;
            Shown += (s, e) => cancel.Focus();
        }
    }
}
