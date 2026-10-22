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
        private readonly Label selectionTitle = new Label();
        private readonly DrawingPanel attackLegend = new DrawingPanel(), defenseLegend = new DrawingPanel();
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly IconButton combinedButton, separateButton;
        private bool combined = true;
        internal RadarDialog(int main, int sub, Keys[] configuredBindings)
        {
            bindings = (Keys[])configuredBindings.Clone();
            selection.Main = main;
            selection.Sub = sub;
            Text = "Affinity radar";
            ClientSize = new Size(900, 578);
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
                var button = new IconButton { Type = type, Glyph = "reset", TabIndex = i == 0 ? 9 : i - 1, Shortcut = KeyBindings.Label(bindings[i]), AccessibleName = i == 0 ? "Clear types" : Model.Names[type] };
                button.Click += (s, e) => { if (action == 0) selection.Clear(); else selection.Toggle(type); RefreshSelection(); };
                selector.Controls.Add(button);
                tips.SetToolTip(button, button.AccessibleName + (button.Shortcut.Length > 0 ? " · " + button.Shortcut : ""));
                button.AccessibleDescription = button.Shortcut.Length > 0 ? "Shortcut: " + button.Shortcut : "No shortcut";
            }
            var modes = new TypeSelector { IconModes = true, Location = new Point(784, 38), Height = 54, TabIndex = 10, AccessibleName = "Radar layout" };
            combinedButton = new IconButton { Glyph = "combined", AccessibleName = "Combined radars", TabIndex = 0 };
            separateButton = new IconButton { Glyph = "separate", AccessibleName = "Separate radars", TabIndex = 1 };
            combinedButton.Click += (s, e) => { combined = true; RefreshSelection(); };
            separateButton.Click += (s, e) => { combined = false; RefreshSelection(); };
            modes.Controls.Add(combinedButton);
            modes.Controls.Add(separateButton);
            modes.Arrange(false);
            Controls.Add(modes);
            tips.SetToolTip(combinedButton, "Overlay attack and defense on the same scale");
            tips.SetToolTip(separateButton, "Show attack and defense side by side");
            selectionTitle.Location = new Point(462, 48);
            selectionTitle.Size = new Size(312, 38);
            UiFonts.Assign(selectionTitle, UiFonts.Create("Segoe UI", 11, FontStyle.Bold));
            selectionTitle.TextAlign = ContentAlignment.MiddleCenter;
            selectionTitle.AutoEllipsis = true;
            Controls.Add(selectionTitle);
            chart.Location = new Point(0, 88);
            chart.Size = new Size(900, 450);
            chart.Paint += (s, e) => PaintChart(e.Graphics);
            Controls.Add(chart);
            ConfigureLegend(attackLegend, "atk", "Attack", "Higher values deal more damage. Uses the best available attacking type.\nFixed scale: 0 to ×2.56 · Amber curve.");
            ConfigureLegend(defenseLegend, "def", "Defense", "Lower values mean less damage received. Combines both selected types.\nFixed scale: 0 to ×2.56 · Blue curve.");
            Activated += (s, e) => selector.Invalidate(true);
            Deactivate += (s, e) => selector.Invalidate(true);
            RefreshSelection();
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
                Glyphs.Draw(e.Graphics, glyph, new RectangleF(7, 7, 18, 18), color);
                using (var font = UiFonts.Create("Segoe UI", 9))
                    Drawing.Text(e.Graphics, title, font, color, new RectangleF(32, 0, 78, 32), false);
            };
            Controls.Add(legend);
            tips.SetToolTip(legend, title + "\n" + explanation);
        }
        private void RefreshSelection()
        {
            attackLegend.Location = new Point((int)((combined ? 336 : 170) * AppliedWindowScale), (int)(534 * AppliedWindowScale));
            defenseLegend.Location = new Point((int)((combined ? 454 : 620) * AppliedWindowScale), (int)(534 * AppliedWindowScale));
            selector.Arrange(selection.Main >= 0);
            foreach (var button in selector.Controls.OfType<IconButton>())
            {
                button.Active = button.Type >= 0 && (button.Type == selection.Main || button.Type == selection.Sub);
                button.Badge = button.Type < 0 ? "" : button.Type == selection.Main ? "Main" : button.Type == selection.Sub ? "Sub" : "";
                button.Invalidate();
            }
            combinedButton.Active = combined;
            separateButton.Active = !combined;
            combinedButton.Invalidate();
            separateButton.Invalidate();
            chart.Invalidate();
            chart.AccessibleRole = AccessibleRole.Graphic;
            chart.AccessibleText = () => AccessibleMatchups.Describe(selection);
            selectionTitle.Text = selection.Main < 0 ? "Select one or two types" : Model.Names[selection.Main] + (selection.Sub < 0 ? "" : " + " + Model.Names[selection.Sub]);
            chart.AccessibleName = selection.Main < 0 ? "Select one or two types" : Model.Names[selection.Main] + (selection.Sub < 0 ? "" : " + " + Model.Names[selection.Sub]) + " attack and defense radar";
        }
        // Inputs: multiplier on the shared fixed scale. Output: radial fraction. Fallback: clamps to the supported range.


        private void PaintChart(Graphics g)
        {
            g.Clear(Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(0, -28);
            if (combined)
                RadarRenderer.DrawRadar(g, new PointF(450, 256), 152, true, true, selection);
            else
            {
                RadarRenderer.DrawRadar(g, new PointF(225, 274), 142, true, false, selection);
                RadarRenderer.DrawRadar(g, new PointF(675, 274), 142, false, true, selection);
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
