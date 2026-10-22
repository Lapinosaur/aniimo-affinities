using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace AniimoAffinities
{
    internal static class VectorIcon
    {
        // Inputs: embedded SVG and destination. Output: vector icon. Fallback: none. Raises: malformed paths. Effects: paints only supplied graphics.
        internal static void Draw(Graphics g, int type, RectangleF box)
        {
            using (var brush = new SolidBrush(Model.Colors[type]))
                g.FillEllipse(brush, box);
            var state = g.Save();
            g.TranslateTransform(box.X + box.Width * .15f, box.Y + box.Height * .15f);
            g.ScaleTransform(box.Width * .7f / 24, box.Height * .7f / 24);
            foreach (var path in Paths[type])
                g.FillPath(Brushes.White, path);
            g.Restore(state);
        }
        private static readonly GraphicsPath[][] Paths = Load();
        // Inputs: nine embedded SVG resources. Output: reusable paths. Fallback: none. Raises: missing or unsupported resources. Effects: reads assembly resources.
        private static GraphicsPath[][] Load()
        {
            var result = new GraphicsPath[Model.Names.Length][];
            for (int i = 0; i < result.Length; i++)
            {
                var xml = new XmlDocument();
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Model.Names[i].ToLowerInvariant() + ".svg"))
                    xml.Load(stream);
                var nodes = xml.GetElementsByTagName("path");
                result[i] = new GraphicsPath[nodes.Count];
                for (int j = 0; j < nodes.Count; j++)
                    result[i][j] = Parse(nodes[j].Attributes["d"].Value);
            }
            return result;
        }
        // Inputs: SVG M/L/H/V/C/Z path. Output: even-odd geometry. Fallback: none. Raises: unsupported commands. Effects: none.
        private static GraphicsPath Parse(string data)
        {
            var tokens = Regex.Matches(data, @"[A-Za-z]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?");
            var p = new GraphicsPath(FillMode.Alternate);
            int index = 0;
            char command = ' ';
            float x = 0, y = 0, sx = 0, sy = 0;
            Func<float> number = () => float.Parse(tokens[index++].Value, CultureInfo.InvariantCulture);
            while (index < tokens.Count)
            {
                if (char.IsLetter(tokens[index].Value[0]))
                    command = tokens[index++].Value[0];
                bool relative = char.IsLower(command);
                char op = char.ToUpperInvariant(command);
                float ox = relative ? x : 0, oy = relative ? y : 0;
                if (op == 'Z')
                {
                    p.CloseFigure();
                    x = sx;
                    y = sy;
                    command = ' ';
                    continue;
                }
                if (op == 'M')
                {
                    x = number() + ox;
                    y = number() + oy;
                    sx = x;
                    sy = y;
                    p.StartFigure();
                    command = relative ? 'l' : 'L';
                }
                else if (op == 'L')
                {
                    float nx = number() + ox, ny = number() + oy;
                    p.AddLine(x, y, nx, ny);
                    x = nx;
                    y = ny;
                }
                else if (op == 'H')
                {
                    float nx = number() + ox;
                    p.AddLine(x, y, nx, y);
                    x = nx;
                }
                else if (op == 'V')
                {
                    float ny = number() + oy;
                    p.AddLine(x, y, x, ny);
                    y = ny;
                }
                else if (op == 'C')
                {
                    float ax = number() + ox, ay = number() + oy, bx = number() + ox, by = number() + oy, nx = number() + ox, ny = number() + oy;
                    p.AddBezier(x, y, ax, ay, bx, by, nx, ny);
                    x = nx;
                    y = ny;
                }
                else
                    throw new InvalidDataException("Unsupported SVG command: " + command);
            }
            return p;
        }
    }
}
