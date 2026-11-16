namespace Game.Core
{
    /// <summary>
    /// The short way to player-facing text: <c>Loc.Get("menu.start")</c>, or <c>Loc.Format("hud.wave", wave, total)</c>
    /// for a text with <c>{0}</c> placeholders. Keys in code are always written out as literals, so the localisation
    /// tests can find them and check them against the tables. Definitions keep a key in their text fields and resolve
    /// it here; a value that is not a key (an asset not yet converted) is shown as it is.
    /// Before boot has registered an <see cref="ILocalizationService"/> (EditMode tests, editor tools) the English tables
    /// are loaded once and used directly.
    /// </summary>
    public static class Loc
    {
        private static ILocalizationService _editorFallback;

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return Service.TryGet(key, out string text) ? text : key;
        }

        /// <summary>True when the key has a text in the current language or in English.</summary>
        public static bool Has(string key) => !string.IsNullOrEmpty(key) && Service.TryGet(key, out _);

        public static string Format(string key, object arg0) => string.Format(Get(key), arg0);

        public static string Format(string key, object arg0, object arg1) => string.Format(Get(key), arg0, arg1);

        public static string Format(string key, object arg0, object arg1, object arg2) => string.Format(Get(key), arg0, arg1, arg2);

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        private static ILocalizationService Service
        {
            get
            {
                if (Services.TryGet(out ILocalizationService service))
                {
                    return service;
                }

                return _editorFallback ??= new StringTableService(StringTableService.FallbackLanguage);
            }
        }

        /// <summary>Drops the cached fallback tables, so tests and editor tools see tables they just wrote.</summary>
        public static void ResetFallback()
        {
            _editorFallback = null;
        }
    }
}
