using Game.Enemies;

namespace Game.Levels
{
    /// <summary>One enemy that is due: what to spawn, which level path it follows and where it enters.</summary>
    public readonly struct SpawnRequest
    {
        public SpawnRequest(EnemyDefinition enemy, int pathIndex, SpawnSource source = SpawnSource.Gate, int group = -1, int index = -1)
        {
            Enemy = enemy;
            PathIndex = pathIndex;
            Source = source;
            Group = group;
            Index = index;
        }

        public EnemyDefinition Enemy { get; }
        public int PathIndex { get; }
        public SpawnSource Source { get; }

        /// <summary>The wave's group it belongs to, or -1.</summary>
        public int Group { get; }

        /// <summary>Its place in the group (0 for the first), or -1.</summary>
        public int Index { get; }
    }
}
