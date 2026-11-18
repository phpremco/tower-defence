using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Balance;
using Game.Editor;

/// <summary>
/// Round 84, what a level's surprises do to its other waves, on paper (no asset written): every level of the first
/// continent solved with and without surprises, and per wave that carries none the multiplier with over the multiplier
/// without. The reading behind WaveGrammarTests.ASurprise_MakesNoOtherWaveTougher: the most per kind of wave (plain,
/// swarm, boss wave, raid) and every wave above 5 %. Output Tools/PlayTest/surprise_lift.txt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/SurpriseLiftLab.cs --entry SurpriseLiftLab.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class SurpriseLiftLab
{
    public static string Run()
    {
        BalanceStages.Loaded loaded = BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        string[] names = { "Easy", "Medium", "Hard", "Psycho" };
        var most = new Dictionary<string, (float Ratio, string Where)>();
        var text = new StringBuilder("Waves without a surprise, with the level's surprises over without them (above 5 %):\n");
        foreach (string key in new[] { "C1_01", "C1_02", "C1_03", "C1_04", "C1_05", "C1_06" })
        {
            int index = model.IndexOf(key);
            List<List<WaveSpec>> sets = model.BuildWaves(index, WaveScripts.ByKey(key), out _);
            List<List<WaveSpec>> calm;
            model.Rules.Grammar.Surprises = false;
            try
            {
                calm = model.BuildWaves(index, WaveScripts.ByKey(key), out _);
            }
            finally
            {
                model.Rules.Grammar.Surprises = true;
            }

            for (int d = 0; d < sets.Count; d++)
            {
                for (int i = 0; i < sets[d].Count; i++)
                {
                    WaveSpec wave = sets[d][i];
                    if (wave.Surprise != SurpriseKind.None || wave.AirRaid != calm[d][i].AirRaid)
                    {
                        continue;
                    }

                    bool boss = wave.Groups.Exists(g => g.Enemy != null && g.Enemy.IsBoss);
                    string kind = wave.AirRaid ? "raid" : boss ? "boss wave" : wave.Pattern == WavePattern.Swarm ? "swarm" : "plain";
                    // A boss wave is read on the health of the whole wave: its crowd is what is left beside the boss.
                    float ratio = boss ? Health(wave) / Health(calm[d][i]) : wave.HealthMultiplier / calm[d][i].HealthMultiplier;
                    string where = string.Format(CultureInfo.InvariantCulture, "{0} {1} wave {2} ({3}): x{4:0.###} against x{5:0.###} = x{6:0.00}", key, names[d], i + 1, kind, wave.HealthMultiplier, calm[d][i].HealthMultiplier, ratio);
                    if (!most.TryGetValue(kind, out (float Ratio, string Where) best) || ratio > best.Ratio)
                    {
                        most[kind] = (ratio, where);
                    }

                    if (ratio > 1.05f)
                    {
                        text.Append("  ").Append(where).Append('\n');
                    }
                }
            }
        }

        text.Append("\nThe most per kind:\n");
        foreach (KeyValuePair<string, (float Ratio, string Where)> pair in most)
        {
            text.Append("  ").Append(pair.Value.Where).Append('\n');
        }

        System.IO.File.WriteAllText("Tools/PlayTest/surprise_lift.txt", text.ToString());
        return text.ToString();
    }

    private static float Health(WaveSpec wave)
    {
        float health = 0f;
        foreach (GroupSpec group in wave.Groups)
        {
            if (group.Enemy != null)
            {
                health += group.Count * group.Enemy.Health * wave.HealthMultiplier * (group.Enemy.IsBoss ? wave.BossHealthScale : 1f);
            }
        }

        return health;
    }
}
