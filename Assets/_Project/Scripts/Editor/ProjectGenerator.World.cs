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

        // How soon the laser's beam is on the next creature after a kill: two frames at 60 fps. The pulse that kills never
        // costs less than this (BeamBudget), so it stays the share of the Laser's shortest interval it was before round 87
        // made the pulses smaller and faster (0.05 of 0.111 s, now 0.033 of 0.073 s): at 0.05 the sweep alone was two
        // thirds of a pulse and a level 3 Laser lost a tenth of its data on small bodies.
        private const float LaserSweepSeconds = 1f / 30f;

        // Round 87 (Remco: the Laser is the answer to big, fast swarms of light creatures: a high rate of fire, a small
        // pulse, on the next creature at once and nothing wasted on a dead one; weak on armour and on one big target).
        // Its strength is the rules', not its numbers: a turret pays a swing for every new target (Towers.cs,
        // TurretRetargetSeconds) and a beam does not, and a beam works on armour at a tenth (ProjectGenerator.Damage.cs).
        // So on one big soft body it buys a little less per coin than the Gun Turret: every level's damage per second is
        // this share of what it was (40, 66.7 and 120), which is 0.97 of the Gun Turret per coin at each level. The
        // ratios between its levels are untouched, so the sheet prices its steps as before (130, 55, 100). The Cryo
        // Beam, its hybrid, keeps its own numbers.
        private const float LaserRetune = 0.9f;

        // Smaller than the Gun Turret's round at every level (5, 5, 10): the Laser's pulse is 4, 4 and 7.2, at 9 and 15 a
        // second. Its first step is the emitter (the Gun Turret's is its rate too) and its second the lens.
        private const float LaserPulse = 4f;

        /// <summary>The Laser's levels: the level it is built at and its two named steps (prices come from the sheet).</summary>
        private static TowerLevel[] LaserLevels() =>
            Levels(Lvl(110, LaserPulse, LightGunReach + 0.3f, LaserPulse / (8f / 0.2f * LaserRetune)), Faster("Overcharged emitter", 12f / 0.18f * LaserRetune), Harder("Focused lens", 18f / 0.15f * LaserRetune));

        private static void SetBeam(TowerDefinition tower, float sweepSeconds)
        {
            var so = new SerializedObject(tower);
            so.FindProperty("_beamSweepSeconds").floatValue = sweepSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Roster CreateRoster(TowerDefinition pulse, TowerDefinition cannon, TowerDefinition rocket)
        {
            var roster = new Roster { Pulse = pulse, Cannon = cannon, Rocket = rocket };

            // Fast light pulses on the ground. The beam lands at once (hitscan speed), holds its creature and sweeps on to
            // the next one right after a kill (2026-11-05, Docs/BALANCE.md "Laser and gun"); the pulse that kills costs the
            // share of it the creature needed (BeamBudget, rounds 85 and 86), so it lands its data and no more.
            // Cyan like the model's lens (curator decision 2026-11-04: our colours follow the models; warm is for the aliens).
            roster.Laser = CreateTower("Laser", "Laser Turret", TargetLayers.Ground, BeamSpeed, 0f, new Color(0.35f, 0.9f, 1f),
                LaserLevels(), 160f,
                roleLine: "The swarm gun: a fast beam of small pulses that is on the next creature at once, for streams of fast light creatures. Weak on armour and on one big target; cannot aim up.", roles: new[] { TowerRole.LightFire });
            SetBeam(roster.Laser, LaserSweepSeconds);

            roster.Cryo = CreateTower("Cryo", "Cryo", TargetLayers.Ground | TargetLayers.Air, 12f, 0f, new Color(0.55f, 0.95f, 1f),
                Levels(Lvl(90, 2f, 3.7f, 0.5f, 0f, 0.5f, 2f), Colder("Deeper chill", 0.35f, 110), Further("Extended nozzle", 4.6f, 160)), 140f, slowFactor: 0.5f, slowSeconds: 2f,
                roleLine: "Control: slows what passes, so every other gun has longer. Its own damage is a token.", roles: new[] { TowerRole.Control });

            // Lightning is approximated by a small splash, so it works well on groups.
            roster.Tesla = CreateTower("Tesla", "Tesla", TargetLayers.Ground, 40f, 1.3f, new Color(0.7f, 0.5f, 1f),
                Levels(Lvl(140, 16f, 3.5f, 0.5f), Harder("Higher voltage", 26f / 0.45f), Faster("Faster discharge", 40f / 0.4f)), 160f,
                roleLine: "Chain lightning: jumps through a bunched group at short range. A fifth on armour; cannot aim up.", roles: new[] { TowerRole.Splash });

            roster.Railgun = CreateTower("Railgun", "Railgun", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(1f, 1f, 1f),
                Levels(Lvl(220, 80f, 6f, 2.2f), Harder("Heavier slug", 120f / 2f), Faster("Faster capacitors", 180f / 1.8f)), 180f,
                roleLine: "Slug: made for armour and bosses, from far away. Slow: wasted on swarms, and half in the air.", roles: new[] { TowerRole.AntiArmour });

            // Signal yellow like the model's halos (was green).
            roster.Support = CreateTower("Support", "Support", TargetLayers.Ground, 1f, 0f, new Color(1f, 0.74f, 0.2f),
                Levels(Lvl(120, 0f, 3f, 1f), Further("Extended relay", 4f, 140), Stronger("Stronger signal", 2f, 200)), 140f, support: true, rangeBonus: 0.15f, rateBonus: 0.2f,
                roleLine: "Booster: the guns around it reach further and fire faster. It fires nothing itself.", roles: new[] { TowerRole.Booster });

            // Hybrids come from recipes, so their first level costs nothing to build: the recipe has the price. Every
            // level beats the two parents at that level put together (by 15 to 50 percent), in one cell, and the
            // upgrades cost close to what upgrading both parents would.
            // The Artillery glows amber like its model (was orange).
            roster.Artillery = CreateTower("Artillery", "Artillery", TargetLayers.Ground | TargetLayers.Air, 8f, 1.8f, new Color(1f, 0.78f, 0.3f),
                Levels(Lvl(0, 70f, 5.1f, 1.6f), Harder("Heavier shells", 130f / 1.4f, 220), Faster("Autoloader", 220f / 1.2f, 320)), 240f,
                roleLine: "Cannon and Flak in one: a big blast for groups and armour that also bursts among flyers.", roles: new[] { TowerRole.Splash, TowerRole.AntiArmour, TowerRole.AntiAir });

            roster.StormRifle = CreateTower("StormRifle", "Storm Rifle", TargetLayers.Ground | TargetLayers.Air, 20f, 0.8f, new Color(0.6f, 0.8f, 1f),
                Levels(Lvl(0, 14f, 4.1f, 0.2f), Faster("Faster cycling", 20f / 0.18f, 200), Harder("Higher voltage", 28f / 0.15f, 300)), 190f,
                roleLine: "Gun Turret and Tesla in one: rapid arcs for swarms and small groups. A fifth on armour, under half in the air.", roles: new[] { TowerRole.LightFire, TowerRole.Splash });
            // The Gun Turret's hybrid keeps its turret, and so its swing.
            SetRetarget(roster.StormRifle, TurretRetargetSeconds);

            roster.CryoBeam = CreateTower("CryoBeam", "Cryo Beam", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(0.7f, 1f, 1f),
                Levels(Lvl(0, 8f, 4.3f, 0.15f, 0f, 0.4f, 2.5f), Harder("Focused lens", 13f / 0.13f, 210), Faster("Overcharged emitter", 20f / 0.11f, 310)), 180f, slowFactor: 0.4f, slowSeconds: 2.5f,
                roleLine: "Laser and Cryo in one: a beam that burns one big soft body and chills it. Glances off armour, a fifth in the air.", roles: new[] { TowerRole.LightFire, TowerRole.Control });

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
            StampDamage(so, $"PathObjects/{id}");
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static DroneDefinition CreateDrone(string id, string displayName, string role, string summary, DroneKind kind, float speed, float health, int cost, int maxActive, Color color, float repair = 0f, float harvestSeconds = 4f, int harvestValue = 0, float damage = 0f, float fireInterval = 0.5f, float range = 3f, CreatureSkin.Kind skin = CreatureSkin.Kind.Plate, Color accent = default)
        {
            var asset = LoadOrCreate<DroneDefinition>($"{DataRoot}/Drones/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_role").stringValue = role;
            so.FindProperty("_summary").stringValue = summary;
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
            StampDamage(so, $"Drones/{id}");
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

        // Round 88a (Remco: "it lasts very briefly and costs 45 while a tower costs 50"): a field lasts a whole wave, as the
        // plan model has always counted it (PlanEvaluator.FieldWaveShare): the longest ground wave of the first continent keeps
        // coming for 55 s (Tools/PlayTest/r88_wave_spans.txt) and a slowed walker needs a few seconds more to cross it. Over that
        // minute a field in reach of two Gun Turrets gives what 1.09 times its coins in Gun Turrets give, and nothing after:
        // a tool for the last waves and the boss (Round88aTests holds both halves of the rule on the data).
        public const float ChronoFieldSeconds = 60f;

        private static void GeneratePathObjects(GeneratorContext context)
        {
            context.SlowField = CreatePathObject("SlowField", "Chrono Field", PathObjectKind.SlowField, PathObjectPlacement.Road, TargetLayers.Ground, 45, 1.6f, 0.5f, 0f, 0.45f, 0f, ChronoFieldSeconds, new Color(0.45f, 0.75f, 1f), PathObjectEffect.Slow, "A Crysium field laid across the road. Time runs slow inside it: everything that walks through moves at less than half speed, for as long as the field lasts. The answer to fast runners.");
            // Amber like the model's armed ring (curator decision 2026-11-04: our colours follow the models).
            context.Mine = CreatePathObject("Mine", "Land Mine", PathObjectKind.Mine, PathObjectPlacement.Road, TargetLayers.Ground, 35, 1.4f, 0.6f, 140f, 1f, 0f, 0f, new Color(1f, 0.72f, 0.25f), PathObjectEffect.Trap, "Buried in the road. Goes off once, under the first enemy that steps on it, and hurts everything close by.");
            context.LavaBall = CreatePathObject("LavaBall", "Lava ball", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 70, 0.9f, 0.5f, 55f, 1f, 2.2f, 0f, new Color(1f, 0.45f, 0.1f), PathObjectEffect.Roll, "Dropped on the road, it rolls on towards the base and burns every walker it touches along the way.");
            MarkBurning(context.LavaBall);
            context.Barrel = CreatePathObject("ExplosiveBarrel", "Barrel", PathObjectKind.Mine, PathObjectPlacement.Beside, TargetLayers.Ground, 50, 2f, 1.1f, 200f, 1f, 0f, 0f, new Color(0.9f, 0.5f, 0.1f), PathObjectEffect.Blast, "Stands beside the road. When the enemy passes close it bursts, and the blast reaches far across the road.");
            context.GroundDrone = CreatePathObject("GroundDrone", "Ground drone", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 80, 1.1f, 0.5f, 30f, 1f, 1.1f, 0f, new Color(0.4f, 0.9f, 1f), PathObjectEffect.Patrol, "Drives down the road at a steady pace and rams every walker it meets, dealing damage as long as it runs.");
        }

        // Round 60 (Remco: "not clear which drone does what"): what each drone's card says while it is focused or pointed at.
        public const string RepairDroneSummary = "Flies to the most damaged tower, gate or base and repairs it for free.";
        public const string HarvestDroneSummary = "Mines the crystal nodes on the field and flies the money home.";
        public const string AttackDroneSummary = "Hunts the creature furthest down the road and shoots it from the air.";

        // Three drones, one per job, bought with money in the level, so a drone competes with a tower for the same
        // purse: a harvester earns its price back in a few minutes of trips.
        private static void GenerateDrones(GeneratorContext context)
        {
            // The repair drone's price follows the investment rule (BalanceSheet.RepairDronePrice, round 22d) when the level data exists.
            context.RepairDrone = CreateDrone("RepairDrone", "Repair drone", "Repair", RepairDroneSummary, DroneKind.Repair, 3.5f, 40f, BalanceSheet.RepairDronePriceFromData(8f, 120), 3, new Color(0.4f, 0.9f, 1f), repair: 8f, skin: CreatureSkin.Kind.Plate, accent: new Color(1f, 0.22f, 0.28f));
            context.HarvestDrone = CreateDrone("HarvestDrone", "Harvest drone", "Harvest", HarvestDroneSummary, DroneKind.Harvest, 4f, 30f, 150, 2, new Color(1f, 0.85f, 0.3f), harvestSeconds: 4f, harvestValue: 10, skin: CreatureSkin.Kind.Alloy, accent: new Color(1f, 0.75f, 0.1f));
            // Azure like the model's tail glow (curator decision 2026-11-04: our colours follow the models).
            context.AttackDrone = CreateDrone("AttackDrone", "Attack drone", "Attack", AttackDroneSummary, DroneKind.Attack, 4.5f, 35f, 180, 3, new Color(0.3f, 0.7f, 1f), damage: 6f, fireInterval: 0.5f, range: 3f, skin: CreatureSkin.Kind.Plate, accent: new Color(0.92f, 0.95f, 1f));
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
