using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class SettingsTests
    {
        internal static void Run()
        {
            using (var scrollHost = new Form { ClientSize = new Size(240, 140) })
            {
                var content = new SlimScrollPanel { Bounds = new Rectangle(0, 0, 220, 120) };
                var last = new Button { Text = "Last action", Bounds = new Rectangle(0, 700, 180, 30) };
                content.Controls.Add(last);
                scrollHost.Controls.Add(content);
                scrollHost.Show();
                content.ScrollControlIntoView(last);
                Application.DoEvents();
                if (content.AutoScrollPosition.Y >= 0 || !content.ClientRectangle.IntersectsWith(last.Bounds))
                    throw new Exception("Slim scrolling must keep distant keyboard actions reachable.");
                if (!scrollHost.Controls.OfType<SlimScrollBar>().Any(bar => bar.Visible))
                    throw new Exception("Overflow must use the shared slim scroll component.");
            }
            using (var main = new MainForm())
            using (var ownedSettings = new SettingsDialog(KeyBindings.Defaults()))
            {
                main.Show();
                ownedSettings.Show(main);
                ownedSettings.TutorialSection("Display");
                var display = ownedSettings.Controls.OfType<DisplayDialog>().Single();
                var card = display.Controls.OfType<TransparencyPreview>().Single(choice => choice.Text == "Main window");
                var state = typeof(PortableWindow).GetField("glassPreviewEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                typeof(Control).GetMethod("OnMouseEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(card, new object[] { EventArgs.Empty });
                if (!Equals(state.GetValue(main), !card.Checked) || state.GetValue(ownedSettings) != null)
                    throw new Exception("Main window hover must target its parent main window, never Settings.");
                typeof(Control).GetMethod("OnMouseLeave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(card, new object[] { EventArgs.Empty });
                if (state.GetValue(main) != null)
                    throw new Exception("Main window preview must restore on mouse leave.");
            }
            int previewMain = Array.IndexOf(Model.Names, "Fire");
            var previewKeys = KeyBindings.Defaults();
            using (var displayPreview = new DisplayDialog(null, () => previewMain, () => previewKeys))
            {
                var choices = displayPreview.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<DisplayChoice>()).Where(choice => choice.BadgePreview).ToArray();
                if (choices.Length != 2 || choices.Any(choice => choice.PreviewType() != previewMain))
                    throw new Exception("Badge previews must use selected Main");
                previewMain = Array.IndexOf(Model.Names, "Water");
                if (choices.Any(choice => choice.PreviewType() != previewMain))
                    throw new Exception("Badge previews did not follow Main change");
                previewKeys[Array.IndexOf(KeyBindings.Actions, "Water")] = Keys.Q;
                if (choices.Any(choice => choice.PreviewBindings()[Array.IndexOf(KeyBindings.Actions, "Water")] != Keys.Q))
                    throw new Exception("Badge previews must use draft rebinds");
                previewMain = -1;
                int fallback = choices[0].PreviewType();
                if (fallback < 0 || fallback >= Model.Names.Length || choices.Any(choice => choice.PreviewType() != fallback))
                    throw new Exception("Empty Main requires one stable shared fallback");
            }

            using (var settings = new SettingsDialog(KeyBindings.Defaults()))
            {
                settings.Show();
                Application.DoEvents();
                int initial = settings.ClientSize.Height;
                settings.Controls.OfType<SettingsButton>().Single(button => button.Text == "Window & overlay").PerformClick();
                if (settings.ClientSize.Height != initial)
                    throw new Exception("Tab height should animate instead of jumping");
                settings.Controls.OfType<SettingsButton>().Single(button => button.Text == "Display").PerformClick();
                var transition = System.Diagnostics.Stopwatch.StartNew();
                while (transition.ElapsedMilliseconds < 260)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
                if (settings.ClientSize.Height != Math.Min(670, Screen.FromControl(settings).WorkingArea.Height - 32))
                    throw new Exception("Rapid tab changes must settle at latest height");
                var embedded = settings.Controls.OfType<DisplayDialog>().Single();
                if (embedded.TopLevel || embedded.Controls.OfType<Panel>().Any(panel => panel.Dock == DockStyle.Top))
                    throw new Exception("Embedded display settings must not add a caption over their controls.");
                if (embedded.AutoScroll || embedded.Controls.Cast<Control>().Where(control => control.Visible).Any(control => !embedded.ClientRectangle.Contains(control.Bounds)))
                    throw new Exception("Compact Display must keep every option and Apply fully visible without scrolling.");
                if (embedded.Controls.OfType<DisplayChoice>().Any(choice => choice.Height < 100))
                    throw new Exception("Compacting settings must preserve the full text previews.");
                var options = embedded.Controls.OfType<TransparencyPreview>().ToArray();
                if (options.Length != 3 || embedded.Controls.OfType<SettingsButton>().Any(button => button.Text == "Transparency…"))
                    throw new Exception("Display must expose all three glass options directly, without a launcher.");
                options[0].Focus();
                var keyMessage = Message.Create(options[0].Handle, 0x100, new IntPtr((int)Keys.Tab), IntPtr.Zero);
                settings.PreFilterMessage(ref keyMessage);
                if (!settings.KeyboardNavigation)
                    throw new Exception("Embedded settings must participate in keyboard navigation hints.");
                var themes = embedded.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<ThemePreview>()).ToArray();
                if (themes.Length != 3)
                    throw new Exception("Display needs Dark, System and Light previews.");
                int originalTheme = Theme.Appearance;
                themes.First(choice => choice.Mode != originalTheme).Checked = true;
                if (Theme.Appearance != originalTheme)
                    throw new Exception("Theme previews must wait for Apply.");
                string appearancePath = PreferenceFiles.FilePath("appearance.txt");
                string savedAppearance = System.IO.File.Exists(appearancePath) ? System.IO.File.ReadAllText(appearancePath) : null;
                Color appliedBackground = Theme.Background;
                var stagedTheme = themes.Single(choice => choice.Checked);
                var hoverTheme = themes.Single(choice => choice.Mode == (appliedBackground.GetBrightness() < .5f ? 2 : 0));
                var enterTheme = typeof(Control).GetMethod("OnMouseEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var leaveTheme = typeof(Control).GetMethod("OnMouseLeave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                enterTheme.Invoke(hoverTheme, new object[] { EventArgs.Empty });
                if (Theme.Background == appliedBackground || settings.BackColor != Theme.Background || Theme.Appearance != originalTheme)
                    throw new Exception("Theme hover must render on the window without committing the preference.");
                leaveTheme.Invoke(hoverTheme, new object[] { EventArgs.Empty });
                if (!stagedTheme.Checked || Theme.Background != appliedBackground || (System.IO.File.Exists(appearancePath) ? System.IO.File.ReadAllText(appearancePath) : null) != savedAppearance)
                    throw new Exception("Leaving a theme preview must restore the applied palette without writing preferences.");
                hoverTheme.Checked = true;
                embedded.Controls.OfType<SettingsButton>().Single(button => button.Text == "Apply").PerformClick();
                string expectedGlyph = hoverTheme.Mode == 0 ? "moon" : hoverTheme.Mode == 1 ? "system" : "sun";
                if (Theme.Appearance != hoverTheme.Mode || settings.Controls.OfType<SettingsButton>().Single(button => (button.AccessibleName ?? "").StartsWith("Appearance: ")).Glyph != expectedGlyph)
                    throw new Exception("Apply must commit the theme and immediately synchronize the appearance icon.");
                Theme.SaveAppearance(originalTheme);
                themes.Single(choice => choice.Mode == originalTheme).Checked = true;
                var glassPreviews = ((System.Collections.Generic.List<TransparencyPreview>)typeof(DisplayDialog).GetField("transparencyPreviews", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(embedded)).ToArray();
                if (glassPreviews.Any(preview => preview.Parent != embedded || !embedded.ClientRectangle.Contains(preview.Bounds)))
                    throw new Exception("Transparency previews must stay inside the Display page.");
                var help = glassPreviews[0].Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Lock tutorial");
                foreach (var target in options.Cast<Control>().Concat(new Control[] { help }))
                    if (target.Parent.GetChildAtPoint(new Point(target.Left + target.Width / 2, target.Top + target.Height / 2)) != target)
                        throw new Exception("Glass control is covered by another Display control: " + target.Text);
                int previewWindows = Application.OpenForms.Count;
                string transparencyPath = PreferenceFiles.FilePath("transparency.txt");
                string storedTransparency = System.IO.File.Exists(transparencyPath) ? System.IO.File.ReadAllText(transparencyPath) : null;
                var windowGlassPreview = typeof(PortableWindow).GetField("glassPreviewEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                for (int i = 0; i < options.Length; i++)
                {
                    bool selected = options[i].Checked;
                    if (windowGlassPreview.GetValue(settings) != null || (System.IO.File.Exists(transparencyPath) ? System.IO.File.ReadAllText(transparencyPath) : null) != storedTransparency)
                        throw new Exception("Leaving glass preview must restore the applied rendering without saving.");
                    if (!glassPreviews[i].Visible || glassPreviews[i].PreviewEnabled != selected)
                        throw new Exception("Inline previews must always show the selected state at rest.");
                    typeof(Control).GetMethod("OnMouseEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(options[i], new object[] { EventArgs.Empty });
                    if (!Equals(windowGlassPreview.GetValue(settings), i == 2 ? (object)null : !selected))
                        throw new Exception("Hover must preview the inverse glass state on the settings window.");
                    if (!glassPreviews[i].Visible || glassPreviews[i].PreviewEnabled == selected || options[i].Checked != selected || Application.OpenForms.Count != previewWindows)
                        throw new Exception("Checkbox hover must preview the inverse without toggling or opening a window.");
                    typeof(Control).GetMethod("OnMouseLeave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(options[i], new object[] { EventArgs.Empty });
                    if (windowGlassPreview.GetValue(settings) != null || (System.IO.File.Exists(transparencyPath) ? System.IO.File.ReadAllText(transparencyPath) : null) != storedTransparency)
                        throw new Exception("Leaving glass preview must restore the applied rendering without saving.");
                    if (!glassPreviews[i].Visible || glassPreviews[i].PreviewEnabled != selected)
                        throw new Exception("Inline preview must remain visible and restore the selected state on mouse leave.");
                }
                if (glassPreviews.Length != 3)
                    throw new Exception("Each glass option needs a visual preview.");
                using (var before = new Bitmap(glassPreviews[0].Width, glassPreviews[0].Height))
                using (var after = new Bitmap(before.Width, before.Height))
                {
                    bool saved = TransparencyPreferences.Locked;
                    glassPreviews[0].DrawToBitmap(before, glassPreviews[0].ClientRectangle);
                    options.Single(option => option.Text == "Locked overlay").Checked = !saved;
                    glassPreviews[0].DrawToBitmap(after, glassPreviews[0].ClientRectangle);
                    if (before.GetPixel(12, 12).ToArgb() == after.GetPixel(12, 12).ToArgb() || TransparencyPreferences.Locked != saved)
                        throw new Exception("Glass preview must change immediately without applying preferences.");
                    options.Single(option => option.Text == "Locked overlay").Checked = saved;
                }
                bool previous = TransparencyPreferences.Locked;
                options.Single(option => option.Text == "Locked overlay").Checked = !previous;
                int windows = Application.OpenForms.Count;
                embedded.Controls.OfType<SettingsButton>().Single(button => button.Text == "Apply").PerformClick();
                if (Application.OpenForms.Count != windows || TransparencyPreferences.Locked == previous)
                    throw new Exception("Applying glass options must save inline without opening another dialog.");
                options.Single(option => option.Text == "Locked overlay").Checked = previous;
                embedded.Controls.OfType<SettingsButton>().Single(button => button.Text == "Apply").PerformClick();
                using (var glassSettings = new TransparencyDialog(() => { }))
                {
                    var controls = glassSettings.Controls.Cast<Control>().Where(control => control.TabStop).ToArray();
                    if (controls.Select(control => control.TabIndex).Distinct().Count() != controls.Length)
                        throw new Exception("Transparency settings need a unique tab position for every action.");
                    if (glassSettings.Controls.OfType<RoundCheckBox>().Any(check => string.IsNullOrWhiteSpace(check.AccessibleDescription)))
                        throw new Exception("Transparency options must expose their purpose to screen readers.");
                }
                settings.Controls.OfType<SettingsButton>().Single(button => button.Text == "Types").PerformClick();
                transition.Restart();
                while (transition.ElapsedMilliseconds < 260)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
                if (settings.ClientSize.Height != initial)
                    throw new Exception("Types height was not restored after animation");
            }

        }
    }
}
