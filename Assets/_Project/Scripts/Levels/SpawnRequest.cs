using Game.Enemies;

namespace Game.Levels
{
    /// <summary>One enemy that is due: what to spawn, which level path it follows and where it enters.</summary>
    public readonly struct SpawnRequest
    {
        public SpawnRequest(EnemyDefinition enemy, int pathIndex, SpawnSource source = SpawnSource.Gate)
        {
            Enemy = enemy;
            PathIndex = pathIndex;
            Source = source;
        }

        public EnemyDefinition Enemy { get; }

        public int PathIndex { get; }

        public SpawnSource Source { get; }
    }
}
