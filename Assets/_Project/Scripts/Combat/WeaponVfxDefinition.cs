using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// The look of one weapon from muzzle to target: the flash at the barrel, the shot in flight (a glowing sprite, a
    /// trail, a rocket's flame and smoke, or a beam or arc from the barrel to the target) and the impact. The shot
    /// glows in the colour of the tower that fired it. Any part left empty falls back to the plain procedural look.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapon Vfx", fileName = "WeaponVfx")]
    public sealed class WeaponVfxDefinition : ScriptableObject
    {
        [Header("Muzzle")]
        [Tooltip("Side-view flash atlas: one row per weapon family, one column per variant.")]
        [SerializeField] private VfxSheet _muzzle;
        [SerializeField] [Min(0)] private int _muzzleRow;
        [Tooltip("Length of the flash along the barrel, in metres.")]
        [SerializeField] [Min(0f)] private float _muzzleLength = 0.5f;
        [SerializeField] [Min(0.01f)] private float _muzzleSeconds = 0.05f;

        [Header("Shot in flight")]
        [Tooltip("A sprite stretched along the flight (bolt, slug, tracer, plasma).")]
        [SerializeField] private VfxSheet _projectile;
        [SerializeField] [Min(0f)] private float _projectileLength = 0.5f;
        [SerializeField] [Min(0f)] private float _projectileWidth = 0.14f;
        [SerializeField] private VfxSheet _trail;
        [SerializeField] [Min(0f)] private float _trailWidth = 0.1f;
        [SerializeField] [Min(0f)] private float _trailSeconds = 0.2f;
        [Tooltip("A rocket's flame behind the body.")]
        [SerializeField] private VfxSheet _exhaust;
        [SerializeField] [Min(0f)] private float _exhaustLength = 0.5f;
        [Tooltip("A rocket's smoke ribbon.")]
        [SerializeField] private VfxSheet _smokeTrail;
        [SerializeField] [Min(0f)] private float _smokeTrailWidth = 0.25f;
        [SerializeField] [Min(0f)] private float _smokeTrailSeconds = 0.8f;
        [Tooltip("A beam or arc from the barrel to the target.")]
        [SerializeField] private VfxSheet _beam;
        [SerializeField] [Min(0f)] private float _beamWidth = 0.3f;

        [Header("Impact")]
        [SerializeField] private ImpactVfxDefinition _impact;

        public VfxSheet Muzzle => _muzzle;
        public int MuzzleRow => _muzzleRow;
        public float MuzzleLength => _muzzleLength;
        public float MuzzleSeconds => _muzzleSeconds;
        public VfxSheet Projectile => _projectile;
        public float ProjectileLength => _projectileLength;
        public float ProjectileWidth => _projectileWidth;
        public VfxSheet Trail => _trail;
        public float TrailWidth => _trailWidth;
        public float TrailSeconds => _trailSeconds;
        public VfxSheet Exhaust => _exhaust;
        public float ExhaustLength => _exhaustLength;
        public VfxSheet SmokeTrail => _smokeTrail;
        public float SmokeTrailWidth => _smokeTrailWidth;
        public float SmokeTrailSeconds => _smokeTrailSeconds;
        public VfxSheet Beam => _beam;
        public float BeamWidth => _beamWidth;
        public ImpactVfxDefinition Impact => _impact;

        private void OnValidate()
        {
            if (_impact == null)
            {
                Debug.LogWarning($"{name}: the weapon has no impact look.", this);
            }

            if (_muzzle != null && _muzzleRow >= _muzzle.Rows)
            {
                Debug.LogWarning($"{name}: muzzle row {_muzzleRow} is past the {_muzzle.Rows} rows of {_muzzle.name}.", this);
            }
        }
    }
}
