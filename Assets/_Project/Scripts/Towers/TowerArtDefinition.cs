using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// The imported (Blender) look of one tower type: a model per upgrade level,
    /// with how the head aims, where shots leave, and the wreck it leaves. A <see cref="TowerDefinition"/> points at
    /// one of these; without it the tower keeps its procedural model. Pure look data: logic never reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Tower Art Definition", fileName = "TowerArt")]
    public sealed class TowerArtDefinition : ScriptableObject
    {
        public const string DefaultMuzzlePrefix = "Muzzle";

        [Header("Looks")]
        [SerializeField] private TowerTierArt[] _tiers = new TowerTierArt[0];

        [Header("Aim and shots")]
        [SerializeField] private TowerAimMode _aim;
        [Tooltip("Lowest and highest barrel elevation in degrees when the head aims up and down.")]
        [SerializeField] private Vector2 _pitchLimits = new Vector2(-12f, 60f);
        [Tooltip("Bone name prefix of the shot origins: Muzzle_1..n, Arc_1..n, Tube_1..n.")]
        [SerializeField] private string _muzzlePrefix = DefaultMuzzlePrefix;
        [SerializeField] private TowerMuzzleChoice _muzzleChoice;
        [Tooltip("Glow factor reached at the end of a wind-up (1 keeps the glow).")]
        [SerializeField] [Min(1f)] private float _chargeGlow = 1f;
        [Tooltip("The gun hides its rank plate stars (a gate shows its stars on the beam instead).")]
        [SerializeField] private bool _hideStars;

        [Header("Wreck")]
        [Tooltip("The shared wreck (burnt pad, husk and scrap), or empty to only hide the tower.")]
        [SerializeField] private ModelArtDefinition _wreck;
        [Tooltip("The wreck's debris object for this tower (Debris_Gun, Debris_Laser, ...).")]
        [SerializeField] private string _debris = string.Empty;
        [Tooltip("One rank star, for structures that show their stars apart from a model.")]
        [SerializeField] private GameObject _star;

        public TowerAimMode Aim => _aim;
        public Vector2 PitchLimits => _pitchLimits;
        public string MuzzlePrefix => _muzzlePrefix;
        public TowerMuzzleChoice MuzzleChoice => _muzzleChoice;
        public float ChargeGlow => _chargeGlow;
        public bool HideStars => _hideStars;
        public ModelArtDefinition Wreck => _wreck;
        public string Debris => _debris;
        public GameObject Star => _star;

        /// <summary>How many looks the tower has.</summary>
        public int TierCount => _tiers.Length;

        /// <summary>The look of a level: the tier of that level, else the newest look below it.</summary>
        public ModelArtDefinition ArtFor(int levelIndex)
        {
            for (int level = levelIndex; level >= 0; level--)
            {
                ModelArtDefinition look = Find(level);
                if (look != null)
                {
                    return look;
                }
            }

            return null;
        }

        private ModelArtDefinition Find(int level)
        {
            foreach (TowerTierArt tier in _tiers)
            {
                if (tier != null && tier.Art != null && tier.LevelIndex == level)
                {
                    return tier.Art;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (ArtFor(0) == null)
            {
                Debug.LogWarning($"TowerArtDefinition '{name}' has no level-1 look.", this);
            }

            if (_pitchLimits.x > _pitchLimits.y)
            {
                Debug.LogWarning($"TowerArtDefinition '{name}' has its lowest elevation above its highest.", this);
            }

            if (_wreck != null && string.IsNullOrEmpty(_debris))
            {
                Debug.LogWarning($"TowerArtDefinition '{name}' has a wreck but no debris name.", this);
            }
        }
    }
}
