using System;
using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Which look every kind of shot and blast uses: one weapon recipe per projectile style (muzzle, flight, impact),
    /// plus the bursts that are not projectiles (a dying enemy, a boss going down, a tripped mine, a boss pulse). Path
    /// objects may carry their own definition and fall back to the mine entry. Missing entries fall back to the
    /// default, so a level never plays without a look.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Vfx Library", fileName = "VfxLibrary")]
    public sealed class VfxLibrary : ScriptableObject
    {
        [Serializable]
        private struct StyleEntry
        {
            public ProjectileStyle Style;
            public WeaponVfxDefinition Weapon;
        }

        [SerializeField] private ImpactVfxDefinition _default;
        [SerializeField] private StyleEntry[] _styles = new StyleEntry[0];
        [SerializeField] private ImpactVfxDefinition _death;
        [Tooltip("A boss going down: a chain of blasts, then lingering smoke.")]
        [SerializeField] private ImpactVfxDefinition _bossDeath;
        [SerializeField] private ImpactVfxDefinition _mine;
        [SerializeField] private ImpactVfxDefinition _bossPulse;
        [Tooltip("The hero's shockwave: rings, glow, refraction, flash and what the blow kicks up (round 61).")]
        [SerializeField] private ShockwaveVfxDefinition _shockwave;
        [Tooltip("The gameplay markers, bars, projectiles and debris painted in Blender (registered at boot).")]
        [SerializeField] private MarkerArt _markers;
        [Tooltip("The marks, sheets and props bosses leave on the field (round 29; registered at boot).")]
        [SerializeField] private BossFxArt _bossFx;

        public ImpactVfxDefinition Default => _default;
        public ImpactVfxDefinition Death => _death != null ? _death : _default;
        public ImpactVfxDefinition BossDeath => _bossDeath != null ? _bossDeath : Death;
        public ImpactVfxDefinition Mine => _mine != null ? _mine : _default;
        public ImpactVfxDefinition BossPulse => _bossPulse != null ? _bossPulse : _default;

        /// <summary>The look of a ground shockwave, or null when the library was built without it (the views keep their plain ring).</summary>
        public ShockwaveVfxDefinition Shockwave => _shockwave;

        /// <summary>The round-25b marker art, or null when the library was built without it.</summary>
        public MarkerArt Markers => _markers;

        /// <summary>The round-29 boss art, or null when the library was built without it.</summary>
        public BossFxArt BossFx => _bossFx;

        /// <summary>The impact of a projectile style.</summary>
        public ImpactVfxDefinition For(ProjectileStyle style)
        {
            WeaponVfxDefinition weapon = WeaponFor(style);
            return weapon != null && weapon.Impact != null ? weapon.Impact : _default;
        }

        /// <summary>The whole recipe of a projectile style, or null when the style has none.</summary>
        public WeaponVfxDefinition WeaponFor(ProjectileStyle style)
        {
            for (int i = 0; i < _styles.Length; i++)
            {
                if (_styles[i].Style == style && _styles[i].Weapon != null)
                {
                    return _styles[i].Weapon;
                }
            }

            return null;
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
