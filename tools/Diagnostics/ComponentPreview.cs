using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ComponentPreview
    {
        internal static void Save(string path)
        {
            using (var form = new Form { ClientSize = new Size(660, 420), BackColor = Theme.Background })
            {
                var plan = new TeamPlan();
                plan.Allies[0].Main = 0;
                for (int column = 0; column < 3; column++)
                {
                    int x = 16 + column * 214;
                    form.Controls.Add(new Label { Text = column == 0 ? "Available" : column == 1 ? "Selected / busy" : "Disabled", ForeColor = Theme.TextColor, Bounds = new Rectangle(x, 10, 200, 24) });
                    var button = new SettingsButton { Text = "Action", Bounds = new Rectangle(x, 42, 195, 36) };
                    var icon = new IconButton { Glyph = "help", Active = column == 1, Bounds = new Rectangle(x, 84, 44, 36), AccessibleName = "Help" };
                    var hold = new HoldSizeButton { Text = "75%", Bounds = new Rectangle(x + 56, 84, 72, 36) };
                    var card = new TeamCard(plan, 0) { Selected = column == 1, Bounds = new Rectangle(x, 130, 195, 86) };
                    var check = new RoundCheckBox { Text = "Preference", Checked = column == 1, ForeColor = Theme.TextColor, Bounds = new Rectangle(x, 228, 195, 30) };
                    var name = new NameInput("Ally", "Name", "Ally name") { Bounds = new Rectangle(x, 268, 195, 38) };
                    var capture = new KeyCapture { Binding = Keys.F8, Bounds = new Rectangle(x, 318, 195, 34) };
                    foreach (Control control in new Control[] { button, icon, hold, card, check, name, capture })
                    {
                        control.Enabled = column != 2;
                        form.Controls.Add(control);
                    }
                    if (column == 1)
                        button.BeginBusy("Working…");
                }
                form.Show();
                Application.DoEvents();
                using (var bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                    bitmap.Save(path);
                }
            }
        }
    }
}
