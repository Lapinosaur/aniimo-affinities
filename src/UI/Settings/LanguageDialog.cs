using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class LanguageDialog : PortableWindow
    {
        private readonly LanguagePicker picker;
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            return picker.FocusSearch(keyData) || base.ProcessCmdKey(ref message, keyData);
        }
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        internal LanguageDialog()
        {
            Text = Language.T("Language");
            ClientSize = new Size(440, 450);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            var description = new Label { Text = Language.T("Choose your language. Restart to apply it everywhere.\nYour team and other settings are kept."), Bounds = new Rectangle(24, 48, 392, 48), ForeColor = Theme.Muted };
            picker = new LanguagePicker { Bounds = new Rectangle(24, 106, 392, 264) };
            var apply = new SettingsButton { Text = Language.T("Apply and restart"), Primary = true, Bounds = new Rectangle(210, 392, 206, 36), Enabled = false };
            var cancel = new SettingsButton { Text = Language.T("Cancel"), Bounds = new Rectangle(24, 392, 160, 36), DialogResult = DialogResult.Cancel };
            picker.Selected += code => { apply.Enabled = code != Language.Code; picker.RefreshLanguage(); };
            apply.Click += (s, e) =>
            {
                try
                {
                    LanguagePreferences.Save(picker.SelectedCode);
                    Program.Restart();
                }
                catch (IOException) { description.Text = Language.T("Unable to save. Please try again."); }
                catch (UnauthorizedAccessException) { description.Text = Language.T("Unable to save: access denied. Please try again."); }
            };
            Controls.AddRange(new Control[] { description, picker, apply, cancel });
            Shown += (s, e) => picker.Search.Focus();
            AcceptButton = apply;
            CancelButton = cancel;
        }
    }
}
