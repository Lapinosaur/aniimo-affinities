using System;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace AniimoAffinities
{
    // Release notes are data: emit escaped RTF only, never HTML or executable content.
    internal static class MarkdownRtf
    {
        internal static string Escape(string value)
        {
            var result = new StringBuilder();
            foreach (char c in value)
                if (c == '\\' || c == '{' || c == '}')
                    result.Append('\\').Append(c);
                else if (c > 127)
                    result.Append("\\u").Append((short)c).Append('?');
                else if (c == '\n')
                    result.Append("\\line ");
                else if (c != '\r')
                    result.Append(c);
            return result.ToString();
        }
        private static string ColorEntry(Color color)
        {
            return "\\red" + color.R + "\\green" + color.G + "\\blue" + color.B + ";";
        }
        private static string Inline(string source)
        {
            var result = new StringBuilder();
            var pattern = new Regex(@"`([^`]+)`|\*\*(.+?)\*\*|__(.+?)__|\*([^*]+)\*|~~(.+?)~~|\[([^\]]+)\]\(([^\s)]+)\)|\\([\\`*_{}\[\]()#+.!|>-])");
            int end = 0;
            foreach (Match match in pattern.Matches(source))
            {
                result.Append(Escape(source.Substring(end, match.Index - end)));
                if (match.Groups[1].Success)
                    result.Append("{\\f1 ").Append(Escape(match.Groups[1].Value)).Append('}');
                else if (match.Groups[2].Success || match.Groups[3].Success)
                    result.Append("{\\b ").Append(Inline(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value)).Append('}');
                else if (match.Groups[4].Success)
                    result.Append("{\\i ").Append(Inline(match.Groups[4].Value)).Append('}');
                else if (match.Groups[5].Success)
                    result.Append("{\\strike ").Append(Inline(match.Groups[5].Value)).Append('}');
                else if (match.Groups[6].Success)
                {
                    result.Append(Inline(match.Groups[6].Value));
                    Uri uri;
                    if (Uri.TryCreate(match.Groups[7].Value, UriKind.Absolute, out uri) && (uri.Scheme == "https" || uri.Scheme == "http"))
                        result.Append(" {\\cf2 (").Append(Escape(uri.AbsoluteUri)).Append(")}");
                }
                else
                    result.Append(Escape(match.Groups[8].Value));
                end = match.Index + match.Length;
            }
            return result.Append(Escape(source.Substring(end))).ToString();
        }
        private static string[] Cells(string line)
        {
            return Regex.Split(line.Trim().Trim('|'), @"(?<!\\)\|");
        }
        internal static string Render(string markdown, int width)
        {
            var output = new StringBuilder(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}{\f1 Consolas;}}{\colortbl ;");
            output.Append(ColorEntry(Theme.TextColor)).Append(ColorEntry(Theme.AccentForeground)).Append(ColorEntry(Theme.Border)).Append("}\\viewkind4\\uc1\\f0\\fs18\\cf1 ");
            string[] lines = Regex.Replace(markdown ?? "", @"<!--[\s\S]*?-->", "").Replace("\r", "").Split('\n');
            bool code = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i], trimmed = line.Trim();
                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    code = !code;
                    continue;
                }
                if (code)
                {
                    output.Append("\\pard\\li160\\sa40\\f1\\fs17 ").Append(Escape(line)).Append("\\par ");
                    continue;
                }
                if (trimmed.Length == 0)
                    continue;
                if (i + 1 < lines.Length && line.Contains("|") && Regex.IsMatch(lines[i + 1], @"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$"))
                {
                    int columns = Cells(line).Length;
                    int cellWidth = Math.Max(600, (width - 12) * 15 / columns);
                    string[] alignment = Cells(lines[i + 1]);
                    bool header = true;
                    while (i < lines.Length && lines[i].Contains("|"))
                    {
                        var cells = Cells(lines[i]);
                        output.Append("\\trowd\\trgaph90\\trleft0 ");
                        for (int c = 0; c < columns; c++)
                            output.Append("\\clbrdrt\\brdrs\\brdrw10\\brdrcf3\\clbrdrl\\brdrs\\brdrw10\\brdrcf3\\clbrdrb\\brdrs\\brdrw10\\brdrcf3\\clbrdrr\\brdrs\\brdrw10\\brdrcf3\\cellx").Append((c + 1) * cellWidth);
                        for (int c = 0; c < columns; c++)
                        {
                            string align = c < alignment.Length ? alignment[c].Trim() : "";
                            output.Append("\\pard\\intbl\\f0\\fs18\\sa100\\sb100").Append(align.EndsWith(":", StringComparison.Ordinal) ? (align.StartsWith(":", StringComparison.Ordinal) ? "\\qc " : "\\qr ") : "\\ql ");
                            output.Append(header ? "{\\b " : "{").Append(Inline(c < cells.Length ? cells[c].Trim() : "")).Append("}\\cell ");
                        }
                        output.Append("\\row ");
                        i += header ? 2 : 1;
                        header = false;
                    }
                    i--;
                    output.Append("\\pard\\par ");
                    continue;
                }
                var title = Regex.Match(trimmed, @"^(#{1,6})\s+(.+?)\s*#*$");
                var list = Regex.Match(line, @"^(\s*)([-+*]|\d+[.)])\s+(.+)$");
                output.Append("\\pard\\f0\\fs18\\cf1\\sa140 ");
                if (title.Success)
                    output.Append("\\sb180\\b\\fs").Append(title.Groups[1].Length == 1 ? 28 : 23).Append(' ').Append(Inline(title.Groups[2].Value)).Append("\\b0");
                else if (list.Success)
                {
                    int indent = 240 + Math.Min(8, list.Groups[1].Length) * 90;
                    string marker = list.Groups[2].Value.Length == 1 ? "•" : list.Groups[2].Value;
                    output.Append("\\li").Append(indent).Append("\\fi-220\\tx").Append(indent).Append(' ').Append(Escape(marker)).Append("\\tab ").Append(Inline(list.Groups[3].Value));
                }
                else if (trimmed.StartsWith(">", StringComparison.Ordinal))
                    output.Append("\\li240\\i ").Append(Inline(trimmed.TrimStart('>', ' '))).Append("\\i0");
                else if (Regex.IsMatch(trimmed, @"^([-*_])\1{2,}$"))
                    output.Append("\\brdrb\\brdrs\\brdrcf3 ");
                else
                    output.Append(Inline(trimmed));
                output.Append("\\par ");
            }
            return Regex.Replace(output.Append('}').ToString(), @"(?<!\\)\\fs(\d+)", match => "\\fs" + Math.Round(int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) * DisplaySettings.FontScale));
        }
    }
}
