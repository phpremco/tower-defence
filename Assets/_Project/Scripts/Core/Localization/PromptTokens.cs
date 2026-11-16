using System;

namespace Game.Core
{
    /// <summary>
    /// The button tokens of localised text, as text: <c>{action:Confirm}</c> names a game action, <c>{control:&lt;Keyboard&gt;/escape}</c>
    /// a fixed control. The UI draws each one as the glyph of the device in use (<c>Game.UI.PromptText</c>); this part only
    /// finds them, so the dialogue typewriter can count a token as the one character its glyph takes. Tokens belong in
    /// texts read with <c>Loc.Get</c>: <c>Loc.Format</c> would take their braces for placeholders.
    /// </summary>
    public static class PromptTokens
    {
        public const string ActionPrefix = "{action:";
        public const string ControlPrefix = "{control:";
        private const char TokenEnd = '}';

        /// <summary>True when the text holds at least one token.</summary>
        public static bool HasToken(string text)
        {
            return !string.IsNullOrEmpty(text) && (text.IndexOf(ActionPrefix, StringComparison.Ordinal) >= 0 || text.IndexOf(ControlPrefix, StringComparison.Ordinal) >= 0);
        }

        /// <summary>The length of the text when each token shows as one glyph.</summary>
        public static int VisibleLength(string text)
        {
            if (!HasToken(text))
            {
                return text?.Length ?? 0;
            }

            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                int end = TokenEnding(text, i);
                if (end >= 0)
                {
                    i = end;
                }

                count++;
            }

            return count;
        }

        /// <summary>
        /// Splits a text into plain runs and whole tokens ("{action:Confirm}", braces included); anything that only
        /// looks like the start of a token stays text.
        /// </summary>
        public static void Walk(string text, Action<string> run, Action<string> token)
        {
            int start = 0;
            int i = 0;
            while (i < text.Length)
            {
                int end = TokenEnding(text, i);
                if (end < 0)
                {
                    i++;
                    continue;
                }

                if (i > start)
                {
                    run(text.Substring(start, i - start));
                }

                token(text.Substring(i, end - i + 1));
                i = end + 1;
                start = i;
            }

            if (start < text.Length)
            {
                run(text.Substring(start));
            }
        }

        /// <summary>The body after the prefix ("Confirm" of "{action:Confirm}"); null when the token has another prefix.</summary>
        public static string Body(string token, string prefix)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith(prefix, StringComparison.Ordinal) || token.Length <= prefix.Length)
            {
                return null;
            }

            return token.Substring(prefix.Length, token.Length - prefix.Length - 1);
        }

        // The index of the closing brace when a token starts at i, else -1.
        private static int TokenEnding(string text, int i)
        {
            if (text[i] != '{')
            {
                return -1;
            }

            bool opens = string.CompareOrdinal(text, i, ActionPrefix, 0, ActionPrefix.Length) == 0
                || string.CompareOrdinal(text, i, ControlPrefix, 0, ControlPrefix.Length) == 0;
            return opens ? text.IndexOf(TokenEnd, i) : -1;
        }
    }
}
