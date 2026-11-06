using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// Tower roles (round 20, a measuring tool for the next round's rebalance by role; report only, nothing asserted on the
    /// data): (1) upgrade against a second tower of the same type at equal coins (Remco: an upgrade must hit harder, per hit
    /// and single-target, than two side by side; more towers only win on targets and rate of fire, and spread the risk);
    /// (2) a role matrix, effective DPS per 100 coins against swarm, clustered group, air, armour (the current pool armour
    /// and a flat per-hit plating with 20% minimum) and one high-health target; (3) how much a good build spot is worth
    /// against a median one on Glass Meadows (C1_03). Written to Tools/PlayTest/tower_roles.txt. The in-game DPS per tower
    /// (the Laser question) comes from the PlayMode monoculture probe: Tools/PlayTest/monoculture.txt.
    /// </summary>
    public sealed class TowerRolesTests
    {
        private const string TowersFolder = "Assets/_Project/Data/Towers";
        private const string RecipesFolder = "Assets/_Project/Data/Recipes";
        private const string SpotLevelPath = "Assets/_Project/Data/Levels/Level_C1_03.asset";
        private const string TablePath = "Tools/PlayTest/tower_roles.txt";
        private const float CoinUnit = 100f;

        // The reference targets of the role matrix (health is per creature, density creatures per cell of road).
        private const float SwarmHealth = 40f;
        private const float SwarmDensity = 1.2f;
        private const float GroupHealth = 150f;
        private const float GroupDensity = 2f;
        private const float AirHealth = 45f;
        private const float AirDensity = 0.8f;
        private static readonly float[] ArmourPerHit = { 4f, 8f, 12f };

        /// <summary>A flat plating lets at least this share of a hit through.</summary>
        private const float PlatingFloor = 0.2f;

        // The spot lens: cells beside the road a tower may stand on, the road sampled every half cell, and what counts as
        // bunched (the queue in front of a gate, or a bend sharper than 45 degrees).
        private const float BesideRoadCells = 2f;
        private const float SampleCells = 0.5f;
        private const float QueueCells = 3f;
        private const float BendCos = 0.707f;
        private const float BendReachCells = 1.5f;

        [Test]
        public void TowerRoles_ReportUpgradesRolesAndSpots()
        {
            List<TowerDefinition> guns = Guns();
            Assert.IsNotEmpty(guns, "Run Tools/Generate/Scenes and Starter Data first.");
            BalanceRules rules = BalanceRules.Default;
            var report = new StringBuilder();
            report.Append("Tower roles, round 20 (report only; see Docs/BALANCE.md, Monoculture check). Data DPS = damage / interval.\n\n");
            UpgradeLens(guns, rules, report);
            RoleMatrix(guns, rules, report);
            SpotLens(guns, report);
            Directory.CreateDirectory(Path.GetDirectoryName(TablePath));
            File.WriteAllText(TablePath, report.ToString());
            Debug.Log("[TowerRoles] written to " + TablePath);
        }

        // ---- (1) upgrade against a second tower ----------------------------------------------------------------

        // For every step L -> L+1: one tower at L+1 against the same coins spent on towers at L (n = cost to L+1 / cost to L).
        private static void UpgradeLens(List<TowerDefinition> guns, BalanceRules rules, StringBuilder report)
        {
            report.Append("1. Upgrade vs more towers of the same type at equal coins\n");
            report.Append("   step: one tower at L+1 (upgrade) vs n towers at L for the same coins. hit = damage per hit; single = DPS on one\n");
            report.Append("   target (no armour); plated8 = single-target DPS through a flat 8 plating (20% floor); multi = DPS into a\n");
            report.Append("   clustered group (splash); targets = creatures hit per volley; risk = share of that damage lost when one tower is wrecked.\n");
            report.Append("   Remco's rule: the upgrade wins on hit and single; n towers win only on targets/rate. 'rule' = upgrade single >= n towers single.\n");
            int met = 0;
            int steps = 0;
            foreach (TowerDefinition gun in guns)
            {
                TowerStats stats = BalanceLedger.Stats(gun);
                for (int level = 0; level + 1 < stats.Levels.Length; level++)
                {
                    TowerLevelStats low = stats.Levels[level];
                    TowerLevelStats high = stats.Levels[level + 1];
                    float n = CostTo(gun, level + 1) / (float)Mathf.Max(1, CostTo(gun, level));
                    float splash = Splash(stats.Splash, GroupDensity, rules);
                    bool rule = high.Dps >= n * low.Dps;
                    met += rule ? 1 : 0;
                    steps++;
                    report.Append(Invariant($"   {gun.name,-11} L{level}->L{level + 1} coins {CostTo(gun, level + 1),4} | upgrade: hit {high.Damage,6:0.#} single {high.Dps,6:0.#} plated8 {Plated(high, 8f),6:0.#} multi {high.Dps * splash,6:0.#} targets {splash,3:0.#} rate {1f / high.Interval,4:0.##}/s risk 100%"));
                    report.Append(Invariant($" | {n:0.00} x L{level}: hit {low.Damage,6:0.#} single {n * low.Dps,6:0.#} plated8 {n * Plated(low, 8f),6:0.#} multi {n * low.Dps * splash,6:0.#} targets {n * splash,3:0.#} rate {n / low.Interval,4:0.##}/s risk {100f / n,3:0}%"));
                    report.Append(Invariant($" | single ratio {high.Dps / Mathf.Max(0.01f, n * low.Dps):0.00} plated8 ratio {Plated(high, 8f) / Mathf.Max(0.01f, n * Plated(low, 8f)):0.00} rule {(rule ? "met" : "NOT met")}\n"));
                }
            }

            report.Append(Invariant($"   rule met on {met} of {steps} steps\n\n"));
        }

        // ---- (2) role matrix ------------------------------------------------------------------------------------

        private static void RoleMatrix(List<TowerDefinition> guns, BalanceRules rules, StringBuilder report)
        {
            report.Append("2. Role matrix: effective DPS per 100 coins (cost to reach the level)\n");
            report.Append(Invariant($"   swarm = {SwarmHealth} hp, {SwarmDensity}/cell (overkill + splash); group = {GroupHealth} hp, {GroupDensity}/cell; air = {AirHealth} hp, {AirDensity}/cell;\n"));
            report.Append("   armour: 'pool' is the current formula (Health: an armour pool soaks a share of every hit until it is gone, the same\n");
            report.Append("   for every hit size, so per coin it ranks like single) and 'plate A' a flat per-hit plating (hit - A, at least 20%);\n");
            report.Append(Invariant($"   single = one high-health target (no overkill, no splash). Splash cap {rules.MaxSplashTargets}.\n"));
            report.Append("   tower       lvl  coins   swarm   group     air    pool  plate4  plate8 plate12  single\n");
            foreach (TowerDefinition gun in guns)
            {
                TowerStats stats = BalanceLedger.Stats(gun);
                bool ground = Layer.Has(stats.Targets, Layer.Ground);
                bool air = Layer.Has(stats.Targets, Layer.Air);
                for (int level = 0; level < stats.Levels.Length; level++)
                {
                    TowerLevelStats at = stats.Levels[level];
                    float per = CoinUnit / Mathf.Max(1, CostTo(gun, level));
                    report.Append(Invariant($"   {gun.name,-11} L{level} {CostTo(gun, level),6}"));
                    report.Append(Cell(ground, at.Dps * Overkill(at.Damage, SwarmHealth) * Splash(stats.Splash, SwarmDensity, rules) * per));
                    report.Append(Cell(ground, at.Dps * Overkill(at.Damage, GroupHealth) * Splash(stats.Splash, GroupDensity, rules) * per));
                    report.Append(Cell(air, at.Dps * Overkill(at.Damage, AirHealth) * Splash(stats.Splash, AirDensity, rules) * per));
                    report.Append(Cell(ground, at.Dps * per));
                    foreach (float armour in ArmourPerHit)
                    {
                        report.Append(Cell(ground, Plated(at, armour) * per));
                    }

                    report.Append(Cell(ground || air, at.Dps * per)).Append('\n');
                }
            }

            report.Append('\n');
        }

        // ---- (3) spots ------------------------------------------------------------------------------------------

        // Per tower type at its first level's range: how much walking road a spot reaches, and how much of it is bunched
        // (the queue before a gate, a bend), at the best and the median build spot beside the road of C1_03.
        private static void SpotLens(List<TowerDefinition> guns, StringBuilder report)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SpotLevelPath);
            report.Append("3. Build spots on ").Append(level != null ? level.DisplayName : "(missing)").Append(" (C1_03): road in reach (cells of walking route,\n");
            report.Append("   every route counted, so the shared trunk weighs more) and bunched road (within 3 cells before a gate or 1.5 of a bend),\n");
            report.Append("   best spot vs the median spot beside the road (within 2 cells). The ratio is what a good spot is worth over an average one.\n");
            if (level == null)
            {
                return;
            }

            List<(Vector2 Point, bool Bunched)> walk = Walk(level);
            List<Vector2Int> cells = Spots(level);
            foreach (TowerDefinition gun in guns)
            {
                TowerStats stats = BalanceLedger.Stats(gun);
                if (!Layer.Has(stats.Targets, Layer.Ground) || stats.Levels.Length == 0)
                {
                    continue;
                }

                float range = stats.Levels[0].Range;
                var reach = new List<float>();
                var bunched = new List<float>();
                foreach (Vector2Int cell in cells)
                {
                    float all = 0f;
                    float packed = 0f;
                    foreach ((Vector2 point, bool isBunched) in walk)
                    {
                        if (Vector2.Distance(cell, point) <= range)
                        {
                            all += SampleCells;
                            packed += isBunched ? SampleCells : 0f;
                        }
                    }

                    reach.Add(all);
                    bunched.Add(packed);
                }

                float bestReach = Max(reach);
                float medianReach = Median(reach);
                float bestBunch = Max(bunched);
                float medianBunch = Median(bunched);
                string role = stats.Splash > 0f ? "splash: bunched road counts" : "rate of fire: road in reach counts";
                report.Append(Invariant($"   {gun.name,-11} range {range,4:0.0} | reach best {bestReach,5:0.0} median {medianReach,5:0.0} x{bestReach / Mathf.Max(0.5f, medianReach),4:0.0} | bunched best {bestBunch,5:0.0} median {medianBunch,5:0.0} x{bestBunch / Mathf.Max(0.5f, medianBunch),4:0.0} | {role}\n"));
            }

            report.Append(Invariant($"   ({cells.Count} build spots beside the road)\n"));
        }

        private static List<(Vector2 Point, bool Bunched)> Walk(LevelDefinition level)
        {
            var gates = new List<Vector2>();
            foreach (BlockadeSpot gate in level.Blockades)
            {
                gates.Add(gate.Cell);
            }

            var samples = new List<(Vector2, bool)>();
            foreach (EnemyPath path in level.Paths)
            {
                if (path.Flying || path.Waypoints == null)
                {
                    continue;
                }

                var bends = new List<Vector2>();
                for (int i = 1; i + 1 < path.Waypoints.Length; i++)
                {
                    Vector2 a = ((Vector2)path.Waypoints[i] - path.Waypoints[i - 1]).normalized;
                    Vector2 b = ((Vector2)path.Waypoints[i + 1] - path.Waypoints[i]).normalized;
                    if (Vector2.Dot(a, b) < BendCos)
                    {
                        bends.Add(path.Waypoints[i]);
                    }
                }

                for (int i = 0; i + 1 < path.Waypoints.Length; i++)
                {
                    Vector2 from = path.Waypoints[i];
                    Vector2 to = path.Waypoints[i + 1];
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / SampleCells));
                    for (int s = 0; s < steps; s++)
                    {
                        Vector2 point = Vector2.Lerp(from, to, s / (float)steps);
                        Vector2 heading = (to - from).normalized;
                        bool queue = gates.Exists(g => Vector2.Distance(point, g) <= QueueCells && Vector2.Dot(g - point, heading) >= 0f);
                        bool bend = bends.Exists(b => Vector2.Distance(point, b) <= BendReachCells);
                        samples.Add((point, queue || bend));
                    }
                }
            }

            return samples;
        }

        private static List<Vector2Int> Spots(LevelDefinition level)
        {
            var grid = new BuildGrid(level);
            var road = new List<Vector2Int>(level.Road.Cells);
            var cells = new List<Vector2Int>();
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (grid.CanBuild(cell) && road.Exists(r => Vector2Int.Distance(r, cell) <= BesideRoadCells))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        // ---- helpers --------------------------------------------------------------------------------------------

        private static List<TowerDefinition> Guns()
        {
            var guns = new List<TowerDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { TowersFolder }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (tower != null && BalanceLedger.Stats(tower).IsGun)
                {
                    guns.Add(tower);
                }
            }

            guns.Sort((a, b) => a.BuildCost.CompareTo(b.BuildCost));
            return guns;
        }

        // Coins to own a tower at a level: its level costs, or for a hybrid both partners at the combine level, the recipe and
        // its own steps after it, combining at the cheapest level (as TowerParityTests prices them).
        private static int CostTo(TowerDefinition tower, int level, int depth = 0)
        {
            if (!Recipes.TryGetValue(tower, out CombineRecipe recipe) || depth > Recipes.Count)
            {
                return StepCost(tower, 0, level);
            }

            int best = int.MaxValue;
            for (int combine = 0; combine <= level; combine++)
            {
                int cost = CostTo(recipe.First, combine, depth + 1) + CostTo(recipe.Second, combine, depth + 1) + recipe.Cost + StepCost(tower, combine + 1, level);
                best = Mathf.Min(best, cost);
            }

            return best;
        }

        private static int StepCost(TowerDefinition tower, int from, int to)
        {
            int sum = 0;
            for (int i = from; i <= Mathf.Min(to, tower.MaxLevelIndex); i++)
            {
                sum += tower.GetLevel(i).Cost;
            }

            return sum;
        }

        private static readonly Dictionary<TowerDefinition, CombineRecipe> Recipes = LoadRecipes();

        private static Dictionary<TowerDefinition, CombineRecipe> LoadRecipes()
        {
            var recipes = new Dictionary<TowerDefinition, CombineRecipe>();
            foreach (string guid in AssetDatabase.FindAssets("t:CombineRecipe", new[] { RecipesFolder }))
            {
                var recipe = AssetDatabase.LoadAssetAtPath<CombineRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe != null && recipe.Result != null && recipe.First != null && recipe.Second != null)
                {
                    recipes[recipe.Result] = recipe;
                }
            }

            return recipes;
        }

        private static float Plated(TowerLevelStats stats, float armour) =>
            stats.Damage <= 0f ? 0f : stats.Dps * Mathf.Max(PlatingFloor, (stats.Damage - armour) / stats.Damage);

        private static float Overkill(float damage, float health) => damage <= health ? 1f : health / damage;

        private static float Splash(float radius, float density, BalanceRules rules) =>
            radius <= 0f ? 1f : Mathf.Min(rules.MaxSplashTargets, 1f + 2f * radius * density);

        private static string Cell(bool applies, float value) => applies ? Invariant($" {value,7:0.0}") : "       -";

        private static float Max(List<float> values)
        {
            float max = 0f;
            foreach (float value in values)
            {
                max = Mathf.Max(max, value);
            }

            return max;
        }

        private static float Median(List<float> values)
        {
            if (values.Count == 0)
            {
                return 0f;
            }

            var sorted = new List<float>(values);
            sorted.Sort();
            return sorted[sorted.Count / 2];
        }
    }
}
