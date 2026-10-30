using System;
using System.Collections.Generic;
using Game.PathObjects;

namespace Game.Levels
{
    /// <summary>Which level, on which difficulty, the generic Level scene must build.</summary>
    public sealed class LevelSession
    {
        public LevelSession(LevelDefinition level, DifficultyProfile difficulty, bool briefing = false, IReadOnlyList<PathObjectDefinition> loadout = null)
        {
            Briefing = briefing;
            Loadout = loadout;
            Level = level != null ? level : throw new ArgumentNullException(nameof(level));
            Difficulty = difficulty != null ? difficulty : throw new ArgumentNullException(nameof(difficulty));
        }

        public LevelDefinition Level { get; }

        public DifficultyProfile Difficulty { get; }

        /// <summary>Whether the level opens with its story briefing; the campaign flow asks for it, tests and tools do not.</summary>
        public bool Briefing { get; }

        /// <summary>The street weapons the player takes along, or null to offer whatever the level lists (tests and tools).</summary>
        public IReadOnlyList<PathObjectDefinition> Loadout { get; }
    }
}
