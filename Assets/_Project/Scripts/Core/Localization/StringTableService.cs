using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The string tables from <c>Resources/Localization</c>. A language is every text file named
    /// <c>&lt;code&gt;.&lt;part&gt;</c> (en.ui, en.data, ...): parts let the generator own its table while the UI table is
    /// written by hand, and none of them needs a scene or asset reference. A key missing in the current language falls
    /// back to English, and a key missing there too shows the key itself, so a gap is visible but never empty.
    /// </summary>
    public sealed class StringTableService : ILocalizationService
    {
        public const string ResourceFolder = "Localization";
        public const string FallbackLanguage = "en";

        /// <summary>Every table names its own language under this key, in that language.</summary>
        public const string LanguageNameKey = "language.name";

        private const char PartSeparator = '.';

        private readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>();
        private readonly List<LanguageInfo> _languages = new List<LanguageInfo>();
        private readonly List<string> _errors = new List<string>();
        private Dictionary<string, string> _current;
        private Dictionary<string, string> _fallback;

        /// <summary>Loads every table from Resources and shows <paramref name="languageCode"/> (English when unknown).</summary>
        public StringTableService(string languageCode)
            : this(LoadSources(), languageCode)
        {
        }

        /// <summary>Builds the tables from (name, text) pairs; the name is the file name without extension.</summary>
        public StringTableService(IEnumerable<KeyValuePair<string, string>> sources, string languageCode)
        {
            foreach (KeyValuePair<string, string> source in sources)
            {
                StringTableParser.Parse(source.Value, source.Key, TableFor(LanguageOf(source.Key)), _errors);
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> table in _tables)
            {
                string name = table.Value.TryGetValue(LanguageNameKey, out string native) ? native : table.Key;
                _languages.Add(new LanguageInfo(table.Key, name));
            }

            _languages.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
            _tables.TryGetValue(FallbackLanguage, out _fallback);
            _fallback ??= new Dictionary<string, string>();
            if (!SetLanguage(languageCode))
            {
                CurrentLanguage = FallbackLanguage;
                _current = _fallback;
            }
        }

        public string CurrentLanguage { get; private set; }

        public IReadOnlyList<LanguageInfo> Languages => _languages;

        /// <summary>Parse problems (bad lines, duplicate keys) found while loading; the localisation tests read these.</summary>
        public IReadOnlyList<string> Errors => _errors;

        public event Action LanguageChanged;

        public bool TryGet(string key, out string text)
        {
            if (string.IsNullOrEmpty(key))
            {
                text = string.Empty;
                return false;
            }

            return _current.TryGetValue(key, out text) || _fallback.TryGetValue(key, out text);
        }

        public bool SetLanguage(string code)
        {
            if (string.IsNullOrEmpty(code) || !_tables.TryGetValue(code, out Dictionary<string, string> table))
            {
                return false;
            }

            bool changed = CurrentLanguage != null && CurrentLanguage != code;
            CurrentLanguage = code;
            _current = table;
            if (changed)
            {
                LanguageChanged?.Invoke();
            }

            return true;
        }

        /// <summary>The keys of one language, for validators that compare languages or check references.</summary>
        public IReadOnlyCollection<string> KeysOf(string code)
        {
            return _tables.TryGetValue(code, out Dictionary<string, string> table) ? table.Keys : (IReadOnlyCollection<string>)Array.Empty<string>();
        }

        private Dictionary<string, string> TableFor(string code)
        {
            if (!_tables.TryGetValue(code, out Dictionary<string, string> table))
            {
                table = new Dictionary<string, string>(StringComparer.Ordinal);
                _tables.Add(code, table);
            }

            return table;
        }

        private static string LanguageOf(string sourceName)
        {
            int separator = sourceName.IndexOf(PartSeparator);
            return separator > 0 ? sourceName.Substring(0, separator) : sourceName;
        }

        private static IEnumerable<KeyValuePair<string, string>> LoadSources()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>(ResourceFolder);
            var sources = new List<KeyValuePair<string, string>>(assets.Length);
            foreach (TextAsset asset in assets)
            {
                sources.Add(new KeyValuePair<string, string>(asset.name, asset.text));
            }

            // Resources gives no order; sorted names make duplicate-key reports and the language list stable.
            sources.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return sources;
        }
    }
}
