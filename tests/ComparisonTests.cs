using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ComparisonTests
    {
        internal static void Run()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var main = new MainForm())
            {
                main.Show();
                var plan = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(main);
                var sample = TeamTests.Sample();
                for (int i = 0; i < 6; i++)
                    plan.Allies[i] = sample.Allies[i];
                plan.Visible = true;
                typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(main, null);
                main.FinishTeamTransition();
                typeof(MainForm).GetMethod("ToggleInlineRadar", flags).Invoke(main, null);
                var board = (TeamBoard)typeof(MainForm).GetField("teamBoard", flags).GetValue(main);
                var cards = board.Controls.OfType<TeamCard>().OrderBy(card => card.TabIndex).ToArray();
                var seriesMethod = typeof(MainForm).GetMethod("ComparisonSeries", flags);
                var selected = (Selection)typeof(MainForm).GetField("selection", flags).GetValue(main);
                int originalMain = selected.Main, originalSub = selected.Sub, originalCard = board.SelectedIndex;
                int events = 0;
                board.ComparisonChanged += () => events++;
                for (int i = 0; i < cards.Length; i++)
                {
                    cards[i].CompareCheck.Checked = true;
                    var series = (RadarRenderer.Series[])seriesMethod.Invoke(main, null);
                    if (series.Length != i + 1 || series.Last().Slot != i || !ReferenceEquals(series.Last().Member, plan.Allies[i]))
                        throw new Exception("Each checked ally must contribute exactly one radar series.");
                    double maximum = series.Max(item => Model.Factors(item.Member.Main, item.Member.Sub).Max());
                    if (RadarRenderer.ComparisonMaximum(series) != maximum)
                        throw new Exception("Every comparison must share its actual maximum across both directions.");
                }
                if (events != 6 || selected.Main != originalMain || selected.Sub != originalSub || board.SelectedIndex != originalCard)
                    throw new Exception("Comparison checkboxes must not activate the ordinary card selection.");
                var hoverField = typeof(MainForm).GetField("teamPreview", flags);
                var enter = typeof(Control).GetMethod("OnMouseEnter", flags);
                var leave = typeof(Control).GetMethod("OnMouseLeave", flags);
                foreach (Control target in new Control[] { cards[2], cards[2].CompareCheck })
                {
                    enter.Invoke(target, new object[] { EventArgs.Empty });
                    if ((int)hoverField.GetValue(main) != 2 || board.ComparedSlots.Length != 6 || events != 6
                        || selected.Main != originalMain || selected.Sub != originalSub)
                        throw new Exception("Card and checkbox hover must identify the ally without changing comparison or selection.");
                    using (var hoverBitmap = new Bitmap(main.Width, main.Height))
                        main.DrawToBitmap(hoverBitmap, new Rectangle(Point.Empty, hoverBitmap.Size));
                    leave.Invoke(target, new object[] { EventArgs.Empty });
                    if ((int)hoverField.GetValue(main) != -1)
                        throw new Exception("Leaving an ally must clear its trace highlight.");
                }
                using (var bitmap = new Bitmap(main.Width, main.Height))
                    main.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                plan.Allies[2].Main = plan.Allies[2].Sub = -1;
                board.RefreshCards();
                if (cards[2].CompareCheck.Enabled || cards[2].CompareCheck.Checked || board.ComparedSlots.Length != 5)
                    throw new Exception("Cleared allies must leave the comparison and disable their checkbox.");
                var actionsBeforeLock = cards.Select(card => card.SuggestButton.Visible).ToArray();
                var setLock = typeof(PortableWindow).GetMethod("SetOverlayLocked", flags);
                setLock.Invoke(main, new object[] { true });
                board.RefreshCards();
                if (cards.Any(card => card.CompareCheck.Visible || card.SuggestButton.Visible || card.PreviewButton.Visible) || board.ComparedSlots.Length != 5)
                    throw new Exception("Lock must hide ally actions without losing comparison selections, including after refresh.");
                setLock.Invoke(main, new object[] { false });
                for (int i = 0; i < cards.Length; i++)
                    if (cards[i].CompareCheck.Visible != plan.Allies[i].Known || cards[i].SuggestButton.Visible != actionsBeforeLock[i])
                        throw new Exception("Unlock must restore the appropriate ally actions.");
                foreach (var card in cards)
                    card.CompareCheck.Checked = false;
                if (((RadarRenderer.Series[])seriesMethod.Invoke(main, null)).Length != 0)
                    throw new Exception("Unchecking all cards must restore the ordinary radar.");
            }
            int appearance = Theme.Appearance;
            foreach (int mode in new[] { 0, 2 })
            {
                Theme.Appearance = mode;
                Theme.ApplyAppearance();
                if (Enumerable.Range(0, 6).Select(Theme.ComparisonInk).Distinct().Count() != 6)
                    throw new Exception("Every ally needs its own comparison color.");
            }
            Theme.Appearance = appearance;
            Theme.ApplyAppearance();
        }
    }
}
