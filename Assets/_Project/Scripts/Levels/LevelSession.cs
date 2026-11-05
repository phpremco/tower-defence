using System;
using System.Collections.Generic;
using Game.PathObjects;

namespace Game.Levels
{
    /// <summary>Which level, on which difficulty, the generic Level scene must build.</summary>
    public sealed class LevelSession
    {
        // Keeps the streams of neighbouring seeds apart.
        private const int StreamSpread = 7919;

        /// <summary>The draws of the spawner (nest choice, lateral spread, pool cells).</summary>
        public const int SpawnStream = 1;

        /// <summary>The draws of the wave flow (nest assignment, group jitter).</summary>
        public const int WaveStream = 2;

        public LevelSession(LevelDefinition level, DifficultyProfile difficulty, bool briefing = false, IReadOnlyList<PathObjectDefinition> loadout = null, WaveDefinition[] waves = null, int startingMoney = -1, int? seed = null)
        {
            Seed = seed;
            Waves = waves;
            StartingMoney = startingMoney;
            Briefing = briefing;
            Loadout = loadout;
            Level = level != null ? level : throw new ArgumentNullException(nameof(level));
            Difficulty = difficulty != null ? difficulty : throw new ArgumentNullException(nameof(difficulty));
        }

        public LevelDefinition Level { get; }

        public DifficultyProfile Difficulty { get; }

        /// <summary>Whether the level opens with its story briefing; the campaign flow asks for it, tests and tools do not.</summary>
        public bool Briefing { get; }

        /// <summary>
        /// Plays the boss intros and barks even without the story briefing (a tool or test that wants them; the campaign
        /// flow has them with its story anyway).
        /// </summary>
        public bool BossIntros { get; set; }

        /// <summary>The street weapons the player takes along, or null to offer whatever the level lists (tests and tools).</summary>
        public IReadOnlyList<PathObjectDefinition> Loadout { get; }

        /// <summary>Waves that replace the level's own (the developer boss arena), or null for the level's waves.</summary>
        public WaveDefinition[] Waves { get; }

        /// <summary>A practice run (the boss arena): it pays nothing and saves no progress.</summary>
        public bool Practice => Waves != null;

        /// <summary>Money to start with instead of the level's, or below zero for the level's own.</summary>
        public int StartingMoney { get; }

        /// <summary>
        /// The seed of the level's draws (which nest a creature comes out of, the spacing of a group), or null for a fresh
        /// draw every run. A balance probe seeds it so two runs of the same build see the same waves.
        /// </summary>
        public int? Seed { get; }

        /// <summary>The generator for one stream of the level's draws: seeded from <see cref="Seed"/> when there is one.</summary>
        public Random RandomFor(int stream) => Seed.HasValue ? new Random(unchecked(Seed.Value * StreamSpread + stream)) : new Random();
    }
}
