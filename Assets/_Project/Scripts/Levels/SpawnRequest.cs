using Game.Enemies;

namespace Game.Levels
{
    /// <summary>One enemy that is due: what to spawn and which level path it follows.</summary>
    public readonly struct SpawnRequest
    {
        public SpawnRequest(EnemyDefinition enemy, int pathIndex)
        {
            Enemy = enemy;
            PathIndex = pathIndex;
        }

        public EnemyDefinition Enemy { get; }

        public int PathIndex { get; }
    }
}
