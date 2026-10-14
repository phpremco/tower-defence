using System;

namespace Game.Levels
{
    /// <summary>Which level, on which difficulty, the generic Level scene must build.</summary>
    public sealed class LevelSession
    {
        public LevelSession(LevelDefinition level, DifficultyProfile difficulty)
        {
            Level = level != null ? level : throw new ArgumentNullException(nameof(level));
            Difficulty = difficulty != null ? difficulty : throw new ArgumentNullException(nameof(difficulty));
        }

        public LevelDefinition Level { get; }

        public DifficultyProfile Difficulty { get; }
    }
}
