using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 88c labs on the assets (no Play Mode). FirstRaid: per campaign level and difficulty the first air raid, the wave
/// that warns of it, whether a boss walks in that wave or the wave before, the health in the air of that raid, the purse
/// the rule pays and the coins the level pays in all (start plus every bounty through the income multiplier), and the
/// bounties of the two waves between the warning and the raid. Ladder: per level the crowd health and the last boss of
/// Medium and Hard side by side (the gap Remco felt on Landing Site).
/// Run: bash Tools/PlayTest/stage.sh run Round88cLab.cs Round88cLab.FirstRaid   (writes Tools/PlayTest/r88c_first_raid.txt)
///      bash Tools/PlayTest/stage.sh run Round88cLab.cs Round88cLab.Ladder      (writes Tools/PlayTest/r88c_ladder.txt)
/// </summary>
public static class Round88cLab
{
    private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

    private static string F(float value) => value.ToString("0", CultureInfo.InvariantCulture);

    private static float Bounty(WaveDefinition wave, DifficultyProfile difficulty) =>
        wave.Groups.Where(g => g.Enemy != null).Sum(g => WaveScheduler.CountOf(g, difficulty) * g.Enemy.Reward) * difficulty.IncomeMultiplier;

    private static float CrowdHealth(WaveDefinition wave, DifficultyProfile difficulty, LevelDefinition level, bool air) =>
        wave.Groups.Where(g => g.Enemy != null && !g.Enemy.IsBoss && g.Enemy.Flies == air)
            .Sum(g => WaveScheduler.CountOf(g, difficulty) * (g.Enemy.MaxHealth + g.Enemy.Armor) * difficulty.HealthMultiplier * level.EnemyHealthScale * wave.HealthMultiplier);

    public static string FirstRaid()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
        int ahead = BalanceRules.Default.FirstRaidWarningWaves;
        var text = new StringBuilder("Round 88c: the first air raid per level and difficulty (FirstRaidRule). Purse = the level's sky gun x the difficulty's factor; all towers of the level offered.\n");
        text.Append("| Level | Difficulty | First raid | Warning at wave | Boss in the raid wave / the wave before | Air health of that raid | Sky gun | Purse | Bounties of the waves from the warning to the raid | Level's coins (start + bounties) | Purse share |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level == null)
            {
                continue;
            }

            foreach (DifficultyProfile difficulty in catalog.Difficulties)
            {
                WaveDefinition[] waves = node.Level.GetWaves(difficulty);
                int raid = waves == null ? FirstRaidRule.None : FirstRaidRule.FirstRaid(waves);
                if (raid == FirstRaidRule.None)
                {
                    continue;
                }

                int warning = FirstRaidRule.WarningWave(raid, ahead);
                bool bossIn = waves[raid].Groups.Any(g => g.Enemy != null && g.Enemy.IsBoss);
                bool bossBefore = raid > 0 && waves[raid - 1].Groups.Any(g => g.Enemy != null && g.Enemy.IsBoss);
                float between = 0f;
                for (int w = warning; w < raid; w++)
                {
                    between += Bounty(waves[w], difficulty);
                }

                float all = Mathf.RoundToInt(node.Level.StartingMoney * difficulty.StartingMoneyMultiplier) + waves.Sum(w => Bounty(w, difficulty));
                int purse = FirstRaidRule.Purse(node.Level.AvailableTowers, difficulty);
                text.Append($"| {node.Level.name.Replace("Level_", string.Empty)} | {difficulty.name} | w{raid + 1} | w{warning + 1} | {(bossIn ? "yes" : "no")} / {(bossBefore ? "yes" : "no")} | {F(CrowdHealth(waves[raid], difficulty, node.Level, true))} | {FirstRaidRule.SkyGunPrice(node.Level.AvailableTowers)} | {purse} | {F(between)} | {F(all)} | {(purse / all).ToString("0.0%", CultureInfo.InvariantCulture)} |\n");
            }
        }

        File.WriteAllText("Tools/PlayTest/r88c_first_raid.txt", text.ToString());
        return "written Tools/PlayTest/r88c_first_raid.txt";
    }

    public static string Ladder()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
        var text = new StringBuilder("Round 88c: Medium against Hard on paper. Crowd health = every creature of every wave as it walks in (bosses apart); last boss = its bar as spawned.\n");
        text.Append("| Level | Medium: crowd health / last boss / coins | Hard: crowd health / last boss / coins | Hard over Medium: crowd / boss / health per coin |\n| --- | --- | --- | --- |\n");
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level == null || catalog.Difficulties.Count < 3)
            {
                continue;
            }

            var crowd = new float[2];
            var boss = new float[2];
            var coins = new float[2];
            for (int k = 0; k < 2; k++)
            {
                DifficultyProfile difficulty = catalog.Difficulties[k + 1];
                WaveDefinition[] waves = node.Level.GetWaves(difficulty);
                if (waves == null)
                {
                    continue;
                }

                crowd[k] = waves.Sum(w => CrowdHealth(w, difficulty, node.Level, false) + CrowdHealth(w, difficulty, node.Level, true));
                coins[k] = Mathf.RoundToInt(node.Level.StartingMoney * difficulty.StartingMoneyMultiplier) + waves.Sum(w => Bounty(w, difficulty));
                WaveDefinition last = waves[waves.Length - 1];
                foreach (SpawnGroup group in last.Groups)
                {
                    if (group.Enemy != null && group.Enemy.IsBoss)
                    {
                        boss[k] = (group.Enemy.MaxHealth + group.Enemy.Armor) * difficulty.HealthMultiplier * node.Level.EnemyHealthScale * last.HealthMultiplier * last.BossHealthScale;
                    }
                }
            }

            if (crowd[0] <= 0f || coins[0] <= 0f)
            {
                continue;
            }

            string Ratio(float a, float b) => b > 0f ? "x" + (a / b).ToString("0.00", CultureInfo.InvariantCulture) : "-";
            text.Append($"| {node.Level.name.Replace("Level_", string.Empty)} | {F(crowd[0])} / {F(boss[0])} / {F(coins[0])} | {F(crowd[1])} / {F(boss[1])} / {F(coins[1])} | {Ratio(crowd[1], crowd[0])} / {Ratio(boss[1], boss[0])} / {Ratio((crowd[1] + boss[1]) / coins[1], (crowd[0] + boss[0]) / coins[0])} |\n");
        }

        File.WriteAllText("Tools/PlayTest/r88c_ladder.txt", text.ToString());
        return "written Tools/PlayTest/r88c_ladder.txt";
    }
}
