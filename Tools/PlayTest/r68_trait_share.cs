// Round 68: per cell and wave, the share of the wave's troops (bosses apart) that is armoured and that flies, by body
// (health + armour pool) and by head count, from the stored waves. Writes Tools/PlayTest/r68_trait_share.txt.
var sb = new System.Text.StringBuilder();
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
int nodes = 0;
foreach (var node in catalog.Nodes)
{
    if (node.Level == null || nodes++ >= 6)
    {
        continue;
    }

    for (int d = 0; d < catalog.Difficulties.Count; d++)
    {
        var difficulty = catalog.Difficulties[d];
        var waves = node.Level.GetWaves(difficulty);
        if (waves == null)
        {
            continue;
        }

        sb.Append(node.Level.name).Append(' ').Append(difficulty.name).Append('\n');
        for (int i = 0; i < waves.Length; i++)
        {
            float total = 0f;
            float plated = 0f;
            float air = 0f;
            int heads = 0;
            int platedHeads = 0;
            int airHeads = 0;
            string boss = string.Empty;
            foreach (var group in waves[i].Groups)
            {
                if (group.Enemy.IsBoss)
                {
                    boss = " boss " + group.Enemy.name;
                    continue;
                }

                int count = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(group.Count * difficulty.CountMultiplier));
                float body = count * (group.Enemy.MaxHealth + group.Enemy.Armor);
                total += body;
                heads += count;
                if (group.Enemy.Armor > 0f)
                {
                    plated += body;
                    platedHeads += count;
                }

                if (group.Enemy.Flies)
                {
                    air += body;
                    airHeads += count;
                }
            }

            sb.Append("  w").Append(i + 1).Append(" x").Append(waves[i].HealthMultiplier.ToString("0.00")).Append(" body ").Append((total * waves[i].HealthMultiplier).ToString("0"))
                .Append(" heads ").Append(heads)
                .Append(" plated ").Append(platedHeads).Append(" (").Append((total > 0f ? plated / total * 100f : 0f).ToString("0")).Append("%)")
                .Append(" air ").Append(airHeads).Append(" (").Append((total > 0f ? air / total * 100f : 0f).ToString("0")).Append("%)").Append(boss).Append('\n');
        }
    }
}

System.IO.File.WriteAllText("Tools/PlayTest/r68_trait_share.txt", sb.ToString());
return "written Tools/PlayTest/r68_trait_share.txt (" + sb.Length + " chars)";
