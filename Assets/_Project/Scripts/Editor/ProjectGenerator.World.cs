using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Drones;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for the shared world content: the tower roster, path objects, drones, recipes, and the terrain writers the campaign levels use.</summary>
    public static partial class ProjectGenerator
    {
        private struct Palette
        {
            public Color Background;
            public Color Ground;
            public Color Path;
            public Color Slot;
            public Color Base;
            public Color Light;
            public float LightIntensity;
            public Color Fog;
            public float FogDensity;
            public float Bloom;
            public Color Rock;
            public Color Accent;
            public Color Water;
            public Color Lava;
            public Color Sand;
            public Color Ice;
            public Color Bog;
            public Color Rift;
            public Color Spawn;
            public PropKind[] Props;
            public float Vein;
            public float AmbientRate;
            public float AmbientRise;
            public AmbienceKind Ambience;
            public Color Plant;
            // Light rig and grading; left at default they are derived from the light, fog and accent colours.
            public Color Sky;
            public Color Rim;
            public float SunPitch;
            public float SunYaw;
            public float Contrast;
            public float Saturation;
            public Color ShadowTint;
            public Color HighlightTint;
        }

        private sealed class Roster
        {
            public TowerDefinition Pulse;
            public TowerDefinition Cannon;
            public TowerDefinition Laser;
            public TowerDefinition Cryo;
            public TowerDefinition Rocket;
            public TowerDefinition Tesla;
            public TowerDefinition Railgun;
            public TowerDefinition Support;
            public TowerDefinition Artillery;
            public TowerDefinition StormRifle;
            public TowerDefinition CryoBeam;

            public TowerDefinition[] Standard => new[] { Pulse, Cannon, Laser, Cryo, Rocket, Tesla, Railgun, Support };
            public TowerDefinition[] Hybrids => new[] { Artillery, StormRifle, CryoBeam };
        }

        // A beam pulse lands the frame it is fired (it crosses any tower's reach in one frame), so the beam knows at once
        // that its creature died and never spends a second pulse on it.
        private const float BeamSpeed = 400f;

        // How soon the laser's beam is on the next creature after a kill: three frames at 60 fps.
        private const float LaserSweepSeconds = 0.05f;

        private static void SetBeam(TowerDefinition tower, float sweepSeconds)
        {
            var so = new SerializedObject(tower);
            so.FindProperty("_beamSweepSeconds").floatValue = sweepSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Roster CreateRoster(TowerDefinition pulse, TowerDefinition cannon, TowerDefinition rocket)
        {
            var roster = new Roster { Pulse = pulse, Cannon = cannon, Rocket = rocket };

            // The swarm gun: fast light pulses on the ground. The beam lands at once (hitscan speed), holds its creature and
            // sweeps on to the next one right after a kill (2026-11-05, Docs/BALANCE.md "Laser and gun"), so small creatures
            // die at the rate the beam can swing, not at its pulse rate; on a big creature it is a plain 40 DPS.
            // Cyan like the model's lens (curator decision 2026-11-04: our colours follow the models; warm is for the aliens).
            roster.Laser = CreateTower("Laser", "Laser Turret", TargetLayers.Ground, BeamSpeed, 0f, new Color(0.35f, 0.9f, 1f),
                new[] { Lvl(110, 8f, LightGunReach + 0.3f, 0.2f), Lvl(130, 12f, LightGunReach + 0.5f, 0.18f), Lvl(190, 18f, LightGunReach + 0.8f, 0.15f) }, 160f);
            SetBeam(roster.Laser, LaserSweepSeconds);

            roster.Cryo = CreateTower("Cryo", "Cryo", TargetLayers.Ground | TargetLayers.Air, 12f, 0f, new Color(0.55f, 0.95f, 1f),
                new[] { Lvl(90, 2f, 3.7f, 0.5f, 0f, 0.5f, 2f), Lvl(110, 2f, 4f, 0.45f, 0f, 0.42f, 3f), Lvl(160, 2f, 4.3f, 0.4f, 0f, 0.35f, 4f) }, 140f, slowFactor: 0.5f, slowSeconds: 2f);

            // Lightning is approximated by a small splash, so it works well on groups.
            roster.Tesla = CreateTower("Tesla", "Tesla", TargetLayers.Ground, 40f, 1.3f, new Color(0.7f, 0.5f, 1f),
                new[] { Lvl(140, 16f, 3.5f, 0.5f), Lvl(160, 26f, 3.7f, 0.45f), Lvl(230, 40f, 4f, 0.4f) }, 160f);

            roster.Railgun = CreateTower("Railgun", "Railgun", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(1f, 1f, 1f),
                new[] { Lvl(220, 80f, 6f, 2.2f), Lvl(260, 120f, 6.5f, 2f), Lvl(360, 180f, 7f, 1.8f) }, 180f);

            // Signal yellow like the model's halos (was green).
            roster.Support = CreateTower("Support", "Support", TargetLayers.Ground, 1f, 0f, new Color(1f, 0.74f, 0.2f),
                new[] { Lvl(120, 0f, 3f, 1f), Lvl(140, 0f, 3.5f, 1f), Lvl(200, 0f, 4f, 1f) }, 140f, support: true, rangeBonus: 0.15f, rateBonus: 0.2f);

            // Hybrids come from recipes, so their first level costs nothing to build: the recipe has the price. Every
            // level beats the two parents at that level put together (by 15 to 50 percent), in one cell, and the
            // upgrades cost close to what upgrading both parents would.
            // The Artillery glows amber like its model (was orange).
            roster.Artillery = CreateTower("Artillery", "Artillery", TargetLayers.Ground | TargetLayers.Air, 8f, 1.8f, new Color(1f, 0.78f, 0.3f),
                new[] { Lvl(0, 70f, 5.1f, 1.6f), Lvl(220, 130f, 5.5f, 1.4f), Lvl(320, 220f, 5.9f, 1.2f) }, 240f);

            roster.StormRifle = CreateTower("StormRifle", "Storm Rifle", TargetLayers.Ground | TargetLayers.Air, 20f, 0.8f, new Color(0.6f, 0.8f, 1f),
                new[] { Lvl(0, 14f, 4.1f, 0.2f), Lvl(200, 20f, 4.4f, 0.18f), Lvl(300, 28f, 4.7f, 0.15f) }, 190f);

            roster.CryoBeam = CreateTower("CryoBeam", "Cryo Beam", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(0.7f, 1f, 1f),
                new[] { Lvl(0, 8f, 4.3f, 0.15f, 0f, 0.4f, 2.5f), Lvl(210, 13f, 4.6f, 0.13f, 0f, 0.33f, 3.5f), Lvl(310, 20f, 4.9f, 0.11f, 0f, 0.27f, 4.5f) }, 180f, slowFactor: 0.4f, slowSeconds: 2.5f);

            return roster;
        }

        // A lava ball burns what it rolls over: the Frost Warden's ice armour cracks under it (Docs/BOSSES.md).
        internal static void MarkBurning(PathObjectDefinition asset)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("_burns").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static PathObjectDefinition CreatePathObject(
            string id,
            string displayName,
            PathObjectKind kind,
            PathObjectPlacement placement,
            TargetLayers targets,
            int cost,
            float radius,
            float triggerRadius,
            float damage,
            float slowFactor,
            float speed,
            float lifetime,
            Color color,
            PathObjectEffect effect,
            string description)
        {
            var asset = LoadOrCreate<PathObjectDefinition>($"{DataRoot}/PathObjects/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_effect").enumValueIndex = (int)effect;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_placement").enumValueIndex = (int)placement;
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_cost").intValue = cost;
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_triggerRadius").floatValue = triggerRadius;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_slowFactor").floatValue = slowFactor;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_lifetime").floatValue = lifetime;
            so.FindProperty("_color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static DroneDefinition CreateDrone(string id, string displayName, DroneKind kind, float speed, float health, int cost, int maxActive, Color color, float repair = 0f, float harvestSeconds = 4f, int harvestValue = 0, float damage = 0f, float fireInterval = 0.5f, float range = 3f, CreatureSkin.Kind skin = CreatureSkin.Kind.Plate, Color accent = default)
        {
            var asset = LoadOrCreate<DroneDefinition>($"{DataRoot}/Drones/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_cost").intValue = cost;
            so.FindProperty("_maxActive").intValue = maxActive;
            so.FindProperty("_repairPerSecond").floatValue = repair;
            so.FindProperty("_harvestSeconds").floatValue = harvestSeconds;
            so.FindProperty("_harvestValue").intValue = harvestValue;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_fireInterval").floatValue = fireInterval;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_targets").intValue = (int)(TargetLayers.Ground | TargetLayers.Air);
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_skin").enumValueIndex = (int)skin;
            so.FindProperty("_accentColor").colorValue = accent == default ? Color.white : accent;
            so.ApplyModifiedPropertiesWithoutUndo();
            // A value equal to the field default leaves the asset clean, so force the save or a stale field lingers on disk.
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CombineRecipe CreateRecipe(string id, TowerDefinition first, TowerDefinition second, TowerDefinition result, int cost)
        {
            var asset = LoadOrCreate<CombineRecipe>($"{DataRoot}/Recipes/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_first").objectReferenceValue = first;
            so.FindProperty("_second").objectReferenceValue = second;
            so.FindProperty("_result").objectReferenceValue = result;
            so.FindProperty("_cost").intValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        // Path objects and recipes are shared by all levels; each level only chooses which it sells.
        private static void ConfigureLevelExtras(LevelDefinition level, PathObjectDefinition[] pathObjects, CombineRecipe[] recipes)
        {
            var so = new SerializedObject(level);
            SetReferences(so.FindProperty("_pathObjects"), pathObjects);
            SetReferences(so.FindProperty("_recipes"), recipes);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A drone kind is offered from the level whose spec introduces it on, in campaign order, and never taken away again
        // (Remco 2026-11-05: no drone before its introduction). A kind not introduced yet leaves its slot empty.
        private static void ConfigureLevelDrones(LevelDefinition level, string key, GeneratorContext context)
        {
            var offered = new HashSet<string>();
            foreach (CampaignLevelSpec[] continent in LevelSpecs.Continents)
            {
                foreach (CampaignLevelSpec spec in continent)
                {
                    offered.UnionWith(spec.IntroduceDrones);
                    if (spec.Key == key)
                    {
                        WriteDrones(level, offered, context);
                        return;
                    }
                }
            }

            throw new System.InvalidOperationException($"No campaign spec with key {key}.");
        }

        private static void WriteDrones(LevelDefinition level, HashSet<string> offered, GeneratorContext context)
        {
            foreach (DroneDefinition drone in context.Drones)
            {
                if (drone == null)
                {
                    throw new System.InvalidOperationException("The drones must be generated before the campaign levels.");
                }
            }

            foreach (string id in offered)
            {
                if (System.Array.Find(context.Drones, d => d.name == id) == null)
                {
                    throw new System.InvalidOperationException($"A level spec introduces drone '{id}', which does not exist.");
                }
            }

            var so = new SerializedObject(level);
            so.FindProperty("_repairDrone").objectReferenceValue = offered.Contains(context.RepairDrone.name) ? context.RepairDrone : null;
            so.FindProperty("_harvestDrone").objectReferenceValue = offered.Contains(context.HarvestDrone.name) ? context.HarvestDrone : null;
            so.FindProperty("_attackDrone").objectReferenceValue = offered.Contains(context.AttackDrone.name) ? context.AttackDrone : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferences(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void GeneratePathObjects(GeneratorContext context)
        {
            context.SlowField = CreatePathObject("SlowField", "Chrono Field", PathObjectKind.SlowField, PathObjectPlacement.Road, TargetLayers.Ground, 45, 1.6f, 0.5f, 0f, 0.45f, 0f, 20f, new Color(0.45f, 0.75f, 1f), PathObjectEffect.Slow, "A Crysium field laid across the road. Time runs slow inside it: everything that walks through moves at less than half speed, for as long as the field lasts. The answer to fast runners.");
            // Amber like the model's armed ring (curator decision 2026-11-04: our colours follow the models).
            context.Mine = CreatePathObject("Mine", "Land Mine", PathObjectKind.Mine, PathObjectPlacement.Road, TargetLayers.Ground, 35, 1.4f, 0.6f, 140f, 1f, 0f, 0f, new Color(1f, 0.72f, 0.25f), PathObjectEffect.Trap, "Buried in the road. Goes off once, under the first enemy that steps on it, and hurts everything close by.");
            context.LavaBall = CreatePathObject("LavaBall", "Lava ball", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 70, 0.9f, 0.5f, 55f, 1f, 2.2f, 0f, new Color(1f, 0.45f, 0.1f), PathObjectEffect.Roll, "Dropped on the road, it rolls on towards the base and burns every walker it touches along the way.");
            MarkBurning(context.LavaBall);
            context.Barrel = CreatePathObject("ExplosiveBarrel", "Barrel", PathObjectKind.Mine, PathObjectPlacement.Beside, TargetLayers.Ground, 50, 2f, 1.1f, 200f, 1f, 0f, 0f, new Color(0.9f, 0.5f, 0.1f), PathObjectEffect.Blast, "Stands beside the road. When the enemy passes close it bursts, and the blast reaches far across the road.");
            context.GroundDrone = CreatePathObject("GroundDrone", "Ground drone", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 80, 1.1f, 0.5f, 30f, 1f, 1.1f, 0f, new Color(0.4f, 0.9f, 1f), PathObjectEffect.Patrol, "Drives down the road at a steady pace and rams every walker it meets, dealing damage as long as it runs.");
        }

        // Three drones, one per job, bought with money in the level, so a drone competes with a tower for the same
        // purse: a harvester earns its price back in a few minutes of trips.
        private static void GenerateDrones(GeneratorContext context)
        {
            // The repair drone's price follows the investment rule (BalanceSheet.RepairDronePrice, round 22d) when the level data exists.
            context.RepairDrone = CreateDrone("RepairDrone", "Repair drone", DroneKind.Repair, 3.5f, 40f, BalanceSheet.RepairDronePriceFromData(8f, 120), 3, new Color(0.4f, 0.9f, 1f), repair: 8f, skin: CreatureSkin.Kind.Plate, accent: new Color(1f, 0.22f, 0.28f));
            context.HarvestDrone = CreateDrone("HarvestDrone", "Harvest drone", DroneKind.Harvest, 4f, 30f, 150, 2, new Color(1f, 0.85f, 0.3f), harvestSeconds: 4f, harvestValue: 10, skin: CreatureSkin.Kind.Alloy, accent: new Color(1f, 0.75f, 0.1f));
            // Azure like the model's tail glow (curator decision 2026-11-04: our colours follow the models).
            context.AttackDrone = CreateDrone("AttackDrone", "Attack drone", DroneKind.Attack, 4.5f, 35f, 180, 3, new Color(0.3f, 0.7f, 1f), damage: 6f, fireInterval: 0.5f, range: 3f, skin: CreatureSkin.Kind.Plate, accent: new Color(0.92f, 0.95f, 1f));
        }

        private static void GenerateRecipes(GeneratorContext context)
        {
            Roster roster = context.Roster;
            context.Recipes = new[]
            {
                CreateRecipe("Artillery", roster.Cannon, roster.Rocket, roster.Artillery, 150),
                CreateRecipe("StormRifle", roster.Pulse, roster.Tesla, roster.StormRifle, 120),
                CreateRecipe("CryoBeam", roster.Laser, roster.Cryo, roster.CryoBeam, 140),
            };
        }

        // Rectangles look like building plots. Cells on the outline are dropped now and then (the same ones every
        // run), which turns them into rough natural shapes; interior cells are never touched.
        private static TerrainPatch[] Erode(TerrainPatch[] patches)
        {
            var byCell = patches.GroupBy(p => p.Cell).ToDictionary(g => g.Key, g => g.Last().Kind);
            var kept = new List<TerrainPatch>();
            var random = new System.Random(7);
            Vector2Int[] steps = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

            foreach (var pair in byCell)
            {
                bool outline = steps.Any(s => !byCell.TryGetValue(pair.Key + s, out TerrainKind other) || other != pair.Value);
                if (outline && random.NextDouble() < ErodeChance)
                {
                    continue;
                }

                kept.Add(new TerrainPatch { Cell = pair.Key, Kind = pair.Value });
            }

            return kept.ToArray();
        }

        private const double ErodeChance = 0.4;

        // Terrain never covers the road, and liquid keeps clear of the blockades where the hero stands.
        private static TerrainPatch[] WriteTerrain(SerializedProperty array, TerrainPatch[] patches, HashSet<Vector2Int> road, Vector2Int[] blockades)
        {
            patches = Erode(patches);
            TerrainPatch[] kept = patches
                .Where(p => !road.Contains(p.Cell))
                .Where(p => !TerrainMap.IsLiquid(p.Kind) || !blockades.Any(b => Mathf.Abs(b.x - p.Cell.x) <= 1 && Mathf.Abs(b.y - p.Cell.y) <= 1))
                .GroupBy(p => p.Cell)
                .Select(g => g.Last())
                .ToArray();

            array.arraySize = kept.Length;
            for (int i = 0; i < kept.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cell").vector2IntValue = kept[i].Cell;
                element.FindPropertyRelative("Kind").enumValueIndex = (int)kept[i].Kind;
            }

            return kept;
        }
    }
}
