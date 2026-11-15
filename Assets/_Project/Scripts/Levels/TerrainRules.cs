using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// What the landscape does to a tower standing on it (round 65): one asset every level points at, so the game, the
    /// range ring, the info line and the balance model all read the same numbers and a tweak is one field.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Terrain Rules", fileName = "TerrainRules")]
    public sealed class TerrainRules : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Data/Balance/TerrainRules.asset";

        [Tooltip("Reach of a tower on raised ground, as a multiple of its own range (1.1 = ten percent further).")]
        [SerializeField] [Min(1f)] private float _highGroundRange = 1.1f;

        [Tooltip("A raised cell holds a tower only when its line to every road cell in reach leaves the plateau within this share of the way; further in, the shot would dip under the plateau's lip. The lowest muzzle (0.34 over the pad) clears the rounded lip of a 0.33 high plateau a little past half-way.")]
        [SerializeField] [Range(0.05f, 1f)] private float _sightRatio = 0.54f;

        /// <summary>Reach multiplier of a tower on raised ground.</summary>
        public float HighGroundRange => _highGroundRange;

        /// <summary>See <see cref="PlateauSkirt.Shadowed"/>.</summary>
        public float SightRatio => _sightRatio;

        /// <summary>The bonus as the interface words it: "+10%".</summary>
        public string HighGroundLabel => $"+{Mathf.RoundToInt((_highGroundRange - 1f) * 100f)}%";

        /// <summary>Reach multiplier of a tower on a cell of this kind.</summary>
        public float RangeMultiplier(TerrainKind kind) => kind == TerrainKind.Raised ? _highGroundRange : 1f;
    }
}
