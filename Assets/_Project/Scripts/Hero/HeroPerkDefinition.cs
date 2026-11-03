using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// A hero unlock a level grants on its first clear, such as Quick Reload. It is saved, shown on the result screen and,
    /// once unlocked, folded into <see cref="HeroPerkModifiers"/> when a level starts. A multiplier of one changes nothing.
    /// The id is the asset name.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Perk", fileName = "HeroPerk")]
    public sealed class HeroPerkDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Perk";
        [SerializeField] private HeroPerkSlot _slot;
        [SerializeField] [TextArea] private string _description = string.Empty;

        /// <summary>Scales the time between two shots of the hero's ranged weapon.</summary>
        [SerializeField] [Range(0.2f, 1f)] private float _rangedIntervalMultiplier = 1f;

        /// <summary>Scales the time the hero's ranged weapon needs to reload.</summary>
        [SerializeField] [Range(0.1f, 1f)] private float _reloadMultiplier = 1f;

        public string Id => name;
        public string DisplayName => _displayName;
        public HeroPerkSlot Slot => _slot;
        public string Description => _description;
        public float RangedIntervalMultiplier => _rangedIntervalMultiplier;
        public float ReloadMultiplier => _reloadMultiplier;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_displayName) || string.IsNullOrWhiteSpace(_description))
            {
                Debug.LogWarning($"Hero perk '{name}' needs a display name and a description.", this);
            }
        }
    }
}
