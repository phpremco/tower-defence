using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using Game.Platform;
using Game.Towers;
using Game.UI;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// Composition root of the generic Level scene: builds the level from the LevelDefinition in the LevelSession
    /// and wires the controllers. Which tower is available, and on which difficulty, comes entirely from data.
    /// </summary>
    public sealed class LevelBootstrapper : MonoBehaviour
    {
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

            WaveDefinition[] waves = session.Waves ?? level.GetWaves(session.Difficulty);
            if (waves == null)
            {
                Debug.LogError($"Level '{level.name}' has no waves for difficulty '{session.Difficulty.name}'.", level);
                return;
            }

            Services.TryGet(out PlaytestSettings debug);
            _cameraRig.Apply(level);
            PostProcessingBuilder.Build(_camera, level.Theme, Services.Get<IPlatformSettings>());
            Dictionary<int, GameObject> routes = BuildEnvironment(level);
            BuildGameplay(level, session.Difficulty, waves, debug, routes, session.Briefing, session.Loadout, session.StartingMoney);
        }

        private static Dictionary<int, GameObject> BuildEnvironment(LevelDefinition level)
        {
            ThemeLighting.Apply(level.Theme);
            var visuals = new GameObject("LevelVisuals").transform;
            IPlatformSettings platform = Services.Get<IPlatformSettings>();
            return LevelVisualBuilder.Build(level, visuals, platform.ParticleScale, platform.FogEnabled);
        }

        // The perks the hero can earn that the player has unlocked (the debug switch unlocks them all, like the weapons).
        private static List<HeroPerkDefinition> UnlockedPerks(HeroDefinition hero, ProgressService progress, bool unlockAll)
        {
            var perks = new List<HeroPerkDefinition>();
            if (hero == null)
            {
                return perks;
            }

            foreach (HeroPerkDefinition perk in hero.Perks)
            {
                if (perk != null && (unlockAll || progress.IsHeroPerkUnlocked(perk.Id)))
                {
                    perks.Add(perk);
                }
            }

            return perks;
        }

        private void BuildGameplay(LevelDefinition level, DifficultyProfile difficulty, WaveDefinition[] waves, PlaytestSettings debug, Dictionary<int, GameObject> routes, bool briefing, IReadOnlyList<Game.PathObjects.PathObjectDefinition> loadout, int startingMoney)
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
            IPlatformSettings platformSettings = Services.Get<IPlatformSettings>();
            var quality = new VfxQuality(platformSettings.ParticleScale, platformSettings.LowDetailModels, platformSettings.DistortionEnabled);
            var projectiles = new ProjectilePool(targets, root, vfx, quality, _camera);
            int money = startingMoney >= 0 ? startingMoney : Mathf.RoundToInt(level.StartingMoney * difficulty.StartingMoneyMultiplier);
            var wallet = new Wallet(money, infiniteMoney, difficulty.IncomeMultiplier);
            var baseHealth = new Health(level.BaseHealth + global.BaseHealthBonus);
            var context = new LevelContext(level, difficulty, wallet, baseHealth, targets, structures, projectiles, level.AvailableTowers, towers, towerModifiers, waves.Length, skipAllowed);
            context.BriefingPending = briefing && level.Briefing.Length > 0;
            LevelSession session = Services.Get<LevelSession>();
            context.Practice = session.Practice;
            context.BossIntros = briefing || session.BossIntros;
            context.GlobalModifiers = global;
            context.HeroPerks = UnlockedPerks(level.Hero, progress, unlockAll);
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
            var spawner = new EnemySpawner(context, root, session.RandomFor(LevelSession.SpawnStream));
            var slime = new GameObject("Slime");
            slime.transform.SetParent(root, false);
            slime.AddComponent<SlimeFieldView>().Initialize(spawner.Slime);

            var controllers = new GameObject("Controllers");
            // The continent bosses' brains, before the level controller sends the first creature out.
            controllers.AddComponent<Game.Bosses.BossDirector>().Initialize(context, spawner, progress, waves);
            controllers.AddComponent<RouteVisibility>().Initialize(context, routes);
            var levelController = controllers.AddComponent<LevelController>();
            levelController.Initialize(context, waves, spawner, input, progress, skipAllowed, session.RandomFor(LevelSession.WaveStream));
            var placement = controllers.AddComponent<PlacementController>();
            placement.Initialize(context, input, _camera, root);
            controllers.AddComponent<HeroController>().Initialize(context, input, placement.Blockades, root);
            if (context.Hero != null)
            {
                panZoom.SetView(context.Hero.transform.position, panZoom.PlayZoom);
            }

            _cameraRig.gameObject.AddComponent<CameraShaker>().Initialize(_cameraRig, context);
            // Ripples read the opaque texture, which only the high profile renders.
            if (Services.Get<IPlatformSettings>().DistortionEnabled)
            {
                controllers.AddComponent<RippleEffects>().Initialize(context);
            }

            var time = controllers.AddComponent<TimeController>();
            time.Initialize(context, input);
            controllers.AddComponent<ReconView>().Initialize(context, panZoom, time, input, routes);
            // Before the HUD: the story must see the level end before the result screen does.
            Services.TryGet(out CampaignCatalog campaign);
            StoryDefinition story = campaign != null ? campaign.Story : null;
            controllers.AddComponent<LevelStoryListener>().Initialize(context, time, input, progress, briefing, story != null ? story.ScanHints : null);
            var mix = Services.Get<AudioMix>();
            controllers.AddComponent<SfxPlayer>().Initialize(context, mix, _cameraRig, spawner.Living, drones.Drones);
            controllers.AddComponent<MusicPlayer>().Initialize(context, mix);
            FindAnyObjectByType<BaseView>()?.Bind(context);
            // The spawn origins follow the director the level controller made; the route gates follow the gate spawns.
            foreach (SpawnOriginView origin in FindObjectsByType<SpawnOriginView>())
            {
                origin.Bind(context);
            }

            foreach (SpawnGateView gate in FindObjectsByType<SpawnGateView>())
            {
                gate.Bind(context);
            }

            var hud = controllers.AddComponent<HudView>();
            hud.Initialize(context, time, _camera);
        }
    }
}







