using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Which impact definition every kind of blast uses: one per projectile style, plus the bursts that are not
    /// projectiles (a dying enemy, a tripped mine, a boss pulse). Path objects may carry their own definition and
    /// fall back to the mine entry. Missing entries fall back to the default, so a level never plays without a look.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Vfx Library", fileName = "VfxLibrary")]
    public sealed class VfxLibrary : ScriptableObject
    {
        [Serializable]
        private struct StyleEntry
        {
            public ProjectileStyle Style;
            public ImpactVfxDefinition Vfx;
        }

        [SerializeField] private ImpactVfxDefinition _default;
        [SerializeField] private StyleEntry[] _styles = new StyleEntry[0];
        [SerializeField] private ImpactVfxDefinition _death;
        [SerializeField] private ImpactVfxDefinition _mine;
        [SerializeField] private ImpactVfxDefinition _bossPulse;

        public ImpactVfxDefinition Default => _default;
        public ImpactVfxDefinition Death => _death != null ? _death : _default;
        public ImpactVfxDefinition Mine => _mine != null ? _mine : _default;
        public ImpactVfxDefinition BossPulse => _bossPulse != null ? _bossPulse : _default;

        public ImpactVfxDefinition For(ProjectileStyle style)
        {
            for (int i = 0; i < _styles.Length; i++)
            {
                if (_styles[i].Style == style && _styles[i].Vfx != null)
                {
                    return _styles[i].Vfx;
                }
            }

            return _default;
        }

        /// <summary>A library with one plain definition for everything, for tests and tools that run without assets.</summary>
        public static VfxLibrary CreateFallback()
        {
            var library = CreateInstance<VfxLibrary>();
            library._default = CreateInstance<ImpactVfxDefinition>();
            return library;
        }

        private void OnValidate()
        {
            if (_default == null)
            {
                Debug.LogWarning($"{name}: the default impact definition is missing.", this);
            }
        }
    }
}
