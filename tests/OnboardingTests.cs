using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class OnboardingTests
    {
        private static bool Command(OnboardingWindow window, Keys key)
        {
            return (bool)typeof(OnboardingWindow).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, new object[] { Message.Create(window.Handle, 0x100, new IntPtr((int)key), IntPtr.Zero), key });
        }
        internal static void Run()
        {
            foreach (string route in new[] { "search-enter", "dialog-enter", "continue" })
                using (var welcome = new OnboardingWindow())
                {
                    welcome.Show();
                    var picker = welcome.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<LanguagePicker>()).Single();
                    var list = picker.Controls.OfType<SlimScrollPanel>().Single();
                    var french = list.Controls.OfType<SettingsButton>().Single(button => ((Language.Choice)button.Tag).Code == "fr");
                    var english = list.Controls.OfType<SettingsButton>().Single(button => ((Language.Choice)button.Tag).Code == "en");
                    picker.Search.Text = "F";
                    int redraws = 0;
                    french.Invalidated += (s, e) => redraws++;
                    picker.Search.Text = "Fr";
                    if (!french.Primary || redraws == 0 || Language.Code != "en")
                        throw new Exception("A unique result must repaint as provisional, without changing the active language.");
                    picker.Search.Text = "F";
                    picker.Search.Clear();
                    if (!english.Primary || french.Primary || picker.SelectedCode != "en")
                        throw new Exception("Erasing the query must restore English as the default selection.");
                    picker.Search.Text = "Fr";
                    if (route == "search-enter")
                    {
                        picker.Search.Focus();
                        Command(welcome, Keys.Enter);
                    }
                    else if (route == "dialog-enter")
                        typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(welcome, new object[] { Keys.Enter });
                    else
                        welcome.AcceptButton.PerformClick();
                    if (welcome.PageIndex != 1 || Language.Code != "fr")
                        throw new Exception("Enter and Continue must commit the unique language before advancing: " + route);
                    var comfort = welcome.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<RoundCheckBox>()).Single();
                    comfort.Focus();
                    Command(welcome, Keys.Down);
                    if (!welcome.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<ContentWindowPreview>()).Single().Focused)
                        throw new Exception("Down must move to the next comfort option.");
                    Command(welcome, Keys.Down);
                    Command(welcome, Keys.Down);
                    if (welcome.PageIndex != 1)
                        throw new Exception("Arrow focus on themes must not advance onboarding.");
                    comfort.Focus();
                    bool badgesBefore = comfort.Checked;
                    var enter = Message.Create(comfort.Handle, 0x100, new IntPtr((int)Keys.Enter), IntPtr.Zero);
                    bool handledEnter = comfort.PreProcessMessage(ref enter);
                    if (!handledEnter)
                        NativeMethods.SendMessage(comfort.Handle, enter.Msg, enter.WParam, enter.LParam);
                    if (welcome.PageIndex != 2 || comfort.Checked != badgesBefore)
                        throw new Exception("Enter failed page=" + welcome.PageIndex + " focus=" + comfort.Focused + " checked=" + comfort.Checked + " before=" + badgesBefore + " modifiers=" + Control.ModifierKeys + " visible=" + comfort.Visible);
                    var guideChoices = welcome.Controls.OfType<Panel>().Where(panel => panel.Visible).SelectMany(panel => panel.Controls.OfType<SettingsButton>()).OrderBy(button => button.Top).ToArray();
                    guideChoices[0].Focus();
                    Command(welcome, Keys.Down);
                    if (!guideChoices[1].Focused || !welcome.StartGuide)
                        throw new Exception("Arrows must move focus between start options without selecting them.");
                    Command(welcome, Keys.Up);
                    if (!guideChoices[0].Focused)
                        throw new Exception("Up must return to the previous choice.");
                    var back = welcome.Controls.OfType<SettingsButton>().Single(button => button.ShortcutBadge == "Backspace");
                    Command(welcome, Keys.PageDown);
                    if (!((Control)welcome.AcceptButton).Focused)
                        throw new Exception("Page Down must focus the last onboarding control.");
                    Command(welcome, Keys.Down);
                    if (!guideChoices[0].Focused)
                        throw new Exception("Down must wrap from the bottom to the top.");
                    Command(welcome, Keys.Up);
                    if (!((Control)welcome.AcceptButton).Focused)
                        throw new Exception("Up must wrap from the top to the bottom.");
                    Command(welcome, Keys.PageUp);
                    if (!guideChoices[0].Focused)
                        throw new Exception("Page Up must focus the first onboarding choice.");
                    back.Focus();
                    enter = Message.Create(back.Handle, 0x100, new IntPtr((int)Keys.Enter), IntPtr.Zero);
                    if (!back.PreProcessMessage(ref enter))
                        NativeMethods.SendMessage(back.Handle, enter.Msg, enter.WParam, enter.LParam);
                    if (welcome.PageIndex != 3)
                        throw new Exception("Enter must advance even when Back has focus.");
                    welcome.Close();
                }
            using (var window = new OnboardingWindow())
            {
                window.Show();
                var panels = window.Controls.OfType<Panel>().ToArray();
                var picker = panels.SelectMany(panel => panel.Controls.OfType<LanguagePicker>()).Single();
                picker.Search.Text = "english";
                picker.SelectFirst();
                if (window.PageIndex != 1)
                    throw new Exception("Choosing a language must advance automatically.");
                var badges = panels.SelectMany(panel => panel.Controls.OfType<RoundCheckBox>()).Single();
                badges.Checked = false;
                if (window.Controls.OfType<SettingsButton>().Any(button => button.ShortcutBadge.Length > 0))
                    throw new Exception("Hidden keyboard badges must also be hidden on onboarding navigation.");
                if (!Command(window, Keys.D3) || window.PageIndex != 2)
                    throw new Exception("3 must select the light theme and advance.");
                if (!Command(window, Keys.Back) || window.PageIndex != 1)
                    throw new Exception("Backspace must go back.");
                Command(window, Keys.NumPad2);
                if (window.PageIndex != 2)
                    throw new Exception("Numpad theme shortcuts must also work.");
                if (!Command(window, Keys.D2) || window.StartGuide || window.PageIndex != 2)
                    throw new Exception("2 must select independent exploration without advancing.");
                if (!Command(window, Keys.NumPad1) || !window.StartGuide)
                    throw new Exception("Numpad 1 must select the guided tour.");
                window.Navigate(-1);
                window.Navigate(-1);
                picker.Search.Focus();
                if (Command(window, Keys.Back))
                    throw new Exception("Backspace must be left to the language search when it has focus.");
                window.Close();
            }
            string root = Path.Combine(Path.GetTempPath(), "affinisaur-onboarding-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                if (!OnboardingPreferences.ShouldShow(root))
                    throw new Exception("Fresh installations must show onboarding.");
                File.WriteAllText(Path.Combine(root, "session.txt"), "existing");
                if (OnboardingPreferences.ShouldShow(root))
                    throw new Exception("Existing installations must not be sent through onboarding.");
                File.WriteAllText(Path.Combine(root, "onboarding.txt"), "pending-v1");
                if (!OnboardingPreferences.ShouldShow(root))
                    throw new Exception("Interrupted setup must remain available after partial preference saves.");
                File.WriteAllText(Path.Combine(root, "onboarding.txt"), "complete-v1\nen");
                if (OnboardingPreferences.ShouldShow(root))
                    throw new Exception("Completed onboarding must not repeat.");
            }
            finally { Directory.Delete(root, true); }
            string marker = PreferenceFiles.FilePath("onboarding.txt");
            string before = File.Exists(marker) ? File.ReadAllText(marker) : null;
            using (var window = new OnboardingWindow())
            {
                window.Show();
                for (int i = 0; i < 3; i++)
                    window.Navigate(1);
                window.Navigate(-1);
                if (window.PageIndex != 2)
                    throw new Exception("Onboarding must support back navigation.");
                window.Close();
            }
            if ((File.Exists(marker) ? File.ReadAllText(marker) : null) != before)
                throw new Exception("Closing setup must not mark it completed.");
            OnboardingPreferences.Begin();
            using (var window = new OnboardingWindow())
            {
                window.Show();
                window.Navigate(1);
                window.Navigate(1);
                var explore = window.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<SettingsButton>()).Single(button => button.Text == "I'll explore on my own");
                explore.PerformClick();
                window.Navigate(1);
                Command(window, Keys.Enter);
                if (window.DialogResult != DialogResult.OK || OnboardingPreferences.ShouldShow() || !TutorialPreferences.Hidden || TutorialNews.ReturningUser)
                    throw new Exception("Finishing must save the skip choice without classifying a new user as returning.");
            }
        }
    }
}
