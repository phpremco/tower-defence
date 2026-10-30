using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Enemies;
using Game.PathObjects;
using Game.Hero;
using Game.Towers;
using UnityEngine;

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
            AvailablePathObjects = level.PathObjects;
        }

        public LevelDefinition Level { get; }
        public DifficultyProfile Difficulty { get; }
        public Wallet Wallet { get; }
        public Health BaseHealth { get; }

        /// <summary>The base as a structure on the field: it blocks the end of the road and takes the hits.</summary>
        public BaseStructure Base { get; set; }
        public TargetRegistry Targets { get; }
        public StructureRegistry Structures { get; }
        public ProjectilePool Projectiles { get; }

        /// <summary>Every tower this level offers, including ones the player has not unlocked yet.</summary>
        public IReadOnlyList<TowerDefinition> AllTowers { get; }

        /// <summary>The towers the player may build right now.</summary>
        public IReadOnlyList<TowerDefinition> AvailableTowers { get; }

        /// <summary>Permanent upgrades that do not belong to one tower: base, blockades and hero.</summary>
        public PlayerModifiers GlobalModifiers { get; set; } = PlayerModifiers.None;

        public int WaveCount { get; }

        /// <summary>The test mode lets the player skip to the next wave.</summary>
        public bool SkipWaveAllowed { get; }

        /// <summary>Number of the wave that started most recently, zero before the first wave.</summary>
        public int WaveNumber { get; set; }

        public LevelOutcome Outcome { get; set; }

        /// <summary>Set once the level has ended.</summary>
        public LevelResult Result { get; set; }

        public bool Paused { get; set; }

        /// <summary>True while the level waits for the player to read the briefing and deploy.</summary>
        public bool BriefingPending { get; set; }

        /// <summary>True while the recon fly-over before the first wave is still to be shown.</summary>
        public bool ReconPending { get; set; }

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

        /// <summary>Raised whenever an enemy is defeated, for audio and effects.</summary>
        public GameEvent EnemyDefeated { get; } = new GameEvent();

        /// <summary>The commander, or null when the level has none.</summary>
        public Commander Hero { get; set; }

        /// <summary>Display names of the hero's posts, in the order the hero indexes them.</summary>
        public string[] HeroPostNames { get; set; } = new string[0];

        /// <summary>Asks the hero to take up the post of the placed tower that is currently selected.</summary>
        public GameEvent HeroSendRequested { get; } = new GameEvent();

        /// <summary>Asks the camera to glide to a point on the ground, as when the hero is sent to a post.</summary>
        public GameEvent<Vector3> CameraFocusRequested { get; } = new GameEvent<Vector3>();

        /// <summary>Raised with the enemy that was just defeated, while its position and definition are still valid.</summary>
        public GameEvent<Enemy> EnemyKilled { get; } = new GameEvent<Enemy>();

        /// <summary>Raised with every enemy as it enters the field.</summary>
        public GameEvent<Enemy> EnemySpawned { get; } = new GameEvent<Enemy>();

        /// <summary>Raised with the place an enemy hit a tower, blockade or the base.</summary>
        public GameEvent<Vector3> StructureHit { get; } = new GameEvent<Vector3>();

        /// <summary>Raised with the index of a mining node once the drones have emptied it.</summary>
        public GameEvent<int> MineDepleted { get; } = new GameEvent<int>();

        /// <summary>Raised with the position of a tower or gate that has just been destroyed.</summary>
        public GameEvent<Vector3> StructureWrecked { get; } = new GameEvent<Vector3>();

        /// <summary>Raised when a wave begins spawning.</summary>
        public GameEvent WaveStarted { get; } = new GameEvent();

        /// <summary>True while the wave that started last carries a boss.</summary>
        public bool BossWave { get; set; }

        /// <summary>Raised when a wave with a boss in it begins, before the boss itself appears.</summary>
        public GameEvent BossWaveStarted { get; } = new GameEvent();

        /// <summary>How many bosses are on the field right now.</summary>
        public int BossesAlive { get; set; }

        /// <summary>Raised with the centre of a boss shockwave, for the camera and audio.</summary>
        public GameEvent<Vector3> BossPulsed { get; } = new GameEvent<Vector3>();

        /// <summary>Raised when a boss laser switches on (true) or off (false).</summary>
        public GameEvent<bool> BossLaserChanged { get; } = new GameEvent<bool>();

        /// <summary>Raised with the boss position when it releases minions.</summary>
        public GameEvent<Vector3> BossMinionsReleased { get; } = new GameEvent<Vector3>();

        /// <summary>Raised with the launch point whenever a boss hurls a volley of bolts.</summary>
        public GameEvent<Vector3> BossVolleyed { get; } = new GameEvent<Vector3>();

        /// <summary>Raised with the boss position whenever it rallies the field.</summary>
        public GameEvent<Vector3> BossRallied { get; } = new GameEvent<Vector3>();

        /// <summary>Confirmations of the player's actions on the field, for audio and effects.</summary>
        public GameEvent TowerBuilt { get; } = new GameEvent();
        public GameEvent TowerUpgraded { get; } = new GameEvent();
        public GameEvent TowerRepaired { get; } = new GameEvent();
        public GameEvent TowerSold { get; } = new GameEvent();
        public GameEvent PathObjectPlaced { get; } = new GameEvent();

        /// <summary>The HUD asks for a drone of the given type; the drone controller buys it if energy and room allow.</summary>
        public GameEvent<DroneDefinition> DroneRequested { get; } = new GameEvent<DroneDefinition>();

        private readonly Dictionary<DroneDefinition, int> _activeDrones = new Dictionary<DroneDefinition, int>();

        public int ActiveDrones(DroneDefinition definition) => definition != null && _activeDrones.TryGetValue(definition, out int count) ? count : 0;

        public void SetActiveDrones(DroneDefinition definition, int count) => _activeDrones[definition] = count;

        /// <summary>Raised when the number of flying drones changes, so the HUD can grey out a full type.</summary>
        public GameEvent DronesChanged { get; } = new GameEvent();

        /// <summary>The path object that is chosen for placing, or null when towers are being placed.</summary>
        public PathObjectDefinition SelectedPathObject { get; set; }

        /// <summary>Path objects the level sells, in the order of the bar.</summary>
        public IReadOnlyList<PathObjectDefinition> AvailablePathObjects { get; private set; }

        /// <summary>Narrows the street weapons on offer to the loadout the player chose for this level.</summary>
        public void SetLoadout(IReadOnlyList<PathObjectDefinition> loadout) => AvailablePathObjects = loadout;

        /// <summary>The recipe the selected placed tower can use with a neighbour right now, or null.</summary>
        public CombineRecipe CombineOffer { get; set; }

        /// <summary>Asks to combine the selected placed tower with its neighbour.</summary>
        public GameEvent CombineRequested { get; } = new GameEvent();

        /// <summary>Asks for the hero's emergency repair, which costs money.</summary>
        public GameEvent HeroRepairRequested { get; } = new GameEvent();

        /// <summary>The grid cell where the hero stands for each post, in the same order as the post names.</summary>
        public Vector2Int[] HeroPostCells { get; set; } = new Vector2Int[0];

        /// <summary>The post whose spot the player clicked, or -1. Opens the hero panel or the "move here" icon.</summary>
        public int HeroMenuPost { get; set; } = -1;

        /// <summary>True while the drone panel has the gamepad focus: confirming on the base doorstep gives it, cancel takes it.</summary>
        public bool BaseMenuOpen { get; set; }

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
        /// <summary>Raised when the player picks something to build, so the preview appears at once.</summary>
        public GameEvent SelectionChanged { get; } = new GameEvent();

        // Picking a card starts placing right away: the preview follows the cursor until it is confirmed or cancelled.
        public void SelectTower(TowerDefinition tower)
        {
            if (tower == null || !IsUnlocked(tower))
            {
                return;
            }

            SelectedTower = tower;
            SelectedPathObject = null;
            StartPlacing();
        }

        /// <summary>Selects a path object to place instead of a tower.</summary>
        public void SelectPathObject(PathObjectDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            SelectedPathObject = definition;
            StartPlacing();
        }

        private void StartPlacing()
        {
            if (Mode == PlacementMode.Browsing)
            {
                Mode = PlacementMode.Placing;
            }

            SelectionChanged.Raise();
            Changed.Raise();
        }
    }
}

