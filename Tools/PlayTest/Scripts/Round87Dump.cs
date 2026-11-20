using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Editor;
using Game.Levels;

/// <summary>
/// Round 87: the stored waves of every cell as one flat table, for the fit of the health formula
/// (Tools/PlayTest/r87_fit.py). One line per wave: level key, level index, difficulty, wave, waves, kind, raid, boss id,
/// surprise, walk-in multiplier, boss scale, the difficulty's health factor, the level's health scale. Reads the
/// assets only. Run: bash Tools/PlayTest/stage.sh run Round87Dump.cs Round87Dump.Run (argument: none; writes
/// Tools/PlayTest/r87_dump.txt).
/// </summary>
public static class Round87Dump
{
    private const string OutPath = "Tools/PlayTest/r87_dump.txt";

    public static string Run()
    {
        BalanceStages.Loaded loaded = BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        var text = new StringBuilder("key\tlevel\tdifficulty\twave\twaves\tkind\traid\tboss\tsurprise\twalkin\tbossscale\tdiffhealth\tlevelhealth\n");
        foreach (MapNode node in loaded.Catalog.Nodes)
        {
            if (node.Level == null)
            {
                continue;
            }

            string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
            int index = model.IndexOf(key);
            WaveScriptSpec script = WaveScripts.ByKey(key);
            if (index < 0 || script == null)
            {
                continue;
            }

            // The solve leaves the flyer floors on the script: the kinds below are read with them, as the stored waves were.
            model.BuildWaves(index, script, out _);
            for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
            {
                int number = 0;
                foreach (WaveDefinition definition in node.Level.GetWaves(loaded.Catalog.Difficulties[d]))
                {
                    number++;
                    WaveSpec wave = BalanceLedger.Spec(definition, model.Enemies);
                    BossSpec boss = WaveComposer.BossOn(script, d, number);
                    text.Append(string.Join("\t", new[]
                    {
                        key, model.ContinentIndexOf(index).ToString(CultureInfo.InvariantCulture), d.ToString(CultureInfo.InvariantCulture),
                        number.ToString(CultureInfo.InvariantCulture), script.Waves.ToString(CultureInfo.InvariantCulture),
                        WaveComposer.KindOf(script, d, number).ToString(), wave.AirRaid ? "1" : "0", boss != null ? boss.Enemy : "-",
                        wave.Surprise.ToString(), wave.HealthMultiplier.ToString("0.####", CultureInfo.InvariantCulture),
                        wave.BossHealthScale.ToString("0.####", CultureInfo.InvariantCulture),
                        model.Difficulties[d].Health.ToString("0.####", CultureInfo.InvariantCulture),
                        model.Facts[index].HealthScale.ToString("0.####", CultureInfo.InvariantCulture),
                    })).Append('\n');
                }
            }
        }

        File.WriteAllText(OutPath, text.ToString());
        return "written " + OutPath;
    }
}
