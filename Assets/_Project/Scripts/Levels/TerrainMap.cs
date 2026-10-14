using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Lookup of the terrain kind per cell. Pure data: the look of the landscape is built elsewhere from the same map,
    /// so what the player sees and what blocks building can never disagree.
    /// </summary>
    public sealed class TerrainMap
    {
        /// <summary>How far raised ground stands above the plain, in world units. Towers on it are lifted by this much.</summary>
        public const float PlateauHeight = 0.35f;

        private readonly Dictionary<Vector2Int, TerrainKind> _kinds = new Dictionary<Vector2Int, TerrainKind>();

        public TerrainMap(IEnumerable<TerrainPatch> patches)
        {
            foreach (TerrainPatch patch in patches)
            {
                _kinds[patch.Cell] = patch.Kind;
            }
        }

        public IEnumerable<KeyValuePair<Vector2Int, TerrainKind>> Cells => _kinds;

        public TerrainKind KindAt(Vector2Int cell) => _kinds.TryGetValue(cell, out TerrainKind kind) ? kind : TerrainKind.Ground;

        public bool IsLiquid(Vector2Int cell) => IsLiquid(KindAt(cell));

        /// <summary>Height of the ground a tower on this cell stands on.</summary>
        public float HeightAt(Vector2Int cell) => KindAt(cell) == TerrainKind.Raised ? PlateauHeight : 0f;

        public static bool IsLiquid(TerrainKind kind) => kind == TerrainKind.Water || kind == TerrainKind.Lava || kind == TerrainKind.Quicksand;
    }
}
