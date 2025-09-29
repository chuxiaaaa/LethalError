using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Text;

namespace LethalError.Lang
{
    public static class LocalText
    {
        static LocalText()
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(Properties.Resources.LocalText);
            if (data != null)
            {
                _localizationData = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in data)
                {
                    _localizationData[item.Key] = item.Value;
                }
            }
        }

        public enum Language
        {
            Auto = -1,
            English = 0,
            Chinese = 1
        }

        public static Language CurrentLanguage { get; set; } = Language.English;

        private static Dictionary<string, string[]> _localizationData = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public static string GetText(string key,params string[] args)
        {
            if (_localizationData.TryGetValue(key, out string[] texts))
            {
                int languageIndex = (int)CurrentLanguage;
                if (languageIndex >= 0 && languageIndex < texts.Length && !string.IsNullOrEmpty(texts[languageIndex]))
                {
                    return string.Format(texts[languageIndex], args);
                }
                if (texts.Length > 0 && !string.IsNullOrEmpty(texts[0]))
                {
                    return string.Format(texts[languageIndex], args);
                }
            }

            return $"[Missing Text: {key}]";
        }

      
    }
}
