using System;
using System.IO;

namespace AniimoAffinities
{
    internal static class LanguagePreferences
    {
        internal static string Load()
        {
            string path = PreferenceFiles.FilePath("language.txt");
            try
            {
                if (!File.Exists(path))
                    return "en";
                string code = File.ReadAllText(path).Trim();
                return Array.Exists(Language.Choices, choice => choice.Code == code) && Language.Available(code) ? code : "en";
            }
            catch (IOException) { return "en"; }
            catch (UnauthorizedAccessException) { return "en"; }
        }
        internal static void Save(string code)
        {
            if (!Array.Exists(Language.Choices, choice => choice.Code == code) || !Language.Available(code))
                throw new ArgumentException(Language.T("Unknown language."));
            PreferenceFiles.WriteText(PreferenceFiles.FilePath("language.txt"), code);
        }
    }
}
