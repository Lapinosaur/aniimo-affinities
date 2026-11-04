using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;

namespace AniimoAffinities
{
    internal static class Language
    {
        internal sealed class Choice
        {
            internal readonly string Code, NativeName, EnglishName;
            internal Choice(string code, string nativeName, string englishName)
            {
                Code = code;
                NativeName = nativeName;
                EnglishName = englishName;
            }
        }
        internal static readonly Choice[] Choices = {
            new Choice("en", "English", "English"), new Choice("fr", "Français", "French"),
            new Choice("de", "Deutsch", "German"), new Choice("es", "Español", "Spanish"),
            new Choice("it", "Italiano", "Italian"), new Choice("pt", "Português", "Portuguese"),
            new Choice("nl", "Nederlands", "Dutch"), new Choice("pl", "Polski", "Polish"),
            new Choice("cs", "Čeština", "Czech"), new Choice("sk", "Slovenčina", "Slovak"),
            new Choice("sl", "Slovenščina", "Slovenian"), new Choice("hu", "Magyar", "Hungarian"),
            new Choice("ro", "Română", "Romanian"), new Choice("bg", "Български", "Bulgarian"),
            new Choice("el", "Ελληνικά", "Greek"), new Choice("sv", "Svenska", "Swedish"),
            new Choice("da", "Dansk", "Danish"), new Choice("fi", "Suomi", "Finnish"),
            new Choice("et", "Eesti", "Estonian"), new Choice("lv", "Latviešu", "Latvian"),
            new Choice("lt", "Lietuvių", "Lithuanian"), new Choice("ga", "Gaeilge", "Irish"),
            new Choice("nb", "Norsk bokmål", "Norwegian"), new Choice("uk", "Українська", "Ukrainian"),
            new Choice("ru", "Русский", "Russian"), new Choice("tr", "Türkçe", "Turkish"),
            new Choice("sq", "Shqip", "Albanian"), new Choice("ca", "Català", "Catalan"),
            new Choice("eu", "Euskara", "Basque"), new Choice("gl", "Galego", "Galician")
        };
        private static Dictionary<string, string> translations = new Dictionary<string, string>();
        internal static string Code = "en";
        internal static string T(string source)
        {
            if (source == null)
                return null;
            string translated;
            return translations.TryGetValue(source, out translated) ? translated : source;
        }
        internal static bool Available(string code)
        {
            if (code == "en")
                return true;
            return Array.Exists(Assembly.GetExecutingAssembly().GetManifestResourceNames(), name => name == "locale-" + code + ".txt");
        }
        internal static void Set(string code)
        {
            var loaded = new Dictionary<string, string>();
            if (code != "en")
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("locale-" + code + ".txt"))
                {
                    if (stream == null)
                        throw new ArgumentException("Unknown language.");
                    using (var reader = new StreamReader(stream))
                        loaded = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
                }
            translations = loaded;
            Code = code;
        }
        internal static string Original(string translated)
        {
            foreach (var pair in translations)
                if (ReferenceEquals(pair.Value, translated))
                    return pair.Key;
            foreach (var pair in translations)
                if (pair.Value == translated)
                    return pair.Key;
            return translated;
        }
    }
}
