using System;
using System.IO;
using System.Text;
namespace AniimoAffinities
{
    // File formats belong to their preference owners; atomic I/O and the root live here.
    internal static class PreferenceFiles
    {
        internal static string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AniimoAffinities");
        internal static string FilePath(string name)
        {
            return Path.Combine(Root, name);
        }
        internal static void WriteLines(string path, string[] lines)
        {
            WriteText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }
        internal static void WriteText(string path, string content)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, content, new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
