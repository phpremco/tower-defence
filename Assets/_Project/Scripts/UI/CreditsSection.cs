using System;
using UnityEngine;

namespace Game.UI
{
    /// <summary>One block of the credits screen: a heading and its lines, all as string-table keys.</summary>
    [Serializable]
    public sealed class CreditsSection
    {
        [SerializeField] private string _titleKey;
        [Tooltip("String-table keys; a text may hold several lines (\\n in the table).")]
        [SerializeField] private string[] _lineKeys = new string[0];

        public CreditsSection(string titleKey, params string[] lineKeys)
        {
            _titleKey = titleKey;
            _lineKeys = lineKeys ?? new string[0];
        }

        public string TitleKey => _titleKey;

        public string[] LineKeys => _lineKeys;
    }
}
