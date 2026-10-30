// Prints every level's waves per difficulty as one line each: creature counts, flyers, bosses, the threat total and
// the kill money, so the curve and the economy can be judged without playing. Run in the editor after the generator.
var sb = new System.Text.StringBuilder();
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
foreach (var node in catalog.Nodes)
{
    foreach (var difficulty in catalog.Difficulties)
    {
        sb.Append(node.Level.name).Append(' ').Append(difficulty.name).Append('\n');
        var waves = node.Level.GetWaves(difficulty);
        int totalMoney = 0;
        for (int i = 0; i < waves.Length; i++)
        {
            var counts = new System.Collections.Generic.SortedDictionary<string, int>();
            float threat = 0f;
            float last = 0f;
            int money = 0;
            foreach (var group in waves[i].Groups)
            {
                int count = group.Enemy.IsBoss ? group.Count : UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(group.Count * difficulty.CountMultiplier));
                string key = group.Enemy.name + (group.PathIndex > 0 ? "*" : "") + (group.Source != Game.Levels.SpawnSource.Gate ? "~" : "");
                counts[key] = (counts.TryGetValue(key, out int have) ? have : 0) + count;
                threat += count * group.Enemy.MaxHealth / 20f;
                money += count * UnityEngine.Mathf.RoundToInt(group.Enemy.Reward * difficulty.RewardMultiplier);
                last = UnityEngine.Mathf.Max(last, group.Delay + (group.Stagger + (count - 1) * group.Interval) * difficulty.SpawnIntervalMultiplier);
            }

            totalMoney += money;
            sb.Append("  w").Append(i + 1).Append(" threat ").Append(threat.ToString("0")).Append(" money ").Append(money).Append(" span ").Append(last.ToString("0")).Append("s:");
            foreach (var pair in counts)
            {
                sb.Append(' ').Append(pair.Value).Append('x').Append(pair.Key);
            }

            sb.Append('\n');
        }

        sb.Append("  total kill money ").Append(totalMoney).Append('\n');
    }
}

System.IO.File.WriteAllText("Tools/PlayTest/waves_dump.txt", sb.ToString());
return "written Tools/PlayTest/waves_dump.txt (" + sb.Length + " chars)";
