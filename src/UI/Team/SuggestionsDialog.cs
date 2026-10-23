using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SuggestionsDialog : PortableWindow
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
                return "suggest";
            }
        }
        private readonly TeamPlan plan;
        private readonly Func<int, TeamMember, bool> apply;
        private readonly TeamCard[] tabs = new TeamCard[6];
        private readonly Panel tabArea = new Panel();
        private readonly Panel scroll = new SlimScrollPanel();
        private readonly DrawingPanel detail = new DrawingPanel();
        private readonly SettingsButton applyButton = new SettingsButton { Text = "Apply suggestion", Primary = true };
        private TeamSuggestions.Recommendation[] suggestions;
        internal int ActiveSlot
        {
            get; private set;
        }
        internal SuggestionsDialog(TeamPlan value, int active, Func<int, TeamMember, bool> onApply)
        {
            plan = value;
            apply = onApply;
            ActiveSlot = active;
            Text = "Team suggestions";
            ClientSize = new Size(470, Math.Min(740, Screen.PrimaryScreen.WorkingArea.Height - 60));
            MinimumSize = new Size(420, 460);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            for (int i = 0; i < tabs.Length; i++)
            {
                int slot = i;
                tabs[i] = new TeamCard(plan, i) { TabIndex = i };
                tabs[i].CompareCheck.Visible = tabs[i].SuggestButton.Visible = tabs[i].PreviewButton.Visible = false;
                tabs[i].Click += (s, e) => SelectSlot(slot);
                tabArea.Controls.Add(tabs[i]);
            }
            detail.Paint += (s, e) =>
            {
                e.Graphics.Clear(Theme.Surface);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (ActiveSlot >= 0 && suggestions[ActiveSlot] != null)
                    SuggestionGraphic.Draw(e.Graphics, new RectangleF(12, 8, 350, detail.Height - 16), suggestions[ActiveSlot], "Apply only this slot; remaining proposals refresh");
            };
            scroll.Controls.Add(detail);
            Controls.AddRange(new Control[] { tabArea, scroll, applyButton });
            applyButton.Click += (s, e) =>
            {
                if (ActiveSlot >= 0 && suggestions[ActiveSlot] != null && apply(ActiveSlot, suggestions[ActiveSlot].Types))
                    RefreshSuggestions();
            };
            Resize += (s, e) => ArrangeContent();
            Theme.Changed += RefreshTheme;
            Disposed += (s, e) => Theme.Changed -= RefreshTheme;
            RefreshTheme();
            RefreshSuggestions();
        }
        private void RefreshTheme()
        {
            BackColor = tabArea.BackColor = Theme.Background;
            scroll.BackColor = detail.BackColor = Theme.Surface;
            Invalidate(true);
        }
        internal void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= suggestions.Length || suggestions[slot] == null)
                return;
            ActiveSlot = slot;
            for (int i = 0; i < tabs.Length; i++)
                tabs[i].Selected = i == slot;
            detail.Height = SuggestionGraphic.Size(suggestions[slot]).Height + 16;
            detail.AccessibleText = () => TeamTypeSelector.RecommendationTip(suggestions[ActiveSlot]);
            scroll.AutoScrollPosition = Point.Empty;
            detail.Invalidate();
        }
        private void RefreshSuggestions()
        {
            suggestions = TeamSuggestions.ComplementaryAdditions(plan);
            if (ActiveSlot < 0 || suggestions[ActiveSlot] == null)
                ActiveSlot = Array.FindIndex(suggestions, item => item != null);
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Suggestion = suggestions[i];
                tabs[i].Visible = suggestions[i] != null;
                tabs[i].Invalidate();
            }
            applyButton.Enabled = ActiveSlot >= 0;
            applyButton.Text = ActiveSlot < 0 ? "No empty slots" : "Apply suggestion";
            detail.Visible = ActiveSlot >= 0;
            if (ActiveSlot >= 0)
                SelectSlot(ActiveSlot);
            ArrangeContent();
        }
        private void ArrangeContent()
        {
            if (suggestions == null)
                return;
            int count = 0, width = (ClientSize.Width - 32) / 3;
            tabArea.SetBounds(12, 42, ClientSize.Width - 24, 190);
            for (int i = 0; i < tabs.Length; i++)
                if (suggestions[i] != null)
                {
                    tabs[i].SetBounds(count % 3 * (width + 4), count / 3 * 91, width, 87);
                    count++;
                }
            int top = 48 + ((count + 2) / 3) * 91;
            tabArea.Height = ((count + 2) / 3) * 91;
            scroll.SetBounds(12, top, ClientSize.Width - 24, Math.Max(60, ClientSize.Height - top - 52));
            detail.Width = 376;
            detail.Left = Math.Max(0, (scroll.ClientSize.Width - 394) / 2);
            applyButton.SetBounds(ClientSize.Width - 184, ClientSize.Height - 42, 170, 32);
        }
    }
}
