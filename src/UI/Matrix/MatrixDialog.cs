using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class MatrixDialog : PortableWindow
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
                return "matrix";
            }
        }
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly DrawingPanel[,] cells = new DrawingPanel[10, 10];
        private readonly IconButton[] selectors = new IconButton[9];
        private readonly IconButton single;
        private readonly SettingsButton highlight;
        private readonly TypeSelector selector = new TypeSelector();
        private int fixedType, hoverRow = -1, hoverColumn = -1;
        private bool bestPinned, bestPreview;
        private readonly Keys[] bindings;
        internal MatrixDialog(int initialBase, Keys[] configuredBindings = null, int radarMain = -1, int radarSub = -1)
        {
            bindings = (Keys[])(configuredBindings ?? KeyBindings.Load(KeyBindings.FilePath)).Clone();
            fixedType = initialBase;
            Text = "Effectiveness matrix";
            ClientSize = new Size(720, 592);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            AddAxisLegend(20, "rows", "atk", "Rows: attacker");
            AddAxisLegend(96, "columns", "def", "Columns: defender");
            Controls.Add(new Label { Text = "× Damage multiplier", ForeColor = Theme.Muted, Location = new Point(176, 46), Size = new Size(180, 20) });
            selector.Location = new Point(8, 66);
            selector.Height = 54;
            Controls.Add(selector);
            tips.SetToolTip(selector, "Select a type to compare dual-type defenders.");
            single = new IconButton { Glyph = "reset", Shortcut = KeyBindings.Label(bindings[0]), TabIndex = 9, AccessibleName = "Single types" };
            single.Click += (s, e) => SelectBase(-1);
            selector.Controls.Add(single);
            SetShortcutTip(single, "Single-type matrix");
            for (int i = 0; i < 9; i++)
            {
                int type = Array.IndexOf(Model.Names, KeyBindings.Actions[i + 1]);
                var button = new IconButton { Type = type, Shortcut = KeyBindings.Label(bindings[i + 1]), TabIndex = i, AccessibleName = Model.Names[type] };
                button.Click += (s, e) => SelectBase(fixedType == type ? -1 : type);
                selectors[i] = button;
                selector.Controls.Add(button);
                SetShortcutTip(button, Model.Names[type] + " + each defending column");
            }
            highlight = new SettingsButton { Text = "Best matchups", Location = new Point(556, 80), Size = new Size(140, 30), TabIndex = 10 };
            highlight.Click += (s, e) => { bestPinned = !bestPinned; RefreshGrid(); };
            highlight.MouseEnter += (s, e) => { bestPreview = true; RefreshGrid(); };
            highlight.MouseLeave += (s, e) => { bestPreview = false; RefreshGrid(); };
            Controls.Add(highlight);
            tips.SetToolTip(highlight, "Highlight strongest attacking rows and resisting columns" + (bindings.Contains(Keys.H) ? "" : " · H"));
            Activated += (s, e) => selector.Invalidate(true);
            Deactivate += (s, e) => selector.Invalidate(true);
            for (int row = 0; row < 10; row++)
                for (int column = 0; column < 10; column++)
                {
                    int r = row, c = column;
                    var cell = new DrawingPanel { Location = new Point(20 + column * 68, 124 + row * 44), Size = new Size(64, 40), TabStop = false, AccessibleRole = AccessibleRole.StaticText };
                    cell.Paint += (s, e) => PaintCell(e.Graphics, r, c, cell.ClientRectangle);
                    cell.MouseEnter += (s, e) => { hoverRow = r; hoverColumn = c; InvalidateGrid(); };
                    cell.MouseLeave += (s, e) => { hoverRow = -1; hoverColumn = -1; InvalidateGrid(); };
                    cells[row, column] = cell;
                    Controls.Add(cell);
                }
            RefreshGrid();
        }
        private void AddAxisLegend(int left, string axis, string role, string description)
        {
            var legend = new DrawingPanel { Location = new Point(left, 42), Size = new Size(64, 24), TabStop = false, AccessibleRole = AccessibleRole.StaticText, AccessibleName = description };
            legend.Paint += (s, e) =>
            {
                e.Graphics.Clear(Theme.Background);
                Glyphs.Draw(e.Graphics, axis, new RectangleF(0, 2, 20, 20), Theme.Muted);
                Glyphs.Draw(e.Graphics, role, new RectangleF(30, 2, 20, 20), Theme.TextColor);
            };
            Controls.Add(legend);
            tips.SetToolTip(legend, description);
        }
        private void SelectBase(int type)
        {
            fixedType = type;
            RefreshGrid();
        }
        private void SetShortcutTip(IconButton button, string label)
        {
            button.AccessibleDescription = button.Shortcut.Length > 0 ? "Shortcut: " + button.Shortcut : "No shortcut";
            tips.SetToolTip(button, label + (button.Shortcut.Length > 0 ? " · " + ShortcutText.Mark(button.Shortcut) : ""));
        }
        private double Factor(int attack, int defense)
        {
            return Model.Factors(defense, fixedType)[9 + attack];
        }
        private void RefreshGrid()
        {
            selector.Arrange(fixedType >= 0);
            highlight.Primary = bestPinned;
            highlight.Invalidate();
            foreach (var button in selectors)
            {
                button.Active = button.Type == fixedType;
                button.Invalidate();
            }
            for (int row = 0; row < 10; row++)
                for (int column = 0; column < 10; column++)
                {
                    var cell = cells[row, column];
                    if (cell == null)
                        continue;
                    string label;
                    if (row == 0 && column == 0)
                        label = "Rows attack · Columns defend";
                    else if (column == 0)
                        label = Model.Names[MatrixRenderer.Order[row - 1]] + " attack";
                    else
                    {
                        int defense = MatrixRenderer.Order[column - 1];
                        string defenders = Model.Names[defense] + (fixedType >= 0 && fixedType != defense ? " + " + Model.Names[fixedType] : "");
                        label = defenders + " defense";
                        if (row > 0)
                        {
                            int attack = MatrixRenderer.Order[row - 1];
                            label = Model.Names[attack] + " → " + defenders + ": " + Model.Factor(Factor(attack, defense));
                            if (fixedType >= 0 && fixedType != defense)
                                label += " (" + Model.Factor(Model.Factors(defense, -1)[9 + attack]) + " × " + Model.Factor(Model.Factors(fixedType, -1)[9 + attack]) + ")";
                        }
                    }
                    cell.AccessibleName = label;
                    tips.SetToolTip(cell, label);
                    cell.Invalidate();
                }
        }
        private void InvalidateGrid()
        {
            foreach (var cell in cells)
                cell.Invalidate();
        }
        private void PaintCell(Graphics g, int row, int column, Rectangle area)
        {
            g.Clear(Theme.Background);
            MatrixRenderer.DrawCell(g, row, column, area, fixedType, hoverRow, hoverColumn, bestPinned || bestPreview);
        }

        protected override bool ProcessCmdKey(ref Message message, Keys input)
        {
            if (input == Keys.Escape)
            {
                Close();
                return true;
            }
            int index = KeyBindings.Resolve(bindings, input);
            if (index >= 0 && index < 10)
            {
                if ((message.LParam.ToInt64() & (1L << 30)) == 0)
                {
                    int type = index == 0 ? -1 : selectors[index - 1].Type;
                    SelectBase(type == fixedType ? -1 : type);
                }
                return true;
            }
            if (input == Keys.H && !bindings.Contains(Keys.H))
            {
                if ((message.LParam.ToInt64() & (1L << 30)) == 0)
                {
                    bestPinned = !bestPinned;
                    RefreshGrid();
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
