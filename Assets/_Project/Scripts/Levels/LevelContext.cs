using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Hero;
using Game.Towers;

namespace Game.Levels
{
    /// <summary>
    /// Shared state of the level being played. Controllers write to it and raise events; the HUD reads it.
    /// Gameplay never references the HUD.
    /// </summary>
    public sealed class LevelContext
    {
        private readonly Dictionary<string, PlayerModifiers> _towerModifiers;

        public LevelContext(
            LevelDefinition level,
            DifficultyProfile difficulty,
            Wallet wallet,
            Health baseHealth,
            TargetRegistry targets,
            StructureRegistry structures,
            ProjectilePool projectiles,
            IReadOnlyList<TowerDefinition> allTowers,
            IReadOnlyList<TowerDefinition> availableTowers,
            Dictionary<string, PlayerModifiers> towerModifiers,
            int waveCount,
            bool skipWaveAllowed)
        {
            Level = level;
            Difficulty = difficulty;
            Wallet = wallet;
            BaseHealth = baseHealth;
            Targets = targets;
            Structures = structures;
            Projectiles = projectiles;
            AllTowers = allTowers;
            AvailableTowers = availableTowers;
            _towerModifiers = towerModifiers;
            WaveCount = waveCount;
            SkipWaveAllowed = skipWaveAllowed;
        }

        public LevelDefinition Level { get; }
        public DifficultyProfile Difficulty { get; }
        public Wallet Wallet { get; }
        public Health BaseHealth { get; }
        public TargetRegistry Targets { get; }
        public StructureRegistry Structures { get; }
        public ProjectilePool Projectiles { get; }

        /// <summary>Every tower this level offers, including ones the player has not unlocked yet.</summary>
        public IReadOnlyList<TowerDefinition> AllTowers { get; }

        /// <summary>The towers the player may build right now.</summary>
        public IReadOnlyList<TowerDefinition> AvailableTowers { get; }

        public int WaveCount { get; }

        /// <summary>The test mode lets the player skip to the next wave.</summary>
        public bool SkipWaveAllowed { get; }

        /// <summary>Number of the wave that started most recently, zero before the first wave.</summary>
        public int WaveNumber { get; set; }

        public LevelOutcome Outcome { get; set; }

        /// <summary>Set once the level has ended.</summary>
        public LevelResult Result { get; set; }

        public bool Paused { get; set; }

        public TowerDefinition SelectedTower { get; set; }

        public PlacementMode Mode { get; set; }

        /// <summary>The placed tower whose action icons are open, or null.</summary>
        public Tower SelectedPlacedTower { get; set; }

        /// <summary>Short message about the current placement, such as why a spot is not allowed. Empty when idle.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Seconds until the next wave begins, or zero while a wave is spawning.</summary>
        public float NextWaveCountdown { get; set; }

        /// <summary>Set by the HUD every frame so a click on a HUD icon does not also act on the field.</summary>
        public bool PointerOverUi { get; set; }

        /// <summary>Raised whenever something the HUD shows has changed.</summary>
        public GameEvent Changed { get; } = new GameEvent();

        /// <summary>Indices of the flying paths the current or upcoming wave will use. Only these routes are drawn.</summary>
        public HashSet<int> ActiveRoutes { get; } = new HashSet<int>();

        public GameEvent RoutesChanged { get; } = new GameEvent();

        public GameEvent UpgradeRequested { get; } = new GameEvent();
        public GameEvent RepairRequested { get; } = new GameEvent();
        public GameEvent SellRequested { get; } = new GameEvent();
        public GameEvent SkipWaveRequested { get; } = new GameEvent();

        /// <summary>The commander, or null when the level has none.</summary>
        public Commander Hero { get; set; }

        /// <summary>Display names of the hero's posts, in the order the hero indexes them.</summary>
        public string[] HeroPostNames { get; set; } = new string[0];

        /// <summary>Asks the hero to take up the post of the placed tower that is currently selected.</summary>
        public GameEvent HeroSendRequested { get; } = new GameEvent();

        public PlayerModifiers ModifiersFor(TowerDefinition tower)
        {
            return _towerModifiers.TryGetValue(tower.Id, out PlayerModifiers modifiers) ? modifiers : PlayerModifiers.None;
        }

        public bool IsUnlocked(TowerDefinition tower)
        {
            for (int i = 0; i < AvailableTowers.Count; i++)
            {
                if (AvailableTowers[i] == tower)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Selects a tower to build. Ignored for towers that are not unlocked.</summary>
        public void SelectTower(TowerDefinition tower)
        {
            if (tower != null && IsUnlocked(tower) && tower != SelectedTower)
            {
                SelectedTower = tower;
                Changed.Raise();
            }
        }
    }
}

