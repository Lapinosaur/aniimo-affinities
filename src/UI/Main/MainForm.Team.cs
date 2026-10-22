using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class MainForm
    {
        private TeamPlan teamPlan;
        private PanelTransition teamTransition;
        private int teamHovered = -1;
        private int teamPreview = -1;
        private int teamCounter = -1, teamOpponentMask;
        private TeamBoard teamBoard;
        private Panel teamBody;
        private Panel teamToolbar;
        private DrawingPanel affinityHeader;
        private SettingsButton teamToggle, teamEdit;
        private Size teamExtra;
        private float teamLayoutScale = 1;
        private readonly SettingsButton[] teamPositions = new SettingsButton[4];
        protected override Action LockedPanelToggle
        {
            get
            {
                return () => { teamPlan.Visible = !teamPlan.Visible; ApplyTeamLayout(); SaveTeam(); };
            }
        }
        protected override string LockedPanelGlyph
        {
            get
            {
                return "team-" + teamPlan.Position;
            }
        }
        private void InitializeTeam(bool remember)
        {
            teamPlan = remember ? TeamPreferences.Load(TeamPreferences.FilePath) : new TeamPlan();
            affinityHeader = new DrawingPanel { Dock = DockStyle.Top, Height = 66 };
            affinityHeader.Paint += PaintAffinityHeader;
            canvas.Invalidated += (s, e) => affinityHeader.Invalidate();
            EnableWindowDrag(affinityHeader);
            teamBody = new Panel { Dock = DockStyle.Fill };
            Controls.Remove(canvas);
            teamBody.Controls.Add(canvas);
            teamBoard = new TeamBoard(teamPlan, index =>
            {
                var member = teamPlan.Allies[index];
                if (!member.Known)
                {
                    EditTeam();
                    return;
                }
                hovered = -1;
                teamPreview = -1;
                selection.Main = member.Main;
                selection.Sub = member.Sub;
                teamBoard.SelectCard(index);
                RefreshSelection();
            }, type => { teamHovered = type; canvas.Invalidate(); }, (counter, mask) => { teamCounter = counter; teamOpponentMask = mask; canvas.Invalidate(); }, index =>
            {
                teamPreview = index >= 0 && teamPlan.Allies[index].Known ? index : -1;
                hovered = -1;
                canvas.Invalidate();
            });
            Deactivate += (s, e) => { teamPreview = -1; teamHovered = -1; teamCounter = -1; teamOpponentMask = 0; canvas.Invalidate(); };
            teamBody.Controls.Add(teamBoard);
            teamTransition = new PanelTransition(this, teamBody, teamBoard, canvas);
            Disposed += (s, e) => teamTransition.Dispose();
            ResizeBegin += (s, e) => FinishTeamTransition();
            SupportBoundsChanged += bounds => teamBoard.AvoidSupportBubble(bounds);
            SupportPlacement = bounds =>
            {
                if (teamBoard.Visible && !teamBoard.CanAvoidSupportBubble(bounds))
                {
                    var boardBounds = teamBoard.RectangleToScreen(teamBoard.ClientRectangle);
                    var area = Screen.FromControl(this).WorkingArea;
                    // Keep the six cards intact when there is no readable compact layout.
                    if (Right + bounds.Width + 6 <= area.Right)
                        bounds.X = Right + 6;
                    else if (Left - bounds.Width - 6 >= area.Left)
                        bounds.X = Left - bounds.Width - 6;
                    else
                        bounds.Y = Math.Min(area.Bottom - bounds.Height, boardBounds.Bottom + 6);
                }
                return bounds;
            };
            Controls.Add(teamBody);
            teamBody.SendToBack();
            var bar = new Panel { Size = new Size(204, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            teamToolbar = bar;
            teamEdit = new SettingsButton { Glyph = "team", TabIndex = 5, Location = new Point(170, 0), Size = new Size(34, 30), AccessibleName = "Edit your team and known opponents" };
            teamEdit.Click += (s, e) => EditTeam();
            tips.SetToolTip(teamEdit, "Open team editor");
            teamToggle = new SettingsButton { Glyph = "separate", TabIndex = 4, AccessibleName = "Show team panel", Location = new Point(136, 0), Size = new Size(34, 30) };
            teamToggle.Click += (s, e) => { teamPlan.Visible = !teamPlan.Visible; ApplyTeamLayout(); SaveTeam(); };
            string[] positions = { "Top", "Bottom", "Left", "Right" }, symbols = { "↑", "↓", "←", "→" };
            for (int i = 0; i < positions.Length; i++)
            {
                string destination = positions[i];
                var button = new SettingsButton { Text = symbols[i], TabIndex = 3 - i, Tag = destination, AccessibleName = "Team panel: " + destination, Location = new Point((3 - i) * 34, 0), Size = new Size(34, 30) };
                button.Click += (s, e) => { teamPlan.Position = destination; ApplyTeamLayout(); SaveTeam(); };
                tips.SetToolTip(button, "Place team panel: " + destination);
                teamPositions[i] = button;
                bar.Controls.Add(button);
            }
            teamEdit.Segment = teamToggle.Segment = true;
            for (int i = 0; i < teamPositions.Length; i++)
            {
                teamPositions[i].Segment = true;
                teamPositions[i].GroupShape = new RectangleF(-34 * (3 - i), 0, 203, 29);
            }
            bar.Controls.Add(teamEdit);
            bar.Controls.Add(teamToggle);
            affinityHeader.Controls.Add(bar);
            Action arrangeBar = () => bar.SetBounds(Math.Max(0, affinityHeader.Width - (int)(216 * AppliedWindowScale)), (int)(14 * AppliedWindowScale), (int)(204 * AppliedWindowScale), (int)(32 * AppliedWindowScale));
            affinityHeader.Resize += (s, e) => arrangeBar();
            arrangeBar();
            bar.BringToFront();
            Controls.Add(affinityHeader);
            Controls.SetChildIndex(teamBody, 0);
            Controls.SetChildIndex(affinityHeader, 1);
            ApplyTeamLayout();
        }
        private void EditTeam()
        {
            if (!teamPlan.Visible)
            {
                teamPlan.Visible = true;
                ApplyTeamLayout();
            }
            using (var editor = new TeamEditor(teamPlan, () => { teamBoard.RefreshCards(); TeamPreferences.Save(TeamPreferences.FilePath, teamPlan); }))
                editor.ShowDialog(this);
            SaveTeam();
        }
        private void SaveTeam()
        {
            try
            {
                TeamPreferences.Save(TeamPreferences.FilePath, teamPlan);
            }
            catch (System.IO.IOException) { MessageBox.Show(this, "Unable to save team settings. Your edits remain in this session.", "Team settings"); }
            catch (UnauthorizedAccessException) { MessageBox.Show(this, "Unable to save team settings. Check access to local settings.", "Team settings"); }
        }
        internal void FinishTeamTransition()
        {
            if (teamTransition != null)
            {
                teamTransition.Finish();
                // A pending last request may have started one final transition.
                teamTransition.Finish();
            }
        }
        private void ApplyTeamLayout()
        {
            if (teamTransition.Defer(ApplyTeamLayout))
                return;
            using (var update = new VisualUpdate(this))
            {
                var transition = teamTransition.Capture(true);
                teamTransition.Finish();
                int anchoredRight = Right, anchoredTop = Top;
                teamPreview = -1;
                bool side = teamPlan.Position == "Left" || teamPlan.Position == "Right";
                float scale = AppliedWindowScale;
                var extra = !teamPlan.Visible ? Size.Empty : new Size(side ? (int)(216 * scale) : 0, side ? 0 : (int)(104 * scale));
                teamBody.SuspendLayout();
                teamBoard.Visible = teamPlan.Visible;
                teamBoard.Dock = (DockStyle)Enum.Parse(typeof(DockStyle), teamPlan.Position);
                teamBoard.Width = (int)(216 * scale);
                teamBoard.Height = (int)(104 * scale);
                float ratio = scale / teamLayoutScale;
                var baseSize = new Size(Width - (int)Math.Round(teamExtra.Width * ratio), Height - (int)Math.Round(teamExtra.Height * ratio));
                var nextSize = new Size(baseSize.Width + extra.Width, baseSize.Height + extra.Height);
                if (Visible)
                {
                    // Keep the controls under the pointer. When expansion meets an edge,
                    // use the available content space instead of moving the right/top anchor.
                    var work = Screen.FromControl(this).WorkingArea;
                    nextSize.Width = Math.Max(MinimumSize.Width, Math.Min(nextSize.Width, anchoredRight - work.Left));
                    nextSize.Height = Math.Max(MinimumSize.Height, Math.Min(nextSize.Height, work.Bottom - anchoredTop));

                }
                else
                    Size = nextSize;
                var targetWindow = new Rectangle(anchoredRight - nextSize.Width, anchoredTop, nextSize.Width, nextSize.Height);
                // Measure only the body at its future size. Do not resize the native window
                // to the destination and then rewind it before the first animation frame.
                teamBody.Dock = DockStyle.None;
                teamBody.Size = new Size(teamBody.Width + nextSize.Width - Width, teamBody.Height + nextSize.Height - Height);
                teamExtra = new Size(nextSize.Width - baseSize.Width, nextSize.Height - baseSize.Height);
                teamLayoutScale = scale;
                teamToggle.AccessibleName = teamPlan.Visible ? "Hide team panel" : "Show team panel";
                teamToggle.Glyph = "team-" + teamPlan.Position;
                tips.SetToolTip(teamToggle, teamToggle.AccessibleName);
                teamEdit.GroupShape = new RectangleF(teamPlan.Visible ? -170 : -34, 0, teamPlan.Visible ? 203 : 67, 29);
                teamToggle.GroupShape = new RectangleF(teamPlan.Visible ? -136 : 0, 0, teamPlan.Visible ? 203 : 67, 29);
                teamEdit.Invalidate();
                teamToggle.Invalidate();
                teamToggle.Primary = teamPlan.Visible;
                foreach (var button in teamPositions)
                {
                    button.Visible = teamPlan.Visible;
                    button.Primary = (string)button.Tag == teamPlan.Position;
                    button.Invalidate();
                }
                teamBoard.RefreshCards();
                teamBody.ResumeLayout(true);
                canvas.Invalidate();
                update.LayoutNow();
                teamTransition.Start(transition, targetWindow);
            }
        }
    }
}
