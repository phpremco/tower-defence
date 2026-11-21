using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 88b: the escort of every level boss as stored, on paper (no Play Mode). Per level, difficulty and boss: its bar
/// as spawned, the seconds of its unhindered walk from spawn to the base and to the first gate on its road, and what it
/// sends out of its body over that walk: before (the plain Spawn power of rounds up to 88a: a fixed count every interval,
/// creatures at the health of the wave under way, full bounty) and after (the escort rule: EscortBrood driven with the
/// same calls). Health is body plus armour as the creatures walk in.
/// Run: bash Tools/PlayTest/stage.sh run Round88bEscortLab.cs Round88bEscortLab.Run (writes Tools/PlayTest/r88b_escort.txt).
/// </summary>
public static class Round88bEscortLab
{
    private const string ReportPath = "Tools/PlayTest/r88b_escort.txt";
    private const string DifficultyFolder = "Assets/_Project/Data/Difficulty";
    private static readonly string[] DifficultyIds = { "Easy", "Medium", "Hard", "Psycho" };

    // The Spawn powers that existed before round 88b; a boss without one of them sent nothing.
    private static readonly HashSet<string> OldPowers = new HashSet<string> { "Brood", "BatSwarm", "SpawnOozers", "QueenBrood" };

    // A gate counts as on the boss's road when its middle lies within this many cells of the route.
    private const float GateOnRoadCells = 2f;

    private struct Sent
    {
        public int Calls;
        public int Light;
        public int Heavy;
        public float Health;
        public float BehindGate;
        public float SecondHalf;
        public float Coins;
    }

    public static string Run()
    {
        BalanceRules rules = BalanceRules.Default;
        var text = new StringBuilder();
        text.Append(Line($"Round 88b: the escort of every level boss as stored. Rule: worth per difficulty x boss bar over an unhindered walk; weights {rules.EscortStartWeight:0.##} (start) to {rules.EscortEndWeight:0.##} (end); plated share {rules.EscortHeavyShare:0.##}; cap x{rules.EscortMostOfWorth:0.##} of the worth; bounty share {rules.EscortBountyShare:0.##}.\n"));
        text.Append("Health = body plus armour as walked in. 'gate' = the first gate on the boss's road. 'crowd' = the ordinary creatures of the boss's own wave.\n");
        text.Append("BEFORE = the plain Spawn power (fixed count every interval, health of the boss's wave, full bounty); AFTER = the escort rule.\n\n");
        text.Append("level | difficulty | wave | boss > creature (+ plated) | bar | walk s | gate at s (share) | calls | BEFORE: creatures | health | % of bar | % of crowd | behind gate | coins | AFTER: light + plated | health | % of bar | % of crowd | behind gate (share) | in 2nd half | coins\n");

        var sums = new float[DifficultyIds.Length, 6];
        var counts = new int[DifficultyIds.Length];
        var profiles = new DifficultyProfile[DifficultyIds.Length];
        for (int d = 0; d < DifficultyIds.Length; d++)
        {
            profiles[d] = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DifficultyFolder}/{DifficultyIds[d]}.asset");
        }

        var paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
        {
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }

        paths.Sort(string.CompareOrdinal);
        foreach (string path in paths)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (level == null || !level.name.StartsWith("Level_"))
            {
                continue;
            }

            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                int d = System.Array.IndexOf(profiles, set.Difficulty);
                if (d < 0)
                {
                    continue;
                }

                EnemyDefinition plated = null;
                int number = 0;
                foreach (WaveDefinition wave in set.Waves)
                {
                    number++;
                    // The plated part of a brood is the toughest plated walker sent before the boss walks in.
                    EnemyDefinition platedBefore = plated;
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        plated = EscortRule.Tougher(plated, group.Enemy);
                    }

