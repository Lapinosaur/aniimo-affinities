using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TeamTypeSelector : Panel
    {
        private readonly TypeSelector selector = new TypeSelector();
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly DrawingPanel modifierHint = new DrawingPanel { PhysicalPainting = true, TabStop = false, Visible = false, AccessibleName = "Ctrl modifier" };
        private readonly TeamMember member;
        private readonly Action changed;
        internal event Action<bool> FilterFocusChanged;
        private TeamPlan suggestionPlan;
        private bool suggestionEnemy;
        private readonly SettingsButton applySuggestion = new SettingsButton { Text = "+", Visible = false, TabStop = false, AccessibleName = "Apply suggested elements", Location = new Point(426, 3), Size = new Size(32, 32) };
        private TeamSuggestions.Recommendation recommendation;
        private TeamSuggestions.Recommendation[] plannedAdditions;
        internal void SetSuggestions(TeamPlan plan, bool enemy = false, TeamSuggestions.Recommendation[] additions = null)
        {
            suggestionPlan = plan;
            suggestionEnemy = enemy;
            plannedAdditions = additions;
            RefreshSelection();
        }
        internal TeamTypeSelector(TeamMember value, Action changed)
        {
            member = value;
            this.changed = changed;
            TabStop = false;
            Enter += (s, e) => SetFilterFocus(true);
            Leave += (s, e) => SetFilterFocus(false);
            Size = new Size(464, 40);
            selector.Location = new Point(22, -9);
            selector.Height = 50;
            Controls.Add(selector);
            Controls.Add(applySuggestion);
            applySuggestion.Click += (s, e) => ApplySuggestion();
            modifierHint.SetBounds(0, 0, 22, 18);
            modifierHint.Paint += (s, e) =>
            {
                e.Graphics.Clear(Theme.Background);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var bounds = new RectangleF(0, 3, 21, 11);
                Drawing.Box(e.Graphics, bounds, Theme.Border, Theme.Border, 3);
                using (var font = UiFonts.Create("Segoe UI", 6.5f, FontStyle.Bold))
                {
                    Drawing.Text(e.Graphics, "Ctrl", font, Theme.TextColor, bounds, true);
                }
            };
            UiFonts.Assign(modifierHint, UiFonts.Create("Segoe UI", 7));
            Controls.Add(modifierHint);
            modifierHint.BringToFront();
            tips.SetToolTip(modifierHint, "Hold Ctrl and press a number to select an element. Ctrl+0 clears the row.");
            string[] order = { "Dark", "Earth", "Fire", "Grass", "Ice", "Light", "Lightning", "Water", "Wind" };
            for (int i = 0; i < order.Length; i++)
            {
                int type = Array.IndexOf(Model.Names, order[i]);
                var button = new IconButton { Type = type, TabIndex = i, TabStop = false, AccessibleName = order[i] };
                button.Click += (s, e) =>
                {
                    var selection = new Selection { Main = member.Main, Sub = member.Sub };
                    selection.Toggle(type);
                    member.Main = selection.Main;
                    member.Sub = selection.Sub;
                    SelectionChanged();
                };
                selector.Controls.Add(button);
            }
            var reset = new IconButton { Glyph = "reset", TabIndex = 9, TabStop = false, AccessibleName = "Clear elements" };
            reset.Click += (s, e) => ClearSelection();
            tips.SetToolTip(reset, "Clear elements (keep name)");
            selector.Controls.Add(reset);
            RefreshSelection();
        }
        private void SetFilterFocus(bool active)
        {
            ShowShortcuts(active);
            if (FilterFocusChanged != null)
                FilterFocusChanged(active);
        }
        private void SelectionChanged()
        {
            RefreshSelection();
            changed();
        }
        private void ClearSelection()
        {
            member.Main = member.Sub = -1;
            SelectionChanged();
        }
        private void ApplySuggestion()
        {
            if (member.Known || recommendation == null)
                return;
            member.Main = recommendation.Types.Main;
            member.Sub = recommendation.Types.Sub;
            SelectionChanged();
        }
        internal void FocusElements()
        {
            var first = selector.Controls.OfType<IconButton>().OrderBy(button => button.TabIndex).FirstOrDefault(button => button.Enabled && button.Visible);
            if (Enabled && first != null)
                first.Focus();
        }
        internal void ShowShortcuts(bool active)
        {
            modifierHint.Visible = active && DisplaySettings.ShowBadges;
            foreach (var button in selector.Controls.OfType<IconButton>())
            {
                button.Shortcut = active ? (button.Type < 0 ? 0 : button.TabIndex + 1).ToString() : "";
                button.Invalidate();
            }
        }
        internal bool HandleShortcut(Keys keys, bool repeated, bool nameFocused = false)
        {
            if (!Enabled || (!ContainsFocus && !nameFocused) || (keys & Keys.Modifiers) != Keys.Control)
                return false;
            if (keys == (Keys.Control | Keys.Enter))
            {
                if (recommendation == null)
                    return false;
                if (!repeated)
                    ApplySuggestion();
                return true;
            }
            int code = (int)KeyBindings.NormalizeNumber(keys & Keys.KeyCode);
            int digit = code >= (int)Keys.D0 && code <= (int)Keys.D9 ? code - (int)Keys.D0 : -1;
            if (digit < 0)
                return false;
            if (repeated)
                return true;
            if (digit == 0)
            {
                ClearSelection();
            }
            else
                selector.Controls.OfType<IconButton>().Single(b => b.Type >= 0 && b.TabIndex == digit - 1).PerformClick();
            return true;
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (HandleShortcut(keys, (message.LParam.ToInt64() & (1L << 30)) != 0))
                return true;
            if (ContainsFocus && (keys == Keys.Left || keys == Keys.Right))
            {
                var buttons = selector.Controls.OfType<IconButton>().Where(b => b.Visible && b.Enabled).OrderBy(b => b.TabIndex).ToArray();
                if (buttons.Length == 0)
                    return true;
                int index = Array.FindIndex(buttons, b => b.Focused);
                buttons[(index + (keys == Keys.Right ? 1 : buttons.Length - 1)) % buttons.Length].Focus();
                return true;
            }
            if (ContainsFocus && (keys == Keys.Tab || keys == (Keys.Shift | Keys.Tab)))
            {
                // Leave the whole element group, even after moving focus with arrows.
                bool forward = keys == Keys.Tab;
                var inputs = Parent.Controls.OfType<NameInput>();
                var target = forward ? inputs.Where(n => n.TabIndex > TabIndex).OrderBy(n => n.TabIndex).FirstOrDefault() : inputs.Where(n => n.TabIndex < TabIndex).OrderByDescending(n => n.TabIndex).FirstOrDefault();
                if (target != null)
                    target.Editor.Focus();
                else
                    FindForm().SelectNextControl(Parent, forward, true, false, true);
                return true;
            }
            return base.ProcessCmdKey(ref message, keys);
        }
        private void RefreshSelection()
        {
            var factors = member.Known ? Model.Factors(member.Main, member.Sub) : null;
            recommendation = null;
            if (suggestionPlan != null && !member.Known)
            {
                var additions = plannedAdditions ?? TeamSuggestions.ComplementaryAdditions(suggestionPlan, suggestionEnemy);
                int slot = Array.IndexOf(suggestionEnemy ? suggestionPlan.Opponents : suggestionPlan.Allies, member);
                if (slot >= 0)
                    recommendation = additions[slot];
            }
            var gains = suggestionPlan == null || !member.Known ? new int[Model.Names.Length] : TeamSuggestions.Gains(suggestionPlan, member, suggestionEnemy);
            int best = gains.Max();
            string combination = recommendation == null ? "" : Model.Names[recommendation.Types.Main] + (recommendation.Types.Sub < 0 ? "" : " + " + Model.Names[recommendation.Types.Sub]);
            string advice = recommendation == null ? "" : "Suggested " + (recommendation.Types.Sub < 0 ? "mono" : "dual") + ": " + combination + " · adds " + recommendation.Gain + " coverage points after the suggestions above. Apply with + or Ctrl+Enter. Other suggestions update after each selection. Names are kept.";
            applySuggestion.Visible = recommendation != null;
            applySuggestion.AccessibleDescription = advice;
            string visualAdvice = RecommendationTip(recommendation);
            SuggestionGraphic.Attach(tips, applySuggestion, recommendation, "Apply with + or Ctrl+Enter");
            foreach (var button in selector.Controls.OfType<IconButton>().Where(b => b.Type >= 0))
            {
                button.Active = button.Type == member.Main || button.Type == member.Sub;
                button.Badge = button.Type == member.Main ? "Main" : button.Type == member.Sub ? "Sub" : "";
                button.IncomingFactor = factors == null ? double.NaN : factors[9 + button.Type];
                button.Suggested = recommendation != null ? button.Type == recommendation.Types.Main || button.Type == recommendation.Types.Sub : best > 0 && gains[button.Type] == best;
                string matchup = factors == null ? "" : " · Incoming " + Model.Factor(button.IncomingFactor) + (button.IncomingFactor > 1.0001 ? " · Weakness" : button.IncomingFactor < .9999 ? " · Resistance" : " · Neutral");
                string suggestion = button.Suggested ? " · " + (recommendation != null ? advice : "Suggested: improves team coverage by " + best + " points (2 per strong answer, 1 per resistance)") : "";
                button.AccessibleDescription = (button.Badge.Length == 0 ? "Not selected" : button.Badge + " element") + matchup + suggestion;
                string visualSuggestion = !button.Suggested ? "" : recommendation != null ? "\n\n" + visualAdvice : "\nSuggested: +" + best + " coverage points";
                tips.SetToolTip(button, "[types:" + button.Type + ",-1]" + (button.Badge.Length > 0 ? button.Badge : "Element") + "\n" + (matchup.Length > 0 ? matchup.Substring(3) + "\n" : "") + (button.Badge.Length > 0 ? "Click to remove" : "Click to select") + visualSuggestion);
                button.Invalidate();
                if (button.Suggested && recommendation != null)
                    SuggestionGraphic.Attach(tips, button, recommendation, "Apply with + or Ctrl+Enter");
            }
            selector.Arrange(member.Known);
        }
        internal static string RecommendationTip(TeamSuggestions.Recommendation value, string action = "Apply with + or Ctrl+Enter")
        {
            if (value == null)
                return "";
            return "[types:" + value.Types.Main + "," + value.Types.Sub + "]" + SuggestionGraphic.Title(value)
                + (value.Gain > 0 ? "\n+" + value.Gain + " coverage points" : "\nReinforces coverage with backup answers")
                + "\nProjected team: " + value.AppliedAllies + " applied allies + " + value.EarlierProposals.Length + " earlier unapplied suggestions"
                + "\n\n" + action
                + "\nOnly this slot is applied; other suggestions recalculate.";
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
