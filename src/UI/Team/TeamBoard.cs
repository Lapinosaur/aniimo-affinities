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
                cards[i].Click += (s, e) => select(index);
                cards[i].MouseEnter += (s, e) => { if (onCardHover != null) onCardHover(index); };
                cards[i].MouseLeave += (s, e) => { if (onCardHover != null) onCardHover(-1); };
                cards[i].ElementHovered += type => { if (onHover != null) onHover(type); };
                cards[i].CounterHovered += counter =>
                {
                    tips.SetToolTip(cards[index], counter < 0 ? cards[index].DefaultTooltip : cards[index].CounterTooltip(counter == 0));
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
            for (int i = 0; i < 6; i++)
            {
                var member = plan.Allies[i];
                string detail = member.Known ? "[types:" + member.Main + "," + member.Sub + "]" + member.Title(i) : member.Title(i);
                if (member.Known)
                {
                    detail += "\n\nHover to preview · Click to select\nHover counters for matchups";
                }
                else
                    detail += "\n\nClick to choose this ally's elements.";
                cards[i].AccessibleName = member.Title(i);
                cards[i].AccessibleDescription = member.Title(i) + ". " + plan.Summary(member);
                cards[i].DefaultTooltip = detail;
                tips.SetToolTip(cards[i], detail);
                cards[i].Invalidate();
            }
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

    internal sealed class TeamCard : Button
    {
        private readonly TeamPlan plan;
        private readonly int index;
        private int hoveredElement = -1;
        private int hoveredCounter = -1;
        internal bool Selected;
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
            Drawing.Box(g, new RectangleF(1, 1, w - 3, h - 3), Selected ? Theme.AccentSurface : Theme.Surface, Selected || cardHovered ? Theme.Accent : Theme.Border, Theme.ControlRadius);
            if (Selected)
            {
                Drawing.Box(g, new RectangleF(3, 10, 3, h - 20), Theme.Accent, Color.Transparent, 1);
                Glyphs.Draw(g, "check", new RectangleF(w - 23, 9, 13, 13), Theme.AccentForeground);
            }
            if (Focused)
                ControlPaint.DrawFocusRectangle(g, Rectangle.Round(new RectangleF(7, 4, w - 14, h - 9)), Theme.Muted, Selected ? Theme.AccentSurface : Theme.Surface);
            var member = plan.Allies[index];
            bool compact = h < 80 && w >= 180;
            using (var font = UiFonts.Create("Segoe UI", 9, FontStyle.Bold))
                Drawing.Text(g, member.Title(index), font, Theme.TextColor, new RectangleF(10, 5, w - (Selected ? 37 : 20), 22), false);
            if (member.Known)
            {
                for (int slot = 0; slot < (member.Sub >= 0 ? 2 : 1); slot++)
                {
                    int type = slot == 0 ? member.Main : member.Sub;
                    var bounds = ElementBounds(slot);
                    Drawing.Box(g, bounds, hoveredElement == type ? Theme.AccentSurface : Theme.Surface, hoveredElement == type ? Theme.Accent : Theme.Surface, 7);
                    VectorIcon.Draw(g, type, new RectangleF(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4));
                }
            }
            var opponents = plan.Opponents.Where(x => x.Known).ToArray();
            if (member.Known && opponents.Length > 0)
            {
                DrawScore(g, CounterBounds(0), "atk", opponents.Count(x => TeamPlan.Damage(member, x) > 1.0001), opponents.Length);
                DrawScore(g, CounterBounds(1), "def", opponents.Count(x => TeamPlan.Damage(x, member) > 1.0001), opponents.Length);
            }
            else
                using (var font = UiFonts.Create("Segoe UI", 7))
                    Drawing.Text(g, member.Known ? "No opponents" : "Choose elements", font, Theme.Muted, new RectangleF(compact ? 80 : 10, compact ? 33 : h - 27, compact ? w - 90 : w - 20, 20), false);
        }
    }
}
