// Round 61: the sky per wave. For every campaign level and difficulty, each wave with flyers: how many of which flyer,
// their health as spawned (difficulty x level x wave), the share of the wave's health that flies, and the span they
// arrive in. Read-only; writes Tools/PlayTest/r61_air_waves.txt.
var sb = new System.Text.StringBuilder();
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
foreach (var node in catalog.Nodes)
{
    if (node.Level == null)
    {
        continue;
    }

    foreach (var difficulty in catalog.Difficulties)
    {
        var waves = node.Level.GetWaves(difficulty);
        if (waves == null)
        {
            continue;
        }

        sb.Append(node.Level.name).Append(' ').Append(difficulty.name).Append(" (").Append(waves.Length).Append(" waves)\n");
        float levelAir = 0f;
        float levelAll = 0f;
        for (int i = 0; i < waves.Length; i++)
        {
            var counts = new System.Collections.Generic.SortedDictionary<string, int>();
            float air = 0f;
            float all = 0f;
            float scale = difficulty.HealthMultiplier * node.Level.EnemyHealthScale * waves[i].HealthMultiplier;
            foreach (var group in waves[i].Groups)
            {
                if (group.Enemy == null)
                {
                    continue;
                }

                int count = group.Enemy.IsBoss ? group.Count : UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(group.Count * difficulty.CountMultiplier));
                float health = count * group.Enemy.MaxHealth * scale;
                all += health;
                if (group.Enemy.Flies)
                {
                    air += health;
                    string key = group.Enemy.name + "@" + group.PathIndex + (group.Source != Game.Levels.SpawnSource.Gate ? "~" : "") + " every " + (group.Interval * difficulty.SpawnIntervalMultiplier).ToString("0.00") + "s";
                    counts[key] = (counts.TryGetValue(key, out int have) ? have : 0) + count;
                }
            }

            levelAir += air;
            levelAll += all;
            if (air <= 0f)
            {
                continue;
            }

            sb.Append("  w").Append(i + 1).Append(" air hp ").Append(air.ToString("0")).Append(" of ").Append(all.ToString("0")).Append(" (").Append((100f * air / all).ToString("0")).Append("%), scale ").Append(scale.ToString("0.00")).Append(':');
            foreach (var pair in counts)
            {
                sb.Append(' ').Append(pair.Value).Append("x ").Append(pair.Key).Append(';');
            }

            sb.Append('\n');
        }

        sb.Append("  level air hp ").Append(levelAir.ToString("0")).Append(" of ").Append(levelAll.ToString("0")).Append(" (").Append(levelAll > 0f ? (100f * levelAir / levelAll).ToString("0.0") : "0").Append("%)\n");
    }
}

System.IO.File.WriteAllText("Tools/PlayTest/r61_air_waves.txt", sb.ToString());
return "written Tools/PlayTest/r61_air_waves.txt (" + sb.Length + " chars)";
