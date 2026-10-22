using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class MatchupsTests
    {
        internal static void Run()
        {
            int grass = Array.IndexOf(Model.Names, "Grass"), water = Array.IndexOf(Model.Names, "Water");
            if (Math.Abs(Model.Factors(grass, water)[9 + water] - .390625) > 1e-8)
                throw new Exception("Grass Water regression");
            using (var image = new Bitmap(64, 64))
            using (var g = Graphics.FromImage(image))
                for (int type = 0; type < 9; type++)
                    VectorIcon.Draw(g, type, new RectangleF(8, 8, 48, 48));
            int ice = Array.IndexOf(Model.Names, "Ice"), fire = Array.IndexOf(Model.Names, "Fire"), wind = Array.IndexOf(Model.Names, "Wind"), dark = Array.IndexOf(Model.Names, "Dark");
            var origins = OriginGroups.Build(grass, ice, 1, new[] { dark, fire, wind });
            if (origins.Single(v => v.Type == fire).Mask != 3 || origins.Single(v => v.Type == wind).Mask != 1 || origins.Single(v => v.Type == dark).Mask != 1)
                throw new Exception("Grass Ice source contours");
            for (int a = 0; a < 9; a++)
                for (int b = -1; b < 9; b++)
                    for (int column = 0; column < 2; column++)
                    {
                        var factors = Model.Factors(a, b);
                        var targets = Enumerable.Range(0, 9).Where(t => factors[column * 9 + t] != 1).ToArray();
                        var groups = OriginGroups.Build(a, b, column, targets);
                        if (groups.Length != targets.Length || groups.Select(v => v.Type).Distinct().Count() != targets.Length || groups.Any(v => v.Mask == 0))
                            throw new Exception("Origin coverage");
                    }
            if (Math.Abs(Model.Factors(grass, ice)[water] - 1.6) > 1e-8)
                throw new Exception("Best available attack");
            if (Math.Abs(Model.Factors(grass, ice)[9 + fire] - 2.56) > 1e-8)
                throw new Exception("Defense double weakness");
            if (Math.Abs(Model.Factors(grass, ice)[9 + water] - .390625) > 1e-8)
                throw new Exception("Defense double resistance");
            for (int a = 0; a < 9; a++)
                for (int b = 0; b < 9; b++)
                {
                    if (a == b)
                        continue;
                    for (int i = 0; i < 9; i++)
                    {
                        if (Math.Abs(Model.Factors(a, b)[i] - Math.Max(Model.Factors(a, -1)[i], Model.Factors(b, -1)[i])) > 1e-8)
                            throw new Exception("Attack parity");
                        if (Math.Abs(Model.Factors(a, b)[9 + i] - Model.Factors(a, -1)[9 + i] * Model.Factors(b, -1)[9 + i]) > 1e-8)
                            throw new Exception("Defense parity");
                    }
                }
            var selection = new Selection();
            selection.Toggle(grass);
            selection.Toggle(water);
            var temporary = selection.Preview(ice);
            if (temporary.Main != grass || temporary.Sub != ice || selection.Sub != water)
                throw new Exception("Hover must not commit");
            if (selection.Preview(-1).Sub != water || selection.Preview(grass).Sub != water)
                throw new Exception("Hover restore");
            if (new Selection().Preview(ice).Main != ice)
                throw new Exception("Empty hover");
            if (selection.Main != grass || selection.Sub != water)
                throw new Exception("Main/Sub selection");
            selection.Toggle(grass);
            if (selection.Main != water || selection.Sub != -1)
                throw new Exception("Promotion");
            selection.Toggle(water);
            if (selection.Main != -1)
                throw new Exception("Deselection");
            for (int a = 0; a < 9; a++)
                for (int b = 0; b < 9; b++)
                    for (int c = 0; c < 9; c++)
                    {
                        selection.Clear();
                        selection.Toggle(a);
                        selection.Toggle(b);
                        selection.Toggle(c);
                        if (selection.Main < 0 && selection.Sub >= 0 || selection.Main >= 0 && selection.Main == selection.Sub)
                            throw new Exception("Selection invariant");
                    }
            if (typeof(Program).Assembly.GetType("AniimoAffinities.CaptureSelection") != null || typeof(Program).Assembly.GetType("AniimoAffinities.Recognition") != null)
                throw new Exception("Capture must not ship");
            using (var form = new MainForm())
            {
                form.Show();
                Application.DoEvents();
                using (var image = new Bitmap(form.Width, form.Height))
                    form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
            }
        }
    }
}
