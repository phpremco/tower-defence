using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 88b lab on the assets (no Play Mode): what the stored waves do to a tower beside the road. Per level and
/// difficulty a reference tower stands 1.5, 3 and 4.5 cells off the main road, <see cref="RefBack"/> cells before the
/// first gate, and every creature of every wave walks its own stored road past it at its average speed: each second
/// within the reach of its attack costs damage / interval, times the difficulty's structure damage in play. Boss powers
/// (pulse, volley, laser) are counted the same way over their own reach.
///
/// Assumptions, all stated in the report: the first gate holds (a walker stops before it, so later legs of the road do
/// not count); a creature out of a nest or a boss entrance is taken to walk the main road; "alone" means the tower is the
/// nearest structure for the whole pass and nothing dies on the way (the worst case); "as played" takes
/// <see cref="LiveShare"/> of the ordinary creatures' pass (the guns kill them on the way) and <see cref="NearestShare"/>
/// of every bite that picks only the nearest structure (two guns equally near); a volley lands <see cref="VolleyShare"/>
/// of its bolts on the tower (three bolts among the six guns a defence has in a boss's reach); a pulse hits it in full.
/// The boss pass is the worst case for the tower: it stands among the three nearest guns all the way.
///
/// Run: bash Tools/PlayTest/stage.sh run Round88bThreatLab.cs Round88bThreatLab.Before   (writes r88b_threat_before.txt)
///      bash Tools/PlayTest/stage.sh run Round88bThreatLab.cs Round88bThreatLab.After    (writes r88b_threat.txt, with the before table)
/// </summary>
public static class Round88bThreatLab
{
    private const string BeforePath = "Tools/PlayTest/r88b_threat_before.txt";
    private const string AfterPath = "Tools/PlayTest/r88b_threat.txt";
    private const string ReferenceTower = "Assets/_Project/Data/Towers/Cannon.asset";
    private const float RefBack = 6f;
    private const float GateStop = 1.4f;
    private const float Step = 0.05f;
    private const float LiveShare = 0.5f;
    private const float NearestShare = 0.5f;
    private const float VolleyShare = 0.5f;
    private static readonly float[] Offsets = { 1.5f, 3f, 4.5f };

    public static string Before() => Write(BeforePath, "BEFORE (the data as it stood)", false);

    public static string After() => Write(AfterPath, "AFTER (round 88b reach rule and boss volleys)", true);

    private static string Write(string path, string title, bool appendBefore)
    {
        float towerHealth = AssetDatabase.LoadAssetAtPath<TowerDefinition>(ReferenceTower).MaxHealth;
        var text = new StringBuilder();
        text.Append("Round 88b: tower threat on paper, ").Append(title).Append('\n');
        text.Append(F($"Reference tower: a level-1 Cannon ({towerHealth:0} hit points), {RefBack:0} cells before the first gate of the main road, 1.5 / 3 / 4.5 cells off the road (the worse side).\n"));
        text.Append("Every figure is a share of that tower's health over the whole level (100 % = one wreck if never repaired).\n");
        text.Append(F($"alone    = the tower is the nearest structure all the time and nothing dies on the way (worst case)\n"));
        text.Append(F($"played   = ordinary creatures x{LiveShare:0.##} (shot on the way), nearest-only bites x{NearestShare:0.##} (two guns equally near), boss volleys x{VolleyShare:0.##} (three bolts among six guns in reach), pulses in full\n"));
        text.Append("boss     = one pass of the level's worst boss alone with the tower among its three nearest guns all the way (every bolt lands), and what that costs per 10 s the boss is held within its powers' reach\n");
        text.Append("Assumptions: the first gate holds; nest and boss-entrance spawns walk the main road; flyers fly their stored route; speeds are averages (rolls included).\n\n");

        var summary = new StringBuilder();
        _summaryLevel = null;
        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (level == null || !level.name.StartsWith("Level_"))
            {
                continue;
            }

            text.Append(level.name).Append(" (").Append(level.DisplayName).Append(")\n");
            text.Append("  difficulty  x      | 1.5 cells: alone played boss-pass /10s held | 3 cells: alone played boss-pass /10s held | 4.5 cells: alone played boss-pass /10s held | who hurts it at 3 cells (played)\n");
            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                Row(level, set, towerHealth, text, summary);
            }

