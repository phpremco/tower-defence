using System.Collections.Generic;
using System.Linq;
using Game.Boot;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using Game.Towers;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    /// <summary>
    /// Generates the data assets and scenes for the first playable level. Safe to run repeatedly:
    /// existing assets are reused (and their values refreshed) and scenes are rebuilt in place.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string InputAssetPath = "Assets/_Project/Settings/Input/GameInput.inputactions";
        private const string UiFolder = "Assets/_Project/Art/UI/";
        private const string SkinAssetPath = "Assets/_Project/Settings/UiSkin.asset";
        private const string DebugAssetPath = "Assets/_Project/Settings/PlaytestSettings.asset";
        private const string UnlockAssetPath = DataRoot + "/Unlocks/UnlockTable.asset";

        // Money a mining node holds; with three nodes a level, the harvest drones earn a bounded sum, not a stream.
        private const int MineStock = 140;

        // A downed hero is gone long enough to hurt: two minutes, shown as a countdown in the HUD.
        private const float HeroReviveSeconds = 120f;

        // The base is besieged like a gate, so it needs gate-sized health: a few gates' worth, grown by the armour upgrade.
        private const int BaseHealth = 600;
        private const float BaseHealthUpgradeStep = 100f;

        // The camera looks down a little less steeply and from a slight angle, so gates, towers and creatures show a
        // side as well as a top and read as things with height instead of symbols on a map.
        private const float CameraPitchDegrees = 44f;
        private const float CameraYawDegrees = 16f;

        // A battered tower still fires at three quarters, and mending a point of health costs less than a third of a coin,
        // so damage is a dent in the defence rather than its collapse.
        private const float DamagedPowerFloor = 0.75f;
        private const float RepairCostPerHealth = 0.3f;

        // An aimed shot whose creature died in flight turns to a live one this close (cells): Docs/BALANCE.md, "Roles by enemy".
        private const float ShotRetargetRadius = 1.5f;

        // Light guns (Gun Turret, Laser) aim past plates that would stop more than half of their hit (round 21a2).
        internal const float LightGunSoftShare = 0.5f;

        /// <summary>Runs every registered generator step (see <see cref="Steps"/>), then saves the assets.</summary>
        [MenuItem("Tools/Generate/Scenes and Starter Data")]
        public static void GenerateAll()
        {
            RunSteps();
            AssetDatabase.SaveAssets();
            Debug.Log("Generated the campaign data and the Boot and Level scenes.");
        }

        // The script's level 1 brings the Gun and the Cannon: they are the starting set, every later weapon is a reward.
        private static void GenerateUnlocks(GeneratorContext context)
        {
            var unlocks = LoadOrCreate<UnlockTable>(UnlockAssetPath);
            var unlockProps = new SerializedObject(unlocks);
            SerializedProperty ids = unlockProps.FindProperty("_startingTowerIds");
            string[] starting = { context.Pulse.Id, context.Cannon.Id };
            ids.arraySize = starting.Length;
            for (int i = 0; i < starting.Length; i++)
            {
                ids.GetArrayElementAtIndex(i).stringValue = starting[i];
            }

            unlockProps.ApplyModifiedPropertiesWithoutUndo();
            context.Unlocks = unlocks;
        }

        private static HeroWeaponDefinition CreateHeroWeapon(string id, string displayName, HeroWeaponKind kind, HeroWeaponStyle style, float damage, float reach, float interval, TargetLayers targets, Color color)
        {
            var weapon = LoadOrCreate<HeroWeaponDefinition>($"{DataRoot}/Hero/Weapon_{id}.asset");
            var so = new SerializedObject(weapon);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").intValue = (int)kind;
            so.FindProperty("_style").intValue = (int)style;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_reach").floatValue = reach;
            so.FindProperty("_attackInterval").floatValue = interval;
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        private static TowerLevel Lvl(int cost, float damage, float range, float interval, float healthBonus = 0f, float slowFactor = 0f, float slowSeconds = 0f) =>
            new TowerLevel { Cost = cost, Damage = damage, Range = range, FireInterval = interval, HealthBonus = healthBonus, SlowFactor = slowFactor, SlowSeconds = slowSeconds };

        private static void SetShape(TowerDefinition tower, TowerShape shape)
        {
            var so = new SerializedObject(tower);
            so.FindProperty("_shape").enumValueIndex = (int)shape;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TowerDefinition CreateTower(string id, string displayName, TargetLayers targets, float projectileSpeed, float splash, Color color, TowerLevel[] levels, float health, bool blocksGround = false, float slowFactor = 1f, float slowSeconds = 0f, bool support = false, float rangeBonus = 0f, float rateBonus = 0f, bool baseDefence = false, float airDamageFactor = 1f, float softTargetShare = 0f)
        {
            var tower = LoadOrCreate<TowerDefinition>($"{DataRoot}/Towers/{id}.asset");
            var so = new SerializedObject(tower);
            so.FindProperty("_blocksGroundEnemies").boolValue = blocksGround;
            so.FindProperty("_isBaseDefence").boolValue = baseDefence;
            so.FindProperty("_slowFactor").floatValue = slowFactor;
            so.FindProperty("_slowSeconds").floatValue = slowSeconds;
            so.FindProperty("_isSupport").boolValue = support;
            so.FindProperty("_supportRangeBonus").floatValue = rangeBonus;
            so.FindProperty("_supportRateBonus").floatValue = rateBonus;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("_splashRadius").floatValue = splash;
            so.FindProperty("_groundDamageFactor").floatValue = 1f;
            so.FindProperty("_airDamageFactor").floatValue = airDamageFactor;
            so.FindProperty("_retargetRadius").floatValue = ShotRetargetRadius;
            so.FindProperty("_softTargetShare").floatValue = softTargetShare;
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_damagedPowerFloor").floatValue = DamagedPowerFloor;
            so.FindProperty("_repairCostPerHealth").floatValue = RepairCostPerHealth;
            SerializedProperty array = so.FindProperty("_levels");
            array.arraySize = levels.Length;
            // The balance sheet prices the guns it lists; the others keep the price given here.
            int[] prices = BalanceSheet.Prices(id);
            for (int i = 0; i < levels.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cost").intValue = prices != null && i < prices.Length ? prices[i] : levels[i].Cost;
                element.FindPropertyRelative("Damage").floatValue = levels[i].Damage;
                element.FindPropertyRelative("Range").floatValue = levels[i].Range;
                element.FindPropertyRelative("FireInterval").floatValue = levels[i].FireInterval;
                element.FindPropertyRelative("HealthBonus").floatValue = levels[i].HealthBonus;
                element.FindPropertyRelative("SlowFactor").floatValue = levels[i].SlowFactor;
                element.FindPropertyRelative("SlowSeconds").floatValue = levels[i].SlowSeconds;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return tower;
        }

        private static DifficultyProfile CreateDifficulty(BalanceSheet.Difficulty sheet)
        {
            var profile = LoadOrCreate<DifficultyProfile>($"{DataRoot}/Difficulty/{sheet.Id}.asset");
            var so = new SerializedObject(profile);
            so.FindProperty("_displayName").stringValue = sheet.DisplayName;
            so.FindProperty("_healthMultiplier").floatValue = sheet.Health;
            so.FindProperty("_countMultiplier").floatValue = sheet.Count;
            so.FindProperty("_spawnIntervalMultiplier").floatValue = sheet.Interval;
            so.FindProperty("_rewardMultiplier").floatValue = sheet.Research;
            so.FindProperty("_powerCurrencyMultiplier").floatValue = sheet.Power;
            so.FindProperty("_structureDamageMultiplier").floatValue = sheet.StructureDamage;
            so.FindProperty("_incomeMultiplier").floatValue = sheet.Income;
            so.FindProperty("_startingMoneyMultiplier").floatValue = sheet.StartMoney;
            so.FindProperty("_bossHealthShare").floatValue = sheet.BossHealth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static UiSkin CreateSkin()
        {
            var skin = LoadOrCreate<UiSkin>(SkinAssetPath);
            var so = new SerializedObject(skin);
            so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(UiFolder + "Fonts/KenneyFuture.ttf");
            so.FindProperty("_narrowFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(UiFolder + "Fonts/KenneyFutureNarrow.ttf");

            so.ApplyModifiedPropertiesWithoutUndo();
            // Panels, buttons, bars, cards, icons and the dialogue art are painted in Blender (README round 25).
            BlenderUiBuilder.ApplyToSkin(skin, BlenderUiBuilder.SyncAndBuild());
            return skin;
        }

        // A grading tint shifts hue only: scaled so its brightest channel is one, it never darkens the picture.
        private static Color Hue(Color tint)
        {
            float peak = Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b), 0.001f);
            return new Color(tint.r / peak, tint.g / peak, tint.b / peak, 1f);
        }

        private static void ConfigureThemePalette(VisualTheme theme, Palette p)
        {
            var so = new SerializedObject(theme);
            so.FindProperty("_backgroundColor").colorValue = p.Background;
            so.FindProperty("_groundColor").colorValue = p.Ground;
            so.FindProperty("_pathColor").colorValue = p.Path;
            so.FindProperty("_slotColor").colorValue = p.Slot;
            so.FindProperty("_baseColor").colorValue = p.Base;
            so.FindProperty("_lightColor").colorValue = p.Light;
            so.FindProperty("_lightIntensity").floatValue = p.LightIntensity;
            so.FindProperty("_fogColor").colorValue = p.Fog;
            so.FindProperty("_fogDensity").floatValue = p.FogDensity;
            so.FindProperty("_bloomIntensity").floatValue = p.Bloom;
            so.FindProperty("_sunPitch").floatValue = p.SunPitch > 0f ? p.SunPitch : 42f;
            so.FindProperty("_sunYaw").floatValue = p.SunYaw != 0f ? p.SunYaw : -35f;
            so.FindProperty("_skyColor").colorValue = p.Sky == default ? Color.Lerp(p.Fog, p.Light, 0.6f) : p.Sky;
            so.FindProperty("_rimColor").colorValue = p.Rim == default ? p.Accent : p.Rim;
            so.FindProperty("_contrast").floatValue = p.Contrast != 0f ? p.Contrast : 12f;
            so.FindProperty("_saturation").floatValue = p.Saturation != 0f ? p.Saturation : 8f;
            so.FindProperty("_shadowTint").colorValue = Hue(p.ShadowTint == default ? Color.Lerp(Color.white, p.Fog, 0.5f) : p.ShadowTint);
            so.FindProperty("_highlightTint").colorValue = Hue(p.HighlightTint == default ? Color.Lerp(Color.white, p.Light, 0.4f) : p.HighlightTint);
            so.FindProperty("_rockColor").colorValue = p.Rock;
            so.FindProperty("_accentColor").colorValue = p.Accent;
            so.FindProperty("_waterColor").colorValue = p.Water;
            // Older palettes name no ice or bog: they keep a sensible default so a stray patch still draws.
            so.FindProperty("_iceColor").colorValue = p.Ice == default ? new Color(0.75f, 0.9f, 1f) : p.Ice;
            so.FindProperty("_bogColor").colorValue = p.Bog == default ? new Color(0.18f, 0.24f, 0.12f) : p.Bog;
            so.FindProperty("_riftColor").colorValue = p.Rift == default ? new Color(1f, 0.58f, 0.25f) : p.Rift;
            so.FindProperty("_spawnColor").colorValue = p.Spawn == default ? new Color(1f, 0.4f, 0.25f) : p.Spawn;
            so.FindProperty("_lavaColor").colorValue = p.Lava;
            so.FindProperty("_sandColor").colorValue = p.Sand;
            so.FindProperty("_veinStrength").floatValue = p.Vein;
            so.FindProperty("_ambientRate").floatValue = p.AmbientRate;
            so.FindProperty("_ambientRise").floatValue = p.AmbientRise;
            so.FindProperty("_ambience").enumValueIndex = (int)p.Ambience;
            so.FindProperty("_plantColor").colorValue = p.Plant;
            SerializedProperty props = so.FindProperty("_propKinds");
            props.arraySize = p.Props.Length;
            for (int i = 0; i < p.Props.Length; i++)
            {
                props.GetArrayElementAtIndex(i).enumValueIndex = (int)p.Props[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WritePaths(SerializedProperty array, (string Name, bool Flying, Vector2Int[] Waypoints)[] paths)
        {
            array.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Name").stringValue = paths[i].Name;
                element.FindPropertyRelative("Flying").boolValue = paths[i].Flying;
                WriteCells(element.FindPropertyRelative("Waypoints"), paths[i].Waypoints);
            }
        }

        private static void WriteCells(SerializedProperty array, Vector2Int[] cells)
        {
            array.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++)
            {
                array.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private const float HybridUpgradeCostScale = 1.6f;

        private static void AddTowerUpgrades(List<UpgradeDefinition> all, TowerDefinition tower, float costScale)
        {
            all.Add(CreateUpgrade($"{tower.Id}_Damage", "Damage", UpgradeTarget.TowerDamage, Scaled(new[] { 5, 10, 15 }, costScale), 0.1f, tower.Id, tower.DisplayName));
            all.Add(CreateUpgrade($"{tower.Id}_Range", "Range", UpgradeTarget.TowerRange, Scaled(new[] { 5, 10, 15 }, costScale), 0.05f, tower.Id, tower.DisplayName));
            all.Add(CreateUpgrade($"{tower.Id}_FireRate", "Fire rate", UpgradeTarget.TowerFireRate, Scaled(new[] { 6, 12, 18 }, costScale), 0.1f, tower.Id, tower.DisplayName));
        }

        private static int[] Scaled(int[] costs, float scale)
        {
            var result = new int[costs.Length];
            for (int i = 0; i < costs.Length; i++)
            {
                result[i] = Mathf.RoundToInt(costs[i] * scale);
            }

            return result;
        }

        // The codex lists every recipe and every creature the generator knows, in the order the bestiary names them.
        private static CodexCatalog CreateCodex(CombineRecipe[] recipes, Bestiary bestiary)
        {
            var codex = LoadOrCreate<CodexCatalog>($"{DataRoot}/Levels/CodexCatalog.asset");
            var so = new SerializedObject(codex);
            SetReferences(so.FindProperty("_recipes"), recipes);
            var enemies = new List<EnemyDefinition>();
            foreach (System.Reflection.FieldInfo field in typeof(Bestiary).GetFields())
            {
                if (field.FieldType == typeof(EnemyDefinition) && field.GetValue(bestiary) is EnemyDefinition enemy)
                {
                    enemies.Add(enemy);
                }
            }

            SetReferences(so.FindProperty("_enemies"), enemies.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(codex);
            return codex;
        }

        private static UpgradeCatalog CreateUpgrades(TowerDefinition[] towers, TowerDefinition[] hybrids, DroneDefinition[] drones)
        {
            // Display names stay short: the upgrade screen shows the value before and after next to them.
            var all = new List<UpgradeDefinition>();
            foreach (TowerDefinition tower in towers)
            {
                AddTowerUpgrades(all, tower, 1f);
            }

            // A hybrid is worth two towers, so its permanent upgrades cost more research.
            foreach (TowerDefinition hybrid in hybrids)
            {
                AddTowerUpgrades(all, hybrid, HybridUpgradeCostScale);
            }

            const string BaseText = "The base and the gates that hold the roads";
            all.Add(CreateUpgrade("BaseHealth", "Base armour", UpgradeTarget.BaseHealth, new[] { 4, 8, 12 }, BaseHealthUpgradeStep, string.Empty, "Base", UpgradeCurrency.ResearchPoints, BaseText));
            all.Add(CreateUpgrade("Base_Weapon", "Base turrets", UpgradeTarget.BaseWeapon, new[] { 12, 20, 32 }, 1f, string.Empty, "Base", UpgradeCurrency.ResearchPoints, BaseText));
            all.Add(CreateUpgrade("BlockadeHealth", "Gate strength", UpgradeTarget.BlockadeHealth, new[] { 4, 8, 12 }, 0.2f, string.Empty, "Base", UpgradeCurrency.ResearchPoints, BaseText));

            const string HeroText = "Mercer, who holds the gates";
            all.Add(CreateUpgrade("Hero_Damage", "Blade damage", UpgradeTarget.HeroDamage, new[] { 5, 10, 15 }, 0.15f, string.Empty, "Mercer", UpgradeCurrency.ResearchPoints, HeroText));
            all.Add(CreateUpgrade("Hero_Health", "Armour", UpgradeTarget.HeroHealth, new[] { 5, 10, 15 }, 0.2f, string.Empty, "Mercer", UpgradeCurrency.ResearchPoints, HeroText));

            // Drones are bought with money inside a level; each type has its own permanent path, paid in power currency.
            foreach (DroneDefinition drone in drones)
            {
                string id = drone.name;
                string group = drone.DisplayName;
                all.Add(CreateUpgrade($"{id}_Speed", "Flight speed", UpgradeTarget.DroneSpeed, new[] { 3, 6, 9 }, 0.2f, string.Empty, group, UpgradeCurrency.PowerCurrency, DroneText(drone), id));
                switch (drone.Kind)
                {
                    case DroneKind.Repair:
                        all.Add(CreateUpgrade($"{id}_Output", "Repair rate", UpgradeTarget.DroneOutput, new[] { 4, 8, 12 }, 0.25f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        all.Add(CreateUpgrade($"{id}_Armour", "Armour", UpgradeTarget.DroneArmour, new[] { 3, 6, 9 }, 0.3f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        break;
                    case DroneKind.Harvest:
                        all.Add(CreateUpgrade($"{id}_Output", "Yield per trip", UpgradeTarget.DroneOutput, new[] { 4, 8, 12 }, 0.25f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        all.Add(CreateUpgrade($"{id}_Count", "Extra drone", UpgradeTarget.DroneCount, new[] { 6, 12 }, 1f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        break;
                    default:
                        all.Add(CreateUpgrade($"{id}_Output", "Damage", UpgradeTarget.DroneOutput, new[] { 4, 8, 12 }, 0.15f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        all.Add(CreateUpgrade($"{id}_Tempo", "Fire rate", UpgradeTarget.DroneTempo, new[] { 4, 8, 12 }, 0.15f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        break;
                }
            }

            foreach (UpgradeDefinition upgrade in all)
            {
                TagContinent(upgrade);
            }

            var catalog = LoadOrCreate<UpgradeCatalog>($"{DataRoot}/Upgrades/UpgradeCatalog.asset");
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_upgrades");
            list.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }
        // A tower's upgrades belong to the first continent whose roster has the tower; towers out of the campaign (the
        // hybrids, Cryo, Support) stay dormant (-1). The base, gates, commander and drones are paid by the first continent.
        private static void TagContinent(UpgradeDefinition upgrade)
        {
            int continent = string.IsNullOrEmpty(upgrade.TowerId) ? 0 : LevelSpecs.ContinentOfTower(upgrade.TowerId);
            var so = new SerializedObject(upgrade);
            so.FindProperty("_continent").intValue = continent;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);
        }

        private static string DroneText(DroneDefinition drone)
        {
            switch (drone.Kind)
            {
                case DroneKind.Repair:
                    return "Bought with money in the level. Flies to the most damaged tower, gate or base and repairs it.";
                case DroneKind.Harvest:
                    return "Bought with money in the level. Mines the ore nodes and carries the money home.";
                default:
                    return "Bought with money in the level. Hunts the enemy furthest down the road and shoots it from the air.";
            }
        }

        private static UpgradeDefinition CreateUpgrade(string id, string displayName, UpgradeTarget target, int[] costs, float bonus, string towerId, string groupName, UpgradeCurrency currency = UpgradeCurrency.ResearchPoints, string description = "", string droneId = "")
        {
            var upgrade = LoadOrCreate<UpgradeDefinition>($"{DataRoot}/Upgrades/{id}.asset");
            var so = new SerializedObject(upgrade);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_droneId").stringValue = droneId;
            so.FindProperty("_target").enumValueIndex = (int)target;
            so.FindProperty("_bonusPerLevel").floatValue = bonus;
            so.FindProperty("_towerId").stringValue = towerId;
            so.FindProperty("_groupName").stringValue = groupName;
            so.FindProperty("_currency").enumValueIndex = (int)currency;
            so.FindProperty("_groupDescription").stringValue = description;
            SerializedProperty array = so.FindProperty("_costs");
            array.arraySize = costs.Length;
            for (int i = 0; i < costs.Length; i++)
            {
                array.GetArrayElementAtIndex(i).intValue = costs[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return upgrade;
        }

        private const string KeeperFolder = "Assets/_Project/Art/Materials/ShaderKeepers";

        // Materials saved with the keyword combinations the runtime materials use, so a player build keeps those
        // shader variants. They are referenced by the Boot scene and never drawn.
        private static string[] CreateShaderKeepers()
        {
            EnsureFolder(KeeperFolder);
            return new[]
            {
                KeeperMaterial("Lit_EmissionNormal", false, "_EMISSION", "_NORMALMAP"),
                KeeperMaterial("Lit_EmissionNormalDetail", false, "_EMISSION", "_NORMALMAP", "_DETAIL_MULX2"),
                KeeperMaterial("Lit_Transparent", true, "_EMISSION", "_SURFACE_TYPE_TRANSPARENT"),
                KeeperMaterial("Lit_TransparentFlat", true, "_EMISSION", "_SURFACE_TYPE_TRANSPARENT", "_SPECULARHIGHLIGHTS_OFF"),
                KeeperMaterial("Lit_Planet", false, "_EMISSION", "_NORMALMAP", "_METALLICSPECGLOSSMAP", "_DETAIL_MULX2"),
                KeeperShader("ImpactRipple", "Game/ImpactRipple"),
                KeeperShader("Fireball", "Game/Fireball"),
                KeeperShader("Planet", Game.UI.PlanetMaterials.SurfaceShader),
                KeeperShader("PlanetClouds", Game.UI.PlanetMaterials.CloudShader),
                KeeperShader("PlanetAtmosphere", Game.UI.PlanetMaterials.AtmosphereShader),
                KeeperShader("SpaceSky", Game.UI.SpaceBackdrop.SkyShader),
                KeeperShader("TiltShift", TiltShiftShaderName),
                KeeperShader("UnitLit", Game.Core.SurfaceMaterials.UnitShaderName),
                KeeperShader("UnitLit_Emission", Game.Core.SurfaceMaterials.UnitShaderName, "_EMISSION"),
                KeeperShader("UnitLit_Normal", Game.Core.SurfaceMaterials.UnitShaderName, "_NORMALMAP"),
                KeeperShader("UnitLit_EmissionNormal", Game.Core.SurfaceMaterials.UnitShaderName, "_EMISSION", "_NORMALMAP"),
            };
        }

        // A material on a hand-written shader, so the build keeps the shader the ripples find by name at runtime.
        private static string KeeperShader(string name, string shaderName, params string[] keywords)
        {
            string path = $"{KeeperFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            foreach (string keyword in keywords)
            {
                material.EnableKeyword(keyword);
            }

            EditorUtility.SetDirty(material);
            return path;
        }

        private static string KeeperMaterial(string name, bool transparent, params string[] keywords)
        {
            string path = $"{KeeperFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
                AssetDatabase.CreateAsset(material, path);
            }

            foreach (string keyword in keywords)
            {
                material.EnableKeyword(keyword);
            }

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(material);
            return path;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static void CreateBootScene(CampaignCatalog campaign, UpgradeCatalog upgrades, CodexCatalog codex, UnlockTable unlocks, PlaytestSettings playtest, UiSkin skin, string[] keeperPaths)
        {
            // NewScene unloads assets nobody references yet, so remember the paths and load them again afterwards.
            string campaignPath = AssetDatabase.GetAssetPath(campaign);
            string upgradesPath = AssetDatabase.GetAssetPath(upgrades);
            string codexPath = AssetDatabase.GetAssetPath(codex);
            string unlocksPath = AssetDatabase.GetAssetPath(unlocks);
            string playtestPath = AssetDatabase.GetAssetPath(playtest);
            string skinPath = AssetDatabase.GetAssetPath(skin);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrapper");
            var bootstrapper = go.AddComponent<Bootstrapper>();
            var so = new SerializedObject(bootstrapper);
            so.FindProperty("_inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            so.FindProperty("_campaign").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(campaignPath);
            so.FindProperty("_upgrades").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(upgradesPath);
            so.FindProperty("_codex").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CodexCatalog>(codexPath);
            so.FindProperty("_unlockTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnlockTable>(unlocksPath);
            so.FindProperty("_playtestSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlaytestSettings>(playtestPath);
            so.FindProperty("_sfxLibrary").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SfxLibrary>(SfxLibraryPath);
            so.FindProperty("_vfxLibrary").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Game.Combat.VfxLibrary>(VfxLibraryPath);
            so.FindProperty("_tiltShift").objectReferenceValue = _tiltShift;
            so.FindProperty("_uiSkin").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UiSkin>(skinPath);
            SerializedProperty keepers = so.FindProperty("_shaderKeepers");
            keepers.arraySize = keeperPaths.Length;
            for (int i = 0; i < keeperPaths.Length; i++)
            {
                keepers.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(keeperPaths[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.Boot}.unity");
        }

        private static void CreateMenuScene<T>(string sceneName) where T : Component
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{sceneName}.unity");
        }

        private const string MenuStageAssetPath = DataRoot + "/Menu/MainMenuStage.asset";

        // The menu scene shows the themes of the world levels and a handful of its creatures.
        private static void CreateMenuStage(LevelDefinition[] worldLevels, Bestiary bestiary)
        {
            var stage = LoadOrCreate<MenuStageDefinition>(MenuStageAssetPath);
            var so = new SerializedObject(stage);
            so.FindProperty("_forestTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Forest);
            so.FindProperty("_lavaTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Lava);
            so.FindProperty("_ruinsTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Ruins);
            so.FindProperty("_nestTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Hive);
            so.FindProperty("_iceTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Blizzard);
            so.FindProperty("_swampTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Marsh);
            SerializedProperty walkers = so.FindProperty("_nestCreatures");
            EnemyDefinition[] crawlers = { bestiary.Skitter, bestiary.Prowler, bestiary.Roller, bestiary.Stinger };
            walkers.arraySize = crawlers.Length;
            for (int i = 0; i < crawlers.Length; i++)
            {
                walkers.GetArrayElementAtIndex(i).objectReferenceValue = crawlers[i];
            }

            so.FindProperty("_nestGuardian").objectReferenceValue = bestiary.Queen;
            so.FindProperty("_flyer").objectReferenceValue = bestiary.Skyray;
            so.FindProperty("_waterCreature").objectReferenceValue = bestiary.Tidebat;
            so.FindProperty("_lavaCreature").objectReferenceValue = bestiary.Emberbat;
            so.FindProperty("_sandCreature").objectReferenceValue = bestiary.Dustbat;
            so.FindProperty("_iceCreature").objectReferenceValue = bestiary.Frostbat;
            so.FindProperty("_bogCreature").objectReferenceValue = bestiary.Mirebat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static VisualTheme ThemeWith(LevelDefinition[] levels, AmbienceKind ambience)
        {
            foreach (LevelDefinition level in levels)
            {
                if (level.Theme != null && level.Theme.Ambience == ambience)
                {
                    return level.Theme;
                }
            }

            return levels[0].Theme;
        }

        // Loaded by path: the earlier NewScene calls unload assets nothing references yet, which breaks a held reference.
        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(nameof(MainMenuScreen)).AddComponent<MainMenuScreen>();
            var stageGo = new GameObject(nameof(MenuStage));
            var menuStage = stageGo.AddComponent<MenuStage>();
            var so = new SerializedObject(menuStage);
            so.FindProperty("_stage").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MenuStageDefinition>(MenuStageAssetPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.MainMenu}.unity");
        }        private static void CreateLevelScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            var rig = cameraGo.AddComponent<FixedCameraRig>();

            var root = new GameObject("LevelBootstrapper");
            var bootstrapper = root.AddComponent<LevelBootstrapper>();
            var so = new SerializedObject(bootstrapper);
            so.FindProperty("_cameraRig").objectReferenceValue = rig;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.Level}.unity");
        }
    }
}










