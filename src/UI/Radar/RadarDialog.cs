using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class RadarDialog : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        protected override string CaptionGlyph
        {
            get
            {
                return "radar";
            }
        }
        private readonly Selection selection = new Selection();
        private readonly Keys[] bindings;
        private readonly TypeSelector selector = new TypeSelector();
        private readonly DrawingPanel chart = new DrawingPanel();
        private readonly DrawingPanel selectionTitle = new DrawingPanel();
        private readonly DrawingPanel attackLegend = new DrawingPanel(), defenseLegend = new DrawingPanel();
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly IconButton combinedButton, separateButton;
        private readonly TypeSelector modes;
        private bool combined = true;
        internal RadarDialog(int main, int sub, Keys[] configuredBindings)
        {
            bindings = (Keys[])configuredBindings.Clone();
            selection.Main = main;
            selection.Sub = sub;
            Text = Language.T("Affinity radar");
            ClientSize = new Size(600, 578);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            selector.Location = new Point(8, 38);
            selector.Height = 54;
            Controls.Add(selector);
            for (int i = 0; i < 10; i++)
            {
                int action = i, type = i == 0 ? -1 : Array.IndexOf(Model.Names, KeyBindings.Actions[i]);
                var button = new IconButton { Type = type, Glyph = "reset", TabIndex = i == 0 ? 9 : i - 1, Shortcut = KeyBindings.Label(bindings[i]), AccessibleName = i == 0 ? Language.T("Clear types") : Language.T(Model.Names[type]) };
                button.Click += (s, e) => { if (action == 0) selection.Clear(); else selection.Toggle(type); RefreshSelection(); };
                selector.Controls.Add(button);
                tips.SetToolTip(button, button.AccessibleName + (button.Shortcut.Length > 0 ? " · " + ShortcutText.Mark(button.Shortcut) : ""));
                button.AccessibleDescription = button.Shortcut.Length > 0 ? Language.T("Shortcut: ") + button.Shortcut : Language.T("No shortcut");
            }
            modes = new TypeSelector { IconModes = true, Location = new Point(484, 38), Height = 54, TabIndex = 10, AccessibleName = Language.T("Radar layout") };
            combinedButton = new IconButton { Glyph = "combined", AccessibleName = Language.T("Combined radars"), TabIndex = 0 };
            separateButton = new IconButton { Glyph = "separate", AccessibleName = Language.T("Separate radars"), TabIndex = 1 };
            combinedButton.Click += (s, e) => SetCombined(true);
            separateButton.Click += (s, e) => SetCombined(false);
            modes.Controls.Add(combinedButton);
            modes.Controls.Add(separateButton);
            modes.Arrange(false);
            Controls.Add(modes);
            tips.SetToolTip(combinedButton, Language.T("Overlay attack and defense on the same scale"));
            tips.SetToolTip(separateButton, Language.T("Show attack and defense side by side"));
            selectionTitle.Location = new Point(462, 48);
            selectionTitle.Size = new Size(312, 38);
            UiFonts.Assign(selectionTitle, UiFonts.Create("Segoe UI", 11, FontStyle.Bold));
            selectionTitle.AccessibleRole = AccessibleRole.StaticText;
            selectionTitle.Paint += (s, e) => PaintSelectionTitle(e.Graphics);
            Controls.Add(selectionTitle);
            chart.Location = new Point(0, 88);
            chart.Size = new Size(900, 450);
            chart.Paint += (s, e) => PaintChart(e.Graphics);
            Controls.Add(chart);
            ConfigureLegend(attackLegend, "atk", Language.T("Attack"), Language.T("Higher values deal more damage. Uses the best available attacking type.\nSolid line and round points · Scale shared with defense.\nTeal: effective · Purple: ineffective."));
            ConfigureLegend(defenseLegend, "def", Language.T("Defense"), Language.T("Lower values mean less damage received. Combines both selected types.\nSolid line and square points · Scale shared with attack.\nTeal: resistance · Purple: weakness."));
            Activated += (s, e) => selector.Invalidate(true);
            Deactivate += (s, e) => selector.Invalidate(true);
            RefreshSelection();
            Resize += (s, e) => ArrangeRadar();
            ArrangeRadar();
        }
        private void SetCombined(bool value)
        {
            if (combined == value)
                return;
            int right = Right, top = Top;
            combined = value;
            ClientSize = new Size((int)Math.Round((combined ? 600 : 900) * AppliedWindowScale), ClientSize.Height);
            ApplyWindowScale();
            Location = WindowZoom.FitLocation(new Point(right - Width, top), Size, Screen.FromControl(this).WorkingArea);
            ArrangeRadar();
            RefreshSelection();
        }
        private void ArrangeRadar()
        {
            float scale = AppliedWindowScale;
            modes.Left = ClientSize.Width - (int)Math.Round(116 * scale);
            selectionTitle.SetBounds((int)((combined ? 16 : 462) * scale), (int)((combined ? 86 : 48) * scale), (int)((combined ? 568 : 312) * scale), (int)(30 * scale));
            chart.SetBounds(0, (int)((combined ? 116 : 88) * scale), ClientSize.Width, (int)((combined ? 418 : 450) * scale));
            attackLegend.Location = new Point((int)((combined ? 186 : 170) * scale), (int)(534 * scale));
            defenseLegend.Location = new Point((int)((combined ? 304 : 620) * scale), (int)(534 * scale));
        }
        private void ConfigureLegend(DrawingPanel legend, string glyph, string title, string explanation)
        {
            legend.Size = new Size(110, 32);
            legend.Cursor = Cursors.Help;
            legend.AccessibleName = title;
            legend.AccessibleDescription = explanation;
            legend.AccessibleRole = AccessibleRole.Graphic;
            legend.Paint += (s, e) =>
            {
                e.Graphics.Clear(Theme.Background);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color color = glyph == "atk" ? Theme.Attack : Theme.Defense;
                using (var pen = new Pen(color, 2))
                {
                    e.Graphics.DrawLine(pen, 7, 29, 100, 29);
                }
                Glyphs.Draw(e.Graphics, glyph, new RectangleF(7, 7, 18, 18), color);
                using (var font = UiFonts.Create("Segoe UI", 9))
                    Drawing.Text(e.Graphics, title, font, color, new RectangleF(32, 0, 78, 32), false);
            };
            Controls.Add(legend);
            tips.SetToolTip(legend, title + "\n" + explanation);
        }
        private void RefreshSelection()
        {
            selector.Arrange(selection.Main >= 0);
            foreach (var button in selector.Controls.OfType<IconButton>())
            {
                button.Active = button.Type >= 0 && (button.Type == selection.Main || button.Type == selection.Sub);
                button.Badge = button.Type < 0 ? "" : button.Type == selection.Main ? Language.T("Main") : button.Type == selection.Sub ? Language.T("Sub") : "";
                button.Invalidate();
            }
            combinedButton.Active = combined;
            separateButton.Active = !combined;
            combinedButton.Invalidate();
            separateButton.Invalidate();
            chart.Invalidate();
            chart.AccessibleRole = AccessibleRole.Graphic;
            chart.AccessibleText = () => AccessibleMatchups.Describe(selection);
            selectionTitle.Text = selection.Main < 0 ? Language.T("Select one or two types") : Language.T(Model.Names[selection.Main]) + (selection.Sub < 0 ? "" : " + " + Language.T(Model.Names[selection.Sub]));
            selectionTitle.AccessibleName = selectionTitle.Text;
            selectionTitle.Invalidate();
            chart.AccessibleName = selection.Main < 0 ? Language.T("Select one or two types") : Language.T(Model.Names[selection.Main]) + (selection.Sub < 0 ? "" : " + " + Language.T(Model.Names[selection.Sub])) + Language.T(" attack and defense radar");
        }
        private void PaintSelectionTitle(Graphics graphics)
        {
            graphics.Clear(Theme.Background);
            using (var font = UiFonts.Create("Segoe UI", 11, FontStyle.Bold))
            {
                int icons = selection.Main < 0 ? 0 : selection.Sub < 0 ? 1 : 2;
                float textWidth = graphics.MeasureString(selectionTitle.Text, font).Width;
                float left = (selectionTitle.Width - textWidth - icons * 26) / 2f;
                if (icons > 0)
                    VectorIcon.Draw(graphics, selection.Main, new RectangleF(left, 5, 20, 20));
                if (icons > 1)
                    VectorIcon.Draw(graphics, selection.Sub, new RectangleF(left + 26, 5, 20, 20));
                Drawing.Text(graphics, selectionTitle.Text, font, Theme.TextColor, new RectangleF(left + icons * 26, 0, textWidth, 30), true);
            }
        }


        private void PaintChart(Graphics g)
        {
            g.Clear(Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (combined)
                RadarRenderer.DrawRadar(g, new PointF(chart.Width / 2f, 205), 142, true, true, selection);
            else
            {
                RadarRenderer.DrawRadar(g, new PointF(225, 246), 142, true, false, selection);
                RadarRenderer.DrawRadar(g, new PointF(675, 246), 142, false, true, selection);
            }
        }


        protected override bool ProcessCmdKey(ref Message message, Keys input)
        {
            if (input == Keys.Escape)
            {
                Close();
                return true;
            }
            int action = KeyBindings.Resolve(bindings, input);
            if (action >= 0 && action < 10)
            {
                if ((message.LParam.ToInt64() & (1L << 30)) == 0)
                {
                    if (action == 0)
                        selection.Clear();
                    else
                        selection.Toggle(Array.IndexOf(Model.Names, KeyBindings.Actions[action]));
                    RefreshSelection();
                }
                return true;
            }
            return base.ProcessCmdKey(ref message, input);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
