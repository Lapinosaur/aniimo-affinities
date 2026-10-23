using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TeamBoard : Panel
    {
        private readonly TeamPlan plan;
        private readonly TeamCard[] cards = new TeamCard[6];
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly Action<int> select;
        private Rectangle obstruction;
        private bool arranging;
        internal bool TransitionLayout;
        internal event Action ComparisonChanged;
        internal event Action TeamChanged;
        private bool overlayLocked;
        internal void SetOverlayLocked(bool value)
        {
            overlayLocked = value;
            RefreshActions();
        }
        private void RefreshActions()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].CompareCheck.Visible = !overlayLocked && plan.Allies[i].Known;
                cards[i].SuggestButton.Visible = !overlayLocked && !plan.Allies[i].Known && cards[i].Suggestion != null;
            }
        }
        internal int[] ComparedSlots
        {
            get
            {
                return Enumerable.Range(0, cards.Length).Where(i => plan.Allies[i].Known && cards[i].CompareCheck.Checked).ToArray();
            }
        }
        internal int SelectedIndex
        {
            get; private set;
        }
        internal void SelectCard(int index)
        {
            SelectedIndex = index;
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Selected = i == index;
                cards[i].Invalidate();
            }
        }
        internal void SyncSelection(int main, int sub)
        {
            if (SelectedIndex >= 0 && (plan.Allies[SelectedIndex].Main != main || plan.Allies[SelectedIndex].Sub != sub))
                SelectCard(-1);
        }
        internal void AvoidSupportBubble(Rectangle screenBounds)
        {
            obstruction = screenBounds;
            AutoScrollPosition = Point.Empty;
            Arrange();
        }
        internal TeamBoard(TeamPlan value, Action<int> onSelect, Action<int> onHover = null, Action<int, int> onCounterHover = null, Action<int> onCardHover = null)
        {
            plan = value;
            SelectedIndex = -1;
            select = onSelect;
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                cards[i] = new TeamCard(plan, index) { TabIndex = i };
                cards[i].SuggestButton.Click += (s, e) =>
                {
                    if (plan.Allies[index].Known)
                        return;
                    var addition = TeamSuggestions.ComplementaryAdditions(plan)[index];
                    if (addition == null)
                        return;
                    plan.Allies[index].Main = addition.Types.Main;
                    plan.Allies[index].Sub = addition.Types.Sub;
                    RefreshCards();
                    if (TeamChanged != null)
                        TeamChanged();
                };
                cards[i].CompareCheck.CheckedChanged += (s, e) =>
                {
                    cards[index].Invalidate();
                    if (ComparisonChanged != null)
                        ComparisonChanged();
                };
                tips.SetToolTip(cards[i].CompareCheck, "Include this ally in both radars. Its card color identifies its traces. Space toggles the checkbox.");
                cards[i].Click += (s, e) => select(index);
                cards[i].MouseEnter += (s, e) => { if (onCardHover != null) onCardHover(index); };
                cards[i].MouseLeave += (s, e) => { if (onCardHover != null) onCardHover(-1); };
                cards[i].GotFocus += (s, e) => { if (onCardHover != null) onCardHover(index); };
                cards[i].LostFocus += (s, e) => { if (onCardHover != null) onCardHover(-1); };
                cards[i].ElementHovered += type => { if (onHover != null) onHover(type); };
                cards[i].CounterHovered += counter =>
                {
                    tips.SetToolTip(cards[index], counter < 0 ? cards[index].DefaultTooltip : cards[index].CounterTooltip(counter == 0));
                    if (counter < 0 && cards[index].Suggestion != null)
                        SuggestionGraphic.Attach(tips, cards[index], cards[index].Suggestion, "Click the suggestion button to apply");
                    int mask = 0;
                    if (counter >= 0)
                        foreach (var opponent in plan.Opponents.Where(x => x.Known && (counter == 0 ? TeamPlan.Damage(plan.Allies[index], x) : TeamPlan.Damage(x, plan.Allies[index])) > 1.0001))
                        {
                            mask |= 1 << opponent.Main;
                            if (opponent.Sub >= 0)
                                mask |= 1 << opponent.Sub;
                        }
                    if (onCounterHover != null)
                        onCounterHover(counter, mask);
                };
                Controls.Add(cards[i]);
            }
            Resize += (s, e) => Arrange();
            RefreshCards();
        }
        internal void RefreshCards()
        {
            var additions = TeamSuggestions.ComplementaryAdditions(plan);
            for (int i = 0; i < 6; i++)
            {
                var member = plan.Allies[i];
                cards[i].CompareCheck.Enabled = member.Known;
                cards[i].Suggestion = additions[i];
                cards[i].SuggestButton.AccessibleName = "Apply suggested elements for " + member.Title(i);
                string advice = TeamTypeSelector.RecommendationTip(additions[i], "Click the suggestion button to apply");
                cards[i].SuggestButton.AccessibleDescription = additions[i] == null ? "" : "Apply " + Model.Names[additions[i].Types.Main] + (additions[i].Types.Sub < 0 ? "" : " and " + Model.Names[additions[i].Types.Sub]);
                SuggestionGraphic.Attach(tips, cards[i].SuggestButton, additions[i], "Click the suggestion button to apply");
                cards[i].CompareCheck.AccessibleName = "Compare " + member.Title(i) + " on radars";
                if (!member.Known)
                    cards[i].CompareCheck.Checked = false;
                string detail = member.Known ? "[types:" + member.Main + "," + member.Sub + "]" + member.Title(i) : member.Title(i);
                if (member.Known)
                {
                    detail += "\n\nHover to preview · Click to select\nHover counters for matchups";
                }
                else
                    detail += "\n\n" + (additions[i] == null ? "Click to choose this ally's elements." : advice);
                cards[i].AccessibleName = member.Title(i);
                cards[i].AccessibleDescription = member.Title(i) + ". " + plan.Summary(member);
                if (member.Known)
                    cards[i].AccessibleDescription += ". " + Model.Names[member.Main] + (member.Sub >= 0 ? " + " + Model.Names[member.Sub] : "") + ". Focus to preview; Enter or Space to select.";
                cards[i].DefaultTooltip = detail;
                tips.SetToolTip(cards[i], detail);
                if (additions[i] != null)
                    SuggestionGraphic.Attach(tips, cards[i], additions[i], "Click the suggestion button to apply");
                cards[i].Invalidate();
            }
            RefreshActions();
            Arrange();
        }
        private void Arrange()
        {
            if (arranging || TransitionLayout)
                return;
            arranging = true;
            SuspendLayout();
            AutoScrollMinSize = Size.Empty;
            bool vertical = plan.Position == "Left" || plan.Position == "Right";
            int gap = Math.Max(3, (int)(6 * WindowZoom.Factor(this)));
            var blocked = obstruction.IsEmpty ? Rectangle.Empty : RectangleToClient(obstruction);
            AutoScroll = false;
            for (int i = 0; i < 6; i++)
            {
                Rectangle bounds;
                if (vertical)
                    bounds = new Rectangle(gap, i * Height / 6 + gap, Math.Max(1, Width - gap * 2), Math.Max(1, Height / 6 - gap));
                else
                    bounds = new Rectangle(i * Width / 6 + gap, gap, Math.Max(1, Width / 6 - gap * 2), Math.Max(1, Height - gap * 2));
                if (!blocked.IsEmpty && bounds.IntersectsWith(blocked))
                {
                    int right = bounds.Right;
                    bounds.X = blocked.Right + gap;
                    bounds.Width = Math.Max(1, right - bounds.X);
                }
                cards[i].Bounds = bounds;
            }
            ResumeLayout(false);
            arranging = false;
        }
        internal bool CanAvoidSupportBubble(Rectangle screenBounds)
        {
            var blocked = RectangleToClient(screenBounds);
            int gap = Math.Max(3, (int)(6 * WindowZoom.Factor(this)));
            foreach (Control card in Controls)
                if (card.Bounds.IntersectsWith(blocked) && card.Right - blocked.Right - gap < 110 * WindowZoom.Factor(this))
                    return false;
            return true;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class TeamCard : InteractiveButton
    {
        private readonly TeamPlan plan;
        private readonly int index;
        private int hoveredElement = -1;
        private int hoveredCounter = -1;
        private bool selected;
        internal bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                CompareCheck.BackColor = value ? Theme.AccentSurface : Theme.Surface;
            }
        }
        internal readonly RoundCheckBox CompareCheck = new RoundCheckBox { Text = "", TabIndex = 0, Size = new Size(24, 24) };
        internal readonly SettingsButton SuggestButton = new SettingsButton { Glyph = "suggest", Quiet = true, Text = "", TabIndex = 0, Visible = false };
        internal TeamSuggestions.Recommendation Suggestion;
        private Color CardSurface()
        {
            return Selected || InteractionPressed ? Theme.AccentSurface : Theme.Surface;
        }
        protected override bool IsSelected
        {
            get
            {
                return Selected;
            }
        }
        private bool cardHovered;
        protected override void OnMouseEnter(EventArgs e)
        {
            cardHovered = true;
            base.OnMouseEnter(e);
            Invalidate();
        }
        internal string DefaultTooltip = "";
        internal event Action<int> CounterHovered;
        internal event Action<int> ElementHovered;
        private RectangleF CounterBounds(int counter)
        {
            float scale = WindowZoom.Factor(this), w = Width / scale, h = Height / scale;
            bool compact = h < 80 && w >= 180;
            if (w < 150 && h < 80)
                return new RectangleF(6 + counter * (w - 12) / 2, h - 20, (w - 12) / 2 - 2, 16);
            float left = compact ? 80 : 10, cell = (w - left - 10) / 2;
            return new RectangleF(left + counter * cell, compact ? 33 : h - 27, cell - 3, 20);
        }
        internal string CounterTooltip(bool attack)
        {
            var member = plan.Allies[index];
            var names = plan.Opponents.Select((opponent, slot) => new { opponent, slot }).Where(x => x.opponent.Known).ToArray();
            var affected = names.Where(x => (attack ? TeamPlan.Damage(member, x.opponent) : TeamPlan.Damage(x.opponent, member)) > 1.0001).ToArray();
            string text = (attack ? "Attack" : "Defense") + " · " + affected.Length + " of " + names.Length + " known opponents\n";
            text += attack ? "Deals more than ×1 damage." : "Receives more than ×1 damage. Fewer is better.";
            return text + "\n\n" + (affected.Length == 0 ? "No opponents affected." : string.Join("\n", affected.Select(x => "[types:" + x.opponent.Main + "," + x.opponent.Sub + "]" + x.opponent.Title(x.slot, true) + "  " + Model.Factor(attack ? TeamPlan.Damage(member, x.opponent) : TeamPlan.Damage(x.opponent, member)))));
        }
        private void SetCounter(int counter)
        {
            if (counter == hoveredCounter)
                return;
            hoveredCounter = counter;
            if (CounterHovered != null)
                CounterHovered(counter);
        }
        private RectangleF ElementBounds(int slot)
        {
            float scale = WindowZoom.Factor(this), w = Width / scale, h = Height / scale;
            if (w < 150 && h < 80)
                return new RectangleF(7 + slot * 24, 23, 21, 21);
            return new RectangleF(10 + slot * 34, h < 80 ? 28 : 30, 28, 28);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var member = plan.Allies[index];
            float scale = WindowZoom.Factor(this);
            var point = new PointF(e.X / scale, e.Y / scale);
            int type = member.Known && ElementBounds(0).Contains(point) ? member.Main : member.Sub >= 0 && ElementBounds(1).Contains(point) ? member.Sub : -1;
            SetHovered(type);
            SetCounter(member.Known && plan.Opponents.Any(x => x.Known) ? (CounterBounds(0).Contains(point) ? 0 : CounterBounds(1).Contains(point) ? 1 : -1) : -1);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            cardHovered = false;
            base.OnMouseLeave(e);
            SetHovered(-1);
            SetCounter(-1);
            Invalidate();
        }
        private void SetHovered(int type)
        {
            if (type == hoveredElement)
                return;
            hoveredElement = type;
            Invalidate();
            if (ElementHovered != null)
                ElementHovered(type);
        }
        internal TeamCard(TeamPlan value, int slot)
        {
            plan = value;
            index = slot;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            Controls.Add(CompareCheck);
            Controls.Add(SuggestButton);
            CompareCheck.SurfaceColor = CardSurface;
            SuggestButton.SurroundColor = CardSurface;
            CompareCheck.BackColor = Theme.Surface;
            CompareCheck.Enabled = plan.Allies[index].Known;
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            float scale = WindowZoom.Factor(this);
            if (CompareCheck != null)
                CompareCheck.SetBounds(Math.Max(0, Width - (int)(31 * scale)), (int)(4 * scale), (int)(24 * scale), (int)(24 * scale));
            if (SuggestButton != null)
                SuggestButton.SetBounds(Math.Max(0, Width - (int)(33 * scale)), (int)(2 * scale), (int)(28 * scale), (int)(28 * scale));
        }
        private static void DrawScore(Graphics g, RectangleF bounds, string glyph, int count, int total)
        {
            Color ink = glyph == "atk" ? Theme.Attack : Theme.Defense;
            Drawing.Box(g, bounds, glyph == "atk" ? Theme.AttackSurface : Theme.DefenseSurface, Color.Transparent, 5);
            float icon = bounds.Height < 20 ? 10 : 14;
            Glyphs.Draw(g, glyph, new RectangleF(bounds.X + 3, bounds.Y + 3, icon, icon), ink);
            using (var font = UiFonts.Create("Segoe UI", 7.5f, FontStyle.Bold))
                Drawing.Text(g, count + "/" + total, font, ink, new RectangleF(bounds.X + icon + 5, bounds.Y, bounds.Width - icon - 7, bounds.Height), true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            // Remove the native button background before drawing the rounded card.
            e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);
            float scale = WindowZoom.Factor(this);
            e.Graphics.ScaleTransform(scale, scale);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Width / scale, h = Height / scale;
            Drawing.Box(g, new RectangleF(1, 1, w - 3, h - 3), Selected || InteractionPressed ? Theme.AccentSurface : Theme.Surface, Selected || cardHovered ? Theme.Accent : Theme.Border, Theme.ControlRadius);
            if (CompareCheck.Checked)
                using (var legend = new Pen(Theme.ComparisonInk(index), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(legend, 12, 28, 38, 28);
            if (Selected)
            {
                Drawing.Box(g, new RectangleF(3, 10, 3, h - 20), Theme.Accent, Color.Transparent, 1);
            }
            var member = plan.Allies[index];
            bool compact = h < 80 && w >= 180;
            using (var font = UiFonts.Create("Segoe UI", 9, FontStyle.Bold))
            using (var brush = new SolidBrush(Theme.TextColor))
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                g.DrawString(member.Title(index), font, brush, new RectangleF(10, 5, Math.Max(1, w - 43), 22), format);
            var shown = member.Known ? member : Suggestion == null ? null : Suggestion.Types;
            if (shown != null)
            {
                for (int slot = 0; slot < (shown.Sub >= 0 ? 2 : 1); slot++)
                {
                    int type = slot == 0 ? shown.Main : shown.Sub;
                    var bounds = ElementBounds(slot);
                    Drawing.Box(g, bounds, hoveredElement == type ? Theme.AccentSurface : Theme.Surface, hoveredElement == type ? Theme.Accent : Theme.Surface, 7);
                    VectorIcon.Draw(g, type, new RectangleF(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), !member.Known);
                }
            }
            var counts = plan.CountMatchups(member);
            if (member.Known && counts.Total > 0)
            {
                DrawScore(g, CounterBounds(0), "atk", counts.Strong, counts.Total);
                DrawScore(g, CounterBounds(1), "def", counts.Weak, counts.Total);
            }
            else
                using (var font = UiFonts.Create("Segoe UI", 7))
                    Drawing.Text(g, member.Known ? "No opponents" : Suggestion != null ? SuggestionGraphic.Title(Suggestion) : "Choose elements", font, Theme.Muted, new RectangleF(compact ? 80 : 10, compact ? 33 : h - 27, compact ? w - 90 : w - 20, 20), false);
            FinishPaint(g, w, h);
        }
    }
}