            text.Append('\n');
        }

        text.Append("Summary, played at 3 cells (level: Easy / Medium / Hard / Psycho), then the worst boss pass on Medium:\n").Append(summary);
        if (appendBefore && File.Exists(BeforePath))
        {
            text.Append("\n==================== for comparison ====================\n").Append(File.ReadAllText(BeforePath));
        }

        File.WriteAllText(path, text.ToString());
        return summary.ToString();
    }

    private static string _summaryLevel;

    private static void Row(LevelDefinition level, DifficultyWaves set, float towerHealth, StringBuilder text, StringBuilder summary)
    {
        float factor = set.Difficulty.StructureDamageMultiplier;
        Vector2[] main = Line(level.MainRoad);
        float gateAt = FirstGateDistance(level, main);
        float refAt = Mathf.Max(0f, gateAt - RefBack);
        var best = new Result[Offsets.Length];
        for (int side = -1; side <= 1; side += 2)
        {
            for (int o = 0; o < Offsets.Length; o++)
            {
                Vector2 tower = PointAt(main, refAt) + Normal(main, refAt) * (side * Offsets[o]);
                if (tower.x < 0f || tower.y < 0f || tower.x > level.GridSize.x - 1 || tower.y > level.GridSize.y - 1)
                {
                    continue;
                }

                Result result = Measure(level, set, tower, main, gateAt, factor);
                if (best[o] == null || result.Played > best[o].Played)
                {
                    best[o] = result;
                }
            }
        }

        text.Append("  ").Append(set.Difficulty.name.PadRight(7)).Append(F($" x{factor:0.00}"));
        foreach (Result result in best)
        {
            if (result == null)
            {
                text.Append(" |   (off the map)");
                continue;
            }

            text.Append(F($" | {Pct(result.Alone, towerHealth),5} {Pct(result.Played, towerHealth),5} {Pct(result.BossPass, towerHealth),5} {Pct(result.BossHeld, towerHealth),5}"));
        }

        Result mid = best[1];
        text.Append(" | ").Append(mid != null ? mid.Who(towerHealth) : string.Empty).Append('\n');
        if (_summaryLevel != level.name)
        {
            _summaryLevel = level.name;
            summary.Append(summary.Length > 0 ? "\n" : string.Empty).Append("  ").Append(level.name).Append(": ");
        }

        summary.Append(mid != null ? Pct(mid.Played, towerHealth) : "-").Append(" / ");
        if (set.Difficulty.name == "Medium" && mid != null)
        {
            summary.Append(F($"[Medium boss pass {Pct(mid.BossPass, towerHealth)}] "));
        }
    }

    private sealed class Result
    {
        public float Alone;
        public float Played;
        public float BossPass;
        public float BossHeld;
        public readonly Dictionary<string, float> ByKind = new Dictionary<string, float>();

        public string Who(float towerHealth)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<string, float> pair in ByKind)
            {
                if (pair.Value > 0.005f * towerHealth)
                {
                    parts.Add(pair.Key + " " + Pct(pair.Value, towerHealth));
                }
            }

            return string.Join(", ", parts);
        }
    }

    private static Result Measure(LevelDefinition level, DifficultyWaves set, Vector2 tower, Vector2[] main, float gateAt, float factor)
    {
        var result = new Result();
        var seconds = new Dictionary<string, float>();
        foreach (WaveDefinition wave in set.Waves)
        {
            foreach (SpawnGroup group in wave.Groups)
            {
                EnemyDefinition enemy = group.Enemy;
                if (enemy == null)
                {
                    continue;
                }

                int count = WaveScheduler.CountOf(group, set.Difficulty);
                bool ownRoad = group.Source == SpawnSource.Gate && group.PathIndex < level.Paths.Length;
                Vector2[] route = ownRoad ? Line(level.Paths[group.PathIndex].Waypoints) : main;
                // A walker stops before the first gate of the main road when its road runs through it; a flyer flies on.
                float until = enemy.Flies ? Length(route) : StopDistance(route, main, gateAt);
                float speed = Mathf.Max(0.05f, enemy.AverageSpeed);

                float bite = 0f;
                if (enemy.AttackDamage > 0f)
                {
                    float inReach = CellsInReach(route, until, tower, enemy.StructureReach, seconds, ownRoad ? group.PathIndex : -1);
                    bite = inReach / speed / enemy.AttackInterval * enemy.AttackDamage * factor;
                }

                float powers = 0f;
                float spread = 0f;
                float held = 0f;
                foreach (BossAbilityDefinition ability in enemy.Abilities)
                {
                    if (ability == null || ability.StructureReach <= 0f)
                    {
                        continue;
                    }

                    // A laser burns for its sweep each time it fires; a pulse and a volley land once.
                    float perUse = ability.Kind == BossAbilityKind.Laser ? ability.Damage * ability.SweepSeconds : ability.Damage;
                    float share = ability.Kind == BossAbilityKind.Laser ? NearestShare : 1f;
                    float inReach = CellsInReach(route, until, tower, ability.StructureReach, seconds, ownRoad ? group.PathIndex : -1);
                    float cost = inReach / speed / ability.Interval * perUse * factor * share;
                    powers += cost;
                    spread += ability.Kind == BossAbilityKind.Volley ? cost * VolleyShare : cost;
                    held += 10f / ability.Interval * perUse * factor * share;
                }

                float alone = (bite + powers) * count;
                float live = enemy.IsBoss ? 1f : LiveShare;
                float played = (bite * NearestShare * live + spread) * count;
                result.Alone += alone;
                result.Played += played;
                result.ByKind.TryGetValue(enemy.name, out float soFar);
                result.ByKind[enemy.name] = soFar + played;
                if (enemy.IsBoss && bite * NearestShare + powers > result.BossPass)
                {
                    result.BossPass = bite * NearestShare + powers;
                    result.BossHeld = held;
                }
            }
        }

        return result;
    }

    // Cells of a route (up to a distance along it) that lie within a reach of the tower. Cached per road and reach.
    private static float CellsInReach(Vector2[] route, float until, Vector2 tower, float reach, Dictionary<string, float> cache, int road)
    {
        string key = F($"{road}|{until:0.00}|{reach:0.00}|{route.Length}|{route[0].x:0}|{route[0].y:0}");
        if (cache.TryGetValue(key, out float known))
        {
            return known;
        }

        float cells = 0f;
        for (float at = 0f; at < until; at += Step)
        {
            if ((PointAt(route, at) - tower).sqrMagnitude <= reach * reach)
            {
                cells += Step;
            }
        }

        cache[key] = cells;
        return cells;
    }

    // How far along its own road a walker gets before the first gate of the main road stops it (the whole road when it never meets that gate).
    private static float StopDistance(Vector2[] route, Vector2[] main, float gateAt)
    {
        Vector2 gate = PointAt(main, gateAt);
        float length = Length(route);
        for (float at = 0f; at < length; at += Step)
        {
            if ((PointAt(route, at) - gate).sqrMagnitude <= GateStop * GateStop)
            {
                return at;
            }
        }

        return length;
    }

    private static float FirstGateDistance(LevelDefinition level, Vector2[] main)
    {
        float length = Length(main);
        for (float at = 0f; at < length; at += Step)
        {
            Vector2 point = PointAt(main, at);
            foreach (BlockadeSpot spot in level.Blockades)
            {
                if ((point - new Vector2(spot.Cell.x, spot.Cell.y)).sqrMagnitude <= 0.6f * 0.6f)
                {
                    return at;
                }
            }
        }

        return length;
    }

    private static Vector2[] Line(Vector2Int[] cells)
    {
        var line = new Vector2[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            line[i] = new Vector2(cells[i].x, cells[i].y);
        }

        return line;
    }

    private static float Length(Vector2[] line)
    {
        float length = 0f;
        for (int i = 1; i < line.Length; i++)
        {
            length += Vector2.Distance(line[i - 1], line[i]);
        }

        return length;
    }

    private static Vector2 PointAt(Vector2[] line, float distance)
    {
        for (int i = 1; i < line.Length; i++)
        {
            float leg = Vector2.Distance(line[i - 1], line[i]);
            if (distance <= leg || i == line.Length - 1)
            {
                return Vector2.Lerp(line[i - 1], line[i], leg > 0f ? Mathf.Clamp01(distance / leg) : 0f);
            }

            distance -= leg;
        }

        return line[0];
    }

    private static Vector2 Normal(Vector2[] line, float distance)
    {
        for (int i = 1; i < line.Length; i++)
        {
            float leg = Vector2.Distance(line[i - 1], line[i]);
            if (distance <= leg || i == line.Length - 1)
            {
                Vector2 along = (line[i] - line[i - 1]).normalized;
                return new Vector2(-along.y, along.x);
            }

            distance -= leg;
        }

        return Vector2.up;
    }

    private static string Pct(float damage, float towerHealth) => (damage / towerHealth * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";

    private static string F(System.FormattableString text) => System.FormattableString.Invariant(text);
}
