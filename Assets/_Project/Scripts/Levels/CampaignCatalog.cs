using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One point on the planet. A node without a level is shown as "coming soon".</summary>
    [Serializable]
    public struct MapNode
    {
        public string DisplayName;

        /// <summary>Where the level lies on the globe, in degrees: longitude -180..180 (east positive), latitude -90..90 (north positive).</summary>
        public float Longitude;
        public float Latitude;

        /// <summary>Index of the region (continent) the level belongs to, in CampaignCatalog.Regions.</summary>
        public int Region;

        public LevelDefinition Level;

        /// <summary>The same place on a flat map of the planet, both axes 0..1, for the intro's painted disc.</summary>
        public Vector2 FlatPosition => new Vector2((Longitude + 180f) / 360f, (Latitude + 90f) / 180f);

        /// <summary>The point on a unit sphere this node sits on.</summary>
        public Vector3 Direction => PlanetCoordinates.ToDirection(Longitude, Latitude);
    }

    /// <summary>
    /// The regions of the planet, the levels of the world in order (each inside its region) and the difficulties the
    /// player can choose. Levels are played in list order, so the regions fill one after another.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Campaign Catalog", fileName = "CampaignCatalog")]
    public sealed class CampaignCatalog : ScriptableObject
    {
        [SerializeField] private List<MapRegion> _regions = new List<MapRegion>();
        [SerializeField] private List<MapNode> _nodes = new List<MapNode>();
        [SerializeField] private List<DifficultyProfile> _difficulties = new List<DifficultyProfile>();
        [SerializeField] private StoryDefinition _story;
        [Header("Planet")]
        [Tooltip("How S-69-CRYS looks from space: the cutscene planets and the map globe wear the same style.")]
        [SerializeField] private PlanetStyle _planetStyle = PlanetStyle.Crys();
        [Tooltip("S-69-CRYS painted in Blender; when complete, the map globe and the cutscene wear it instead of the planet painted in code.")]
        [SerializeField] private PlanetTextureSet _planetTextures;
        [Tooltip("Deep space behind the planet: galaxy band, nebula, stars, the sun and the rim light for ships.")]
        [SerializeField] private SpaceStyle _spaceStyle = new SpaceStyle();

        public IReadOnlyList<MapRegion> Regions => _regions;
        public IReadOnlyList<MapNode> Nodes => _nodes;
        public IReadOnlyList<DifficultyProfile> Difficulties => _difficulties;

        /// <summary>The intro and the rest of the story told around the campaign; may be null while there is none.</summary>
        public StoryDefinition Story => _story;

        public PlanetStyle PlanetStyle => _planetStyle ?? (_planetStyle = PlanetStyle.Crys());
        public SpaceStyle SpaceStyle => _spaceStyle ?? (_spaceStyle = new SpaceStyle());

        /// <summary>The painted planet, or null when there is none or it misses maps.</summary>
        public PlanetTextureSet PlanetTextures => _planetTextures != null && _planetTextures.IsComplete ? _planetTextures : null;

        /// <summary>How many levels a region holds.</summary>
        public int NodeCount(int region)
        {
            int count = 0;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].Region == region)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>The index of the first level in a region, or -1 while the region has none.</summary>
        public int FirstNode(int region)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].Region == region)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The layer (surface or Core) a node's region lies in.</summary>
        public MapLayer LayerOf(int nodeIndex)
        {
            int region = _nodes[nodeIndex].Region;
            return region >= 0 && region < _regions.Count ? _regions[region].Layer : MapLayer.Surface;
        }

        private void OnValidate()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                MapNode node = _nodes[i];
                if (node.Region < 0 || node.Region >= _regions.Count)
                {
                    Debug.LogWarning($"Campaign node {i} points at region {node.Region}, which does not exist.", this);
                }
                else if (PlanetCoordinates.AngleBetween(node.Direction, _regions[node.Region].Direction) > _regions[node.Region].RadiusDegrees)
                {
                    Debug.LogWarning($"Campaign node {i} lies outside its region '{_regions[node.Region].DisplayName}'.", this);
                }

                if (i > 0 && node.Region < _nodes[i - 1].Region)
                {
                    Debug.LogWarning($"Campaign node {i} belongs to an earlier region than the node before it; regions fill in order.", this);
                }

                for (int k = 0; k < i; k++)
                {
                    if (LayerOf(k) == LayerOf(i) && PlanetCoordinates.AngleBetween(node.Direction, _nodes[k].Direction) < PlanetCoordinates.MinimumSpacingDegrees)
                    {
                        Debug.LogWarning($"Campaign nodes {k} and {i} sit too close together on the planet.", this);
                    }
                }
            }
        }
    }
}
