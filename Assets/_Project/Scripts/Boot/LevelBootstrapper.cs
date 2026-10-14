using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using Game.Towers;
using Game.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Boot
{
    /// <summary>
    /// Composition root of the generic Level scene: builds the level from the LevelDefinition in the LevelSession
    /// and wires the controllers. Which tower is available, and on which difficulty, comes entirely from data.
    /// </summary>
    public sealed class LevelBootstrapper : MonoBehaviour
    {
        private static readonly Vector3 SunRotation = new Vector3(50f, -30f, 0f);

        [SerializeField] private FixedCameraRig _cameraRig;
        [SerializeField] private Camera _camera;

        private void Start()
        {
            if (!Services.TryGet(out LevelSession session))
            {
                Debug.LogError("No LevelSession registered. Start the game from the Boot scene.", this);
                return;
            }

            LevelDefinition level = session.Level;
            var errors = new List<string>();
            if (!LevelValidator.Validate(level, errors))
            {
                Debug.LogError($"Level '{level.name}' is invalid:\n{string.Join("\n", errors)}", level);
                return;
            }

            WaveDefinition[] waves = level.GetWaves(session.Difficulty);
            if (waves == null)
            {
                Debug.LogError($"Level '{level.name}' has no waves for difficulty '{session.Difficulty.name}'.", level);
                return;
            }

            Services.TryGet(out PlaytestSettings debug);
            _cameraRig.Apply(level);
            PostProcessingBuilder.Build(_camera, level.Theme, Services.Get<IPlatformSettings>());
            BuildEnvironment(level);
            BuildGameplay(level, session.Difficulty, waves, debug);
        }

        private static void BuildEnvironment(LevelDefinition level)
        {
            VisualTheme theme = level.Theme;
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = theme.LightColor;
            light.intensity = theme.LightIntensity;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(SunRotation);

            RenderSettings.fog = theme.FogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = theme.FogColor;
            RenderSettings.fogDensity = theme.FogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = theme.FogColor;

            var visuals = new GameObject("LevelVisuals").transform;
            LevelVisualBuilder.Build(level, visuals);
        }

        private void BuildGameplay(LevelDefinition level, DifficultyProfile difficulty, WaveDefinition[] waves, PlaytestSettings debug)
        {
            bool unlockAll = debug != null && debug.UnlockAllWeapons;
            bool infiniteMoney = debug != null && debug.InfiniteMoney;
            bool skipAllowed = debug != null && debug.AllowSkipWave;

            ProgressService progress = Services.Get<ProgressService>();
            var upgrades = Services.Get<UpgradeCatalog>();
            PlayerModifiers global = UpgradeEffects.ComputeGlobal(progress, upgrades);
            var towerModifiers = new Dictionary<string, PlayerModifiers>();
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                towerModifiers[tower.Id] = UpgradeEffects.ComputeForTower(progress, upgrades, tower.Id);
            }

            var towers = new List<TowerDefinition>();
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (unlockAll || progress.IsTowerUnlocked(tower.Id))
                {
                    towers.Add(tower);
                }
            }

            var root = new GameObject("Gameplay").transform;
            var targets = new TargetRegistry();
            var structures = new StructureRegistry();
            var projectiles = new ProjectilePool(targets, root);
            var wallet = new Wallet(level.StartingMoney, infiniteMoney);
            var baseHealth = new Health(level.BaseHealth + global.BaseHealthBonus);
            var context = new LevelContext(level, difficulty, wallet, baseHealth, targets, structures, projectiles, level.AvailableTowers, towers, towerModifiers, waves.Length, skipAllowed);
            Services.Register(context);

            IInputProvider input = Services.Get<IInputProvider>();
            var spawner = new EnemySpawner(context, root);

            var controllers = new GameObject("Controllers");
            var levelController = controllers.AddComponent<LevelController>();
            levelController.Initialize(context, waves, spawner, input, progress, skipAllowed);
            var placement = controllers.AddComponent<PlacementController>();
            placement.Initialize(context, input, _camera, root);
            var time = controllers.AddComponent<TimeController>();
            time.Initialize(context, input);
            var hud = controllers.AddComponent<HudView>();
            hud.Initialize(context, time, _camera);
        }
    }
}





