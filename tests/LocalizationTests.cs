using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class LocalizationTests
    {
        private static void CheckScrolling(LanguagePicker picker)
        {
            var list = picker.Controls.OfType<SlimScrollPanel>().Single();
            picker.Search.Focus();
            picker.MoveFocus(Keys.Down);
            picker.MoveFocus(Keys.Down);
            if (!list.Controls.OfType<SettingsButton>().Single(button => ((Language.Choice)button.Tag).Code == "fr").Focused)
                throw new Exception("Down must navigate from search through the visible languages.");
            string queryBefore = picker.Search.Text;
            var owner = picker.FindForm();
            if (!(bool)owner.GetType().GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner,
                new object[] { Message.Create(owner.Handle, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.Control | Keys.K })
                || !picker.Search.Focused || picker.Search.Text != queryBefore)
                throw new Exception("Ctrl+K must focus language search without changing the query.");
            picker.MoveFocus(Keys.PageDown);
            var choices = list.Controls.OfType<SettingsButton>().Where(button => button.Visible).ToArray();
            if (!choices.Last().Focused)
                throw new Exception("Page Down must focus the last visible language.");
            if (list.AutoScrollPosition.Y >= 0)
                throw new Exception("Keyboard focus must scroll the language list into view.");
            picker.MoveFocus(Keys.Down);
            if (!choices.First().Focused)
                throw new Exception("Down from the last language must wrap to the first.");
            picker.MoveFocus(Keys.Up);
            if (!choices.Last().Focused)
                throw new Exception("Up from the first language must wrap to the last.");
            picker.MoveFocus(Keys.PageUp);
            if (!choices.First().Focused)
                throw new Exception("Page Up must focus the first visible language.");
            picker.Search.Focus();
            list.AutoScrollPosition = new Point(0, 160);
            Application.DoEvents();
            if (list.AutoScrollPosition.Y >= -100)
                throw new Exception("Language list must retain its scroll position.");
            list.AutoScrollPosition = Point.Empty;
            typeof(Control).GetMethod("OnMouseWheel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(list, new object[] { new HandledMouseEventArgs(MouseButtons.None, 0, 20, 20, -120) });
            Application.DoEvents();
            if (list.AutoScrollPosition.Y >= 0)
                throw new Exception("Mouse wheel must scroll the language list.");
            var bar = picker.Controls.OfType<SlimScrollBar>().Single();
            int before = list.AutoScrollPosition.Y;
            typeof(Control).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 5, bar.Height - 3, 0) });
            Application.DoEvents();
            if (list.AutoScrollPosition.Y >= before)
                throw new Exception("The language scrollbar must move the list.");
            picker.Search.Text = "français";
            Application.DoEvents();
            var candidate = list.Controls.OfType<SettingsButton>().Single(button => button.Visible);
            if (!candidate.Primary || picker.Search.AutoCompleteMode != AutoCompleteMode.None)
                throw new Exception("A sole search result must be provisionally highlighted without a native dropdown.");
            if (list.AutoScrollPosition.Y != 0)
                throw new Exception("Filtering languages must reset the scroll position.");
            picker.Search.Clear();
        }
        private static Dictionary<string, string> Catalog(string code)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("locale-" + code + ".txt"))
            {
                if (stream == null)
                    throw new Exception("Missing language catalog: " + code);
                using (var reader = new StreamReader(stream))
                    return new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
            }
        }
        internal static void Run()
        {
            var source = Catalog("en");
            string[] modelNames = (string[])Model.Names.Clone();
            string[] actions = (string[])KeyBindings.Actions.Clone();
            string initial = Language.Code;
            try
            {
                foreach (var language in Language.Choices)
                {
                    var values = Catalog(language.Code);
                    foreach (var key in source.Keys)
                    {
                        string translated;
                        if (!values.TryGetValue(key, out translated) || string.IsNullOrWhiteSpace(translated))
                            throw new Exception(language.Code + " is missing: " + key);
                        foreach (Match token in Regex.Matches(key, @"\[key:[^\]]*\]|\[types:[^\]]*\]|\bF\d{1,2}\b"))
                            if (!translated.Contains(token.Value))
                                throw new Exception(language.Code + " changed a shortcut/element token: " + key);
                    }
                    Language.Set(language.Code);
                    if (Language.T("User custom name 🥕") != "User custom name 🥕")
                        throw new Exception("Names entered by users must not be translated.");
                }
                Language.Set("fr");
                LanguagePreferences.Save("fr");
                if (LanguagePreferences.Load() != "fr")
                    throw new Exception("Language choice must survive restart.");
                LanguagePreferences.Save(initial);
                if (Language.T("Settings") != "Paramètres" || Language.T("Suggested counter") != "Contre suggéré")
                    throw new Exception("Reviewed French terminology must take precedence over generated text.");
                using (var main = new MainForm())
                using (var settings = new SettingsDialog(KeyBindings.Defaults()))
                using (var language = new LanguageDialog())
                {
                    main.Show();
                    settings.Show(main);
                    language.Show(settings);
                    CheckScrolling(language.Controls.OfType<LanguagePicker>().Single());
                    using (var image = new Bitmap(settings.Width, settings.Height))
                        settings.DrawToBitmap(image, settings.ClientRectangle);
                    if (!settings.Text.Contains("Paramètres") || language.Text != "Langue")
                        throw new Exception("Settings and language selection must use the chosen language.");
                }
                if (!Model.Names.SequenceEqual(modelNames) || !KeyBindings.Actions.SequenceEqual(actions) || MatrixRenderer.Order.Any(index => index < 0))
                    throw new Exception("Localization must not change model IDs, shortcuts or matrix ordering.");
                using (var welcome = new OnboardingWindow())
                {
                    welcome.Show();
                    var picker = welcome.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<LanguagePicker>()).Single();
                    CheckScrolling(picker);
                    picker.Search.Text = "deutsch";
                    picker.SelectFirst();
                    if (welcome.PageIndex != 1 || Language.Code != "de")
                        throw new Exception("Language selection must advance once and preview the selected locale.");
                    welcome.Close();
                }
                if (Language.Code != "fr")
                    throw new Exception("Abandoning onboarding must restore its original locale.");
            }
            finally { Language.Set(initial); }
        }
    }
}
