using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Towers;

namespace Game.Levels
{
    /// <summary>
    /// Shared state of the level being played. Controllers write to it and raise events; the HUD reads it.
    /// Gameplay never references the HUD.
    /// </summary>
    public sealed class LevelContext
    {
        public LevelContext(
            LevelDefinition level,
            DifficultyProfile difficulty,
            Wallet wallet,
            Health baseHealth,
            TargetRegistry targets,
            ProjectilePool projectiles,
            IReadOnlyList<TowerDefinition> availableTowers,
            PlayerModifiers modifiers,
            int waveCount)
        {
            Level = level;
            Difficulty = difficulty;
            Wallet = wallet;
            BaseHealth = baseHealth;
            Targets = targets;
            Projectiles = projectiles;
            AvailableTowers = availableTowers;
            Modifiers = modifiers;
            WaveCount = waveCount;
        }

        public LevelDefinition Level { get; }
        public DifficultyProfile Difficulty { get; }
        public Wallet Wallet { get; }
        public Health BaseHealth { get; }
        public TargetRegistry Targets { get; }
        public ProjectilePool Projectiles { get; }
        public IReadOnlyList<TowerDefinition> AvailableTowers { get; }
        public PlayerModifiers Modifiers { get; }
        public int WaveCount { get; }

        /// <summary>Number of the wave that started most recently, zero before the first wave.</summary>
        public int WaveNumber { get; set; }

        public LevelOutcome Outcome { get; set; }

        /// <summary>Set once the level has ended.</summary>
        public LevelResult Result { get; set; }

        public TowerDefinition SelectedTower { get; set; }

        /// <summary>Tower under the cursor, or null when the cursor is on an empty slot.</summary>
        public Tower HoveredTower { get; set; }

        /// <summary>Seconds until the next wave begins, or zero while a wave is spawning.</summary>
        public float NextWaveCountdown { get; set; }

        /// <summary>Raised whenever something the HUD shows has changed.</summary>
        public GameEvent Changed { get; } = new GameEvent();
    }
}


