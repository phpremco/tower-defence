using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Turns text keys into the player's language. Registered once in the <see cref="Services"/> registry at boot; code
    /// reaches it through <see cref="Loc"/>. See Docs/LOCALISATION.md for keys, tables and adding a language.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>The code of the language shown now, such as "en".</summary>
        string CurrentLanguage { get; }

        /// <summary>Every language with a table, in a stable order; the Options screen lists these.</summary>
        IReadOnlyList<LanguageInfo> Languages { get; }

        /// <summary>Raised after <see cref="SetLanguage"/> switched to another language.</summary>
        event Action LanguageChanged;

        /// <summary>Finds the text for a key in the current language, falling back to English.</summary>
        bool TryGet(string key, out string text);

        /// <summary>Switches the language; an unknown code keeps the current one and returns false.</summary>
        bool SetLanguage(string code);
    }
}
