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
        // The back light comes in low from the far side of the field, so it only touches the edges of shapes.
        private const float RimPitchDegrees = 28f;
        private const float RimYawOffsetDegrees = 180f;
        // Ambient comes from three directions: the sky colour from above, fog at the horizon and near black below,
        // so the underside of every shape stays darker than its top even in shadow.
        private const float AmbientSkyBrightness = 1f;
        private const float AmbientEquatorShare = 0.5f;
        private const float AmbientGroundBrightness = 0.5f;

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
            Dictionary<int, GameObject> routes = BuildEnvironment(level);
            BuildGameplay(level, session.Difficulty, waves, debug, routes, session.Briefing, session.Loadout);
        }

        private static Dictionary<int, GameObject> BuildEnvironment(LevelDefinition level)
        {
            VisualTheme theme = level.Theme;
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = theme.LightColor;
            light.intensity = theme.LightIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1f;
            sun.transform.rotation = Quaternion.Euler(theme.SunPitch, theme.SunYaw, 0f);

            var rimObject = new GameObject("RimLight");
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = theme.RimColor;
            rim.intensity = theme.RimIntensity;
            rim.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(RimPitchDegrees, theme.SunYaw + RimYawOffsetDegrees, 0f);

            RenderSettings.fog = theme.FogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = theme.FogColor;
            RenderSettings.fogDensity = theme.FogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.SkyColor * AmbientSkyBrightness;
            RenderSettings.ambientEquatorColor = Color.Lerp(theme.FogColor, theme.LightColor, AmbientEquatorShare) * AmbientSkyBrightness;
            RenderSettings.ambientGroundColor = theme.FogColor * AmbientGroundBrightness;
            SkyProbe.Apply(theme);

            var visuals = new GameObject("LevelVisuals").transform;
            IPlatformSettings platform = Services.Get<IPlatformSettings>();
            return LevelVisualBuilder.Build(level, visuals, platform.ParticleScale, platform.FogEnabled);
        }

        private void BuildGameplay(LevelDefinition level, DifficultyProfile difficulty, WaveDefinition[] waves, PlaytestSettings debug, Dictionary<int, GameObject> routes, bool briefing, IReadOnlyList<Game.PathObjects.PathObjectDefinition> loadout)
        {
            bool unlockAll = debug != null && debug.UnlockAllWeapons;
            bool infiniteMoney = debug != null && debug.InfiniteMoney;
            // Calling the next wave early is part of the game, not a test switch.
            const bool skipAllowed = true;

            ProgressService progress = Services.Get<ProgressService>();
            var upgrades = Services.Get<UpgradeCatalog>();
            PlayerModifiers global = UpgradeEffects.ComputeGlobal(progress, upgrades);
            var towerModifiers = new Dictionary<string, PlayerModifiers>();
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                towerModifiers[tower.Id] = UpgradeEffects.ComputeForTower(progress, upgrades, tower.Id);
            }

            // Blockades are not bought, so they only take the global upgrades (blockade strength).
            foreach (BlockadeSpot spot in level.Blockades)
            {
                towerModifiers[spot.Blockade.Id] = global;
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
            Services.TryGet(out VfxLibrary vfx);
            var projectiles = new ProjectilePool(targets, root, vfx, Services.Get<IPlatformSettings>().ParticleScale);
            var wallet = new Wallet(level.StartingMoney, infiniteMoney);
            var baseHealth = new Health(level.BaseHealth + global.BaseHealthBonus);
            var context = new LevelContext(level, difficulty, wallet, baseHealth, targets, structures, projectiles, level.AvailableTowers, towers, towerModifiers, waves.Length, skipAllowed);
            context.BriefingPending = briefing && level.Briefing.Length > 0;
            context.GlobalModifiers = global;
            foreach (Game.Drones.DroneDefinition drone in new[] { level.RepairDrone, level.HarvestDrone, level.AttackDrone })
            {
                if (drone != null)
                {
                    context.SetDroneModifiers(drone.name, UpgradeEffects.ComputeForDrone(progress, upgrades, drone.name));
                }
            }

            // The base stands on the field as a blocking structure, so the enemies that reach it besiege it.
            Vector2Int[] mainRoad = level.MainRoad;
            var baseStructure = new BaseStructure(level.Road.CentreOf(mainRoad[mainRoad.Length - 1]), baseHealth);
            structures.Add(baseStructure);
            context.Base = baseStructure;
            Services.Register(context);

            IInputProvider input = Services.Get<IInputProvider>();
            var panZoom = _cameraRig.gameObject.AddComponent<CameraPanZoom>();
            panZoom.Initialize(context, _cameraRig, level, input);
            context.ReconPending = briefing;
            if (loadout != null)
            {
                context.SetLoadout(loadout);
            }

            var drones = new GameObject("Drones").AddComponent<DroneController>();
            drones.Initialize(context, root);
            var spawner = new EnemySpawner(context, root);
            var slime = new GameObject("Slime");
            slime.transform.SetParent(root, false);
            slime.AddComponent<SlimeFieldView>().Initialize(spawner.Slime);

            var controllers = new GameObject("Controllers");
            controllers.AddComponent<RouteVisibility>().Initialize(context, routes);
            var levelController = controllers.AddComponent<LevelController>();
            levelController.Initialize(context, waves, spawner, input, progress, skipAllowed);
            var placement = controllers.AddComponent<PlacementController>();
            placement.Initialize(context, input, _camera, root);
            controllers.AddComponent<HeroController>().Initialize(context, input, placement.Blockades, root);
            if (context.Hero != null)
            {
                panZoom.SetView(context.Hero.transform.position, panZoom.PlayZoom);
            }

            _cameraRig.gameObject.AddComponent<CameraShaker>().Initialize(_cameraRig, context);
            // Ripples read the opaque texture, which only the high profile renders.
            if (Services.Get<IPlatformSettings>().BloomEnabled)
            {
                controllers.AddComponent<RippleEffects>().Initialize(context);
            }

            var time = controllers.AddComponent<TimeController>();
            time.Initialize(context, input);
            controllers.AddComponent<ReconView>().Initialize(context, panZoom, time, input, routes);
            var mix = Services.Get<AudioMix>();
            controllers.AddComponent<SfxPlayer>().Initialize(context, mix, _cameraRig, spawner.Living, drones.Drones);
            controllers.AddComponent<MusicPlayer>().Initialize(context, mix);
            FindAnyObjectByType<BaseView>()?.Bind(context);
            var hud = controllers.AddComponent<HudView>();
            hud.Initialize(context, time, _camera);
        }
    }
}







