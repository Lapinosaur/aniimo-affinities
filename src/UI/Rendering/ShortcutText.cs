using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal static class ShortcutText
    {
        private static readonly Regex tokens = new Regex(@"\[key:[^\]]+\]|\b(?:Ctrl|Alt|Shift|Esc|Escape|Delete|Del|Space|Tab|F\d{1,2}|Enter(?! only| names))\b|\r?\n|[^\s+]+|\+", RegexOptions.Compiled);
        private static readonly Regex key = new Regex(@"^(Ctrl|Alt|Shift|Esc|Escape|Delete|Del|Space|Tab|Enter|F\d{1,2})$", RegexOptions.Compiled);
        internal static string Mark(string value)
        {
            return "[key:" + value + "]";
        }
        internal static int Layout(Graphics graphics, string text, Font font, Color ink, RectangleF area, bool paint)
        {
            float x = 0, y = 0, line = (float)Math.Ceiling(font.GetHeight(graphics)) + 6;
            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            using (var brush = new SolidBrush(ink))
            {
                float space = Math.Max(2, graphics.MeasureString("a a", font, 100, format).Width - graphics.MeasureString("aa", font, 100, format).Width);
                string content = Regex.Replace(text ?? "", @"(?<![\w:])(?:Ctrl|Alt|Shift)(?:\+[A-Za-z0-9]+)+", match => string.Join(" + ", Array.ConvertAll(match.Value.Split('+'), Mark)));
                foreach (Match match in tokens.Matches(content))
                {
                    string value = match.Value;
                    if (value.Contains("\n"))
                    {
                        x = 0;
                        y += line;
                        continue;
                    }
                    bool badge = value.StartsWith("[key:") || key.IsMatch(value);
                    if (value.StartsWith("[key:"))
                        value = value.Substring(5, value.Length - 6);
                    float width = graphics.MeasureString(value, font, 10000, format).Width + (badge ? 8 : 0);
                    if (x > 0 && x + width > area.Width)
                    {
                        x = 0;
                        y += line;
                    }
                    if (paint)
                    {
                        var rect = new RectangleF(area.X + x, area.Y + y, width, line - 2);
                        if (badge)
                        {
                            Drawing.Box(graphics, rect, Theme.Border, Theme.StrongBorder, 3);
                            Drawing.Text(graphics, value, font, Theme.TextColor, rect, true);
                        }
                        else
                            graphics.DrawString(value, font, brush, rect.X, rect.Y + 2, format);
                    }
                    x += width + space;
                }
            }
            return (int)Math.Ceiling(y + line);
        }
        internal static int Measure(string text, Font font, int width)
        {
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
                return Layout(graphics, text, font, Theme.TextColor, new RectangleF(0, 0, width, 0), false);
        }
    }
    internal sealed class ShortcutLabel : Label
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            ShortcutText.Layout(e.Graphics, Text, Font, ForeColor, ClientRectangle, true);
        }
    }
}
