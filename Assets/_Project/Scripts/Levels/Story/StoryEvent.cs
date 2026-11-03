using Game.Enemies;
using Game.PathObjects;

namespace Game.Levels
{
    /// <summary>Something that happened in the level, as the trigger selector matches it.</summary>
    public readonly struct StoryEvent
    {
        private StoryEvent(StoryTriggerKind kind, int wave, EnemyDefinition enemy, PathObjectDefinition pathObject = null)
        {
            Kind = kind;
            Wave = wave;
            Enemy = enemy;
            PathObject = pathObject;
        }

        public StoryTriggerKind Kind { get; }
        public int Wave { get; }
        public EnemyDefinition Enemy { get; }
        public PathObjectDefinition PathObject { get; }

        public static StoryEvent LevelStart() => new StoryEvent(StoryTriggerKind.LevelStart, 0, null);

        public static StoryEvent WaveStart(int waveNumber) => new StoryEvent(StoryTriggerKind.WaveStart, waveNumber, null);

        public static StoryEvent FirstSighting(EnemyDefinition enemy) => new StoryEvent(StoryTriggerKind.FirstSighting, 0, enemy);

        public static StoryEvent BossSpawn(EnemyDefinition boss) => new StoryEvent(StoryTriggerKind.BossSpawn, 0, boss);

        public static StoryEvent BossDefeated(EnemyDefinition boss) => new StoryEvent(StoryTriggerKind.BossDefeated, 0, boss);

        public static StoryEvent LevelWon() => new StoryEvent(StoryTriggerKind.LevelWon, 0, null);

        public static StoryEvent FirstUse(PathObjectDefinition pathObject) => new StoryEvent(StoryTriggerKind.FirstUse, 0, null, pathObject);
    }
}
