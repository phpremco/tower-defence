using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One point on the planet map. A node without a level is shown as "coming soon".</summary>
    [Serializable]
    public struct MapNode
    {
        public string DisplayName;

        /// <summary>Position on the map, both axes from 0 (left, bottom) to 1 (right, top).</summary>
        public Vector2 MapPosition;

        public LevelDefinition Level;
    }

    /// <summary>The levels of the world in order, and the difficulties the player can choose.</summary>
    [CreateAssetMenu(menuName = "Game/Campaign Catalog", fileName = "CampaignCatalog")]
    public sealed class CampaignCatalog : ScriptableObject
    {
        [SerializeField] private List<MapNode> _nodes = new List<MapNode>();
        [SerializeField] private List<DifficultyProfile> _difficulties = new List<DifficultyProfile>();
        [SerializeField] private StoryDefinition _story;

        public IReadOnlyList<MapNode> Nodes => _nodes;
        public IReadOnlyList<DifficultyProfile> Difficulties => _difficulties;

        /// <summary>The intro and the rest of the story told around the campaign; may be null while there is none.</summary>
        public StoryDefinition Story => _story;
    }
}
