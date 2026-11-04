using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class LanguagePicker : Panel
    {
        private readonly NameInput search;
        private readonly SlimScrollPanel list = new SlimScrollPanel();
        private readonly Label empty = new Label();
        internal event Action<string> Selected;
        internal string SelectedCode = Language.Code;
        internal void ShowSearchBadge(bool visible)
        {
            search.NavigationHintVisible = visible;
            search.Invalidate();
        }
        internal bool FocusSearch(Keys key)
        {
            if (key != (Keys.Control | Keys.K) || !Visible || !Enabled)
                return false;
            search.Editor.Focus();
            return true;
        }
        internal TextBox Search
        {
            get
            {
                return search.Editor;
            }
        }
        internal void SelectFirst()
        {
            var choices = list.Controls.OfType<SettingsButton>().Where(button => button.Visible).ToArray();
            var first = choices.FirstOrDefault(button => button.ContainsFocus) ?? choices.FirstOrDefault();
            if (first != null)
                first.PerformClick();
        }
        internal bool MoveFocus(Keys key)
        {
            if (key != Keys.Down && key != Keys.Up && key != Keys.PageUp && key != Keys.PageDown)
                return false;
            var choices = list.Controls.OfType<SettingsButton>().Where(button => button.Visible && button.Enabled).ToArray();
            int index = Array.FindIndex(choices, button => button.ContainsFocus);
            int target = choices.Length == 0 ? -1 : key == Keys.PageUp ? 0 : key == Keys.PageDown ? choices.Length - 1
                : index < 0 ? (key == Keys.Down ? 0 : choices.Length - 1)
                : (index + (key == Keys.Down ? 1 : -1) + choices.Length) % choices.Length;
            if (target < 0 || choices.Length == 0)
                search.Editor.Focus();
            else
            {
                var choice = choices[Math.Min(target, choices.Length - 1)];
                choice.Focus();
                list.ScrollControlIntoView(choice);
            }
            return true;
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (FocusSearch(keyData))
                return true;
            if (MoveFocus(keyData))
                return true;
            if (keyData == Keys.Enter && list.ContainsFocus)
            {
                SelectFirst();
                return true;
            }
            return base.ProcessCmdKey(ref message, keyData);
        }
        internal void RefreshLanguage()
        {
            search.SetHint(Language.T("Search languages…"));
            search.Editor.AccessibleName = Language.T("Search languages");
            empty.Text = Language.T("No languages match your search.");
            Filter();
        }
        internal LanguagePicker()
        {
            search = new NameInput("", Language.T("Search languages…"), Language.T("Search languages")) { Dock = DockStyle.Top, Height = 38, NavigationHint = "Ctrl+K" };
            list.Dock = DockStyle.Fill;
            Controls.Add(list);
            Controls.Add(search);
            empty.Text = Language.T("No languages match your search.");
            empty.ForeColor = Theme.Muted;
            empty.AutoSize = false;
            empty.Height = 36;
            list.Controls.Add(empty);
            foreach (var language in Language.Choices.Where(choice => Language.Available(choice.Code)))
            {
                string code = language.Code;
                var button = new SettingsButton { Text = language.NativeName, AccessibleName = language.NativeName + " · " + Language.T(language.EnglishName), Tag = language, Height = 36, Primary = code == SelectedCode };
                button.Click += (s, e) => { SelectedCode = code; if (Selected != null) Selected(code); };
                list.Controls.Add(button);
            }
            search.Editor.TextChanged += (s, e) => Filter();
            search.Editor.KeyDown += (s, e) => { if (MoveFocus(e.KeyData)) { e.Handled = true; e.SuppressKeyPress = true; } };
            // Hiding the native scrollbar changes the client size during scrolling.
            // Resize rows without filtering or resetting their vertical positions.
            list.SizeChanged += (s, e) => ResizeRows();
            Filter();
        }
        private void ResizeRows()
        {
            int width = Math.Max(40, list.ClientSize.Width - 16);
            foreach (var button in list.Controls.OfType<SettingsButton>())
                button.Width = width;
            empty.Width = width;
        }
        private static string Normalize(string value)
        {
            return new string(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
        }
        private void Filter()
        {
            string query = Normalize(search.Editor.Text.Trim());
            var matches = list.Controls.OfType<SettingsButton>().Where(button =>
            {
                var language = (Language.Choice)button.Tag;
                return Normalize(language.NativeName + " " + language.EnglishName + " " + Language.T(language.EnglishName) + " " + language.Code).Contains(query);
            }).ToArray();
            string typed = search.Editor.Text;
            string completion = typed.Length == 0 ? null : matches.SelectMany(button =>
            {
                var language = (Language.Choice)button.Tag;
                return new[] { language.NativeName, language.EnglishName, Language.T(language.EnglishName) };
            }).FirstOrDefault(name => name.Length > typed.Length && Normalize(name).StartsWith(Normalize(typed), StringComparison.Ordinal));
            search.SetCompletion(completion);
            int y = 6;
            list.SuspendLayout();
            list.AutoScrollPosition = Point.Empty;
            foreach (var button in list.Controls.OfType<SettingsButton>())
            {
                var language = (Language.Choice)button.Tag;
                bool visible = matches.Contains(button);
                button.AccessibleName = language.NativeName + " · " + Language.T(language.EnglishName);
                button.Visible = visible;
                button.Primary = query.Length > 0 && matches.Length == 1 ? visible : language.Code == SelectedCode;
                button.Invalidate();
                if (visible)
                {
                    button.SetBounds(0, y, Math.Max(40, list.ClientSize.Width - 16), 36);
                    y += 40;
                }
            }
            empty.Visible = y == 6;
            empty.SetBounds(2, 10, Math.Max(40, list.Width - 20), 36);
            list.ResumeLayout();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Selected = null;
                Events.Dispose();
            }
        }
    }
}
