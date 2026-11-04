using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class OnboardingWindow : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        private readonly string[] names = { "Language", "Make it yours", "Your first steps", "Welcome" };
        private readonly string[] descriptions = {
            "A few small choices, then you're ready to explore.",
            "Start comfortably. You can change everything in Settings later.",
            "Learn with a guide, or explore at your own pace.",
            "Made by Lapinosaur, for the community. Make yourself at home."
        };
        private readonly Label heading, description, status, counter;
        private readonly Panel[] pages = new Panel[4];
        private readonly SettingsButton back, next, skip;
        private readonly DrawingPanel progress = new DrawingPanel();
        private readonly RoundCheckBox badges;
        private readonly ContentWindowPreview independent;
        private readonly SettingsButton guided, explore;
        private readonly ThemePreview[] themeButtons = new ThemePreview[3];
        private readonly LanguagePicker languages;
        private readonly System.Collections.Generic.Dictionary<Control, string> sourceTexts = new System.Collections.Generic.Dictionary<Control, string>();
        private readonly string initialLanguage = Language.Code;
        private bool finished;
        private int appearance = Theme.Appearance;
        internal int PageIndex
        {
            get; private set;
        }
        internal bool StartGuide = true;
        internal OnboardingWindow()
        {
            Text = Language.T("Affinisaur · Unofficial · Welcome");
            ClientSize = new Size(584, 582);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            counter = AddLabel(this, "", 28, 52, 528, 20, 8, false);
            progress.Bounds = new Rectangle(28, 78, 528, 5);
            progress.Paint += (s, e) =>
            {
                for (int i = 0; i < 4; i++)
                    Drawing.Box(e.Graphics, new RectangleF(i * 134, 0, 126, 4), i <= PageIndex ? Theme.Accent : Theme.Border, Color.Transparent, 2);
            };
            Controls.Add(progress);
            heading = AddLabel(this, "", 28, 104, 528, 40, 22, true);
            description = AddLabel(this, "", 28, 154, 528, 44, 10, false);
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i] = new Panel { Bounds = new Rectangle(28, 210, 528, 274), AccessibleName = names[i], TabIndex = 2 };
                Controls.Add(pages[i]);
            }
            AddLabel(pages[0], Language.T("Choose a language · continues automatically"), 0, 0, 528, 24, 9, true);
            languages = new LanguagePicker { Bounds = new Rectangle(0, 30, 528, 208) };
            languages.Selected += SelectLanguage;
            pages[0].Controls.Add(languages);
            AddLabel(pages[0], Language.T("No account needed. Your data stays on this PC."), 0, 246, 528, 28, 9, false);
            var themes = new Panel { Bounds = new Rectangle(0, 196, 528, 78), AccessibleName = Language.T("Theme") };
            AddLabel(pages[1], Language.T("Optional · adjust before choosing a theme"), 0, 0, 528, 22, 9, true);
            AddLabel(pages[1], Language.T("Choose your theme · continues automatically"), 0, 166, 528, 24, 9, true);
            for (int i = 0; i < 3; i++)
            {
                int mode = i;
                var choice = new ThemePreview { Mode = mode, Text = new[] { Language.T("Dark theme"), Language.T("System theme"), Language.T("Light theme") }[i], Bounds = new Rectangle(i * 178, 0, 172, 76), Checked = appearance == i, AccessibleName = new[] { Language.T("Dark theme"), Language.T("System theme"), Language.T("Light theme") }[i] };
                choice.CheckedChanged += (s, e) => { if (choice.Checked) appearance = mode; };
                choice.Click += (s, e) => { if (PageIndex == 1) { appearance = mode; Navigate(1); } };
                choice.Shortcut = (i + 1).ToString();
                themeButtons[i] = choice;
                themes.Controls.Add(choice);
            }
            pages[1].Controls.Add(themes);
            badges = new RoundCheckBox { Text = Language.T("Show keyboard shortcut badges"), Checked = true, Bounds = new Rectangle(0, 26, 528, 30) };
            independent = new ContentWindowPreview { Text = Language.T("Keep content windows visible when minimizing"), Checked = true, Bounds = new Rectangle(0, 64, 528, 88) };
            badges.CheckedChanged += (s, e) => RefreshShortcutBadges();
            pages[1].Controls.AddRange(new Control[] { badges, independent });
            AddLabel(pages[2], Language.T("Choose how to start"), 0, 0, 528, 24, 9, true);
            guided = new SettingsButton { Text = Language.T("Show me around"), Glyph = "help", GlyphWithText = true, Primary = true, Bounds = new Rectangle(0, 30, 528, 44) };
            explore = new SettingsButton { Text = Language.T("I'll explore on my own"), Glyph = "radar", GlyphWithText = true, Bounds = new Rectangle(0, 130, 528, 44) };
            guided.Click += (s, e) => { StartGuide = true; guided.Primary = true; explore.Primary = false; guided.Invalidate(); explore.Invalidate(); };
            explore.Click += (s, e) => { StartGuide = false; guided.Primary = false; explore.Primary = true; guided.Invalidate(); explore.Invalidate(); };
            pages[2].Controls.AddRange(new Control[] { guided, explore });
            AddLabel(pages[2], Language.T("Animated tutorials, at your pace. Pause or skip whenever you like."), 12, 80, 504, 40, 9, false);
            AddLabel(pages[2], Language.T("No automatic tour. All tutorials stay in Settings > Help & Tutorial."), 12, 180, 504, 42, 9, false);
            AddLabel(pages[2], Language.T("The guide uses a demo: your team stays untouched."), 0, 228, 528, 40, 9, false);
            AddLabel(pages[3], Language.T("Your next matchup starts here."), 0, 0, 528, 32, 14, true);
            AddLabel(pages[3], Language.T("Ask a question, share feedback, or follow what comes next."), 0, 42, 528, 36, 10, false);
            AddSocial("Discord", "community", "https://discord.gg/B6n83stRcf", 0, 96);
            AddSocial(Language.T("Star on GitHub"), "star", "https://github.com/Lapinosaur/aniimo-affinities", 270, 96);
            AddSocial("Twitch", "twitch", "https://www.twitch.tv/lapinosaur", 0, 150);
            AddSocial("YouTube", "youtube", "https://www.youtube.com/@LapinosaurMedia", 270, 150);
            AddLabel(pages[3], Language.T("Unofficial fan tool. Not affiliated with Aniimo or its rights holders."), 0, 218, 528, 40, 8, false);
            status = AddLabel(this, "", 28, 486, 528, 28, 9, false);
            back = new SettingsButton { Text = Language.T("Back"), Bounds = new Rectangle(28, 526, 96, 36), TabIndex = 10 };
            skip = new SettingsButton { Text = Language.T("Use defaults"), Bounds = new Rectangle(134, 526, 152, 36), TabIndex = 11 };
            next = new SettingsButton { Text = Language.T("Continue"), Primary = true, Bounds = new Rectangle(372, 526, 184, 36), TabIndex = 12 };
            back.Click += (s, e) => Navigate(-1);
            next.Click += (s, e) => { if (PageIndex == 0 && languages.Search.TextLength > 0) languages.SelectFirst(); else if (PageIndex < 3) Navigate(1); else Finish(); };
            skip.Click += (s, e) =>
            {
                SelectLanguage("en");
                foreach (ThemePreview choice in themes.Controls)
                    choice.Checked = choice.Mode == 1;
                appearance = 1;
                badges.Checked = independent.Checked = StartGuide = true;
                guided.Primary = true;
                explore.Primary = false;
                PageIndex = 3;
                ShowPage();
            };
            Controls.AddRange(new Control[] { back, skip, next });
            AcceptButton = next;
            KeyPreview = true;
            Shown += (s, e) => { if (PageIndex == 0) languages.Search.Focus(); };
            RememberText(this);
            ShowPage();
        }
        private void RememberText(Control control)
        {
            if (control is LanguagePicker)
                return;
            sourceTexts[control] = Language.Original(control.Text);
            foreach (Control child in control.Controls)
                RememberText(child);
        }
        private void SelectLanguage(string code)
        {
            Language.Set(code);
            languages.SelectedCode = code;
            foreach (var pair in sourceTexts)
                if (!pair.Key.IsDisposed)
                    pair.Key.Text = Language.T(pair.Value);
            languages.RefreshLanguage();
            PageIndex = 1;
            ShowPage();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !finished)
                Language.Set(initialLanguage);
            base.Dispose(disposing);
            if (disposing)
                sourceTexts.Clear();
        }
        private void RefreshShortcutBadges()
        {
            if (next == null || back == null)
                return;
            guided.ShortcutBadge = badges.Checked ? "1" : "";
            languages.ShowSearchBadge(badges.Checked);
            explore.ShortcutBadge = badges.Checked ? "2" : "";
            guided.Invalidate();
            explore.Invalidate();
            next.ShortcutBadge = badges.Checked ? "Enter" : "";
            back.ShortcutBadge = badges.Checked ? "Backspace" : "";
            back.Invalidate();
            next.Invalidate();
            foreach (var choice in themeButtons)
            {
                choice.ShowShortcut = badges.Checked;
                choice.Invalidate();
            }
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (PageIndex == 0 && languages.FocusSearch(keyData))
                return true;
            if (MoveChoiceFocus(keyData))
                return true;
            if (keyData == Keys.Enter)
            {
                next.PerformClick();
                return true;
            }
            if (keyData == Keys.Back && !languages.Search.Focused && !(ActiveControl is TextBoxBase))
            {
                if (PageIndex > 0)
                    Navigate(-1);
                return true;
            }
            if (PageIndex == 1 && (keyData >= Keys.D1 && keyData <= Keys.D3 || keyData >= Keys.NumPad1 && keyData <= Keys.NumPad3))
            {
                int index = (int)keyData - (keyData >= Keys.NumPad1 ? (int)Keys.NumPad1 : (int)Keys.D1);
                themeButtons[index].PerformClick();
                return true;
            }
            if (PageIndex == 2 && (keyData == Keys.D1 || keyData == Keys.NumPad1 || keyData == Keys.D2 || keyData == Keys.NumPad2))
            {
                if (keyData == Keys.D1 || keyData == Keys.NumPad1)
                    guided.PerformClick();
                else
                    explore.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref message, keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (MoveChoiceFocus(e.KeyData))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyData == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                next.PerformClick();
                return;
            }
            base.OnKeyDown(e);
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (MoveChoiceFocus(keyData))
                return true;
            if (keyData == Keys.Enter)
            {
                next.PerformClick();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }
        private bool MoveChoiceFocus(Keys key)
        {
            if (key != Keys.Up && key != Keys.Down && key != Keys.PageUp && key != Keys.PageDown)
                return false;
            if (PageIndex == 0 && languages.ContainsFocus)
                return languages.MoveFocus(key);
            Control[] choices = PageIndex == 1 ? new Control[] { badges, independent, themeButtons[0], themeButtons[1], themeButtons[2] }
                : PageIndex == 2 ? new Control[] { guided, explore }
                : PageIndex == 0 ? new Control[] { languages.Search }
                : pages[3].Controls.OfType<SettingsButton>().OrderBy(button => button.Top).ThenBy(button => button.Left).Cast<Control>().ToArray();
            choices = choices.Concat(new Control[] { back, skip, next }).Where(control => control.Visible && control.Enabled).ToArray();
            int index = Array.FindIndex(choices, control => control.ContainsFocus);
            int target = key == Keys.PageUp ? 0 : key == Keys.PageDown ? choices.Length - 1 : index < 0 ? (key == Keys.Down ? 0 : choices.Length - 1) : (index + (key == Keys.Down ? 1 : -1) + choices.Length) % choices.Length;
            var theme = choices[target] as ThemePreview;
            if (theme != null)
                theme.FocusPreview();
            else
                choices[target].Focus();
            return true;
        }
        internal void Navigate(int direction)
        {
            PageIndex = Math.Max(0, Math.Min(3, PageIndex + direction));
            ShowPage();
        }
        private void ShowPage()
        {
            for (int i = 0; i < pages.Length; i++)
                pages[i].Visible = i == PageIndex;
            counter.Text = Language.T("GET STARTED  ·  ") + (PageIndex + 1) + " / 4";
            heading.Text = PageIndex == 3 ? Language.T("Welcome to Affinisaur") : Language.T(names[PageIndex]);
            description.Text = Language.T(descriptions[PageIndex]);
            back.Visible = PageIndex > 0;
            skip.Visible = PageIndex == 0;
            next.Text = PageIndex == 3 ? Language.T("Open Affinisaur") : Language.T("Continue");
            status.Text = PageIndex == 3 ? (StartGuide ? Language.T("Your guided tour starts next.") : Language.T("You're ready to explore. Tutorials are always available in Settings.")) : PageIndex == 0 ? Language.T("Translations in beta · Your own text stays unchanged.") : "";
            progress.Invalidate();
            RefreshShortcutBadges();
            if (PageIndex == 0 && Visible)
                languages.Search.Focus();
        }
        private void Finish()
        {
            try
            {
                Theme.SaveAppearance(appearance);
                LanguagePreferences.Save(Language.Code);
                DisplaySettings.SaveDisplay(1, badges.Checked, 1);
                ContentWindowPreferences.Save(independent.Checked);
                TutorialPreferences.Save(!StartGuide);
                // Avoid classifying the preferences just created above as an upgrade.
                PreferenceFiles.WriteText(TutorialNews.FilePath, "new");
                OnboardingPreferences.Complete();
                finished = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (IOException) { status.Text = Language.T("Unable to save. Please try again."); }
            catch (UnauthorizedAccessException) { status.Text = Language.T("Unable to save: access denied. Please try again."); }
        }
        private void AddSocial(string text, string glyph, string url, int x, int y)
        {
            var button = new SettingsButton { Text = text, Glyph = glyph, GlyphWithText = true, Bounds = new Rectangle(x, y, 258, 42) };
            ExternalLinkTips.Attach(button, url);
            button.Click += (s, e) => BusyOperation.OpenLink(this, button, url, error => { if (error != null) status.Text = Language.T("Unable to open the link. Please try again."); });
            pages[3].Controls.Add(button);
        }
        private static Label AddLabel(Control parent, string text, int x, int y, int width, int height, float size, bool bold)
        {
            var label = UiFonts.Own(new Label { Text = text, Bounds = new Rectangle(x, y, width, height), ForeColor = bold ? Theme.TextColor : Theme.Muted, Font = UiFonts.Create("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular) });
            parent.Controls.Add(label);
            return label;
        }
    }
}
