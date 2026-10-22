using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TeamEditor : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        private readonly TeamPlan plan;
        private readonly Action changed;
        private readonly Panel rows = new Panel();
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly Label status = new Label();
        private readonly SettingsButton mine, opponents, matchups;
        private readonly SettingsButton clear;
        private bool confirmClear;
        private readonly Label heading;
        private bool enemy;
        private readonly Timer saveTimer = new Timer { Interval = 400 };
        internal TeamEditor(TeamPlan value, Action onChanged)
        {
            plan = value;
            changed = onChanged;
            saveTimer.Tick += (s, e) => FlushSave();
            FormClosing += (s, e) => { if (saveTimer.Enabled) FlushSave(); };
            Disposed += (s, e) => { saveTimer.Dispose(); tips.Dispose(); };
            Text = "Team planner";
            ClientSize = new Size(670, 462);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            mine = new SettingsButton { Text = "Your team", Primary = true, Location = new Point(18, 44), Size = new Size(211, 32) };
            opponents = new SettingsButton { Text = "Known opponents", Location = new Point(229, 44), Size = new Size(211, 32) };
            matchups = new SettingsButton { Text = "Matchups", Location = new Point(440, 44), Size = new Size(211, 32) };
            SettingsButton.Group(mine, opponents, matchups);
            mine.Click += (s, e) => { enemy = false; BuildRows(); };
            opponents.Click += (s, e) => { enemy = true; BuildRows(); };
            matchups.Click += (s, e) => ShowMatchups();
            Controls.Add(mine);
            Controls.Add(opponents);
            Controls.Add(matchups);
            heading = new Label { Location = new Point(32, 82), Size = new Size(620, 20) };
            Controls.Add(heading);
            rows.SetBounds(18, 104, 634, 310);
            Controls.Add(rows);
            const string guidance = "Leave unknown slots empty. Results use only known elements.\nType matchups only: no stats, moves or team synergy are assumed.";
            var info = new SettingsButton { Glyph = "info", AccessibleName = "Team planning information", AccessibleDescription = guidance, Location = new Point(22, 414), Size = new Size(30, 30) };
            tips.SetToolTip(info, guidance);
            Controls.Add(info);
            status.SetBounds(62, 414, 350, 30);
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.Text = "Saved automatically";
            status.ForeColor = Theme.Muted;
            Controls.Add(status);
            clear = new SettingsButton { Text = "Clear all", Location = new Point(420, 414), Size = new Size(110, 32) };
            tips.SetToolTip(clear, "Clear all six names and elements in this tab. Click again to confirm.");
            clear.Click += (s, e) =>
            {
                if (!confirmClear)
                {
                    confirmClear = true;
                    clear.Text = "Confirm clear";
                    clear.Invalidate();
                    return;
                }
                foreach (var member in enemy ? plan.Opponents : plan.Allies)
                {
                    member.Name = "";
                    member.Main = member.Sub = -1;
                }
                BuildRows();
                Save();
            };
            Controls.Add(clear);
            var done = new SettingsButton { Text = "Done", Primary = true, Location = new Point(540, 414), Size = new Size(110, 32) };
            done.Click += (s, e) => Close();
            Controls.Add(done);
            BuildRows();
        }
        private void BuildRows()
        {
            confirmClear = false;
            clear.Text = "Clear all";
            clear.Visible = true;
            foreach (Control old in rows.Controls.Cast<Control>().ToArray())
                old.Dispose();
            mine.Primary = !enemy;
            opponents.Primary = enemy;
            matchups.Primary = false;
            heading.Text = "Name (optional)                    Elements · select up to two";
            mine.Invalidate();
            opponents.Invalidate();
            matchups.Invalidate();
            var members = enemy ? plan.Opponents : plan.Allies;
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                int top = i * 46;
                rows.Controls.Add(new Label { Text = (i + 1).ToString(), Location = new Point(0, top + 9), Size = new Size(20, 24), ForeColor = Theme.Muted });
                var name = new NameInput(member.Name, (enemy ? "Opponent " : "Ally ") + (i + 1), "Slot " + (i + 1) + " name") { Location = new Point(24, top + 2), TabIndex = i * 2 };
                var elements = new TeamTypeSelector(member, Save) { Location = new Point(170, top), AccessibleName = "Slot " + (i + 1) + " elements", TabIndex = i * 2 + 1 };
                int slot = i;
                elements.FilterFocusChanged += active => ShowRowShortcuts(slot, active, false);
                name.Editor.GotFocus += (s, e) => ShowRowShortcuts(slot, true, true);
                name.Editor.AccessibleDescription = "Optional name. Tab moves to the next name. F6 enters this row's elements; use arrows and Space there. Ctrl+1 through Ctrl+9 selects an element; Ctrl+0 clears.";
                name.Editor.LostFocus += (s, e) => ShowRowShortcuts(slot, false, true);
                name.Editor.TextChanged += (s, e) => { member.Name = name.Editor.Text; Save(); };
                rows.Controls.Add(name);
                rows.Controls.Add(elements);
            }
        }
        private void ShowRowShortcuts(int slot, bool active, bool nameFocused)
        {
            var inputs = rows.Controls.OfType<NameInput>().OrderBy(n => n.TabIndex).ToArray();
            var filters = rows.Controls.OfType<TeamTypeSelector>().OrderBy(n => n.TabIndex).ToArray();
            for (int n = 0; n < inputs.Length; n++)
            {
                inputs[n].NavigationHint = active && n == slot - (nameFocused ? 1 : 0) ? "Shift+Tab" : active && n == slot + 1 ? "Tab" : "";
                inputs[n].Invalidate();
            }
            for (int n = 0; n < filters.Length; n++)
                filters[n].ShowShortcuts(active && n == slot);
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            var name = rows.Controls.OfType<NameInput>().FirstOrDefault(n => n.ContainsFocus);
            if (name != null)
            {
                var filter = rows.Controls.OfType<TeamTypeSelector>().FirstOrDefault(n => n.TabIndex == name.TabIndex + 1);
                if (filter != null && keys == Keys.F6)
                {
                    filter.FocusElements();
                    return true;
                }
                if (filter != null && filter.HandleShortcut(keys, (message.LParam.ToInt64() & (1L << 30)) != 0, true))
                    return true;
            }
            return base.ProcessCmdKey(ref message, keys);
        }
        private void ShowMatchups()
        {
            confirmClear = false;
            clear.Visible = false;
            foreach (Control old in rows.Controls.Cast<Control>().ToArray())
                old.Dispose();
            mine.Primary = opponents.Primary = false;
            matchups.Primary = true;
            mine.Invalidate();
            opponents.Invalidate();
            matchups.Invalidate();
            heading.Text = "Teal: advantage · Purple: weakness · Top: dealt / Bottom: received";
            var table = new TeamMatchupPanel(plan) { Dock = DockStyle.Fill };
            table.Configure();
            rows.Controls.Add(table);
        }
        private void Save()
        {
            saveTimer.Stop();
            saveTimer.Start();
            status.Text = "Saving…";
        }
        private void FlushSave()
        {
            saveTimer.Stop();
            try
            {
                changed();
                status.Text = "Saved automatically";
                status.ForeColor = Theme.Muted;
            }
            catch (System.IO.IOException) { status.Text = "Could not save. Your edits remain in this session."; status.ForeColor = Theme.Error; }
            catch (UnauthorizedAccessException) { status.Text = "Could not save. Check access to local settings."; status.ForeColor = Theme.Error; }
        }
    }
}
