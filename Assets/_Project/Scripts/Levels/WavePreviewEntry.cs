using Game.Enemies;

namespace Game.Levels
{
    /// <summary>One creature type of a coming wave, as the wave preview lists it: what it is and how many of it come.</summary>
    public readonly struct WavePreviewEntry
    {
        public WavePreviewEntry(EnemyDefinition enemy, int count, bool brood = false)
        {
            Enemy = enemy;
            Count = count;
            Brood = brood;
        }

        public EnemyDefinition Enemy { get; }

        /// <summary>How many of this creature the wave sends on the level's difficulty, over every group and entrance.</summary>
        public int Count { get; }

        /// <summary>Needs a gun that can aim up.</summary>
        public bool Flies => Enemy.Flies;

        /// <summary>Wears an armour pool: asks for a gun made for armour.</summary>
        public bool Armoured => Enemy.HasArmor;

        public bool Boss => Enemy.IsBoss;

        /// <summary>
        /// True for the creature a level boss of the wave sends out of its body while it walks (round 88c). How many come
        /// depends on how far the boss gets, so the brood adds no number: <see cref="Count"/> is what the wave itself sends.
        /// </summary>
        public bool Brood { get; }

        /// <summary>True for a brood creature the wave itself does not send: an entry without a number.</summary>
        public bool BroodOnly => Brood && Count == 0;
    }
}
