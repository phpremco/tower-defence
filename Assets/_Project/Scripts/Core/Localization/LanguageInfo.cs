namespace Game.Core
{
    /// <summary>One language the game has a table for: its code ("en") and its name in that language ("English").</summary>
    public readonly struct LanguageInfo
    {
        public LanguageInfo(string code, string nativeName)
        {
            Code = code;
            NativeName = nativeName;
        }

        public string Code { get; }

        public string NativeName { get; }
    }
}
