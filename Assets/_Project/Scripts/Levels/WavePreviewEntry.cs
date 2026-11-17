using Game.Enemies;

namespace Game.Levels
{
    /// <summary>One creature type of a coming wave, as the wave preview lists it: what it is and how many of it come.</summary>
    public readonly struct WavePreviewEntry
    {
        public WavePreviewEntry(EnemyDefinition enemy, int count)
        {
            Enemy = enemy;
            Count = count;
        }

        public EnemyDefinition Enemy { get; }

        /// <summary>How many of this creature the wave sends on the level's difficulty, over every group and entrance.</summary>
        public int Count { get; }

        /// <summary>Needs a gun that can aim up.</summary>
        public bool Flies => Enemy.Flies;

        /// <summary>Wears an armour pool: asks for a gun made for armour.</summary>
        public bool Armoured => Enemy.HasArmor;

        public bool Boss => Enemy.IsBoss;
    }
}
