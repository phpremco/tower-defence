using System;

namespace Game.Levels
{
    /// <summary>Which level the generic Level scene must build. Registered in Services by whoever starts the level.</summary>
    public sealed class LevelSession
    {
        public LevelSession(LevelDefinition level)
        {
            Level = level != null ? level : throw new ArgumentNullException(nameof(level));
        }

        public LevelDefinition Level { get; }
    }
}