                    foreach (SpawnGroup group in wave.Groups)
                    {
                        if (!EscortRule.Binds(group.Enemy))
                        {
                            continue;
                        }

                        Row(text, level, set.Difficulty, d, number, wave, group, platedBefore, rules, sums);
                        counts[d]++;
                    }
                }
            }
        }

        text.Append("\nMean per difficulty over every stored level boss (BEFORE % of bar, AFTER % of bar, AFTER % of crowd, AFTER share behind the first gate, AFTER share in the second half of the walk, BEFORE coins per walk):\n");
        string summary = string.Empty;
        for (int d = 0; d < DifficultyIds.Length; d++)
        {
            int n = Mathf.Max(1, counts[d]);
            string line = Line($"{DifficultyIds[d]} ({counts[d]} bosses): before {sums[d, 0] / n:0} % of bar, after {sums[d, 1] / n:0} % of bar, {sums[d, 2] / n:0} % of crowd, {sums[d, 3] / n:0} % behind the first gate, {sums[d, 4] / n:0} % in the second half; before {sums[d, 5] / n:0} coins a walk, after 0");
            text.Append(line).Append('\n');
            summary += line + "; ";
        }

        File.WriteAllText(ReportPath, text.ToString());
        return summary + "see " + ReportPath;
    }

    private static void Row(StringBuilder text, LevelDefinition level, DifficultyProfile difficulty, int d, int number, WaveDefinition wave, SpawnGroup group, EnemyDefinition plated, BalanceRules rules, float[,] sums)
    {
        EnemyDefinition boss = group.Enemy;
        float ordinary = difficulty.HealthMultiplier * level.EnemyHealthScale * wave.HealthMultiplier;
        float bar = (boss.MaxHealth + boss.Armor) * ordinary * wave.BossHealthScale;
        Vector3[] route = RouteOf(level, group);
        float length = LengthOf(route);
        float walk = length / boss.AverageSpeed;
        float gateShare = FirstGateShare(level, route, length);
        float crowd = 0f;
        foreach (SpawnGroup other in wave.Groups)
        {
            if (other.Enemy != null && !other.Enemy.IsBoss)
            {
                crowd += WaveScheduler.CountOf(other, difficulty) * (other.Enemy.MaxHealth + other.Enemy.Armor) * ordinary;
            }
        }

        BossAbilityDefinition power = null;
        foreach (BossAbilityDefinition ability in boss.Abilities)
        {
            if (ability != null && ability.Kind == BossAbilityKind.Spawn && ability.Minion != null)
            {
                power = ability;
                break;
            }
        }

        string key = level.name.Substring(6);
        if (power == null)
        {
            text.Append(Line($"{key} | {DifficultyIds[d]} | {number} | {boss.name} > (no Spawn power) | {bar:0} | {walk:0} | {gateShare * walk:0} ({gateShare:0.00}) | 0 | - | - | - | - | - | - | - | - | - | - | - | - | -\n"));
            return;
        }

        EnemyDefinition heavy = boss.Flies ? null : plated;
        float lightCost = (power.Minion.MaxHealth + power.Minion.Armor) * ordinary;
        float heavyCost = heavy != null ? (heavy.MaxHealth + heavy.Armor) * ordinary : 0f;
        Sent before = default;
        Sent after = default;
        var brood = new EscortBrood();
        brood.Begin(EscortRule.For(difficulty, rules), bar, walk, lightCost, heavyCost);
        bool old = OldPowers.Contains(power.name);
        int plain = Mathf.Max(1, Mathf.RoundToInt(power.MinionCount * difficulty.BossEscortShare));
        float coinsEach = power.Minion.Reward * difficulty.IncomeMultiplier;
        for (float t = power.FirstDelay; t < walk; t += power.Interval)
        {
            float progress = t / walk;
            before.Calls++;
            if (old)
            {
                Add(ref before, plain, 0, plain * lightCost, progress, gateShare, plain * coinsEach);
            }

            brood.Cast(before.Calls == 1 ? power.FirstDelay : power.Interval, progress, out int light, out int heavyCount);
            Add(ref after, light, heavyCount, light * lightCost + heavyCount * heavyCost, progress, gateShare, (light * coinsEach + heavyCount * (heavy != null ? heavy.Reward * difficulty.IncomeMultiplier : 0f)) * rules.EscortBountyShare);
        }

        string creature = power.Minion.name + (brood.SendsHeavy ? " + " + heavy.name : string.Empty);
        text.Append(Line($"{key} | {DifficultyIds[d]} | {number} | {boss.name} > {creature} | {bar:0} | {walk:0} | {gateShare * walk:0} ({gateShare:0.00}) | {before.Calls} | {before.Light} | {before.Health:0} | {Share(before.Health, bar)} | {Share(before.Health, crowd)} | {before.BehindGate:0} | {before.Coins:0} | {after.Light} + {after.Heavy} | {after.Health:0} | {Share(after.Health, bar)} | {Share(after.Health, crowd)} | {after.BehindGate:0} ({Share(after.BehindGate, after.Health)}) | {Share(after.SecondHalf, after.Health)} | {after.Coins:0}\n"));
        sums[d, 0] += Percent(before.Health, bar);
        sums[d, 1] += Percent(after.Health, bar);
        sums[d, 2] += Percent(after.Health, crowd);
        sums[d, 3] += Percent(after.BehindGate, after.Health);
        sums[d, 4] += Percent(after.SecondHalf, after.Health);
        sums[d, 5] += before.Coins;
    }

    private static void Add(ref Sent sent, int light, int heavy, float health, float progress, float gateShare, float coins)
    {
        sent.Light += light;
        sent.Heavy += heavy;
        sent.Health += health;
        sent.Coins += coins;
        if (progress > gateShare)
        {
            sent.BehindGate += health;
        }

        if (progress > 0.5f)
        {
            sent.SecondHalf += health;
        }
    }

    // The road the spawner gives the boss: its group's path from the gate, or from the level's entrance in the ground.
    private static Vector3[] RouteOf(LevelDefinition level, SpawnGroup group)
    {
        if (group.Source == SpawnSource.Entrance && level.BossEntrance.Kind == EntranceKind.Ground)
        {
            return OffRoadRoute.Build(level, level.Grid.CellToWorld(level.BossEntrance.Cell), out _);
        }

        return level.Road.Route(Mathf.Clamp(group.PathIndex, 0, level.Paths.Length - 1));
    }

    // How far down the route (as a share of its length) the first gate on it stands; 1 when no gate lies on it.
    private static float FirstGateShare(LevelDefinition level, Vector3[] route, float length)
    {
        float reach = GateOnRoadCells * level.CellSize;
        float first = length;
        foreach (BlockadeSpot spot in level.Blockades)
        {
            Vector3 gate = level.Road.CentreOf(spot.Cell);
            float along = 0f;
            for (int i = 1; i < route.Length; i++)
            {
                Vector2 a = new Vector2(route[i - 1].x, route[i - 1].z);
                Vector2 b = new Vector2(route[i].x, route[i].z);
                Vector2 p = new Vector2(gate.x, gate.z);
                float segment = Vector2.Distance(a, b);
                float t = segment > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (segment * segment)) : 0f;
                if (Vector2.Distance(p, a + (b - a) * t) <= reach)
                {
                    first = Mathf.Min(first, along + segment * t);
                    break;
                }

                along += segment;
            }
        }

        return length > 0f ? first / length : 1f;
    }

    private static float LengthOf(Vector3[] route)
    {
        float length = 0f;
        for (int i = 1; i < route.Length; i++)
        {
            length += Vector2.Distance(new Vector2(route[i - 1].x, route[i - 1].z), new Vector2(route[i].x, route[i].z));
        }

        return length;
    }

    private static float Percent(float part, float whole) => whole > 0f ? 100f * part / whole : 0f;

    private static string Share(float part, float whole) => whole > 0f ? Percent(part, whole).ToString("0", CultureInfo.InvariantCulture) + " %" : "-";

    private static string Line(System.FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
